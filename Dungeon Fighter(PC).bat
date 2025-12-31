@echo off
setlocal enabledelayedexpansion

title Dungeon Fighter v2

REM Lock file to prevent duplicate execution
set "LOCKFILE=%TEMP%\DF2_Launcher_%USERNAME%.lock"
if exist "%LOCKFILE%" del "%LOCKFILE%" 2>nul >nul
echo %DATE% %TIME% > "%LOCKFILE%"

goto :main

:cleanup
if exist "%LOCKFILE%" del "%LOCKFILE%" 2>nul >nul
exit /b %1

:main
cd /d "%~dp0" 2>nul
if errorlevel 1 (
    echo ERROR: Could not access script directory.
    call :cleanup 1
    pause
    exit /b 1
)

REM Check for .NET 8.0 SDK
REM First, try to find dotnet in common locations
set "DOTNET_FOUND=0"
set "DOTNET_VERSION="

REM Check if dotnet is in PATH
dotnet --version >nul 2>&1
if not errorlevel 1 (
    for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VERSION=%%v"
    echo Found .NET version: !DOTNET_VERSION!
    echo !DOTNET_VERSION! | findstr /R "^8\." >nul
    if not errorlevel 1 (
        set "DOTNET_FOUND=1"
        echo .NET 8.0 SDK detected in PATH.
    ) else (
        echo .NET is installed but not version 8.0 (found: !DOTNET_VERSION!)
    )
)

REM If not found in PATH, check common installation locations
if !DOTNET_FOUND!==0 (
    echo Checking common installation locations...
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        "%ProgramFiles%\dotnet\dotnet.exe" --version >nul 2>&1
        if not errorlevel 1 (
            for /f "tokens=*" %%v in ('"%ProgramFiles%\dotnet\dotnet.exe" --version 2^>nul') do set "DOTNET_VERSION=%%v"
            echo Found .NET version: !DOTNET_VERSION! at %ProgramFiles%\dotnet
            echo !DOTNET_VERSION! | findstr /R "^8\." >nul
            if not errorlevel 1 (
                set "DOTNET_FOUND=1"
                set "PATH=%ProgramFiles%\dotnet;%PATH%"
                echo .NET 8.0 SDK detected at %ProgramFiles%\dotnet
            )
        )
    )
    if !DOTNET_FOUND!==0 if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" (
        "%ProgramFiles(x86)%\dotnet\dotnet.exe" --version >nul 2>&1
        if not errorlevel 1 (
            for /f "tokens=*" %%v in ('"%ProgramFiles(x86)%\dotnet\dotnet.exe" --version 2^>nul') do set "DOTNET_VERSION=%%v"
            echo Found .NET version: !DOTNET_VERSION! at %ProgramFiles(x86)%\dotnet
            echo !DOTNET_VERSION! | findstr /R "^8\." >nul
            if not errorlevel 1 (
                set "DOTNET_FOUND=1"
                set "PATH=%ProgramFiles(x86)%\dotnet;%PATH%"
                echo .NET 8.0 SDK detected at %ProgramFiles(x86)%\dotnet
            )
        )
    )
)

REM If still not found, try to install
if !DOTNET_FOUND!==0 (
    echo.
    echo ========================================
    echo .NET 8.0 SDK Not Found
    echo ========================================
    echo.
    echo The game requires .NET 8.0 SDK to run.
    echo.
    if not "!DOTNET_VERSION!"=="" (
        echo Detected .NET version !DOTNET_VERSION! but need 8.0.x
        echo.
    ) else (
        echo .NET SDK not found in PATH or common locations.
        echo This might be a PATH configuration issue.
        echo.
    )
    echo Attempting to install it automatically...
    echo.
    if not exist "Scripts\install-dotnet.ps1" (
        echo ERROR: Install script not found!
        echo.
        echo Please install .NET 8.0 SDK manually from:
        echo https://dotnet.microsoft.com/download/dotnet/8.0
        echo.
        echo If .NET is already installed, you may need to:
        echo 1. Restart your computer to refresh PATH
        echo 2. Or add the .NET installation directory to your PATH
        echo    (usually: C:\Program Files\dotnet)
        call :cleanup 1
        pause
        exit /b 1
    )
    powershell -ExecutionPolicy Bypass -File "Scripts\install-dotnet.ps1"
    if errorlevel 1 (
        echo.
        echo ========================================
        echo Installation Failed
        echo ========================================
        echo.
        if not "!DOTNET_VERSION!"=="" (
            echo .NET is installed (version !DOTNET_VERSION!) but not 8.0.
            echo.
            echo Solutions:
            echo 1. Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
            echo 2. Or if .NET 8.0 is already installed, restart your computer
            echo    to refresh the PATH environment variable
            echo.
        ) else (
            echo The automatic installation failed. Please install .NET 8.0 SDK manually:
            echo 1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
            echo 2. Download the .NET 8.0 SDK installer for Windows x64
            echo 3. Run the installer
            echo 4. Restart your computer (to refresh PATH)
            echo 5. Run this launcher again
            echo.
        )
        call :cleanup 1
        pause
        exit /b 1
    )
    REM After installation, verify it's available
    dotnet --version >nul 2>&1
    if errorlevel 1 (
        echo.
        echo .NET was installed but may not be in PATH yet.
        echo Please restart your computer and try again.
        call :cleanup 1
        pause
        exit /b 1
    )
)

REM Check if game is already running
tasklist /FI "IMAGENAME eq DF.exe" 2>NUL | find /I /N "DF.exe">NUL
if not errorlevel 1 (
    echo Game is already running!
    call :cleanup 1
    pause
    exit /b 1
)

REM Build the game
dotnet build Code\Code.csproj --configuration Debug >nul 2>&1
if errorlevel 1 (
    echo ERROR: Build failed!
    dotnet build Code\Code.csproj --configuration Debug
    call :cleanup 1
    pause
    exit /b 1
)

REM Verify executable exists
set "GAME_EXE=%~dp0Code\bin\Debug\net8.0\DF.exe"
if not exist "%GAME_EXE%" (
    echo ERROR: Executable not found: %GAME_EXE%
    call :cleanup 1
    pause
    exit /b 1
)

REM Launch the game
set "VBS_FILE=%TEMP%\launch_df_%RANDOM%.vbs"
echo Set WshShell = CreateObject("WScript.Shell"^) > "%VBS_FILE%"
echo WshShell.CurrentDirectory = "%CD%" >> "%VBS_FILE%"
echo WshShell.Run """%GAME_EXE%""", 0, False >> "%VBS_FILE%"
start /min wscript.exe "%VBS_FILE%"
timeout /t 2 /nobreak >nul
del "%VBS_FILE%" 2>nul >nul

REM Wait for game to start
timeout /t 1 /nobreak >nul

REM Check if game is running
tasklist /FI "IMAGENAME eq DF.exe" 2>NUL | find /I /N "DF.exe">NUL
if errorlevel 1 (
    echo ERROR: Game failed to start!
    "%GAME_EXE%"
    call :cleanup 1
    pause
    exit /b 1
)

REM Game launched successfully - close this window
call :cleanup 0
endlocal
exit
