# PowerShell script to install .NET 8.0 SDK
# This script checks for .NET 8.0 SDK and installs it if not present

Write-Host "Checking for .NET 8.0 SDK installation..." -ForegroundColor Cyan

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
    winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host ".NET 8.0 SDK installed successfully!" -ForegroundColor Green
        
        # Refresh environment variables
        Write-Host "Refreshing environment variables..." -ForegroundColor Yellow
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        
        # Verify installation
        Start-Sleep -Seconds 2
        $dotnetVersion = dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Verified: .NET version $dotnetVersion is now available." -ForegroundColor Green
            exit 0
        } else {
            Write-Host "Warning: .NET was installed but may require a new command prompt to be available." -ForegroundColor Yellow
            Write-Host "Please restart this script or open a new command prompt." -ForegroundColor Yellow
            exit 0
        }
    } else {
        Write-Host ""
        Write-Host "Failed to install .NET 8.0 SDK using winget." -ForegroundColor Red
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
    Invoke-WebRequest -Uri $installScriptUrl -OutFile $installScriptPath -ErrorAction Stop
    
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
        $dotnetVersion = & "$dotnetPath\dotnet.exe" --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Verified: .NET version $dotnetVersion is now available." -ForegroundColor Green
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
    Write-Host "Failed to install .NET 8.0 SDK automatically." -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install .NET 8.0 SDK manually:" -ForegroundColor Yellow
    Write-Host "1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Write-Host "2. Download and run the .NET 8.0 SDK installer for Windows x64" -ForegroundColor Yellow
    Write-Host "3. Run the game launcher again" -ForegroundColor Yellow
    Write-Host ""
    
    # Optionally open the download page
    $response = Read-Host "Would you like to open the download page now? (Y/N)"
    if ($response -eq "Y" -or $response -eq "y") {
        Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
    }
    
    # Clean up install script
    Remove-Item $installScriptPath -ErrorAction SilentlyContinue
    
    exit 1
}

