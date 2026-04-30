using System.IO;

namespace RPGGame
{
    /// <summary>
    /// Centralized constants for the game
    /// Eliminates duplication of hardcoded strings and paths across the codebase
    /// </summary>
    public static class GameConstants
    {
        // File and Directory Constants
        public const string GameDataDirectory = "GameData";
        public const string SaveDirectory = "Saves";
        
        // JSON File Names
        public const string ActionsJson = "Actions.json";
        public const string EnemiesJson = "Enemies.json";
        public const string ArmorJson = "Armor.json";
        public const string WeaponsJson = "Weapons.json";
        public const string FlavorTextJson = "FlavorText.json";
        public const string TuningConfigJson = "TuningConfig.json";
        public const string GameSettingsJson = "GameSettings.json";
        public const string StatBonusesJson = "StatBonuses.json";
        public const string ModificationsJson = "Modifications.json";
        /// <summary>Quality / Material / Adjective prefix rows merged into loot with <see cref="ModificationsJson"/> at runtime.</summary>
        public const string PrefixMaterialQualityJson = "PrefixMaterialQuality.json";
        public const string TierDistributionJson = "TierDistribution.json";
        public const string DungeonConfigJson = "DungeonConfig.json";
        public const string RoomsJson = "Rooms.json";
        public const string DungeonsJson = "Dungeons.json";
        /// <summary>CLASS ACTIONS sheet pull → class tier / path → combo action unlocks.</summary>
        public const string ClassActionsJson = "ClassActions.json";
        public const string CharacterSaveJson = "character_save.json";
        /// <summary>Tombstone save for legacy single-slot flow when the hero dies (not loadable).</summary>
        public const string CharacterSaveDeadJson = "character_save_dead.json";
        
        // Common File Paths
        public static readonly string[] PossibleGameDataPaths = {
            // Relative to current working directory (most common case)
            Path.Combine(Directory.GetCurrentDirectory(), "..", GameDataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), GameDataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", GameDataDirectory),
            
            // Relative to executable directory
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GameDataDirectory),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", GameDataDirectory),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", GameDataDirectory),
            
            // Project root GameData (preferred: Code\bin\Debug\net8.0 -> project root)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", GameDataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", GameDataDirectory),
            // Common project structure variations
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", GameDataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", GameDataDirectory),
            
            // Legacy paths for backward compatibility
            GameDataDirectory,
            Path.Combine("..", GameDataDirectory),
            Path.Combine("..", "..", GameDataDirectory)
        };
        
        // UI Constants - Now configurable via TuningConfig.UICustomization
        public static string MenuSeparator => GameConfiguration.Instance.UICustomization.MenuSeparator;
        public static string SubMenuSeparator => GameConfiguration.Instance.UICustomization.SubMenuSeparator;
        public static string InvalidChoiceMessage => GameConfiguration.Instance.UICustomization.InvalidChoiceMessage;
        public static string PressAnyKeyMessage => GameConfiguration.Instance.UICustomization.PressAnyKeyMessage;
        
        // Separator Constants - Standardized separator lines for consistent UI formatting
        /// <summary>
        /// Standard length for separator lines (matches TuningConfig.json menuSeparator length)
        /// </summary>
        public const int StandardSeparatorLength = 42;
        
        /// <summary>
        /// Generates a separator line of the standard length using the specified character
        /// </summary>
        /// <param name="character">The character to use for the separator (default: '=')</param>
        /// <returns>A string of StandardSeparatorLength characters</returns>
        public static string GetSeparator(char character = '=')
        {
            return new string(character, StandardSeparatorLength);
        }
        
        /// <summary>
        /// Generates a separator line of the standard length using equals signs
        /// This is the primary separator used throughout the game UI
        /// </summary>
        public static string StandardSeparator => GetSeparator('=');
        
        /// <summary>
        /// Generates a separator line of the standard length using dashes
        /// Used for sub-menu separators
        /// </summary>
        public static string StandardDashSeparator => GetSeparator('-');
        
        // Game Constants
        public const int DefaultLevel = 1;
        public const int DefaultHealth = 100;
        public const int DefaultMana = 50;
        public const int MaxLevel = 100;
        public const int MinTier = 1;
        public const int MaxTier = 5;

        /// <summary>
        /// Dungeon selection list row that prompts for an arbitrary dungeon level (must match dungeon regeneration).
        /// </summary>
        public const string DungeonCustomLevelMenuName = "Custom difficulty";
        
        // Combat Constants
        public const double DefaultNarrativeBalance = 0.5;
        public const double DefaultCombatSpeed = 0.5;
        public const double MinNarrativeBalance = 0.0;
        public const double MaxNarrativeBalance = 1.0;
        public const double MinCombatSpeed = 0.1;
        public const double MaxCombatSpeed = 2.0;
        
        // Item Constants - Now configurable via TuningConfig.UICustomization
        public static string BasicGearPrefix => "Basic";
        public static string LegendaryPrefix => GameConfiguration.Instance.UICustomization.RarityPrefixes.Legendary;
        public static string EpicPrefix => GameConfiguration.Instance.UICustomization.RarityPrefixes.Epic;
        public static string RarePrefix => GameConfiguration.Instance.UICustomization.RarityPrefixes.Rare;
        public static string UncommonPrefix => GameConfiguration.Instance.UICustomization.RarityPrefixes.Uncommon;
        public static string CommonPrefix => GameConfiguration.Instance.UICustomization.RarityPrefixes.Common;
        
        // Action Constants - Now configurable via TuningConfig.UICustomization
        public static string BasicAttackName => GameConfiguration.Instance.UICustomization.ActionNames.BasicAttackName;
        public static string DefaultActionDescription => GameConfiguration.Instance.UICustomization.ActionNames.DefaultActionDescription;
        
        // Error Messages - Now configurable via TuningConfig.UICustomization
        public static string FileNotFoundError => GameConfiguration.Instance.UICustomization.ErrorMessages.FileNotFoundError;
        public static string JsonDeserializationError => GameConfiguration.Instance.UICustomization.ErrorMessages.JsonDeserializationError;
        public static string InvalidDataError => GameConfiguration.Instance.UICustomization.ErrorMessages.InvalidDataError;
        public static string SaveError => GameConfiguration.Instance.UICustomization.ErrorMessages.SaveError;
        public static string LoadError => GameConfiguration.Instance.UICustomization.ErrorMessages.LoadError;
        
        // Debug Messages - Now configurable via TuningConfig.UICustomization
        public static string DebugPrefix => GameConfiguration.Instance.UICustomization.DebugMessages.DebugPrefix;
        public static string WarningPrefix => GameConfiguration.Instance.UICustomization.DebugMessages.WarningPrefix;
        public static string ErrorPrefix => GameConfiguration.Instance.UICustomization.DebugMessages.ErrorPrefix;
        public static string InfoPrefix => GameConfiguration.Instance.UICustomization.DebugMessages.InfoPrefix;
        
        // Character Constants - Now configurable via TuningConfig.DungeonGeneration
        public static string DefaultCharacterName => GameConfiguration.Instance.DungeonGeneration.DefaultCharacterName;
        public const int DefaultStrength = 10;
        public const int DefaultAgility = 10;
        public const int DefaultTechnique = 10;
        public const int DefaultIntelligence = 10;
        /// <summary>
        /// Effective INT at or above this value follows the combo strip order (ComboStep).
        /// Below this value, combo-slot actions are chosen at random among the strip.
        /// </summary>
        public const int ComboSequenceIntelligenceThreshold = 10;
        
        // Dungeon Constants - Now configurable via TuningConfig.DungeonGeneration
        public static string DefaultDungeonTheme => GameConfiguration.Instance.DungeonGeneration.DefaultTheme;
        public static string DefaultRoomType => GameConfiguration.Instance.DungeonGeneration.DefaultRoomType;
        public static int DefaultDungeonLevels => GameConfiguration.Instance.DungeonGeneration.DefaultDungeonLevels;
        public static int DefaultRoomCount => GameConfiguration.Instance.DungeonGeneration.DefaultRoomCount;
        
        // Equipment Slots - Now configurable via TuningConfig.DungeonGeneration
        public static string HeadSlot => GameConfiguration.Instance.DungeonGeneration.EquipmentSlots[0];
        public static string ChestSlot => GameConfiguration.Instance.DungeonGeneration.EquipmentSlots[1];
        public static string FeetSlot => GameConfiguration.Instance.DungeonGeneration.EquipmentSlots[2];
        public static string WeaponSlot => GameConfiguration.Instance.DungeonGeneration.EquipmentSlots[3];
        
        // Status Effects - Now configurable via TuningConfig.DungeonGeneration
        public static string BleedEffect => GameConfiguration.Instance.DungeonGeneration.StatusEffectTypes[0];
        public static string PoisonEffect => GameConfiguration.Instance.DungeonGeneration.StatusEffectTypes[1];
        public static string BurnEffect => GameConfiguration.Instance.DungeonGeneration.StatusEffectTypes[2];
        public static string SlowEffect => GameConfiguration.Instance.DungeonGeneration.StatusEffectTypes[3];
        public static string WeakenEffect => GameConfiguration.Instance.DungeonGeneration.StatusEffectTypes[4];
        public static string StunEffect => GameConfiguration.Instance.DungeonGeneration.StatusEffectTypes[5];
        
        // Utility Methods
        /// <summary>
        /// Gets the project-root GameData directory when running from the repo (e.g. Code\bin\Debug\net8.0).
        /// Used for settings so load and save always use the same physical folder regardless of launch (VS vs batch).
        /// </summary>
        /// <returns>Full path to project root GameData if it exists; otherwise null.</returns>
        public static string? GetSettingsDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // From Code\bin\Debug\net8.0, four levels up then GameData = project root GameData
            string projectRootGameData = Path.Combine(baseDir, "..", "..", "..", "..", GameDataDirectory);
            try
            {
                string fullPath = Path.GetFullPath(projectRootGameData);
                if (Directory.Exists(fullPath))
                    return fullPath;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Gets a file path for a JSON file in the GameData directory
        /// Uses robust path resolution to find the correct GameData directory
        /// </summary>
        /// <param name="fileName">The JSON file name</param>
        /// <returns>The full path to the file</returns>
        public static string GetGameDataFilePath(string fileName)
        {
            // Prefer project-root GameData first (same folder for dotnet run / terminal)
            string? projectRoot = GetSettingsDirectory();
            if (projectRoot != null)
                return Path.Combine(projectRoot, fileName);
            // Then try to find an existing GameData directory
            string? existingGameDataDir = FindGameDataDirectory();
            if (existingGameDataDir != null)
            {
                return Path.Combine(existingGameDataDir, fileName);
            }

            // If no existing GameData directory found, create one in the most appropriate location
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            
            // Prioritize the root GameData directory (project root level)
            string[] preferredLocations = {
                // Try to find/create GameData at project root level first
                Path.Combine(currentDir, "..", GameDataDirectory),
                Path.Combine(executableDir, "..", GameDataDirectory),
                Path.Combine(currentDir, "..", "..", GameDataDirectory),
                Path.Combine(executableDir, "..", "..", GameDataDirectory),
                // Then try other locations
                Path.Combine(executableDir, GameDataDirectory),
                Path.Combine(currentDir, GameDataDirectory),
                Path.Combine(GameDataDirectory)
            };

            foreach (string location in preferredLocations)
            {
                try
                {
                    // Ensure the directory exists
                    Directory.CreateDirectory(location);
                    return Path.Combine(location, fileName);
                }
                catch
                {
                    // Continue to next location if this one fails
                    continue;
                }
            }

            // Fallback to current directory
            return Path.Combine(GameDataDirectory, fileName);
        }
        
        /// <summary>
        /// Finds an existing GameData directory by checking common locations
        /// Uses case-insensitive matching to handle different directory naming conventions
        /// </summary>
        /// <returns>The path to an existing GameData directory, or null if none found</returns>
        private static string? FindGameDataDirectory()
        {
            // Prefer project-root GameData so all consumers use the same folder
            string? projectRoot = GetSettingsDirectory();
            if (projectRoot != null)
                return projectRoot;
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            
            // Check all possible GameData directory locations
            foreach (string possiblePath in PossibleGameDataPaths)
            {
                if (Directory.Exists(possiblePath))
                {
                    return possiblePath;
                }
            }
            
            // If no exact match found, try case-insensitive search
            // This handles cases where the directory might be "Code" vs "code"
            string[] searchPaths = {
                Path.Combine(currentDir, "..", "GameData"),
                Path.Combine(executableDir, "..", "GameData"),
                Path.Combine(currentDir, "..", "..", "GameData"),
                Path.Combine(executableDir, "..", "..", "GameData"),
                Path.Combine(executableDir, "GameData"),
                Path.Combine(currentDir, "GameData")
            };
            
            foreach (string searchPath in searchPaths)
            {
                try
                {
                    // Get the parent directory to search in
                    string parentDir = Path.GetDirectoryName(searchPath) ?? "";
                    string targetDirName = Path.GetFileName(searchPath);
                    
                    if (Directory.Exists(parentDir))
                    {
                        // Look for directories with case-insensitive matching
                        string[] subdirs = Directory.GetDirectories(parentDir);
                        foreach (string subdir in subdirs)
                        {
                            string dirName = Path.GetFileName(subdir);
                            if (string.Equals(dirName, targetDirName, StringComparison.OrdinalIgnoreCase))
                            {
                                return subdir;
                            }
                        }
                    }
                }
                catch
                {
                    // Continue searching if this path fails
                    continue;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets a file path for a save file
        /// </summary>
        /// <param name="fileName">The save file name</param>
        /// <returns>The full path to the file</returns>
        public static string GetSaveFilePath(string fileName)
        {
            return Path.Combine(SaveDirectory, fileName);
        }
        
        /// <summary>
        /// Returns the first absolute path where <paramref name="fileName"/> exists under a candidate GameData folder
        /// (project root, cwd, executable-relative, etc.). Use for optional files like SheetsPushConfig.json so pushes work
        /// when <see cref="GetGameDataFilePath"/> would otherwise target a newly created empty GameData folder.
        /// </summary>
        public static string? TryGetExistingGameDataFilePath(string fileName)
        {
            foreach (var p in GetPossibleGameDataFilePaths(fileName))
            {
                try
                {
                    string full = Path.GetFullPath(p);
                    if (File.Exists(full))
                        return full;
                }
                catch
                {
                    // ignore invalid path combinations
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all possible paths for a file in the GameData directory
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>Array of possible file paths</returns>
        public static string[] GetPossibleGameDataFilePaths(string fileName)
        {
            // Prefer project-root GameData first so JsonLoader and others use the same folder
            string? projectRoot = GetSettingsDirectory();
            if (projectRoot != null)
            {
                var paths = new string[PossibleGameDataPaths.Length + 1];
                paths[0] = Path.Combine(projectRoot, fileName);
                for (int i = 0; i < PossibleGameDataPaths.Length; i++)
                    paths[i + 1] = Path.Combine(PossibleGameDataPaths[i], fileName);
                return paths;
            }
            var pathsOnly = new string[PossibleGameDataPaths.Length];
            for (int i = 0; i < PossibleGameDataPaths.Length; i++)
                pathsOnly[i] = Path.Combine(PossibleGameDataPaths[i], fileName);
            return pathsOnly;
        }
    }
}
