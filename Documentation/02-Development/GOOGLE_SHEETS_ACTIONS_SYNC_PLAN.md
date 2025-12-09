# Google Sheets to Actions.json Sync Tool

## Overview

Create a standalone C# console tool that pulls action data from a Google Sheets spreadsheet and replaces the entire `GameData/Actions.json` file. This will be a manual-trigger script similar to existing scripts in the `Scripts/` folder.

## Difficulty Assessment

**Medium complexity** - Requires:
- Google API setup (OAuth or service account)
- Google Sheets API integration
- Data mapping from table rows to JSON objects
- Type conversion (strings to ints, doubles, bools, arrays)
- JSON serialization with proper formatting

## Implementation Plan

### 1. Create Standalone Console Tool
- **File**: `Code/Tools/GoogleSheetsSyncTool.cs` (or separate project)
- **Alternative**: PowerShell script using Google Sheets API REST calls
- **Recommendation**: C# console tool for better type safety and reuse of existing `ActionData` class

### 2. Add Google Sheets API Dependency
- **File**: `Code/Code.csproj`
- Add NuGet package: `Google.Apis.Sheets.v4` (latest version)
- May also need: `Google.Apis.Auth` for authentication

### 3. Create Configuration File
- **File**: `Scripts/google-sheets-config.json` (gitignored)
- Store: Spreadsheet ID, Sheet name, OAuth credentials path, or service account JSON path
- Template file: `Scripts/google-sheets-config.template.json` (committed)

### 4. Implement Google Sheets Reader
- **File**: `Code/Tools/GoogleSheetsReader.cs`
- Methods:
  - Authenticate (OAuth or service account)
  - Read spreadsheet data (get all rows from specified sheet)
  - Map header row to ActionData properties
  - Convert rows to `ActionData` objects with proper type conversion

### 5. Implement Data Mapper
- **File**: `Code/Tools/ActionDataMapper.cs` or inline in sync tool
- Handle:
  - Column name mapping (case-insensitive matching)
  - Type conversion: strings → int, double, bool, List<string> (for tags)
  - Default values for missing columns
  - Validation (required fields like "name")

### 6. Implement JSON Writer
- Use existing `System.Text.Json` with `JsonSerializerOptions` for formatting
- **File**: `Code/Tools/GoogleSheetsSyncTool.cs`
- Write to: `GameData/Actions.json`
- Create backup before overwriting: `GameData/Actions.json.backup`

### 7. Create Batch Script Wrapper
- **File**: `Scripts/sync-actions-from-sheets.bat`
- Calls the C# tool
- Provides user-friendly output
- Handles errors gracefully

### 8. Documentation
- **File**: `Scripts/README_GOOGLE_SHEETS_SYNC.md`
- Instructions for:
  - Setting up Google Cloud project
  - Creating OAuth credentials or service account
  - Configuring the spreadsheet (column structure)
  - Running the sync tool
  - Troubleshooting

## Key Technical Details

### Google Sheets API Approach
- Use **Google Sheets API** (not Docs API) - much easier for table data
- Two authentication options:
  1. **Service Account** (recommended for automation): JSON key file, no user interaction
  2. **OAuth 2.0** (for personal use): Requires browser authentication flow

### Data Mapping Strategy
Since table columns match JSON fields:
- First row = headers (column names)
- Subsequent rows = action data
- Map column names to `ActionData` properties using `JsonPropertyName` attributes
- Handle special cases:
  - `tags`: Parse comma-separated string or JSON array string
  - Boolean fields: Convert "true"/"false", "yes"/"no", "1"/"0"
  - Numeric fields: Parse with validation

### Error Handling
- Validate spreadsheet access
- Validate data types
- Create backup before overwriting
- Log errors to console/file
- Preserve original file if sync fails

## Files to Create/Modify

### New Files
- `Code/Tools/GoogleSheetsSyncTool.cs` - Main sync tool
- `Code/Tools/GoogleSheetsReader.cs` - Google API wrapper
- `Scripts/google-sheets-config.template.json` - Config template
- `Scripts/sync-actions-from-sheets.bat` - Batch wrapper
- `Scripts/README_GOOGLE_SHEETS_SYNC.md` - Setup documentation

### Modified Files
- `Code/Code.csproj` - Add Google Sheets API NuGet package

## Alternative: Simpler PowerShell Approach
If C# tool is too complex, could use PowerShell with:
- `Invoke-RestMethod` for Google Sheets API REST calls
- Manual JSON parsing and writing
- Less type safety but faster to implement

## Estimated Complexity
- **Setup time**: 1-2 hours (Google API credentials, testing)
- **Implementation**: 2-3 hours (C# tool) or 1 hour (PowerShell)
- **Testing**: 1 hour
- **Total**: 4-6 hours for C# tool, 2-3 hours for PowerShell

## Implementation Todos

1. **setup-google-api**: Set up Google Cloud project and create OAuth/service account credentials. Document setup process.
2. **add-nuget-packages**: Add Google.Apis.Sheets.v4 and Google.Apis.Auth NuGet packages to Code.csproj
3. **create-config-template**: Create google-sheets-config.template.json with spreadsheet ID, sheet name, and auth settings
4. **implement-sheets-reader**: Create GoogleSheetsReader.cs to handle authentication and read spreadsheet data (depends on: add-nuget-packages)
5. **implement-data-mapper**: Create mapping logic to convert table rows to ActionData objects with proper type conversion (depends on: implement-sheets-reader)
6. **implement-sync-tool**: Create GoogleSheetsSyncTool.cs main console application that orchestrates reading, mapping, and writing (depends on: implement-data-mapper)
7. **create-backup-logic**: Add backup creation before overwriting Actions.json to preserve original data (depends on: implement-sync-tool)
8. **create-batch-wrapper**: Create sync-actions-from-sheets.bat script to run the tool with user-friendly output (depends on: implement-sync-tool)
9. **write-documentation**: Create README_GOOGLE_SHEETS_SYNC.md with setup instructions, usage guide, and troubleshooting (depends on: setup-google-api)

## User Requirements Summary
- **Use Case**: Manual trigger - Run a script/tool when needed to sync from Google Docs
- **API Access**: Need to set up Google API access (OAuth/service account)
- **Table Structure**: Table columns match Actions.json fields (name, type, damageMultiplier, etc.)
- **Update Behavior**: Replace entire Actions.json with Google Docs data

## Related Files
- `Code/Data/ActionLoader.cs` - Contains `ActionData` class that matches JSON structure
- `GameData/Actions.json` - Target file to be updated
- `Code/Data/JsonLoader.cs` - Existing JSON loading utilities

