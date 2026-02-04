# Spreadsheet Parser Status

## ✅ Completed Features

1. **Google Sheets URL Support**: The parser can now fetch directly from a published Google Sheets CSV URL
2. **Default Configuration**: Updated to use your Google Sheets URL by default
3. **CSV Parsing**: Successfully parses 85 actions from the spreadsheet
4. **JSON Generation**: Generates valid Actions.json compatible with the game

## 🔧 Current Issue: Bonus Detection

The parser is successfully reading the spreadsheet and generating JSON, but ABILITY/ACTION keyword bonuses may need verification. 

### Known Data Structure

Keywords: **ABILITY** (bonuses consumed on successful ability use) and **ACTION** (bonuses consumed per attack roll). The parser accepts legacy cadence values and maps them: `ACTION`/`ACTIONS` → ABILITY, `ATTACK`/`ATTACKS` → ACTION.

From the live Google Sheets data:
- **AMPLIFY ACCURACY**: Has `ACCURACY='20'` at column index 12, `CADENCE='ACTION'` or `'ABILITY'`, `DURATION='1'`
- **CONCENTRATE**: Has a bonus value at column index 16, `CADENCE='ATTACK'` or `'ACTION'`, `DURATION='1'`
- **GRUNT**: Has bonus values, `CADENCE='ATTACKS'` or `'ACTIONS'`, `DURATION='2'`

### Column Mapping

Based on the header row:
- Index 8: DURATION
- Index 9: CADENCE  
- Index 12: ACCUARCY (Hero)
- Index 13: HIT (Hero)
- Index 14: COMBO (Hero)
- Index 15: CRIT (Hero)
- Index 16: ACCUARCY (Enemy) - sometimes contains Hero bonus values
- Index 17: HIT (Enemy)

## 🎯 Next Steps

The parser infrastructure is complete. To finish bonus detection:

1. **Verify Column Reading**: Ensure `HeroAccuracy`, `HeroHit`, etc. are being populated from the correct CSV columns
2. **Test Bonus Creation**: Verify that when values are found, they're being added to `bonusItems` correctly
3. **Check JSON Serialization**: Ensure bonus groups are being serialized to JSON properly

## 📝 Usage

### Using Google Sheets URL (Default)
```bash
dotnet run --project Code/Code.csproj -- PARSE
```

### Using Custom URL or File
```bash
dotnet run --project Code/Code.csproj -- PARSE "YOUR_URL_OR_PATH" "GameData/Actions.json"
```

### Using PowerShell Script
```powershell
.\Scripts\parse-from-google-sheets.ps1 -GoogleSheetsUrl "YOUR_URL"
```

## 🔗 Your Google Sheets URL

Currently configured:
```
https://docs.google.com/spreadsheets/d/e/2PACX-1vTD25Fiu9OIwSaBildDnGlE8aaouIyTjO6XlFqgY5XdSwgOh462ZcVueJKsbb4kSQ/pub?gid=2020359111&single=true&output=csv
```

## 📊 Current Output

- ✅ 85 actions successfully parsed
- ✅ JSON file generated at `GameData/Actions.json`
- ⚠️ ACTION/ATTACK bonuses not yet captured (empty bonusGroups arrays)
