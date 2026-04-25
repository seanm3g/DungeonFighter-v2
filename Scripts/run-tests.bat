@echo off
REM Legacy wrapper kept for convenience.
call "%~dp0df.bat" test
if errorlevel 1 exit /b %errorlevel%
pause
