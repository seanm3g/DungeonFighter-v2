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
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Installing .NET 8.0 SDK...
    if not exist "Scripts\install-dotnet.ps1" (
        echo ERROR: Install script not found!
        call :cleanup 1
        pause
        exit /b 1
    )
    powershell -ExecutionPolicy Bypass -File "Scripts\install-dotnet.ps1" >nul 2>&1
    if errorlevel 1 (
        echo ERROR: Failed to install .NET 8.0 SDK
        call :cleanup 1
        pause
        exit /b 1
    )
) else (
    dotnet --version 2>&1 | findstr /R "^8\." >nul
    if errorlevel 1 (
        echo Installing .NET 8.0 SDK...
        powershell -ExecutionPolicy Bypass -File "Scripts\install-dotnet.ps1" >nul 2>&1
        if errorlevel 1 (
            echo ERROR: Failed to install .NET 8.0 SDK
            call :cleanup 1
            pause
            exit /b 1
        )
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
