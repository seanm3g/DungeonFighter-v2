# Show Build and Execution Metrics
# Displays a summary of build and execution time metrics

Write-Host "Loading metrics..." -ForegroundColor Cyan

$metricsPath = Join-Path $PSScriptRoot "..\GameData\build_execution_metrics.json"

if (-not (Test-Path $metricsPath)) {
    Write-Host "No metrics file found. Metrics will be created after first build/execution." -ForegroundColor Yellow
    exit 0
}

try {
    $metrics = Get-Content $metricsPath -Raw | ConvertFrom-Json
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  BUILD & EXECUTION METRICS SUMMARY" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    if ($metrics.BuildMetrics -and $metrics.BuildMetrics.Count -gt 0) {
        $successfulBuilds = $metrics.BuildMetrics | Where-Object { $_.Success -eq $true }
        
        if ($successfulBuilds.Count -gt 0) {
            $avgMs = ($successfulBuilds | Measure-Object -Property BuildTimeMs -Average).Average
            $minMs = ($successfulBuilds | Measure-Object -Property BuildTimeMs -Minimum).Minimum
            $maxMs = ($successfulBuilds | Measure-Object -Property BuildTimeMs -Maximum).Maximum
            $latest = $successfulBuilds[-1]
            
            Write-Host "Build Time Statistics:" -ForegroundColor Yellow
            Write-Host "  Total Successful Builds: $($successfulBuilds.Count)" -ForegroundColor White
            Write-Host "  Average: $([math]::Round($avgMs / 1000, 2))s ($([math]::Round($avgMs))ms)" -ForegroundColor White
            Write-Host "  Min: $([math]::Round($minMs / 1000, 2))s ($minMs ms)" -ForegroundColor White
            Write-Host "  Max: $([math]::Round($maxMs / 1000, 2))s ($maxMs ms)" -ForegroundColor White
            Write-Host "  Latest: $([math]::Round($latest.BuildTimeMs / 1000, 2))s ($($latest.BuildTimeMs) ms)" -ForegroundColor White
            Write-Host ""
        }
    }
    
    if ($metrics.ExecutionMetrics -and $metrics.ExecutionMetrics.Count -gt 0) {
        $avgMs = ($metrics.ExecutionMetrics | Measure-Object -Property ExecutionTimeMs -Average).Average
        $minMs = ($metrics.ExecutionMetrics | Measure-Object -Property ExecutionTimeMs -Minimum).Minimum
        $maxMs = ($metrics.ExecutionMetrics | Measure-Object -Property ExecutionTimeMs -Maximum).Maximum
        $latest = $metrics.ExecutionMetrics[-1]
        
        Write-Host "Execution Time Statistics:" -ForegroundColor Yellow
        Write-Host "  Total Executions: $($metrics.ExecutionMetrics.Count)" -ForegroundColor White
        Write-Host "  Average: $([math]::Round($avgMs / 1000, 2))s ($([math]::Round($avgMs))ms)" -ForegroundColor White
        Write-Host "  Min: $([math]::Round($minMs / 1000, 2))s ($minMs ms)" -ForegroundColor White
        Write-Host "  Max: $([math]::Round($maxMs / 1000, 2))s ($maxMs ms)" -ForegroundColor White
        Write-Host "  Latest: $([math]::Round($latest.ExecutionTimeMs / 1000, 2))s ($($latest.ExecutionTimeMs) ms) - Mode: $($latest.Mode)" -ForegroundColor White
        Write-Host ""
    }
    
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
} catch {
    Write-Host "Error reading metrics file: $_" -ForegroundColor Red
    exit 1
}

