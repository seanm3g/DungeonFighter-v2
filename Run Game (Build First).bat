@echo off
title Dungeon Fighter v2 - Build and Run
echo.
echo ========================================
echo    Dungeon Fighter v2
echo ========================================
echo.

REM Check if .NET 8.0 SDK is installed
echo Checking for .NET 8.0 SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo .NET is not installed. Installing .NET 8.0 SDK...
    echo.
    powershell -ExecutionPolicy Bypass -File "%~dp0Scripts\install-dotnet.ps1"
    if errorlevel 1 (
        echo.
        echo ERROR: Failed to install .NET 8.0 SDK. Please install it manually.
        echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
        echo.
        pause
        exit /b 1
    )
    echo.
    echo .NET 8.0 SDK installed successfully!
    echo Verifying installation...
    REM Wait a moment for PATH to update, then verify
    timeout /t 3 /nobreak >nul
    REM Update PATH from registry
    for /f "tokens=2*" %%A in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v Path 2^>nul') do set "MACHINEPATH=%%B"
    set "PATH=%MACHINEPATH%;%PATH%"
    dotnet --version >nul 2>&1
    if errorlevel 1 (
        echo.
        echo WARNING: .NET was installed but may require a new command prompt.
        echo Please close this window and run the game again, or restart your computer.
        echo.
        pause
        exit /b 1
    )
    echo Verification successful!
    echo.
) else (
    REM Check if it's .NET 8.x
    for /f "tokens=1 delims=." %%a in ('dotnet --version 2^>nul') do set DOTNET_MAJOR=%%a
    if not "%DOTNET_MAJOR%"=="8" (
        echo .NET 8.0 SDK not found (found version %DOTNET_MAJOR%.x). Installing .NET 8.0 SDK...
        echo.
        powershell -ExecutionPolicy Bypass -File "%~dp0Scripts\install-dotnet.ps1"
        if errorlevel 1 (
            echo.
            echo ERROR: Failed to install .NET 8.0 SDK. Please install it manually.
            echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
            echo.
            pause
            exit /b 1
        )
        echo.
        echo .NET 8.0 SDK installed successfully!
        echo Verifying installation...
        REM Wait a moment for PATH to update, then verify
        timeout /t 3 /nobreak >nul
        REM Update PATH from registry
        for /f "tokens=2*" %%A in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v Path 2^>nul') do set "MACHINEPATH=%%B"
        set "PATH=%MACHINEPATH%;%PATH%"
        dotnet --version >nul 2>&1
        if errorlevel 1 (
            echo.
            echo WARNING: .NET was installed but may require a new command prompt.
            echo Please close this window and run the game again, or restart your computer.
            echo.
            pause
            exit /b 1
        )
        echo Verification successful!
        echo.
    ) else (
        echo .NET 8.0 SDK is installed.
        echo.
    )
)

echo Building latest version...
echo.

REM Build the project
dotnet build Code\Code.csproj --configuration Debug
if errorlevel 1 (
    echo.
    echo ERROR: Build failed! Please check the error messages above.
    echo.
    pause
    exit /b 1
)

echo.
echo Build successful! Starting game...
echo.

REM Run the game
start "" "Code\bin\Debug\net8.0\DF.exe"

