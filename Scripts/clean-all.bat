@echo off
REM Clean All - alias kept for familiarity (includes NuGet cache)
call "%~dp0df.bat" clean:all
if errorlevel 1 pause

