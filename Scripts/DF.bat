@echo off
setlocal
REM Single entrypoint wrapper for scripts.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0df.ps1" %*
if errorlevel 1 exit /b %errorlevel%
@echo off
REM Legacy entrypoint kept for convenience.
call "%~dp0df.bat" run
