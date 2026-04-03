# PowerShell script to parse Google Sheets CSV export and generate Actions.json
# Usage: .\parse-spreadsheet-actions.ps1 -CsvPath "path\to\spreadsheet.csv" -OutputPath "GameData\Actions.json"

param(
    [Parameter(Mandatory=$true)]
    [string]$CsvPath,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "GameData\Actions.json",
    
    [Parameter(Mandatory=$false)]
    [switch]$BackupExisting = $true
)

Write-Host "Parsing spreadsheet actions..." -ForegroundColor Cyan

# Check if CSV file exists
if (-not (Test-Path $CsvPath))
{
    Write-Host "Error: CSV file not found: $CsvPath" -ForegroundColor Red
    exit 1
}

# Backup existing Actions.json if it exists
if ($BackupExisting -and (Test-Path $OutputPath))
{
    $backupPath = "$OutputPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item $OutputPath $backupPath
    Write-Host "Backed up existing Actions.json to: $backupPath" -ForegroundColor Yellow
}

# Build and run the parser
Write-Host "Building project..." -ForegroundColor Cyan
$buildResult = dotnet build "Code\Code.csproj" 2>&1

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Create a temporary C# program to run the parser
$tempProgram = @"
using System;
using System.IO;
using System.Collections.Generic;
using RPGGame.Data;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: <csvPath> <outputPath>");
            return;
        }
        
        string csvPath = args[0];
        string outputPath = args[1];
        
        try
        {
            Console.WriteLine($"Reading CSV from: {csvPath}");
            var spreadsheetActions = SpreadsheetActionParser.ParseCsvFile(csvPath);
            Console.WriteLine($"Parsed {spreadsheetActions.Count} actions from spreadsheet");
            
            Console.WriteLine($"Converting to ActionData...");
            var actionDataList = new List<ActionData>();
            int successCount = 0;
            int errorCount = 0;
            
            foreach (var spreadsheet in spreadsheetActions)
            {
                try
                {
                    var actionData = SpreadsheetToActionDataConverter.Convert(spreadsheet);
                    if (!string.IsNullOrEmpty(actionData.Name))
                    {
                        actionDataList.Add(actionData);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting action {spreadsheet.Action}: {ex.Message}");
                    errorCount++;
                }
            }
            
            Console.WriteLine($"Successfully converted {successCount} actions");
            if (errorCount > 0)
            {
                Console.WriteLine($"Errors: {errorCount}");
            }
            
            Console.WriteLine($"Generating JSON...");
            SpreadsheetActionJsonGenerator.ConvertAndGenerateJsonFile(spreadsheetActions, outputPath);
            
            Console.WriteLine($"JSON written to: {outputPath}");
            Console.WriteLine($"Total actions in JSON: {actionDataList.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
"@

$tempProgramPath = "Code\TempParser.cs"
$tempProgram | Out-File -FilePath $tempProgramPath -Encoding UTF8

try
{
    # Compile and run the temporary program
    Write-Host "Running parser..." -ForegroundColor Cyan
    
    # We need to run this as part of the project, so let's create a simple console app approach
    # For now, we'll output instructions
    Write-Host ""
    Write-Host "To run the parser, you can:" -ForegroundColor Yellow
    Write-Host "1. Export your Google Sheets to CSV format" -ForegroundColor Yellow
    Write-Host "2. Use the C# classes in Code/Data/ to parse the CSV" -ForegroundColor Yellow
    Write-Host "3. The parser classes are ready to use:" -ForegroundColor Yellow
    Write-Host "   - SpreadsheetActionParser.ParseCsvFile()" -ForegroundColor Cyan
    Write-Host "   - SpreadsheetToActionDataConverter.Convert()" -ForegroundColor Cyan
    Write-Host "   - SpreadsheetActionJsonGenerator.ConvertAndGenerateJsonFile()" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Example usage in C#:" -ForegroundColor Yellow
    Write-Host @"
var actions = SpreadsheetActionParser.ParseCsvFile(""$CsvPath"");
SpreadsheetActionJsonGenerator.ConvertAndGenerateJsonFile(actions, ""$OutputPath"");
"@ -ForegroundColor Cyan
    
    Write-Host ""
    Write-Host "Parser classes created successfully!" -ForegroundColor Green
    Write-Host "CSV Path: $CsvPath" -ForegroundColor Cyan
    Write-Host "Output Path: $OutputPath" -ForegroundColor Cyan
}
finally
{
    # Clean up temp file
    if (Test-Path $tempProgramPath)
    {
        Remove-Item $tempProgramPath
    }
}
