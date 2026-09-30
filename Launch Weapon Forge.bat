@echo off
cd /d "%~dp0tools\weapon-forge"
start "Relic Forge" /min node server.mjs
start "" "http://127.0.0.1:4318"
