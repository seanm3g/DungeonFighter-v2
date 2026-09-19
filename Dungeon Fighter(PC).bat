@echo off
REM Thin wrapper — real launcher is Scripts\launch-windows.bat (no parentheses in that name).
if not exist "%~dp0Scripts\launch-windows.bat" (
    echo Extract the whole game folder first, then double-click DungeonFighter-PC.bat
    echo in the extracted folder. Do not run this from inside the zip window.
    echo.
    pause
    exit /b 1
)
call "%~dp0Scripts\launch-windows.bat"
