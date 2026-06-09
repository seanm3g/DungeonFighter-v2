@echo off
setlocal
REM Single entrypoint wrapper for scripts. No args => run the game (legacy double-click behavior).
if "%~1"=="" (
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0df.ps1" run
) else (
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0df.ps1" %*
)
exit /b %errorlevel%
