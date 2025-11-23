@echo off
REM Fix Build Cache - Thorough cleanup for stubborn build issues
powershell.exe -ExecutionPolicy Bypass -File "%~dp0fix-build-cache.ps1"
if errorlevel 1 pause

