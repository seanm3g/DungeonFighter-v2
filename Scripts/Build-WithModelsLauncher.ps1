$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$launcherPath = Join-Path $projectRoot 'With models.exe'
$iconPath = Join-Path $projectRoot 'Code/UI/Avalonia/Assets/df_icon.ico'
$sourcePath = Join-Path $PSScriptRoot 'WithModelsLauncher.cs'
& $compilerPath /nologo /target:winexe /reference:System.Windows.Forms.dll "/win32icon:$iconPath" "/out:$launcherPath" $sourcePath
if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }
Write-Output "Created $launcherPath"
