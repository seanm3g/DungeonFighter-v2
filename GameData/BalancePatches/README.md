# Balance Patches Directory

This directory contains shareable balance configuration patches.

## What are Balance Patches?

Balance patches are complete game balance configurations that can be:
- **Saved** from your current tuning
- **Shared** with others (copy the JSON file)
- **Imported** from others
- **Loaded** to try different balance configurations

## How to Use

### Creating a Patch

1. Tune your game balance using the Balance Tuning Console
2. Test your balance with matchup analysis
3. Save as a patch with a descriptive name
4. Export the patch to share with others

### Sharing Patches

1. Export your patch to a folder
2. Share the JSON file (email, Discord, GitHub, etc.)
3. Others can import it into their `BalancePatches/` folder

### Importing Patches

1. Place the patch JSON file in this directory
2. Or use the import function to copy it here automatically
3. Load the patch in-game to try the balance

## Patch File Format

Each patch is a JSON file containing:
- **Metadata**: Name, author, description, version, tags
- **Tuning Config**: Complete game balance configuration

## File Naming

Patches are automatically named with format:
`{name}_v{version}_{date}.json`

Example: `aggressive_enemies_v1.2_20250115.json`

## Notes

- Patches are validated before loading to prevent errors
- Game version compatibility is checked (optional)
- You can have multiple patches and switch between them
- Patches don't modify your base `TuningConfig.json` until you apply them

