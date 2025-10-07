@echo off
echo Testing GameDataGenerator...
echo.

cd /d "%~dp0.."
dotnet run --project Code test-generator

echo.
echo Test complete. Press any key to continue...
pause > nul
