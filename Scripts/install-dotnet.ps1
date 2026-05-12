# PowerShell script to install .NET 8.0 SDK
# This script checks for .NET 8.0 SDK and installs it if not present

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ".NET 8.0 SDK Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator (helpful for troubleshooting)
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Note: Not running as administrator. Some installation methods may require admin rights." -ForegroundColor Yellow
    Write-Host ""
}

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

Write-Host "Checking for .NET 8.0 SDK installation..." -ForegroundColor Cyan
$installedSdk = Find-DotNet8Sdk
if ($installedSdk) {
    Write-Host ".NET 8.0 SDK is already installed (version $($installedSdk.Version))." -ForegroundColor Green
    Write-Host "Location: $($installedSdk.Location)" -ForegroundColor Green
    Write-Host "Skipping SDK installation." -ForegroundColor Green
    Write-Host ""
    exit 0
}

# Check if winget is available (Windows Package Manager)
$wingetAvailable = $false
try {
    $wingetVersion = winget --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        $wingetAvailable = $true
        Write-Host "Windows Package Manager (winget) is available." -ForegroundColor Green
    }
} catch {
    $wingetAvailable = $false
}

if ($wingetAvailable) {
    Write-Host "Installing .NET 8.0 SDK using winget..." -ForegroundColor Yellow
    Write-Host "This may take a few minutes. Please wait..." -ForegroundColor Yellow
    Write-Host ""
    
    # Install .NET 8.0 SDK using winget
    try {
        winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements 2>&1 | Out-Host
    } catch {
        Write-Host "Error during winget installation: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host ".NET 8.0 SDK installed successfully!" -ForegroundColor Green
        
        # Refresh environment variables
        Write-Host "Refreshing environment variables..." -ForegroundColor Yellow
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        
        # Verify installation
        Start-Sleep -Seconds 2
        $installedSdk = Find-DotNet8Sdk
        if ($installedSdk) {
            Write-Host "Verified: .NET 8.0 SDK version $($installedSdk.Version) is now available." -ForegroundColor Green
            Write-Host "Location: $($installedSdk.Location)" -ForegroundColor Green
            exit 0
        } else {
            Write-Host "Warning: .NET was installed but may require a new command prompt to be available." -ForegroundColor Yellow
            Write-Host "Please restart this script or open a new command prompt." -ForegroundColor Yellow
            exit 0
        }
    } else {
        Write-Host ""
        Write-Host "Failed to install .NET 8.0 SDK using winget." -ForegroundColor Red
        if (-not $isAdmin) {
            Write-Host "Tip: Try running this script as administrator (right-click -> Run as administrator)" -ForegroundColor Yellow
        }
        Write-Host "Attempting alternative installation method..." -ForegroundColor Yellow
    }
}

# Fallback: Use dotnet-install script or direct download
Write-Host ""
Write-Host "Attempting alternative installation method..." -ForegroundColor Yellow

# Try using the dotnet-install.ps1 script (official Microsoft script)
$installScriptPath = "$env:TEMP\dotnet-install.ps1"
$installScriptUrl = "https://dot.net/v1/dotnet-install.ps1"

try {
    Write-Host "Downloading .NET installation script..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $installScriptUrl -OutFile $installScriptPath -ErrorAction Stop -UseBasicParsing
    } catch {
        Write-Host "Failed to download installation script. Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "This may be due to:" -ForegroundColor Yellow
        Write-Host "  - No internet connection" -ForegroundColor Yellow
        Write-Host "  - Firewall blocking the download" -ForegroundColor Yellow
        Write-Host "  - Proxy settings issues" -ForegroundColor Yellow
        throw
    }
    
    Write-Host "Installing .NET 8.0 SDK..." -ForegroundColor Yellow
    Write-Host "This may take a few minutes. Please wait..." -ForegroundColor Yellow
    Write-Host ""
    
    # Install .NET 8.0 SDK using the official install script
    & powershell -ExecutionPolicy Bypass -File $installScriptPath -Channel 8.0 -InstallDir "$env:ProgramFiles\dotnet"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host ".NET 8.0 SDK installed successfully!" -ForegroundColor Green
        
        # Add to PATH if not already there
        $dotnetPath = "$env:ProgramFiles\dotnet"
        $currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        if ($currentPath -notlike "*$dotnetPath*") {
            [Environment]::SetEnvironmentVariable("Path", "$currentPath;$dotnetPath", "Machine")
            $env:Path += ";$dotnetPath"
        }
        
        # Clean up install script
        Remove-Item $installScriptPath -ErrorAction SilentlyContinue
        
        # Verify installation
        Start-Sleep -Seconds 2
        $installedSdk = Find-DotNet8Sdk
        if ($installedSdk) {
            Write-Host "Verified: .NET 8.0 SDK version $($installedSdk.Version) is now available." -ForegroundColor Green
            Write-Host "Location: $($installedSdk.Location)" -ForegroundColor Green
            exit 0
        } else {
            Write-Host "Warning: .NET was installed but may require a new command prompt to be available." -ForegroundColor Yellow
            exit 0
        }
    } else {
        throw "Installation script returned error code: $LASTEXITCODE"
    }
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Installation Failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "The automatic installation failed. Common causes:" -ForegroundColor Yellow
    Write-Host "  - Missing administrator privileges" -ForegroundColor Yellow
    Write-Host "  - No internet connection" -ForegroundColor Yellow
    Write-Host "  - Antivirus or firewall blocking the installation" -ForegroundColor Yellow
    Write-Host "  - Insufficient disk space" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please install .NET 8.0 SDK manually:" -ForegroundColor Cyan
    Write-Host "1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    Write-Host "2. Download the '.NET 8.0 SDK' installer for Windows x64" -ForegroundColor White
    Write-Host "3. Run the installer (may require administrator rights)" -ForegroundColor White
    Write-Host "4. Restart the game launcher" -ForegroundColor White
    Write-Host ""
    Write-Host "For more help, see WINDOWS_SETUP_GUIDE.md" -ForegroundColor Cyan
    Write-Host ""
    
    # Optionally open the download page
    try {
        $response = Read-Host "Would you like to open the download page now? (Y/N)"
        if ($response -eq "Y" -or $response -eq "y") {
            Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
        }
    } catch {
        # If Read-Host fails (non-interactive), just continue
    }
    
    # Clean up install script
    Remove-Item $installScriptPath -ErrorAction SilentlyContinue
    
    exit 1
}

