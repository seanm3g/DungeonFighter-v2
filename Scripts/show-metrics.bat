@echo off
REM Legacy wrapper kept for convenience.
call "%~dp0df.bat" metrics
if errorlevel 1 pause

