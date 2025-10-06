@echo off
title Dungeon Fighter v2 - Build Distribution
echo.
echo ========================================
echo    Dungeon Fighter v2 - Build Script
echo ========================================
echo.

REM Get current date in MM-DD-YYYY format
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "datestamp=%MM%-%DD%-%YYYY%"

echo Building distribution for %datestamp%...
echo.

REM Step 1: Build the project
echo [1/5] Building project...
dotnet build Code\Code.csproj --configuration Release --verbosity quiet
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo ✓ Build successful

REM Step 2: Clean and recreate distribution folder
echo [2/5] Preparing distribution folder...
if exist "DungeonFighter-Distribution" rmdir /s /q "DungeonFighter-Distribution"
mkdir "DungeonFighter-Distribution"
mkdir "DungeonFighter-Distribution\GameData"
echo ✓ Distribution folder prepared

REM Step 3: Copy executable files
echo [3/5] Copying executable files...
copy "Code\bin\Release\net8.0\*" "DungeonFighter-Distribution\" >nul
echo ✓ Executable files copied

REM Step 4: Copy game data
echo [4/5] Copying game data...
copy "GameData\*" "DungeonFighter-Distribution\GameData\" >nul
echo ✓ Game data copied

REM Step 5: Create launcher and documentation
echo [5/5] Creating launcher and documentation...

REM Create the launcher batch file
(
echo @echo off
echo title Dungeon Fighter v2
echo echo.
echo echo ========================================
echo echo    Dungeon Fighter v2 - Console RPG
echo echo ========================================
echo echo.
echo echo Use your keyboard to navigate menus and make choices.
echo echo.
echo pause
echo echo.
echo Code.exe
echo echo.
echo echo Game has ended. Press any key to close this window.
echo pause ^>nul
) > "DungeonFighter-Distribution\DUNGEON FIGHTER.bat"

REM Create README
(
echo # Dungeon Fighter v2 - Console RPG
echo.
echo ## Quick Start
echo.
echo 1. **Extract** this zip file to any folder on your computer
echo 2. **Double-click** `DUNGEON FIGHTER.bat` to start the game
echo 3. **Enjoy** your dungeon crawling adventure!
echo.
echo ## System Requirements
echo.
echo - **Windows 10/11** ^(64-bit^)
echo - **.NET 8.0 Runtime** ^(automatically installed on most modern Windows systems^)
echo - **No additional software required**
echo.
echo ## Game Description
echo.
echo Dungeon Fighter v2 is a sophisticated turn-based RPG/dungeon crawler featuring:
echo.
echo ### Core Features
echo - **Advanced Combat System**: Turn-based combat with combo mechanics and dice-based resolution
echo - **Character Progression**: Level up your character with 4 different classes ^(Barbarian, Warrior, Rogue, Wizard^)
echo - **Equipment System**: Weapons, armor, and loot with tier-based stats and special abilities
echo - **18+ Enemy Types**: Each with unique stats, abilities, and specializations
echo - **10 Themed Dungeons**: Forest, Lava, Crypt, Cavern, Swamp, Desert, Ice, Ruins, Castle, Graveyard
echo - **Procedural Generation**: 1-3 rooms per dungeon with unique environmental effects
echo - **Battle Narrative System**: Event-driven narrative with poetic descriptions for significant moments
echo.
echo ### Combat Mechanics
echo - **Action Combo System**: Chain actions together for increased damage ^(1.85x multiplier per combo step^)
echo - **Dice-based Resolution**: 1d20 roll system with thresholds ^(1-5 fail, 6-15 normal, 16-20 combo trigger^)
echo - **Environmental Actions**: Room-specific effects that impact combat
echo - **Intelligent AI**: Enemies with specialized attributes and scaling difficulty
echo.
echo ### Character Classes
echo - **Barbarian**: Strength-focused warrior with high damage output
echo - **Warrior**: Balanced fighter with good defense and offense
echo - **Rogue**: Agility-focused character with quick, precise attacks
echo - **Wizard**: Intelligence-based magic user with powerful spells
echo.
echo ## How to Play
echo.
echo 1. **Launch the game** using `DUNGEON FIGHTER.bat`
echo 2. **Create a character** or load an existing save
echo 3. **Select a dungeon** to explore
echo 4. **Navigate through rooms** using the menu system
echo 5. **Fight enemies** using your equipped weapons and armor
echo 6. **Manage your inventory** and equipment between battles
echo 7. **Level up** and gain new abilities as you progress
echo.
echo ## Controls
echo.
echo - **Arrow Keys**: Navigate menus
echo - **Enter**: Select/confirm choices
echo - **Numbers**: Quick selection in menus
echo - **Any Key**: Continue through text and prompts
echo.
echo ## Game Files
echo.
echo - `Code.exe` - Main game executable
echo - `GameData/` - Contains all game content ^(enemies, items, dungeons, etc.^)
echo - `DUNGEON FIGHTER.bat` - Launcher script
echo - `README.md` - This file
echo.
echo ## Troubleshooting
echo.
echo ### Game won't start
echo - Make sure you have .NET 8.0 Runtime installed
echo - Try running `Code.exe` directly instead of `DUNGEON FIGHTER.bat`
echo - Check that all files are in the same folder
echo.
echo ### Performance issues
echo - The game is optimized for console output and should run smoothly on any modern computer
echo - If you experience delays, this is intentional pacing for the narrative system
echo.
echo ### Save files
echo - Character saves are stored as `character_save.json` in the game folder
echo - You can backup this file to preserve your progress
echo.
echo ## Technical Details
echo.
echo - **Engine**: C# .NET 8.0 Console Application
echo - **Architecture**: Data-driven design with JSON configuration files
echo - **Testing**: Comprehensive test suite with 14+ test categories
echo - **Cross-platform**: Core systems designed for portability
echo.
echo ## Support
echo.
echo This is a standalone game that requires no internet connection or additional downloads. All game data is included in the distribution package.
echo.
echo ---
echo.
echo **Enjoy your dungeon crawling adventure!**
) > "DungeonFighter-Distribution\README.md"

echo ✓ Launcher and documentation created

REM Step 6: Create zip file with date
echo.
echo Creating zip file...
set "zipname=DUNGEON FIGHTER - %datestamp%.zip"
if exist "%zipname%" del "%zipname%"

powershell -command "Compress-Archive -Path 'DungeonFighter-Distribution\*' -DestinationPath '%zipname%' -Force"
if %errorlevel% neq 0 (
    echo ERROR: Failed to create zip file!
    pause
    exit /b 1
)

echo ✓ Zip file created: %zipname%

REM Step 7: Cleanup
echo.
echo Cleaning up temporary files...
rmdir /s /q "DungeonFighter-Distribution"
echo ✓ Cleanup complete

echo.
echo ========================================
echo    BUILD COMPLETE!
echo ========================================
echo.
echo Distribution package created: %zipname%
echo.
echo The zip file contains:
echo - Complete game executable
echo - All game data files
echo - DUNGEON FIGHTER.bat launcher
echo - README with instructions
echo.
echo Ready for distribution!
echo.
pause
