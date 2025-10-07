@echo off
echo Game Data Generator
echo.

cd /d "%~dp0.."

if "%1"=="--force" (
    echo WARNING: Force overwrite mode enabled!
    echo This will overwrite existing files without prompting.
    echo.
    dotnet run --project Code generate-data --force
) else (
    echo Safe mode: Will not overwrite files unless changes are needed.
    echo Use --force flag to force overwrite.
    echo.
    dotnet run --project Code generate-data
)

echo.
echo Generation complete. Press any key to continue...
pause > nul
