@echo off
REM Push Actions.json to Google Sheets (requires GameData\SheetsPushConfig.json + OAuth client secrets JSON).

pushd "%~dp0.."
dotnet run --project Code\Code.csproj -- PUSH_ACTIONS
set ERR=%ERRORLEVEL%
popd
if %ERR% NEQ 0 (
    echo Push failed with exit code %ERR%
    pause
    exit /b %ERR%
)
echo Push completed.
pause
