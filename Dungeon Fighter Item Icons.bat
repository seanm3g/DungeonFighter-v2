@echo off
setlocal
cd /d "%~dp0"
echo Building the Rarity Gallery...
dotnet build Code\Code.csproj -o Code\bin\RarityLab\net8.0 -p:KeepRunningInstance=true --verbosity quiet
if errorlevel 1 (
  echo Build failed. Close an existing Item Icon Lab before rebuilding.
  pause
  exit /b 1
)
dotnet build Scripts\ItemGalleryLauncher\ItemGalleryLauncher.csproj -o Code\bin\RarityLab\net8.0 --configfile Scripts\ItemGalleryLauncher\NuGet.Config --verbosity quiet
if errorlevel 1 exit /b 1
start "Dungeon Fighter Rarity Gallery" "Code\bin\RarityLab\net8.0\ItemGallery.exe"
