@echo off
title Dungeon Fighter v2 - Build and Run
echo.
echo ========================================
echo    Dungeon Fighter v2
echo ========================================
echo.
echo Building latest version...
echo.

REM Build the project
dotnet build Code\Code.csproj --configuration Debug
if errorlevel 1 (
    echo.
    echo ERROR: Build failed! Please check that .NET 8.0 SDK is installed.
    echo.
    pause
    exit /b 1
)

echo.
echo Build successful! Starting game...
echo.

REM Run the game
start "" "Code\bin\Debug\net8.0\DF.exe"

