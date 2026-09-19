@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM Avoid "%ProgramFiles(x86)%" inside parenthesized blocks — the "(x86)" breaks CMD parsing.
set "ProgFiles64=%ProgramFiles%"
set "ProgFiles86=%ProgramFiles(x86)%"

title Dungeon Fighter v2

REM ---------------------------------------------------------------------------
REM  Dungeon Fighter v2 — Windows launcher (called from repo-root trampolines)
REM  Lives in Scripts\ so the filename has no spaces or parentheses.
REM  - Unblocks downloaded files (Mark of the Web)
REM  - Installs .NET 8 SDK to the user folder when needed (no admin)
REM  - Builds from source when an SDK is available
REM  - Otherwise launches an already-built DF.exe if present
REM ---------------------------------------------------------------------------

echo.
echo ========================================
echo    Dungeon Fighter v2 - Launcher
echo ========================================
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
:detectDotnetSdk
set "DOTNET_FOUND=0"
set "DOTNET_RUNTIME_FOUND=0"
set "DOTNET_VERSION="
set "DOTNET_SOURCE="
set "DOTNET_EXE="

where dotnet >nul 2>&1
if errorlevel 1 (
    echo dotnet was not found in PATH.
) else (
    echo SDKs reported by dotnet in PATH:
    call :probeDotnetSdk "dotnet" "PATH"
    if "!DOTNET_FOUND!"=="0" call :probeDotnetRuntime "dotnet"
)

if "!DOTNET_FOUND!"=="0" if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
    echo Checking user-local install: %USERPROFILE%\.dotnet
    echo SDKs reported by %USERPROFILE%\.dotnet\dotnet.exe:
    call :probeDotnetSdk "%USERPROFILE%\.dotnet\dotnet.exe" "%USERPROFILE%\.dotnet"
    if "!DOTNET_FOUND!"=="1" (
        set "PATH=%USERPROFILE%\.dotnet;!PATH!"
        set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
    ) else (
        call :probeDotnetRuntime "%USERPROFILE%\.dotnet\dotnet.exe"
        if "!DOTNET_RUNTIME_FOUND!"=="1" (
            set "PATH=%USERPROFILE%\.dotnet;!PATH!"
            set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
        )
    )
)

if "!DOTNET_FOUND!"=="0" (
    echo Checking common install locations...
    if exist "!ProgFiles64!\dotnet\dotnet.exe" (
        echo SDKs reported by !ProgFiles64!\dotnet\dotnet.exe:
        call :probeDotnetSdk "!ProgFiles64!\dotnet\dotnet.exe" "!ProgFiles64!\dotnet"
        if "!DOTNET_FOUND!"=="1" (
            set "PATH=!ProgFiles64!\dotnet;!PATH!"
            set "DOTNET_ROOT=!ProgFiles64!\dotnet"
        ) else (
            call :probeDotnetRuntime "!ProgFiles64!\dotnet\dotnet.exe"
        )
    )
    if "!DOTNET_FOUND!"=="0" if exist "!ProgFiles86!\dotnet\dotnet.exe" (
        echo SDKs reported by !ProgFiles86!\dotnet\dotnet.exe:
        call :probeDotnetSdk "!ProgFiles86!\dotnet\dotnet.exe" "!ProgFiles86!\dotnet"
        if "!DOTNET_FOUND!"=="1" (
            set "PATH=!ProgFiles86!\dotnet;!PATH!"
            set "DOTNET_ROOT=!ProgFiles86!\dotnet"
        ) else (
            call :probeDotnetRuntime "!ProgFiles86!\dotnet\dotnet.exe"
        )
    )
)

if "!DOTNET_FOUND!"=="1" (
    echo .NET 8.x SDK found: !DOTNET_VERSION! ^(!DOTNET_SOURCE!^)
) else (
    echo .NET 8.x SDK was not found.
    if "!DOTNET_RUNTIME_FOUND!"=="1" echo .NET 8 runtime is available. A pre-built DF.exe can still run.
)
if not defined DOTNET_EXE set "DOTNET_EXE=dotnet"
exit /b 0

REM ---------------------------------------------------------------------------
:probeDotnetSdk
set "DOTNET_CANDIDATE=%~1"
set "DOTNET_LISTED_SDKS=0"
for /f "tokens=1" %%v in ('"%DOTNET_CANDIDATE%" --list-sdks 2^>nul') do (
    set "DOTNET_LISTED_SDKS=1"
    echo   %%v
    echo %%v | findstr /R /C:"^8\." >nul
    if not errorlevel 1 if "!DOTNET_FOUND!"=="0" (
        set "DOTNET_FOUND=1"
        set "DOTNET_VERSION=%%v"
        set "DOTNET_SOURCE=%~2"
        set "DOTNET_EXE=%DOTNET_CANDIDATE%"
    )
)
if "!DOTNET_LISTED_SDKS!"=="0" echo   ^(no SDKs listed^)
exit /b 0

REM ---------------------------------------------------------------------------
:probeDotnetRuntime
set "DOTNET_CANDIDATE=%~1"
for /f "tokens=1,2" %%a in ('"%DOTNET_CANDIDATE%" --list-runtimes 2^>nul') do (
    if /I "%%a"=="Microsoft.NETCore.App" (
        echo %%b | findstr /R /C:"^8\." >nul
        if not errorlevel 1 if "!DOTNET_RUNTIME_FOUND!"=="0" (
            set "DOTNET_RUNTIME_FOUND=1"
            set "DOTNET_EXE=%DOTNET_CANDIDATE%"
            echo   runtime %%a %%b
        )
    )
)
exit /b 0

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
REM %~dp0 is Scripts\ — "%~dp0.." ends with "." so a trailing backslash cannot break quotes.
pushd "%~dp0.." 2>nul
if errorlevel 1 (
    echo ERROR: Could not open the game folder.
    echo Expected this script at: Scripts\launch-windows.bat
    goto fail
)
set "DF_PUSHED=1"
REM %CD% has no trailing backslash - safe for START /D (trailing "\" can break /D)
set "REPO=%CD%"
echo Repo root: %REPO%
echo.

echo %REPO% | findstr /I /C:".zip" >nul
if not errorlevel 1 (
    echo ERROR: This looks like it is still inside a zip folder.
    echo.
    echo Right-click the zip - Extract All - pick Desktop or Downloads,
    echo then double-click DungeonFighter-PC.bat in the extracted folder.
    goto fail
)

if not exist "%REPO%\Code\Code.csproj" goto missingFiles
if not exist "%REPO%\GameData" goto missingFiles
goto filesOk

:missingFiles
echo ERROR: This folder is incomplete. The launcher needs the whole game,
echo including the Code and GameData folders.
echo.
echo Extract the entire zip first. Do not run the .bat from inside the zip window.
echo Path: %REPO%
goto fail

:filesOk
echo [Step 1b] Unblocking downloaded files ^(Windows Mark of the Web^)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -LiteralPath '%REPO%\Scripts' -Recurse -File -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue; Get-ChildItem -LiteralPath '%REPO%' -File -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue; foreach ($p in @('%REPO%\dist\DF.exe','%REPO%\Code\bin\Debug\net8.0\DF.exe')) { if (Test-Path -LiteralPath $p) { Unblock-File -LiteralPath $p -ErrorAction SilentlyContinue } }"
echo.

REM ---------------------------------------------------------------------------
echo [Step 2/5] Checking for .NET 8.x SDK / a playable DF.exe...
call :detectDotnetSdk

set "DIST_EXE=%REPO%\dist\DF.exe"
set "DEBUG_EXE=%REPO%\Code\bin\Debug\net8.0\DF.exe"
set "GAME_EXE="
set "NEED_BUILD=1"

if exist "%DIST_EXE%" (
    set "GAME_EXE=%DIST_EXE%"
)
if not defined GAME_EXE if exist "%DEBUG_EXE%" (
    set "GAME_EXE=%DEBUG_EXE%"
)

if "!DOTNET_FOUND!"=="1" (
    set "NEED_BUILD=1"
) else if defined GAME_EXE (
    if exist "%DIST_EXE%" (
        echo No SDK needed — launching the included self-contained DF.exe.
        set "NEED_BUILD=0"
        set "GAME_EXE=%DIST_EXE%"
    ) else if "!DOTNET_RUNTIME_FOUND!"=="1" (
        echo No SDK needed — launching the included DF.exe with the installed .NET 8 runtime.
        set "NEED_BUILD=0"
        set "GAME_EXE=%DEBUG_EXE%"
    )
)

if "!NEED_BUILD!"=="1" if "!DOTNET_FOUND!"=="0" (
    echo.
    echo .NET 8.x SDK was not detected. A full SDK is required to build from source.
    echo Installing into your user folder ^(no administrator prompt^)...
    echo Download page: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    if not exist "Scripts\install-dotnet.ps1" (
        echo Optional auto-install script not found: Scripts\install-dotnet.ps1
        goto fail
    )
    powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\install-dotnet.ps1"
    if errorlevel 1 goto fail
    call :detectDotnetSdk
    if "!DOTNET_FOUND!"=="0" (
        if defined GAME_EXE if exist "%DIST_EXE%" (
            echo SDK install did not register, but a self-contained DF.exe is present. Launching that.
            set "NEED_BUILD=0"
            set "GAME_EXE=%DIST_EXE%"
        ) else (
            echo dotnet still cannot find a .NET 8 SDK.
            echo Restart the PC, then try DungeonFighter-PC.bat again.
            goto fail
        )
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
if not defined GAME_EXE set "GAME_EXE=%DEBUG_EXE%"

if "!NEED_BUILD!"=="1" (
    echo [Step 4/5] Building - Debug...
    echo Project: "%PROJ%"
    echo.
    "!DOTNET_EXE!" build "%PROJ%" --configuration Debug --nologo -v minimal
    if errorlevel 1 (
        echo.
        echo ERROR: dotnet build failed. Read the messages above.
        echo The first build needs internet access to download NuGet packages.
        goto fail
    )
    echo Build finished OK.
    set "GAME_EXE=%DEBUG_EXE%"
    echo.
) else (
    echo [Step 4/5] Skipping build — using an existing DF.exe.
    echo.
)

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
    echo or it exited immediately. If no window appeared:
    echo   - Extract the whole folder before running the launcher
    echo   - If Windows SmartScreen appears, click More info - Run anyway
    echo   - Try running the .exe above from Explorer
)
echo.
echo You can leave this window open while you play. When finished troubleshooting,
echo press any key to close...
pause
call :cleanup 0
popd >nul 2>&1
endlocal
exit /b 0
