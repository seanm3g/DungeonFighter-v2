# Spreadsheet Action Parser Implementation Summary

## Overview

A complete parser system has been implemented to convert Google Sheets action definitions into JSON format compatible with the game's action system, with full support for ACTION/ATTACK keyword mechanics.

## Implementation Complete

All components have been created and integrated:

### ✅ Data Models
- **`SpreadsheetActionData.cs`** - Models spreadsheet rows with column mapping
- **`ActionAttackBonus.cs`** - Data structures for ACTION/ATTACK bonuses

### ✅ Parsing Components
- **`SpreadsheetActionParser.cs`** - CSV parser for reading spreadsheet exports
- **`ActionAttackKeywordProcessor.cs`** - Processes CADENCE column and generates bonus structures
- **`SpreadsheetToActionDataConverter.cs`** - Converts spreadsheet data to ActionData format

### ✅ Integration
- **`ActionData`** - Added `ActionAttackBonuses` property
- **`Action`** - Added `ActionAttackBonuses` property
- **`CharacterEffects`** - Added tracking and consumption methods for ACTION/ATTACK bonuses
- **`ActionExecutionFlow`** - Integrated bonus application and consumption logic
- **`ActionLoader`** - Updated to handle ACTION/ATTACK bonuses

### ✅ Output Generation
- **`SpreadsheetActionJsonGenerator.cs`** - Generates JSON from parsed actions
- **`parse-spreadsheet-actions.ps1`** - PowerShell script for running the parser

## How ACTION/ATTACK Bonuses Work

### ACTION Keyword
- Bonuses apply to the **next ACTION in sequence**
- Only consumed when action **successfully executes**
- If action misses, bonus remains queued
- More powerful - guaranteed application

### ATTACK Keyword
- Bonuses apply to the **next roll attempt**
- Consumed on roll (regardless of hit/miss)
- Less powerful - can be wasted on misses

### Implementation Flow

1. **Action Execution**: When an action with ACTION/ATTACK bonuses executes successfully, bonuses are added to CharacterEffects queues
2. **ATTACK Bonuses**: Applied before roll calculation, consumed immediately
3. **ACTION Bonuses**: Applied after successful action execution, only consumed on success
4. **Bonus Types**: Supports ACCURACY, HIT, COMBO, CRIT, and stat bonuses (STR, AGI, TECH, INT)

## Usage

### Parsing Spreadsheet

```csharp
// Parse CSV file
var spreadsheetActions = SpreadsheetActionParser.ParseCsvFile("path/to/spreadsheet.csv");

// Convert to ActionData
var actionDataList = new List<ActionData>();
foreach (var spreadsheet in spreadsheetActions)
{
    var actionData = SpreadsheetToActionDataConverter.Convert(spreadsheet);
    actionDataList.Add(actionData);
}

// Generate JSON
SpreadsheetActionJsonGenerator.ConvertAndGenerateJsonFile(spreadsheetActions, "GameData/Actions.json");
```

### Spreadsheet Column Mapping

Key columns:
- **A**: ACTION (name)
- **B**: DESCRIPTION
- **J**: DURATION (count for keywords)
- **K**: CADENCE (keyword type: ACTION, ATTACK, ATTACKS, ACTIONS, etc.)
- **N-Q**: Hero bonuses (ACCURACY, HIT, COMBO, CRIT)
- **V-Y**: Hero stats (STR, AGI, TECH, INT)

### JSON Output Format

Actions with ACTION/ATTACK bonuses will include:

```json
{
  "name": "CONCENTRATE",
  "actionAttackBonuses": {
    "bonusGroups": [
      {
        "keyword": "ATTACK",
        "count": 1,
        "bonuses": [
          { "type": "HIT", "value": 1 }
        ],
        "durationType": "ATTACK"
      }
    ]
  }
}
```

## Testing

The parser can be tested by:

1. Exporting Google Sheets to CSV format
2. Running the parser on the CSV
3. Validating the generated JSON structure
4. Loading actions in-game to verify functionality

## Next Steps

1. Export your Google Sheets to CSV
2. Run the parser to generate Actions.json
3. Test actions in-game to verify ACTION/ATTACK mechanics work correctly
4. Adjust spreadsheet data as needed and re-parse

## Files Created/Modified

### New Files
- `Code/Data/SpreadsheetActionData.cs`
- `Code/Data/ActionAttackBonus.cs`
- `Code/Data/SpreadsheetActionParser.cs`
- `Code/Data/ActionAttackKeywordProcessor.cs`
- `Code/Data/SpreadsheetToActionDataConverter.cs`
- `Code/Data/SpreadsheetActionJsonGenerator.cs`
- `Scripts/parse-spreadsheet-actions.ps1`

### Modified Files
- `Code/Data/ActionLoader.cs` - Added ACTION/ATTACK support
- `Code/Actions/Action.cs` - Added ActionAttackBonuses property
- `Code/Entity/CharacterEffects.cs` - Added tracking and consumption methods
- `Code/Actions/Execution/ActionExecutionFlow.cs` - Integrated bonus application

## Notes

- ACTION bonuses are more powerful than ATTACK bonuses
- Multiple bonuses can stack
- Bonuses are tracked per-character
- Consumption logic follows the keyword mechanics guide exactly
