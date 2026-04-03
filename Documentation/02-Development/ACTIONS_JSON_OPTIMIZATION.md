# Actions.json File Optimization

## Problem

The `Actions.json` file was extremely large (7,482 lines) with massive redundancy:
- **85 actions** in the file
- **88 fields** per action
- Most fields were **empty strings** (`""`)
- Each action object contained ~88 lines of mostly empty properties

This resulted in:
- Large file size (difficult to read and edit)
- Slow loading times
- Unnecessary disk I/O
- Poor developer experience

## Solution

Created a custom JSON converter (`SpreadsheetActionJsonConverter`) that:
1. **Omits empty string properties** when serializing
2. **Maintains backward compatibility** by reading both old and new formats
3. **Automatically fills missing properties** with empty strings during deserialization

## Implementation

### Files Modified

1. **`Code/Data/SpreadsheetActionJsonConverter.cs`** (NEW)
   - Custom `JsonConverter<SpreadsheetActionJson>` implementation
   - `Write()` method only serializes non-empty string properties
   - `Read()` method handles missing properties gracefully

2. **`Code/Data/SpreadsheetActionJson.cs`**
   - Added `[JsonConverter(typeof(SpreadsheetActionJsonConverter))]` attribute
   - Enables automatic use of the custom converter

3. **`Code/Data/SpreadsheetActionJsonGenerator.cs`**
   - Updated comments to document the optimization
   - Removed redundant `DefaultIgnoreCondition` (handled by converter)

## Benefits

### File Size Reduction
- **Before**: ~7,482 lines (all 88 fields per action)
- **After**: Only non-empty fields are written
- **Expected reduction**: 70-90% smaller file size (depending on how many fields are actually used)

### Performance Improvements
- Faster JSON parsing (less data to process)
- Reduced memory usage during loading
- Faster file I/O operations

### Developer Experience
- Much easier to read and edit the file
- Only relevant data is visible
- Easier to spot actual action properties

## Backward Compatibility

The converter maintains full backward compatibility:
- **Reading old format**: Missing properties are filled with empty strings
- **Reading new format**: Works seamlessly with optimized JSON
- **No code changes required**: Existing code continues to work

## Usage

The optimization is **automatic** - no code changes needed:

1. **When generating Actions.json** from spreadsheets:
   - The `SpreadsheetActionJsonGenerator` automatically uses the converter
   - Output will be optimized (empty fields omitted)

2. **When loading Actions.json**:
   - The `ActionLoader` automatically handles both formats
   - Missing properties are filled with empty strings

3. **To regenerate Actions.json** with optimization:
   - Run your existing spreadsheet import scripts
   - The new format will be automatically applied

## Example

### Before (Old Format)
```json
{
  "action": "JAB",
  "description": "RESET ENEMY ACTION CHAIN",
  "columnC": "",
  "rarity": "WEAPON",
  "category": "66%",
  "dps": "66%",
  "numberOfHits": "1",
  "damage": "33%",
  "speed": "0.50",
  "duration": "",
  "cadence": "",
  "opener": "",
  "finisher": "",
  "heroAccuracy": "",
  "heroHit": "",
  // ... 74 more empty fields ...
  "tags": ""
}
```

### After (Optimized Format)
```json
{
  "action": "JAB",
  "description": "RESET ENEMY ACTION CHAIN",
  "rarity": "WEAPON",
  "category": "66%",
  "dps": "66%",
  "numberOfHits": "1",
  "damage": "33%",
  "speed": "0.50"
}
```

Only fields with actual values are included!

## Technical Details

### How It Works

1. **Serialization (Write)**:
   - Custom converter checks each property
   - Only writes properties with non-empty string values
   - Uses `Utf8JsonWriter` for efficient JSON generation

2. **Deserialization (Read)**:
   - Reads JSON document
   - Extracts each property if present
   - Defaults to empty string if property is missing
   - Ensures `SpreadsheetActionJson` object is fully populated

### Performance Considerations

- **Serialization**: Slightly slower (checks each property), but writes less data
- **Deserialization**: Slightly slower (checks each property), but reads less data
- **Net benefit**: Overall faster due to reduced I/O and parsing

## Future Improvements

Potential further optimizations:
1. **Compression**: Use gzip compression for even smaller files
2. **Binary format**: Consider binary serialization for production builds
3. **Field grouping**: Group related fields into nested objects
4. **Default value templates**: Use templates for common action patterns

## Testing

To verify the optimization works:

1. **Load existing Actions.json**: Should work without issues
2. **Generate new Actions.json**: Should be much smaller
3. **Round-trip test**: Load optimized JSON, save it, verify it's still optimized
4. **Compare file sizes**: Old vs new format

## Notes

- The `ActionEditor` class uses the legacy `ActionData` format, which is separate from this optimization
- This optimization only affects the `SpreadsheetActionJson` format
- Both formats can coexist - `ActionLoader` detects and handles both
