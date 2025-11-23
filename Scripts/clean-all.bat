@echo off
REM Clean All - Most thorough cleanup (includes NuGet cache)
powershell.exe -ExecutionPolicy Bypass -File "%~dp0fix-build-cache.ps1"
if errorlevel 1 pause

