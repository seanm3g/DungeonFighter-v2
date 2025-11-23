@echo off
REM Quick Clean - Fast build cache cleanup
powershell.exe -ExecutionPolicy Bypass -File "%~dp0quick-clean.ps1"
if errorlevel 1 pause

