@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM Avoid "%ProgramFiles(x86)%" inside parenthesized blocks — the "(x86)" breaks CMD parsing.
set "ProgFiles64=%ProgramFiles%"
set "ProgFiles86=%ProgramFiles(x86)%"

title Dungeon Fighter v2

REM ---------------------------------------------------------------------------
REM  Dungeon Fighter v2 — Windows launcher (repo root)
REM  - Ensures working directory is the repo root (GameData / paths)
REM  - Builds Debug, then starts DF.exe in a separate process
REM ---------------------------------------------------------------------------

echo.
echo ========================================
echo    Dungeon Fighter v2 - Launcher
echo ========================================
echo.
echo Script folder: %~dp0
echo.

REM Lock file (best-effort; avoids stale lock from crashed runs)
set "LOCKFILE=%TEMP%\DF2_Launcher_%USERNAME%.lock"
if exist "%LOCKFILE%" del "%LOCKFILE%" 2>nul
echo %DATE% %TIME%>"%LOCKFILE%"

goto main

:cleanup
if exist "%LOCKFILE%" del "%LOCKFILE%" 2>nul
exit /b %1

REM ---------------------------------------------------------------------------
:fail
if defined DF_PUSHED (
    popd
    set "DF_PUSHED="
)
echo.
echo ========================================
echo Launcher stopped ^(error^).
echo ========================================
echo.
echo Press any key to close this window...
pause
call :cleanup 1
exit /b 1

REM ---------------------------------------------------------------------------
:main
echo [Step 1/5] Moving to repo root...
pushd "%~dp0" 2>nul
if errorlevel 1 (
    echo ERROR: Could not open the folder containing this launcher.
    echo Path: %~dp0
    goto fail
)
set "DF_PUSHED=1"
REM %CD% has no trailing backslash - safe for START /D (trailing "\" can break /D)
set "REPO=%CD%"
echo Repo root: %REPO%
echo.

REM ---------------------------------------------------------------------------
echo [Step 2/5] Checking for .NET 8.x SDK / dotnet build...
set "DOTNET_FOUND=0"
set "DOTNET_VERSION="

where dotnet >nul 2>&1
if errorlevel 1 (
    echo dotnet was not found in PATH.
) else (
    for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VERSION=%%v"
    if defined DOTNET_VERSION (
        echo dotnet reports version: !DOTNET_VERSION!
        echo !DOTNET_VERSION! | findstr /R /C:"^8\." >nul
        if not errorlevel 1 (
            set "DOTNET_FOUND=1"
            echo .NET 8.x SDK/runtime host OK for this project.
        ) else (
            echo This repo targets .NET 8. Install the .NET 8 SDK if build fails.
        )
    )
)

if "!DOTNET_FOUND!"=="0" (
    echo Checking common install locations...
    if exist "!ProgFiles64!\dotnet\dotnet.exe" (
        for /f "tokens=*" %%v in ('"!ProgFiles64!\dotnet\dotnet.exe" --version 2^>nul') do set "DOTNET_VERSION=%%v"
        echo !DOTNET_VERSION! | findstr /R /C:"^8\." >nul
        if not errorlevel 1 (
            set "DOTNET_FOUND=1"
            set "PATH=!ProgFiles64!\dotnet;%PATH%"
            echo Using dotnet at: !ProgFiles64!\dotnet
        )
    )
    if "!DOTNET_FOUND!"=="0" if exist "!ProgFiles86!\dotnet\dotnet.exe" (
        for /f "tokens=*" %%v in ('"!ProgFiles86!\dotnet\dotnet.exe" --version 2^>nul') do set "DOTNET_VERSION=%%v"
        echo !DOTNET_VERSION! | findstr /R /C:"^8\." >nul
        if not errorlevel 1 (
            set "DOTNET_FOUND=1"
            set "PATH=!ProgFiles86!\dotnet;%PATH%"
            echo Using dotnet at: !ProgFiles86!\dotnet
        )
    )
)

if "!DOTNET_FOUND!"=="0" (
    echo.
    echo .NET 8.x was not detected. A full SDK is required to build from source.
    echo Download: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    if not exist "Scripts\install-dotnet.ps1" (
        echo Optional auto-install script not found: Scripts\install-dotnet.ps1
        goto fail
    )
    echo Running Scripts\install-dotnet.ps1 ...
    powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\install-dotnet.ps1"
    if errorlevel 1 goto fail
    where dotnet >nul 2>&1
    if errorlevel 1 (
        echo dotnet still not on PATH. Restart the PC, then try again.
        goto fail
    )
)

echo.

REM ---------------------------------------------------------------------------
echo [Step 3/5] Checking if DF.exe is already running...
tasklist /FI "IMAGENAME eq DF.exe" 2>nul | find /I "DF.exe" >nul
if not errorlevel 1 (
    echo Please close the existing Dungeon Fighter ^(DF.exe^) window, then run this launcher again.
    goto fail
)
echo OK - no DF.exe process found.
echo.

REM ---------------------------------------------------------------------------
set "PROJ=%REPO%\Code\Code.csproj"
set "GAME_EXE=%REPO%\Code\bin\Debug\net8.0\DF.exe"

echo [Step 4/5] Building - Debug...
echo Project: "%PROJ%"
echo.
dotnet build "%PROJ%" --configuration Debug --nologo -v minimal
if errorlevel 1 (
    echo.
    echo ERROR: dotnet build failed. Read the messages above.
    goto fail
)
echo Build finished OK.
echo.

REM ---------------------------------------------------------------------------
echo [Step 5/5] Starting game...
if not exist "%GAME_EXE%" (
    echo ERROR: Executable not found at:
    echo   "%GAME_EXE%"
    goto fail
)
echo Executable: "%GAME_EXE%"
echo.

REM /D sets the process working directory to repo root (spaces OK via %CD%).
REM First quoted arg to START is always the window title (do not remove).
start "Dungeon Fighter v2" /D "%REPO%" "%GAME_EXE%"
if errorlevel 1 (
    echo ERROR: Could not start the game process ^- start command failed.
    echo Try running manually:
    echo   "%GAME_EXE%"
    goto fail
)

echo Waiting a few seconds to confirm the game process started...
set "SEEN=0"
for /L %%i in (1,1,15) do (
    if "!SEEN!"=="0" (
        timeout /t 1 /nobreak >nul
        tasklist /FI "IMAGENAME eq DF.exe" 2>nul | find /I "DF.exe" >nul
        if not errorlevel 1 set "SEEN=1"
    )
)

if "!SEEN!"=="1" (
    echo Game process detected ^(DF.exe^).
) else (
    echo WARNING: DF.exe not seen in Task Manager yet - it may still be starting,
    echo or it exited immediately. If no window appeared, run the .exe above from
    echo Explorer or check Windows / antivirus blocking the app.
)
echo.
echo You can leave this window open while you play. When finished troubleshooting,
echo press any key to close...
pause
call :cleanup 0
popd >nul 2>&1
endlocal
exit /b 0
