# Copilot instructions — Integrations (sfager/Integrations)

Purpose

- Short, actionable guidance for Copilot-based sessions working in this repository.

Build / test / lint (what exists)

- Solution: Integrations.slnx at repo root.
- Build (solution):
  dotnet restore
  dotnet build Integrations.slnx
- Build (single project):
  dotnet build src\Edi\Integrations.EDI.csproj
- Run tests: There are currently no test projects in this repository. When tests are added, run the full suite with:
  dotnet test
  To run a single test by fully qualified name:
  dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
  Or by test name:
  dotnet test --filter "Name=MyTestMethod"
- Lint / format: No repository-level linter/formatter configured. If formatting/analysis is needed, common commands:
  dotnet tool install -g dotnet-format
  dotnet format .
  (Do not assume these tools are present; add them to repo tooling if desired.)

High-level architecture

- Solution: Integrations.slnx (root)
- Projects:
  - src\Edi — Integrations.EDI (SDK-style .csproj, TargetFramework net8.0)
- Purpose: A small library for EDI-related functionality (types like EdiDocument). The codebase currently contains implementation skeletons in an internal namespace Integrations.EDI.
- Project settings:
  - TargetFramework: net8.0
  - ImplicitUsings: enabled
  - Nullable: enabled

Key conventions and repo specifics

- Namespace and project alignment: code in src\Edi uses namespace Integrations.EDI — keep namespaces aligned to project folder names.
- Visibility: Implementation types are internal by default; add public APIs only when intended to be consumed by other projects.
- Modern SDK-style project: prefer dotnet CLI commands (dotnet build/test/pack) and avoid legacy csproj patterns.
- Implicit usings & nullable: Copilot should respect nullable annotations and avoid suggesting code that assumes nullable is disabled. Don't re-add 'using System;' etc. unless a file opts out.
- Tests: No test projects now — if adding tests, follow common layout: tests\{ProjectName}.Tests or src\{ProjectName}.Tests and reference the target project via PackageReference or ProjectReference.
- Tooling / CI: No CI workflows, CODEOWNERS, or formatting rules committed. Assume local VS/.vs artifacts are ignored via .gitignore.

Files of interest for Copilot sessions

- Integrations.slnx — solution entrypoint
- src\Edi\Integrations.EDI.csproj — project settings (target framework, nullable, implicit usings)
- src\Edi\EdiDocument.cs — current implementation placeholder

When generating changes

- Make focused edits; preserve existing nullable annotations and implicit-using expectations. When adding public APIs, add XML docs and consider adding a test project alongside changes.
- Keep the project-level properties in Integrations.EDI.csproj (net8.0, nullable enabled, implicit usings). If changing these, update the instructions here.

Where to look for more context

- No README.md or CONTRIBUTING.md present. If adding higher-level documentation, place it at README.md in the repo root and update this file to reference it.

Contact / follow-up

- If additional conventions or CI/tooling are added, update this file so future Copilot sessions can use them.