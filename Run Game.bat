@echo off
setlocal enabledelayedexpansion

REM Set window title
title Dungeon Fighter v2

REM Lock file to prevent duplicate execution (optional - we also check for running game)
set "LOCKFILE=%TEMP%\DF2_Launcher_%USERNAME%.lock"

REM Always clean up lock file if it exists - we'll rely on game process check instead
if exist "%LOCKFILE%" (
    del "%LOCKFILE%" 2>nul >nul
)

REM Create new lock file
echo %DATE% %TIME% > "%LOCKFILE%"

REM Cleanup function
goto :main

:cleanup
if exist "%LOCKFILE%" del "%LOCKFILE%" 2>nul >nul
exit /b %1

:main
REM Change to script directory
cd /d "%~dp0" 2>nul
if errorlevel 1 (
    echo ERROR: Could not access script directory.
    call :cleanup 1
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Dungeon Fighter v2 - Launcher
echo ========================================
echo.

REM Step 1: Check for .NET 8.0 SDK
echo [1/3] Checking for .NET 8.0 SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo .NET not found. Installing .NET 8.0 SDK...
    echo.
    if not exist "Scripts\install-dotnet.ps1" (
        echo ERROR: Install script not found!
        call :cleanup 1
        pause
        exit /b 1
    )
    powershell -ExecutionPolicy Bypass -File "Scripts\install-dotnet.ps1"
    if errorlevel 1 (
        echo ERROR: Failed to install .NET 8.0 SDK
        call :cleanup 1
        pause
        exit /b 1
    )
    echo .NET 8.0 SDK installed successfully!
    timeout /t 2 /nobreak >nul
) else (
    REM Check if it's version 8.x
    dotnet --version 2>&1 | findstr /R "^8\." >nul
    if errorlevel 1 (
        echo .NET 8.0 SDK not found. Installing...
        echo.
        powershell -ExecutionPolicy Bypass -File "Scripts\install-dotnet.ps1"
        if errorlevel 1 (
            echo ERROR: Failed to install .NET 8.0 SDK
            call :cleanup 1
            pause
            exit /b 1
        )
        echo .NET 8.0 SDK installed successfully!
        timeout /t 2 /nobreak >nul
    ) else (
        echo .NET 8.0 SDK is installed.
    )
)
echo.

REM Step 2: Check if game is already running
echo [2/3] Checking if game is already running...
tasklist /FI "IMAGENAME eq DF.exe" 2>NUL | find /I /N "DF.exe">NUL
if not errorlevel 1 (
    echo.
    echo WARNING: Game is already running!
    echo Please close the existing game before launching again.
    echo.
    call :cleanup 1
    pause
    exit /b 1
)
echo No existing game process found.
echo.

REM Step 3: Build the game
echo [3/3] Building game...
dotnet build Code\Code.csproj --configuration Debug >nul 2>&1
if errorlevel 1 (
    echo ERROR: Build failed!
    echo.
    echo Running build with output to see errors...
    dotnet build Code\Code.csproj --configuration Debug
    call :cleanup 1
    pause
    exit /b 1
)
echo Build successful!
echo.

REM Verify executable exists
set "GAME_EXE=%~dp0Code\bin\Debug\net8.0\DF.exe"
if not exist "%GAME_EXE%" (
    echo ERROR: Executable not found: %GAME_EXE%
    call :cleanup 1
    pause
    exit /b 1
)

REM Verify GameData folder exists
if not exist "GameData" (
    echo WARNING: GameData folder not found!
    echo The game may fail to start.
    echo.
)

REM Launch the game and close this window
echo Launching game...
REM Use VBScript to launch GUI app without console window
echo Set WshShell = CreateObject("WScript.Shell"^) > "%TEMP%\launch_df_%RANDOM%.vbs"
echo WshShell.CurrentDirectory = "%CD%" >> "%TEMP%\launch_df_%RANDOM%.vbs"
echo WshShell.Run """%GAME_EXE%""", 0, False >> "%TEMP%\launch_df_%RANDOM%.vbs"
start /min wscript.exe "%TEMP%\launch_df_%RANDOM%.vbs"
timeout /t 1 /nobreak >nul
del "%TEMP%\launch_df_*.vbs" 2>nul >nul

REM Wait for game to start
timeout /t 2 /nobreak >nul

REM Check if game is running
tasklist /FI "IMAGENAME eq DF.exe" 2>NUL | find /I /N "DF.exe">NUL
if errorlevel 1 (
    echo.
    echo ERROR: Game failed to start!
    echo Running directly to see error messages...
    echo.
    "%GAME_EXE%"
    echo.
    echo Game exited. Check error messages above.
    call :cleanup 1
    pause
    exit /b 1
)

REM Game launched successfully - close this window
call :cleanup 0

REM End local scope and exit to close window
endlocal
exit
