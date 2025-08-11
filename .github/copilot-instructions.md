# OasReader - OpenAPI Multi Document Reader

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

OasReader is a .NET Standard 2.0 library that merges external references in OpenAPI specifications into a single document using the Microsoft OpenAPI toolset. The main project targets .NET Standard 2.0 while tests target .NET 8.0.

## Working Effectively

### Prerequisites
- .NET 8.0 SDK (command: `dotnet --version` should show 8.0.x)
- No additional SDKs or tools required - everything uses built-in dotnet CLI

### Build and Test Commands

**CRITICAL BUILD TIMINGS - NEVER CANCEL:**
- **First build (with restore): ~45-50 seconds** - NEVER CANCEL, set timeout to 120+ seconds
- **Subsequent builds: ~2-3 seconds** - set timeout to 60+ seconds  
- **Tests: ~5-7 seconds** - NEVER CANCEL, set timeout to 60+ seconds
- **Code formatting: ~7-8 seconds** - set timeout to 60+ seconds

#### Core Build Commands
```bash
# Initial build with restore (NEVER CANCEL - takes ~47 seconds)
dotnet build OasReader.sln

# Release build (as used in CI)
dotnet build -c Release OasReader.sln -p:UseSourceLink=true

# Run all tests (NEVER CANCEL - takes ~6 seconds, 90/91 pass in sandbox)
dotnet test OasReader.sln -c Release

# Apply code formatting (REQUIRED before committing)
dotnet format
```

### Validation Steps
**ALWAYS run these validation steps after making changes:**

1. **Build validation:**
   ```bash
   dotnet build -c Release OasReader.sln -p:UseSourceLink=true
   ```
   
2. **Test validation:**
   ```bash
   dotnet test OasReader.sln -c Release
   ```
   Note: 1 test may fail in sandboxed environments due to network connectivity (ContainsExternalReferences_BeFalse_WhenUrl) - this is expected and acceptable.

3. **Format validation (CRITICAL for CI):**
   ```bash
   dotnet format --verify-no-changes
   ```
   If this fails, run `dotnet format` to apply fixes.

4. **Functional validation:**
   Create a simple test to verify the library works:
   ```csharp
   using Microsoft.OpenApi.Readers;
   var result = await OpenApiMultiFileReader.Read("path/to/openapi.yaml");
   Console.WriteLine($"Loaded: {result.OpenApiDocument.Info.Title}");
   ```

### Repository Structure

```
OasReader.sln                          # Main solution file
├── src/
│   ├── OasReader/                     # Main library project (.NET Standard 2.0)
│   │   ├── OasReader.csproj
│   │   ├── OpenApiMultiFileReader.cs  # Primary API class
│   │   ├── OpenApiDocumentExtensions.cs
│   │   └── Visitors/                  # Reference resolution logic
│   └── OasReader.Tests/               # Test project (.NET 8.0)
│       ├── OasReader.Tests.csproj
│       ├── OpenApiReaderTests.cs      # Main functionality tests
│       ├── OpenApiDocumentExtensionsTests.cs
│       └── Resources/                 # Test OpenAPI files
│           ├── petstore.yaml          # Main test file with external refs
│           ├── petstore.components.yaml # External components file
│           └── v2/, v3/               # Additional test files
```

### Key Library Usage

The primary API is `OpenApiMultiFileReader.Read()`:

```csharp
// Read local file with external references
ReadResult result = await OpenApiMultiFileReader.Read("petstore.yaml");
OpenApiDocument document = result.OpenApiDocument;

// Read with custom validation rules
var ruleSet = ValidationRuleSet.GetDefaultRuleSet();
ReadResult result = await OpenApiMultiFileReader.Read("petstore.yaml", ruleSet);
```

The library automatically:
- Detects external references (e.g., `$ref: 'components.yaml#/components/schemas/Pet'`)
- Merges external files into a single document
- Supports both local files and HTTP URLs (when network is available)
- Handles both YAML and JSON formats

### CI Pipeline
- GitHub Actions runs on `ubuntu-latest`
- Build command: `dotnet build -c Release OasReader.sln -p:UseSourceLink=true`
- Test command: `dotnet test OasReader.sln -c Release`
- Generates NuGet package on Release builds
- Uses SonarCloud for quality analysis and Codecov for coverage

### Development Environment
- VS Code tasks available in `.vscode/tasks.json`
- DevContainer configured with .NET SDK
- No special IDE requirements - works with any editor + dotnet CLI

### Debugging and Testing
- Use test files in `src/OasReader.Tests/Resources/` for local testing
- `petstore.yaml` + `petstore.components.yaml` demonstrate external reference merging
- Test project includes comprehensive scenarios for both v2 and v3 OpenAPI specs
- Network-dependent tests may fail in sandbox environments (expected)

### Common Issues
- **Format violations:** Always run `dotnet format` before committing
- **Network tests failing:** Expected in sandboxed environments
- **Build timeout:** Ensure first build has 120+ second timeout
- **Missing references:** Library handles external references automatically - no manual intervention needed

### NuGet Package
- Package ID: `OasReader`
- Targets: .NET Standard 2.0 (compatible with .NET Core 2.0+, .NET Framework 4.6.1+, .NET 5.0+)
- Dependencies: Microsoft.OpenApi.Readers