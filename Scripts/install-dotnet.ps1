# PowerShell script to install .NET 8.0 SDK
# Prefers a user-local install (%USERPROFILE%\.dotnet) so friends can play without admin.

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ".NET 8.0 SDK Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
$userDotNetDir = Join-Path $env:USERPROFILE ".dotnet"

function Test-DotNet8Sdk {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DotNetCommand,
        [Parameter(Mandatory = $true)]
        [string]$Location
    )

    try {
        $sdkLines = & $DotNetCommand --list-sdks 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $sdkLines) {
            return $null
        }

        foreach ($line in @($sdkLines)) {
            if ($line -match "^8\.") {
                $version = ($line -split "\s+")[0]
                return [pscustomobject]@{
                    Version = $version
                    Path = $DotNetCommand
                    Location = $Location
                }
            }
        }
    } catch {
        # Candidate unavailable or not executable; try the next location.
    }

    return $null
}

function Find-DotNet8Sdk {
    $pathSdk = Test-DotNet8Sdk -DotNetCommand "dotnet" -Location "PATH"
    if ($pathSdk) {
        return $pathSdk
    }

    $commonPaths = @(
        (Join-Path $userDotNetDir "dotnet.exe"),
        "$env:ProgramFiles\dotnet\dotnet.exe",
        "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
    )

    foreach ($path in $commonPaths) {
        if (Test-Path $path) {
            $sdk = Test-DotNet8Sdk -DotNetCommand $path -Location ([System.IO.Path]::GetDirectoryName($path))
            if ($sdk) {
                return $sdk
            }
        }
    }

    return $null
}

function Add-UserDotNetPath {
    param([Parameter(Mandatory = $true)][string]$Dir)

    $env:DOTNET_ROOT = $Dir
    $env:Path = "$Dir;" + $env:Path

    try {
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $Dir, "User")
        $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
        if ([string]::IsNullOrEmpty($userPath)) {
            $userPath = ""
        }
        $parts = @($userPath -split ";" | Where-Object { $_ -and $_.Trim() -ne "" })
        if ($parts -notcontains $Dir) {
            $newPath = if ($userPath) { "$Dir;$userPath" } else { $Dir }
            [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
            Write-Host "Added $Dir to your user PATH." -ForegroundColor Green
        }
    } catch {
        Write-Host "Installed, but could not persist PATH. This launcher session can still use $Dir." -ForegroundColor Yellow
    }
}

function Install-DotNet8UserLocal {
    $installScriptPath = Join-Path $env:TEMP "dotnet-install.ps1"
    $installScriptUrl = "https://dot.net/v1/dotnet-install.ps1"

    Write-Host "Downloading the official .NET install script..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $installScriptUrl -OutFile $installScriptPath -UseBasicParsing

    Write-Host "Installing .NET 8.0 SDK to $userDotNetDir (no administrator required)..." -ForegroundColor Yellow
    Write-Host "This may take a few minutes. Please wait..." -ForegroundColor Yellow
    Write-Host ""

    & powershell -NoProfile -ExecutionPolicy Bypass -File $installScriptPath -Channel 8.0 -InstallDir $userDotNetDir
    $exit = $LASTEXITCODE
    Remove-Item $installScriptPath -ErrorAction SilentlyContinue
    if ($exit -ne 0) {
        throw "User-local install script returned error code: $exit"
    }

    Add-UserDotNetPath -Dir $userDotNetDir
    Start-Sleep -Seconds 2
    return Find-DotNet8Sdk
}

Write-Host "Checking for .NET 8.0 SDK installation..." -ForegroundColor Cyan
$installedSdk = Find-DotNet8Sdk
if ($installedSdk) {
    Write-Host ".NET 8.0 SDK is already installed (version $($installedSdk.Version))." -ForegroundColor Green
    Write-Host "Location: $($installedSdk.Location)" -ForegroundColor Green
    Write-Host "Skipping SDK installation." -ForegroundColor Green
    Write-Host ""
    exit 0
}

if (-not $isAdmin) {
    Write-Host "Installing to your user folder so administrator rights are not required." -ForegroundColor Yellow
    Write-Host ""
}

try {
    $installedSdk = Install-DotNet8UserLocal
    if ($installedSdk) {
        Write-Host ""
        Write-Host ".NET 8.0 SDK installed successfully (version $($installedSdk.Version))." -ForegroundColor Green
        Write-Host "Location: $($installedSdk.Location)" -ForegroundColor Green
        exit 0
    }
    Write-Host "Warning: .NET was installed but the 8.x SDK is not listed yet. Trying another method..." -ForegroundColor Yellow
} catch {
    Write-Host "User-local install did not succeed: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Attempting Windows Package Manager (winget)..." -ForegroundColor Yellow
}

$wingetAvailable = $false
try {
    $null = winget --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        $wingetAvailable = $true
        Write-Host "Windows Package Manager (winget) is available." -ForegroundColor Green
    }
} catch {
    $wingetAvailable = $false
}

if ($wingetAvailable) {
    Write-Host "Installing .NET 8.0 SDK using winget..." -ForegroundColor Yellow
    try {
        winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements 2>&1 | Out-Host
    } catch {
        Write-Host "Error during winget installation: $($_.Exception.Message)" -ForegroundColor Red
    }

    if ($LASTEXITCODE -eq 0) {
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
        Start-Sleep -Seconds 2
        $installedSdk = Find-DotNet8Sdk
        if ($installedSdk) {
            Write-Host "Verified: .NET 8.0 SDK version $($installedSdk.Version) is now available." -ForegroundColor Green
            Write-Host "Location: $($installedSdk.Location)" -ForegroundColor Green
            exit 0
        }
    } else {
        Write-Host "winget install did not succeed." -ForegroundColor Yellow
        if (-not $isAdmin) {
            Write-Host "Tip: winget often needs administrator rights. The user-folder install above does not." -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Red
Write-Host "Installation Failed" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "The automatic installation failed. Common causes:" -ForegroundColor Yellow
Write-Host "  - No internet connection" -ForegroundColor Yellow
Write-Host "  - Antivirus or firewall blocking the download" -ForegroundColor Yellow
Write-Host "  - Insufficient disk space" -ForegroundColor Yellow
Write-Host ""
Write-Host "Please install .NET 8.0 SDK manually:" -ForegroundColor Cyan
Write-Host "1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
Write-Host "2. Download the '.NET 8.0 SDK' installer for Windows x64" -ForegroundColor White
Write-Host "3. Run the installer" -ForegroundColor White
Write-Host "4. Restart the game launcher (DungeonFighter-PC.bat)" -ForegroundColor White
Write-Host ""
Write-Host "For more help, see WINDOWS_SETUP_GUIDE.md" -ForegroundColor Cyan
Write-Host ""

try {
    $response = Read-Host "Would you like to open the download page now? (Y/N)"
    if ($response -eq "Y" -or $response -eq "y") {
        Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
    }
} catch {
    # If Read-Host fails (non-interactive), just continue
}

exit 1
