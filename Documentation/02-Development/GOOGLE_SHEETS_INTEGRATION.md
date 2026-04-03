# Google Sheets Integration Guide

This guide explains how to connect your Google Sheets directly to the action parser, eliminating the need to manually export CSV files.

## Publishing Your Google Sheet as CSV

1. **Open your Google Sheet** with the actions data
2. **Click "File" → "Share" → "Publish to web"**
3. **Select the "ACTIONS" tab** (or whichever tab contains your action data)
4. **Choose "Comma-separated values (.csv)"** as the format
5. **Click "Publish"**
6. **Copy the published link** - it will look like:
   ```
   https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/export?format=csv&gid=TAB_ID
   ```

## Using the Published URL

Once you have the published CSV URL, you can use it directly with the parser:

```bash
dotnet run --project Code/Code.csproj -- PARSE "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/export?format=csv&gid=YOUR_TAB_ID" "GameData/Actions.json"
```

Or update the default in `Program.cs` to use your URL instead of a local file path.

## Benefits

- **Automatic Updates**: Changes in Google Sheets are immediately available
- **No Manual Export**: No need to download and save CSV files
- **Version Control**: The URL stays the same, so you can always fetch the latest data
- **Collaboration**: Multiple people can edit the sheet, and the parser always gets the latest version

## Notes

- The published CSV is **public** (anyone with the link can view it)
- If you need private access, you'll need to use the Google Sheets API with authentication
- The parser automatically handles the CSV format from Google Sheets
- Make sure the "ACTIONS" tab is the one you publish (or update the parser to use a different tab)

## Troubleshooting

If you get connection errors:
- Verify the URL is correct and the sheet is published
- Check that the tab name matches (case-sensitive)
- Ensure the sheet is accessible (not restricted)
