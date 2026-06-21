# Schema-Based Validation

## Overview

The X12 parser supports schema-based validation for transaction sets. Schemas define:
- Required elements and their data types
- Loop structures and occurrence limits
- Segment definitions

## Schema Loaders

The parser supports multiple schema loading strategies through the `ISchemaLoader` interface:

### LocalSchemaLoader (Default)

Loads schemas from the local file system. By default, schemas are loaded from `Schemas/[Release]/[TransactionSet].json` relative to the current working directory.

#### Example Directory Structure
```
Schemas/
  4010/
    810.json
    850.json
  5010/
    810.json
```

#### Custom Schema Directory

You can override the default schema location using `ParseOptions.SchemaDirectory`:

```csharp
var options = new ParseOptions 
{ 
    SchemaDirectory = @"C:\MySchemas" 
};
var doc = X12Document.Parse(content, options);
```

The parser will look for schemas at `[SchemaDirectory]/[Release]/[TransactionSet].json`.

### AzureSchemaLoader

Loads schemas from Azure Blob Storage using DefaultAzureCredential for authentication.

```csharp
var azureLoader = new AzureSchemaLoader(
    storageAccountName: "mystorageaccount",
    containerName: "schemas"  // optional, defaults to "schemas"
);

var options = new ParseOptions 
{ 
    SchemaLoader = azureLoader
};

var doc = X12Document.Parse(content, options);
```

The Azure loader expects blobs at `[Release]/[TransactionSet].json` within the specified container.

#### Authentication

AzureSchemaLoader uses `DefaultAzureCredential`, which supports:
- Environment variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
- Managed Identity (when running in Azure)
- Visual Studio / VS Code authentication
- Azure CLI authentication
- Interactive browser authentication

### Custom Schema Loaders

You can implement your own schema loader by implementing `ISchemaLoader`:

```csharp
public class MySchemaLoader : ISchemaLoader
{
    public TransactionSchema? LoadSchema(string release, string transactionSetId)
    {
        // Load schema from database, API, etc.
        return schema;
    }
}

var options = new ParseOptions 
{ 
    SchemaLoader = new MySchemaLoader()
};

var doc = X12Document.Parse(content, options);
```

## Schema Format

Schemas use a custom JSON format based on X12 transaction set definitions. See `src/Edi/Schemas/4010/810.json` for a complete example.

### Key Structure

```json
{
  "transactionSet": {
    "id": "810",
    "name": "Invoice",
    "version": "004010"
  },
  "headerArea": [
    {
      "segmentId": "ST",
      "elements": [
        {
          "id": "ST01",
          "required": "M",
          "type": "ID"
        }
      ]
    }
  ],
  "detailArea": [
    {
      "loopId": "N1",
      "maxUse": 200,
      "segments": [...]
    }
  ]
}
```

## Validation Behavior

### Default (Tolerant) Mode

Validation warnings are collected in `X12Document.Diagnostics`:

```csharp
var doc = X12Document.Parse(content);
foreach (var warning in doc.Diagnostics)
{
    Console.WriteLine($"{warning.Code}: {warning.Message}");
}
```

### Strict Mode

In strict mode, validation failures throw `X12ParseException`:

```csharp
try
{
    var doc = X12Document.Parse(content, new ParseOptions { Strict = true });
}
catch (X12ParseException ex)
{
    foreach (var diag in ex.Diagnostics)
    {
        Console.WriteLine($"{diag.Code}: {diag.Message}");
    }
}
```

## Loop Detection

When a schema is available, the parser automatically detects and populates loops in each transaction set:

```csharp
var doc = X12Document.Parse(content);
foreach (var interchange in doc.Interchanges)
{
    foreach (var group in interchange.FunctionalGroups)
    {
        foreach (var transaction in group.TransactionSets)
        {
            foreach (var loop in transaction.Loops)
            {
                Console.WriteLine($"Loop {loop.Name}: {loop.Instances.Count} instances");
                foreach (var instance in loop.Instances)
                {
                    // Access segments in this loop instance
                    foreach (var segment in instance.Segments)
                    {
                        Console.WriteLine($"  {segment.Tag}");
                    }
                }
            }
        }
    }
}
```

## Diagnostic Codes

Common validation diagnostic codes:

- `ELEMENT_REQUIRED_MISSING` - Required element is missing
- `ELEMENT_TYPE_MISMATCH` - Element value doesn't match expected type
- `LOOP_MAXUSE_EXCEEDED` - Loop occurs more times than schema allows
- `MISSING_SE` - Transaction set missing closing SE segment
- `MISSING_GE` - Functional group missing closing GE segment
- `MISSING_IEA` - Interchange missing closing IEA segment
- `IEA_COUNT_MISMATCH` - IEA functional group count doesn't match actual
- `GE_COUNT_MISMATCH` - GE transaction set count doesn't match actual
