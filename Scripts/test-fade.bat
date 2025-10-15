@echo off
REM Test Fade Animation System
REM Usage: test-fade.bat [--interactive]

cd /d "%~dp0\.."

if "%1"=="--interactive" (
    echo Running Interactive Fade Animation Test...
    dotnet run --project Code/Code.csproj test-fade --interactive
) else (
    echo Running Fade Animation Demonstrations...
    dotnet run --project Code/Code.csproj test-fade
)

pause

