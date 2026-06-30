@echo off
REM 10 playthroughs per class (40 total), full sim -> analyze -> apply loop
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-playthrough-tune.ps1" %*
exit /b %ERRORLEVEL%
