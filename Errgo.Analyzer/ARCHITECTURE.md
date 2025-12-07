# Roslyn Analyzer Project Structure Fix

## Problem
The CodeFixes project was referencing `ErrgoAnalyzerAnalyzer.DiagnosticId` from the Analyzer project but had no project reference, causing compilation errors.

## Root Cause
When Visual Studio creates a Roslyn analyzer solution using the template, it generates multiple projects:

1. **Errgo.Analyzer** - Contains diagnostic analyzers
2. **Errgo.Analyzer.CodeFixes** - Contains code fixes
3. **Errgo.Analyzer.Test** - Contains unit tests
4. **Errgo.Analyzer.Package** - Packages analyzer + codefixes into NuGet
5. **Errgo.Analyzer.Vsix** - Packages analyzer + codefixes into VSIX for Visual Studio

However, the template sometimes **fails to create all necessary project references** between these projects.

## Solution Applied

### 1. CodeFixes ? Analyzer Reference
**File**: `Errgo.Analyzer.CodeFixes\Errgo.Analyzer.CodeFixes.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\Errgo.Analyzer\Errgo.Analyzer.csproj" />
</ItemGroup>
```

**Why**: CodeFixes needs to reference diagnostic IDs defined in the Analyzer project.

### 2. Test ? CodeFixes Reference
**File**: `Errgo.Analyzer.Test\Errgo.Analyzer.Test.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\Errgo.Analyzer\Errgo.Analyzer.csproj" />
  <ProjectReference Include="..\Errgo.Analyzer.CodeFixes\Errgo.Analyzer.CodeFixes.csproj" />
</ItemGroup>
```

**Why**: Tests need to verify both analyzers and code fixes work correctly.

**Also fixed**: Upgraded target framework from `netcoreapp3.1` ? `net8.0` to resolve Microsoft.NET.Test.Sdk compatibility warning.

### 3. Package ? Analyzer + CodeFixes References
**File**: `Errgo.Analyzer.Package\Errgo.Analyzer.Package.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\Errgo.Analyzer\Errgo.Analyzer.csproj" />
  <ProjectReference Include="..\Errgo.Analyzer.CodeFixes\Errgo.Analyzer.CodeFixes.csproj" />
</ItemGroup>
```

**Why**: Package project needs to build the analyzer and codefixes to include their DLLs in the NuGet package.

### 4. VSIX ? Analyzer + CodeFixes References
**File**: `Errgo.Analyzer.Vsix\Errgo.Analyzer.Vsix.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\Errgo.Analyzer\Errgo.Analyzer.csproj" />
  <ProjectReference Include="..\Errgo.Analyzer.CodeFixes\Errgo.Analyzer.CodeFixes.csproj" />
</ItemGroup>
```

**Why**: VSIX project needs to build the analyzer and codefixes to package them into the Visual Studio extension.

## Dependency Graph

```
Errgo.Analyzer (base analyzer)
    ?
    ??? Errgo.Analyzer.CodeFixes (code fixes)
    ?       ?
    ?       ??? Errgo.Analyzer.Test (tests both)
    ?
    ??? Errgo.Analyzer.Package (NuGet package)
    ?   (references both Analyzer + CodeFixes)
    ?
    ??? Errgo.Analyzer.Vsix (Visual Studio extension)
        (references both Analyzer + CodeFixes)
```

## Why This Matters

### For Compilation
- **CodeFixes** can now access `ErrgoAnalyzerAnalyzer.DiagnosticId`
- **Tests** can test both analyzers and code fixes
- **Package** can find DLLs to include in NuGet package
- **VSIX** can find DLLs to include in Visual Studio extension

### For Distribution
Both packaging projects (NuGet and VSIX) need the compiled DLLs to:
- Include them in the package at `analyzers/dotnet/cs/` path
- Ensure they're loaded by Visual Studio/dotnet CLI at design/build time
- Allow the analyzer to run during compilation

## Verification

Build now succeeds:
```bash
dotnet build
# Build successful
```

All projects compile with proper references in place.
