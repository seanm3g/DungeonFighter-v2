# Running DungeonFighter-v2 on Windows

This guide provides the best methods to run the game on Windows.

## Sharing the game with a friend

1. Send the **entire folder** (zip is fine). They must **Extract All** — do not double-click the `.bat` inside the zip window.
2. They double-click **`DungeonFighter-PC.bat`** in the extracted folder (preferred; no parentheses in the name). `Dungeon Fighter(PC).bat` still works and calls the same script.
3. The first run may download the .NET 8 SDK into `%USERPROFILE%\.dotnet`. That does **not** need administrator rights. Internet is required for that first install and for the first source build (NuGet).
4. If Windows SmartScreen appears, choose **More info** → **Run anyway**.
5. If you already published `dist\DF.exe` before zipping, the launcher skips the SDK/build and just starts that exe.

## Prerequisites

1. **.NET 8.0 SDK** - Installed automatically by the launcher into the user folder when missing
2. **Windows 10/11** - Any modern Windows version
3. **PowerShell** - Usually pre-installed on Windows

## Method 1: Using the Windows Launcher Script (Easiest)

The project includes a ready-to-use launcher script that handles everything automatically.

### Steps:

1. **Extract** the whole game folder if you received a zip
2. **Double-click** `DungeonFighter-PC.bat` in the project root directory (`Dungeon Fighter(PC).bat` also works)

The script will:
- Unblock files Windows marked as downloaded
- Check the installed SDK list for .NET 8.0, including `%USERPROFILE%\.dotnet`, and skip installation when it is already present
- Install .NET 8.0 SDK into the user folder only if needed (no administrator prompt)
- Build the game when an SDK is available, or launch an included `dist\DF.exe` / Debug `DF.exe` when that is enough
- Launch the game in the background

### Troubleshooting the Launcher:

**If the window closes immediately or says the folder is incomplete:**
- Extract the zip to Desktop or Downloads first
- Confirm you can see both a `Code` folder and a `GameData` folder next to the `.bat`

**If you see "ERROR: Failed to install .NET 8.0 SDK":**

**First, check if .NET 8.0 is already installed:**
- The launcher checks `dotnet --list-sdks` instead of only the default `dotnet --version`, so newer SDKs such as 9.x do not hide an installed 8.x SDK.
- It also checks `%USERPROFILE%\.dotnet` and common installation locations such as `C:\Program Files\dotnet`, and adds that path for the current launcher run when needed.
- If .NET is installed but not detected, restart your computer to refresh the PATH environment variable.

**If .NET is not installed, the automatic installation may fail for several reasons:**
- No internet connection
- Antivirus blocking the download
- Windows Package Manager (winget) not available (the launcher still tries the official user-folder installer first)

**Solutions:**

1. **If .NET is already installed** (most common for developers):
   - Restart your computer to refresh PATH
   - Or manually add `C:\Program Files\dotnet` or `%USERPROFILE%\.dotnet` to your PATH (see troubleshooting section)

2. **If .NET is not installed**:
   - Visit: https://dotnet.microsoft.com/download/dotnet/8.0
   - Download the ".NET 8.0 SDK" installer for Windows x64
   - Run the installer
   - **Restart your computer** (important - refreshes PATH)
   - Run `DungeonFighter-PC.bat` again

3. **Check your internet connection**:
   - The automatic installer and the first source build require internet access

If the launcher script doesn't work, try Method 2 below.

## Method 2: Manual Build and Run (Most Reliable)

This method gives you more control and better error messages.

### Step 1: Install .NET 8.0 SDK

**Option A: Using Windows Package Manager (winget) - Recommended**

Open PowerShell as Administrator and run:
```powershell
winget install Microsoft.DotNet.SDK.8
```

**Option B: Official Installer**

1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download the ".NET 8.0 SDK" installer for Windows x64
3. Run the installer
4. Follow the installation wizard

**Verify installation:**

Open a new Command Prompt or PowerShell and run:
```cmd
dotnet --version
```

Should show version `8.0.x` or higher.

### Step 2: Navigate to Project Directory

Open Command Prompt or PowerShell and navigate to the project:
```cmd
cd "D:\Code Projects\github projects\DungeonFighter-v2"
```

(Replace with your actual project path)

### Step 3: Restore Dependencies (First Time)

```cmd
dotnet restore Code\Code.csproj
```

### Step 4: Build the Game

```cmd
dotnet build Code\Code.csproj --configuration Debug
```

### Step 5: Run the Game

```cmd
dotnet run --project Code\Code.csproj
```

Or run the executable directly:
```cmd
Code\bin\Debug\net8.0\DF.exe
```

The game window should open automatically.

## Method 3: Create a Self-Contained Executable (For Distribution)

If you want a standalone app that doesn't require .NET to be installed on the friend's PC, publish once and zip the **whole repo folder** (so `GameData` stays next to the launcher):

```cmd
Scripts\df.bat run
```

That writes `dist\DF.exe`. Zip the extracted project (including `dist`, `GameData`, and `DungeonFighter-PC.bat`) and send that. Their launcher will start `dist\DF.exe` without installing an SDK.

Or publish manually:

```cmd
dotnet publish Code\Code.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

## Common Issues and Solutions

### Issue: "dotnet is not recognized as an internal or external command" OR "ERROR: Failed to install .NET 8.0 SDK" (but .NET is already installed)

**This usually means .NET 8.0 SDK is installed but not in your PATH environment variable.**

**Solutions:**

1. **Restart your computer** (easiest):
   - After installing .NET, Windows needs to refresh the PATH
   - Restart your computer, then try the launcher again

2. **Check if .NET is installed**:
   - Open File Explorer
   - Navigate to `C:\Program Files\dotnet` (or `C:\Program Files (x86)\dotnet`)
   - If you see `dotnet.exe` there, .NET is installed but not in PATH

3. **Manually add to PATH**:
   - Press `Win + X` and select "System"
   - Click "Advanced system settings"
   - Click "Environment Variables"
   - Under "System variables", find "Path" and click "Edit"
   - Click "New" and add: `C:\Program Files\dotnet`
   - Click OK on all dialogs
   - **Restart your computer** or close and reopen all command prompts

4. **Verify it works**:
   - Open a NEW Command Prompt or PowerShell
   - Run: `dotnet --version`
   - Should show `8.0.x` or higher

### Issue: "Build failed" or compilation errors

**Solution:**
- Ensure you have .NET 8.0 SDK installed (not just Runtime)
- Try a clean build:
  ```cmd
  dotnet clean Code\Code.csproj
  dotnet restore Code\Code.csproj
  dotnet build Code\Code.csproj --configuration Debug
  ```

### Issue: "Avalonia" or UI-related errors

**Solution:**
- Ensure you're running on Windows 7 or later
- Check that you have proper graphics drivers installed
- Try running as administrator

### Issue: "Game failed to start" or executable not found

**Solution:**
- The build may have failed silently
- Check that `Code\bin\Debug\net8.0\DF.exe` exists
- Try building manually using Method 2

### Issue: Installation script fails with permission errors

**Solution:**
- You do **not** need administrator rights. The launcher installs to `%USERPROFILE%\.dotnet`
- If that folder already has `dotnet.exe`, restart the launcher so it can pick it up
- Manual install: https://dotnet.microsoft.com/download/dotnet/8.0

### Issue: Windows Package Manager (winget) not found

**Solution:**
- winget is optional. The launcher installs with Microsoft's user-folder script first
- If that fails, use the official installer (Method 2, Option B)

## File Locations

- **Game Data**: `GameData/` folder in project root
- **Save Files**: `GameData/character_save.json`
- **Settings**: `GameData/gamesettings.json`
- **Build Output**: `Code/bin/Debug/net8.0/` or `Code/bin/Release/net8.0/`

## Running Tests

To run the test suite:
```cmd
dotnet test Code\Code.csproj
```

Or access tests through the game's Settings menu → Tests

## Additional Notes

- The game uses **Avalonia UI** framework, which is cross-platform
- All game data is stored in JSON files in the `GameData/` folder
- Save files are automatically created in `GameData/character_save.json`
- The game supports multiple run modes (GUI, PLAY, DEMO, TUNING, MCP)

## Getting Help

If you encounter issues:

1. **Check the build output** for specific error messages
2. **Verify .NET 8.0 SDK** is installed: `dotnet --version` (should show 8.0.x)
3. **Try a clean build**: `dotnet clean` then `dotnet build`
4. **Check the Documentation folder** for more detailed guides
5. **Review error messages** - they usually indicate what's wrong

## Quick Troubleshooting Checklist

- [ ] Whole folder extracted (you can see `Code` and `GameData` next to the `.bat`)?
- [ ] Double-clicked `DungeonFighter-PC.bat` from the extracted folder (not from inside the zip)?
- [ ] Internet connection available? (first SDK install and first source build)
- [ ] If SmartScreen appears: More info → Run anyway
- [ ] Antivirus blocking? (may need to whitelist the project folder)
