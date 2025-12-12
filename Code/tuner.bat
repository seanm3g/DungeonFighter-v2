@echo off
setlocal enabledelayedexpansion

REM Default to 5 iterations if not specified
set iterations=5

REM Check if an argument was provided
if not "%1"=="" (
    set iterations=%1
)

REM Run the tuner
dotnet run -- TUNING !iterations!
