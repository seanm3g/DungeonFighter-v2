@echo off
setlocal
cd /d "%~dp0"
echo Building the Dungeon Fighters Art Lab...
dotnet build Code\Code.csproj -o Code\bin\Art\net8.0 -p:KeepRunningInstance=true --verbosity quiet
if errorlevel 1 (
  echo Build failed. If the Art Lab is already open, close it and try again.
  pause
  exit /b 1
)
start "Dungeon Fighters Art Lab" "Code\bin\Art\net8.0\DF.exe" ART
