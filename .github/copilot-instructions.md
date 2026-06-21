# Copilot Instructions for Integrations.EDI

## Build and Test Commands

### Build
```bash
dotnet build
```

### Test
```bash
# Run all tests
dotnet test --nologo --verbosity minimal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~X12ParserTests"

# Run a specific test
dotnet test --filter "FullyQualifiedName~X12ParserTests.Parse_FullInterchange_GroupsIntoEnvelopes"
```

## Architecture Overview

### X12 EDI Parser with Envelope Hierarchy

This library parses X12 EDI documents using a **hierarchical envelope structure**:

```
X12Document
  └─ Interchange (ISA/IEA)
      └─ FunctionalGroup (GS/GE)
          └─ TransactionSet (ST/SE)
              ├─ Segments (flat list)
              └─ Loops (schema-driven grouping)
```

**Key architectural decisions:**

1. **Dual Access Pattern**: X12Document exposes both:
   - `Segments` - flat list for low-level access
   - `Interchanges` - hierarchical envelope structure for standards-compliant access

2. **Tolerant Parsing by Default**: Parser collects diagnostics without throwing exceptions. Use `ParseOptions.Strict = true` to throw `X12ParseException` on validation failures.

3. **Schema-Driven Validation**: When schemas are available, the parser validates:
   - Required elements (e.g., ST segment must have transaction ID)
   - Element data types (numeric vs alphanumeric)
   - Loop structures and occurrence limits

4. **Pluggable Schema Loaders**: Implement `ISchemaLoader` to load schemas from any source:
   - `LocalSchemaLoader` - file system (default)
   - `AzureSchemaLoader` - Azure Blob Storage with DefaultAzureCredential
   - Custom implementations - database, API, embedded resources, etc.

### Parser Flow

```
X12Parser.Parse()
  1. DetectDelimiters() - ISA format or UNA format (EDIFACT)
  2. SplitSegments() - tokenize into segments, handle release chars
  3. Parse segment elements/components
  4. GroupIntoEnvelopes() - build ISA→GS→ST hierarchy
  5. ValidateEnvelopes() - check counts, missing closers
  6. SchemaValidation() - if schema available, validate elements & detect loops
  7. Return X12Document with Diagnostics
```

### Schema Format

Schemas are JSON files stored at `Schemas/[Release]/[TransactionSet].json` (e.g., `Schemas/4010/810.json`).

**Schema Structure:**
- `transactionSet` - metadata (id, name, version)
- `headerArea` - segments before detail (typically just ST)
- `detailArea` - loops with anchor segments
- `summaryArea` - footer segments (typically just SE)

The parser extracts:
- Element definitions from `headerArea.ST.elements` for validation
- Loop definitions from `detailArea` for grouping segments

## Key Conventions

### Delimiter Detection

- **ISA format**: Element separator at index 3, component separator at index 104
- **UNA format** (EDIFACT): `UNA<comp><elem><dec><release><reserved><segment>`
- Release character (default `?`) escapes separators during tokenization

### Model Objects

- **Internal constructors**: Model classes like `Interchange`, `FunctionalGroup`, `TransactionSet` use internal constructors - only the parser creates them
- **Immutable after construction**: All model properties are `IReadOnlyList<T>` or immutable values
- **RawXX properties**: Each envelope stores its raw segment text (e.g., `RawISA`, `RawGS`, `RawST`)

### Diagnostic Codes

Standard diagnostic codes used throughout:
- `ELEMENT_REQUIRED_MISSING` - Required element missing from segment
- `ELEMENT_TYPE_MISMATCH` - Element value doesn't match expected type
- `LOOP_MAXUSE_EXCEEDED` - Loop occurs more than schema allows
- `MISSING_IEA` / `MISSING_GE` / `MISSING_SE` - Missing envelope closers
- `IEA_COUNT_MISMATCH` / `GE_COUNT_MISMATCH` - Count fields don't match actual

### Test Naming

NUnit tests use the pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `Parse_FullInterchange_GroupsIntoEnvelopes`
- `MissingRequiredElement_ProducesDiagnostic`
- `StrictMode_Throws_OnSchemaWarnings`

### Schema Files

- Schemas are **copied to output** (`CopyToOutputDirectory: Always` in .csproj)
- Schema caching: `LocalSchemaLoader` caches by `"{release}_{transactionSetId}"` key
- Schema JSON uses custom format (not EDI standard) - see existing 810.json for reference

### ParseOptions Configuration

Three ways to configure parsing:

1. **Default (tolerant)**: Collects warnings in `Diagnostics`
   ```csharp
   var doc = X12Document.Parse(content);
   ```

2. **Strict mode**: Throws on any validation failure
   ```csharp
   var doc = X12Document.Parse(content, new ParseOptions { Strict = true });
   ```

3. **Custom schema loader**: Override default file system loader
   ```csharp
   var options = new ParseOptions 
   { 
       SchemaLoader = new AzureSchemaLoader("storageaccount", "container")
   };
   var doc = X12Document.Parse(content, options);
   ```

## Important Files

- `X12Document.cs` - Entry point, static Parse method
- `Parser/X12Parser.cs` - Core parsing engine (internal class)
- `Model/` - Envelope hierarchy: Interchange, FunctionalGroup, TransactionSet, Segment, Element, Loop
- `SchemaLoader.cs` - Local file system schema loader (now `LocalSchemaLoader`)
- `AzureSchemaLoader.cs` - Azure Blob Storage schema loader
- `SchemaValidator.cs` - Static validation method for transactions
- `docs/schema-validation.md` - Detailed schema validation documentation
