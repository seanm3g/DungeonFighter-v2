@echo off
REM Prefer this file when sharing the game — no spaces or parentheses in the name.
if not exist "%~dp0Scripts\launch-windows.bat" (
    echo Extract the whole game folder first, then double-click this file.
    echo This launcher must sit next to the Code and GameData folders.
    echo.
    pause
    exit /b 1
)
call "%~dp0Scripts\launch-windows.bat"
