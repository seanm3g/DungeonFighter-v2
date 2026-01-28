@echo off
REM Batch file wrapper for updating Actions.json from Google Sheets

echo Updating Actions.json from Google Sheets...
echo.

REM Check if URL is provided as first argument
if "%~1"=="" (
    REM No URL provided, use config
    powershell -ExecutionPolicy Bypass -File "%~dp0update-actions-from-sheets.ps1"
) else (
    REM URL provided, pass it to PowerShell script
    powershell -ExecutionPolicy Bypass -File "%~dp0update-actions-from-sheets.ps1" -GoogleSheetsUrl "%~1"
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Update failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Update completed successfully!
pause
