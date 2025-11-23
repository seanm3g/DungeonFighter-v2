@echo off
REM Build Fix - Interactive menu for build cache fixes
echo.
echo ========================================
echo   Build Cache Fix Commands
echo ========================================
echo.
echo   1. Quick Clean (Fast - removes bin/obj)
echo   2. Fix Build (Thorough - includes NuGet cache)
echo   3. Clean All (Most thorough)
echo   4. Exit
echo.
set /p choice="Select option (1-4): "

if "%choice%"=="1" (
    call "%~dp0quick-clean.bat"
) else if "%choice%"=="2" (
    call "%~dp0fix-build.bat"
) else if "%choice%"=="3" (
    call "%~dp0clean-all.bat"
) else if "%choice%"=="4" (
    exit /b 0
) else (
    echo Invalid choice. Please run again.
    pause
)

