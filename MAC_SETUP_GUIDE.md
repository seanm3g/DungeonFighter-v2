# Running DungeonFighter-v2 on macOS

This guide provides the best methods to run the game on Mac.

## Prerequisites

1. **.NET 8.0 SDK** - Required to build and run the game
2. **macOS Terminal** - For running commands

## Method 1: Using the Mac Launcher Script (Easiest)

The project includes a ready-to-use launcher script that handles everything automatically.

### Steps:

1. **Open Terminal** and navigate to the project directory:
   ```bash
   cd "/path/to/DungeonFighter-v2"
   ```

2. **Make the script executable** (first time only):
   ```bash
   chmod +x "Dungeon Fighter(Mac).sh"
   ```

3. **Run the launcher**:
   ```bash
   ./"Dungeon Fighter(Mac).sh"
   ```

The script will:
- Check for .NET 8.0 SDK and install it if needed (via Homebrew or official installer)
- Build the game automatically
- Launch the game in the background

### Troubleshooting the Launcher:

If the launcher script doesn't work, try Method 2 below.

## Method 2: Manual Build and Run (Most Reliable)

This method gives you more control and better error messages.

### Step 1: Install .NET 8.0 SDK

**Option A: Using Homebrew (Recommended)**
```bash
brew install --cask dotnet-sdk
```

**Option B: Official Installer**
1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download the macOS installer
3. Run the installer

**Verify installation:**
```bash
dotnet --version
```
Should show version 8.0.x or higher.

### Step 2: Navigate to Project Directory
```bash
cd "/path/to/DungeonFighter-v2"
```

### Step 3: Restore Dependencies (First Time)
```bash
dotnet restore Code/Code.csproj
```

### Step 4: Build the Game
```bash
dotnet build Code/Code.csproj --configuration Debug
```

### Step 5: Run the Game
```bash
dotnet run --project Code/Code.csproj
```

The game window should open automatically.

## Method 3: Create a Self-Contained Executable (For Distribution)

If you want a standalone app that doesn't require .NET to be installed:

```bash
cd Code
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

This creates an executable at:
```
Code/bin/Release/net8.0/osx-x64/publish/DF
```

You can then run it directly:
```bash
./Code/bin/Release/net8.0/osx-x64/publish/DF
```

## Quick Reference Commands

### Build only:
```bash
dotnet build Code/Code.csproj
```

### Run directly:
```bash
dotnet run --project Code/Code.csproj
```

### Clean build artifacts:
```bash
dotnet clean Code/Code.csproj
```

### Run with specific mode:
```bash
# Run in PLAY mode (interactive)
dotnet run --project Code/Code.csproj -- PLAY

# Run in DEMO mode (automated)
dotnet run --project Code/Code.csproj -- DEMO

# Run in TUNING mode
dotnet run --project Code/Code.csproj -- TUNING
```

## Common Issues and Solutions

### Issue: "dotnet: command not found"
**Solution:** .NET SDK is not installed or not in PATH
- Install using Homebrew: `brew install --cask dotnet-sdk`
- Or download from: https://dotnet.microsoft.com/download/dotnet/8.0
- After installation, restart Terminal

### Issue: "Permission denied" when running script
**Solution:** Make the script executable
```bash
chmod +x "Dungeon Fighter(Mac).sh"
```

### Issue: Build fails with errors
**Solution:** 
1. Clean and rebuild:
   ```bash
   dotnet clean Code/Code.csproj
   dotnet restore Code/Code.csproj
   dotnet build Code/Code.csproj
   ```

2. Check for missing dependencies:
   ```bash
   dotnet restore Code/Code.csproj
   ```

### Issue: Game window doesn't appear
**Solution:**
- Check if the process is running: `ps aux | grep DF`
- Try running in foreground to see errors:
  ```bash
  dotnet run --project Code/Code.csproj
  ```
- Check Console.app for error messages

### Issue: "Avalonia" or UI-related errors
**Solution:**
- Ensure you're running on macOS 10.15 (Catalina) or later
- Avalonia requires modern macOS versions
- Check that you have proper graphics drivers

## File Locations

- **Game Data**: `GameData/` folder in project root
- **Save Files**: `GameData/character_save.json`
- **Settings**: `GameData/gamesettings.json`
- **Build Output**: `Code/bin/Debug/net8.0/` or `Code/bin/Release/net8.0/`

## Running Tests

To run the test suite:
```bash
dotnet test Code/Code.csproj
```

Or access tests through the game's Settings menu → Tests

## Additional Notes

- The game uses **Avalonia UI** framework, which is cross-platform
- All game data is stored in JSON files in the `GameData/` folder
- Save files are automatically created in `GameData/character_save.json`
- The game supports multiple run modes (GUI, PLAY, DEMO, TUNING, MCP)

## Getting Help

If you encounter issues:
1. Check the build output for specific error messages
2. Verify .NET 8.0 SDK is installed: `dotnet --version`
3. Try a clean build: `dotnet clean` then `dotnet build`
4. Check the Documentation folder for more detailed guides

