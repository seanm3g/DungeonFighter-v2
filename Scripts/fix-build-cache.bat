@echo off
REM Fix Build Cache Issues - Windows Batch Wrapper
REM This calls the PowerShell script to clean build artifacts

powershell.exe -ExecutionPolicy Bypass -File "%~dp0fix-build-cache.ps1"
pause

