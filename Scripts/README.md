# Scripts (simplified)

Use `df` as the single entrypoint for common workflows.

## Quick start (Windows)

```powershell
.\Scripts\df.bat help
.\Scripts\df.bat build
.\Scripts\df.bat test
.\Scripts\df.bat run
.\Scripts\df.bat metrics
```

## Commands

- **`run`**: publish Release (self-contained single file) to `dist/` and start `dist/DF.exe`
- **`build [Debug|Release]`**: `dotnet build Code/Code.csproj`
- **`test`**: build Debug then run `DF.exe --run-tests`
- **`clean`**: quick clean (bin/obj + rebuild)
- **`clean:fix`**: thorough clean (clears NuGet cache too)
- **`clean:all`**: alias for `clean:fix` (kept for familiarity)
- **`metrics`**: display `GameData/build_execution_metrics.json` summary
- **`dist`**: run the existing distribution build script

## Legacy / optional tooling

Google Sheets scripts and various analysis/count scripts are still present in this folder, but they are no longer part of the “core” workflow.

