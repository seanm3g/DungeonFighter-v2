# Build Metrics Helper Functions
# Tracks build time and records metrics

function Record-BuildMetric {
    param(
        [string]$BuildType = "Debug",
        [long]$BuildTimeMs,
        [double]$BuildTimeSeconds,
        [bool]$Success
    )

    $metricsPath = Join-Path $PSScriptRoot "..\GameData\build_execution_metrics.json"
    $metricsDir = Split-Path -Parent $metricsPath
    
    # Ensure directory exists
    if (-not (Test-Path $metricsDir)) {
        New-Item -ItemType Directory -Path $metricsDir -Force | Out-Null
    }

    # Load existing metrics
    $metricsData = @{
        BuildMetrics = @()
        ExecutionMetrics = @()
    }
    
    if (Test-Path $metricsPath) {
        try {
            $content = Get-Content $metricsPath -Raw | ConvertFrom-Json
            if ($content.BuildMetrics) {
                $metricsData.BuildMetrics = $content.BuildMetrics
            }
            if ($content.ExecutionMetrics) {
                $metricsData.ExecutionMetrics = $content.ExecutionMetrics
            }
        } catch {
            # If JSON is invalid, start fresh
        }
    }

    # Add new build metric
    $newMetric = @{
        Timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        BuildType = $BuildType
        BuildTimeMs = $BuildTimeMs
        BuildTimeSeconds = $BuildTimeSeconds
        Success = $Success
    }
    
    $metricsData.BuildMetrics += $newMetric
    
    # Keep only last 100 build metrics
    if ($metricsData.BuildMetrics.Count -gt 100) {
        $metricsData.BuildMetrics = $metricsData.BuildMetrics[-100..-1]
    }

    # Save metrics
    $metricsData | ConvertTo-Json -Depth 10 | Set-Content $metricsPath
    
    # Display build time
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Build Time: $($BuildTimeSeconds.ToString('F2')) seconds ($BuildTimeMs ms)" -ForegroundColor Cyan
    Write-Host "  Build Type: $BuildType" -ForegroundColor Cyan
    Write-Host "  Status: $(if ($Success) { 'SUCCESS' } else { 'FAILED' })" -ForegroundColor $(if ($Success) { 'Green' } else { 'Red' })
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

function Measure-Build {
    param(
        [scriptblock]$BuildScript,
        [string]$BuildType = "Debug"
    )
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $success = $false
    
    try {
        & $BuildScript
        $success = ($LASTEXITCODE -eq 0)
    } catch {
        $success = $false
    } finally {
        $stopwatch.Stop()
        $buildTimeMs = $stopwatch.ElapsedMilliseconds
        $buildTimeSeconds = $stopwatch.Elapsed.TotalSeconds
        
        Record-BuildMetric -BuildType $BuildType -BuildTimeMs $buildTimeMs -BuildTimeSeconds $buildTimeSeconds -Success $success
    }
    
    return $success
}

