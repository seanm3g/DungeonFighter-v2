@echo off
echo Killing any running DF.exe processes...
taskkill /f /im DF.exe 2>nul
echo Building DF.exe...
cd Code
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../dist
echo.
echo Running DF.exe...
cd ..
start "" dist\DF.exe
