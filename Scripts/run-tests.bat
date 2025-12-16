@echo off
REM Automated test execution script
REM Runs all unit tests and generates coverage report

echo ========================================
echo   DUNGEON FIGHTER v2 - TEST SUITE
echo ========================================
echo.

cd /d "%~dp0.."

REM Build the project first
echo Building project...
dotnet build Code/Code.csproj --configuration Debug
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo Running tests...
echo.

REM Run tests through the game's test system
REM This will execute all unit tests
Code\bin\Debug\net8.0\DF.exe --run-tests

echo.
echo ========================================
echo   TEST EXECUTION COMPLETE
echo ========================================
pause
