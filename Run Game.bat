@echo off
title Dungeon Fighter v2
echo.
echo Starting Dungeon Fighter v2...
echo.

REM Check if Debug executable exists, if not, build it
if not exist "Code\bin\Debug\net8.0\DF.exe" (
    echo Building game (first time setup)...
    dotnet build Code\Code.csproj --configuration Debug
    if errorlevel 1 (
        echo.
        echo ERROR: Build failed! Please check that .NET 8.0 SDK is installed.
        echo.
        pause
        exit /b 1
    )
)

REM Run the game
start "" "Code\bin\Debug\net8.0\DF.exe"

