@echo off
setlocal enabledelayedexpansion

title Dungeon Fighter v2

REM Ensure we can see output
echo.
echo ========================================
echo    Dungeon Fighter v2 - Launcher
echo ========================================
echo.
echo Starting launcher...
echo Script location: %~dp0
echo Current directory: %CD%
echo.

REM Lock file to prevent duplicate execution
set "LOCKFILE=%TEMP%\DF2_Launcher_%USERNAME%.lock"
if exist "%LOCKFILE%" del "%LOCKFILE%" 2>nul >nul
echo %DATE% %TIME% > "%LOCKFILE%"

goto :main

:cleanup
if exist "%LOCKFILE%" del "%LOCKFILE%" 2>nul >nul
exit /b %1

:main
echo [Step 1/5] Checking script directory...
cd /d "%~dp0" 2>nul
if errorlevel 1 (
    echo.
    echo ERROR: Could not access script directory.
    echo Current directory: %CD%
    echo Script path: %~dp0
    echo.
    call :cleanup 1
    pause
    exit /b 1
)
echo Script directory: %CD%
echo.

REM Check for .NET 8.0 SDK
echo [Step 2/5] Checking for .NET 8.0 SDK...
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
echo [Step 3/5] Checking if game is already running...
tasklist /FI "IMAGENAME eq DF.exe" 2>NUL | find /I /N "DF.exe">NUL
if not errorlevel 1 (
    echo.
    echo Game is already running!
    echo Please close the existing game instance first.
    echo.
    call :cleanup 1
    pause
    exit /b 1
)
echo No existing game instance found.
echo.

REM Build the game
echo [Step 4/5] Building game...
echo This may take a moment...
echo.
dotnet build Code\Code.csproj --configuration Debug
if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Build failed!
    echo ========================================
    echo.
    echo Please check the error messages above.
    echo.
    call :cleanup 1
    pause
    exit /b 1
)
echo Build successful!
echo.

REM Verify executable exists
echo [Step 5/5] Verifying executable...
set "GAME_EXE=%~dp0Code\bin\Debug\net8.0\DF.exe"
if not exist "%GAME_EXE%" (
    echo.
    echo ========================================
    echo ERROR: Executable not found!
    echo ========================================
    echo.
    echo Expected location: %GAME_EXE%
    echo.
    echo The build may have failed. Please check the build output above.
    echo.
    call :cleanup 1
    pause
    exit /b 1
)
echo Executable found: %GAME_EXE%
echo.

REM Launch the game
echo Launching game...
echo.
REM Set working directory to project root so GameData can be found
cd /d "%~dp0"
echo Working directory: %CD%
echo.
REM Launch the game visibly (window style 1 = normal window, not hidden)
start "" "%GAME_EXE%"

REM Wait a moment for game to start
timeout /t 2 /nobreak >nul

REM Check if game is running
tasklist /FI "IMAGENAME eq DF.exe" 2>NUL | find /I /N "DF.exe">NUL
if errorlevel 1 (
    echo.
    echo ========================================
    echo WARNING: Game process not detected
    echo ========================================
    echo.
    echo The game may have started but exited immediately.
    echo Check for error messages in the game window.
    echo.
    echo If the game window appeared, you can close this launcher.
    echo Otherwise, try running the game manually:
    echo   "%GAME_EXE%"
    echo.
    pause
    call :cleanup 0
    exit /b 0
)

REM Game launched successfully
echo.
echo ========================================
echo Game launched successfully!
echo ========================================
echo.
echo The game window should have opened.
echo.
echo If the game window did not appear, check for error messages above.
echo.
echo Press any key to close this window...
pause >nul
call :cleanup 0
endlocal
exit /b 0
