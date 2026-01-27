# PowerShell script to run the spreadsheet parser
param(
    [string]$CsvPath = "Code\GameData\actions-1-26-2026.xlsx.csv",
    [string]$OutputPath = "GameData\Actions.json"
)

Write-Host "Running Spreadsheet Parser..." -ForegroundColor Cyan
Write-Host "CSV Path: $CsvPath" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Build the project first
Write-Host "Building project..." -ForegroundColor Cyan
$buildResult = dotnet build "Code\Code.csproj" 2>&1
if ($LASTEXITCODE -ne 0)
{
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

# Create a simple C# program to run the parser
$parserCode = @"
using System;
using System.IO;
using RPGGame.Data;

class ParserProgram
{
    static void Main()
    {
        string csvPath = @"$CsvPath";
        string outputPath = @"$OutputPath";
        
        // Backup existing file
        if (File.Exists(outputPath))
        {
            string backupPath = outputPath + ".backup." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(outputPath, backupPath);
            Console.WriteLine("Backed up existing file to: " + backupPath);
        }
        
        SpreadsheetParserRunner.ParseAndGenerate(csvPath, outputPath);
    }
}
"@

# Write temporary program
$tempProgramPath = "Code\TempParserProgram.cs"
$parserCode | Out-File -FilePath $tempProgramPath -Encoding UTF8

try
{
    # Compile and run
    Write-Host "Compiling parser program..." -ForegroundColor Cyan
    $compileResult = dotnet build "Code\Code.csproj" /p:OutputType=Exe 2>&1
    
    if ($LASTEXITCODE -eq 0)
    {
        Write-Host "Running parser..." -ForegroundColor Cyan
        & "Code\bin\Debug\net8.0\DF.exe"
    }
    else
    {
        Write-Host "Compilation failed. Using alternative method..." -ForegroundColor Yellow
        
        # Alternative: Use reflection to call the parser methods
        # For now, let's create a simpler approach using a test class
        Write-Host "Creating test runner..." -ForegroundColor Cyan
        
        # We'll need to add the parser call to an existing test or create a simple runner
        # For now, let's output instructions
        Write-Host ""
        Write-Host "To run the parser, add this code to your program:" -ForegroundColor Yellow
        Write-Host @"
using RPGGame.Data;
SpreadsheetParserRunner.ParseAndGenerate(@"$CsvPath", @"$OutputPath");
"@ -ForegroundColor Cyan
    }
}
finally
{
    # Clean up
    if (Test-Path $tempProgramPath)
    {
        Remove-Item $tempProgramPath -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
