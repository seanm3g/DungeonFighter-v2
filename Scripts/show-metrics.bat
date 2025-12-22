@echo off
REM Show Build and Execution Metrics
powershell.exe -ExecutionPolicy Bypass -File "%~dp0show-metrics.ps1"
if errorlevel 1 pause

