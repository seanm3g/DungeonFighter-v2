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
        public const string TierDistributionJson = "TierDistribution.json";
        public const string DungeonConfigJson = "DungeonConfig.json";
        public const string RoomsJson = "Rooms.json";
        public const string CharacterSaveJson = "character_save.json";
        
        // Common File Paths
        public static readonly string[] PossibleGameDataPaths = {
            // Relative to executable directory
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GameDataDirectory),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", GameDataDirectory),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", GameDataDirectory),
            
            // Relative to current working directory
            Path.Combine(Directory.GetCurrentDirectory(), GameDataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", GameDataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", GameDataDirectory),
            
            // Common project structure variations
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", GameDataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", GameDataDirectory),
            
            // Legacy paths for backward compatibility
            GameDataDirectory,
            Path.Combine("..", GameDataDirectory),
            Path.Combine("..", "..", GameDataDirectory),
            Path.Combine("DF4 - CONSOLE", GameDataDirectory),
            Path.Combine("..", "DF4 - CONSOLE", GameDataDirectory)
        };
        
        // UI Constants - Now configurable via TuningConfig.UICustomization
        public static string MenuSeparator => TuningConfig.Instance.UICustomization.MenuSeparator;
        public static string SubMenuSeparator => TuningConfig.Instance.UICustomization.SubMenuSeparator;
        public static string InvalidChoiceMessage => TuningConfig.Instance.UICustomization.InvalidChoiceMessage;
        public static string PressAnyKeyMessage => TuningConfig.Instance.UICustomization.PressAnyKeyMessage;
        
        // Game Constants
        public const int DefaultLevel = 1;
        public const int DefaultHealth = 100;
        public const int DefaultMana = 50;
        public const int MaxLevel = 100;
        public const int MinTier = 1;
        public const int MaxTier = 5;
        
        // Combat Constants
        public const double DefaultNarrativeBalance = 0.5;
        public const double DefaultCombatSpeed = 0.5;
        public const double MinNarrativeBalance = 0.0;
        public const double MaxNarrativeBalance = 1.0;
        public const double MinCombatSpeed = 0.1;
        public const double MaxCombatSpeed = 2.0;
        
        // Item Constants - Now configurable via TuningConfig.UICustomization
        public static string BasicGearPrefix => "Basic";
        public static string LegendaryPrefix => TuningConfig.Instance.UICustomization.RarityPrefixes.Legendary;
        public static string EpicPrefix => TuningConfig.Instance.UICustomization.RarityPrefixes.Epic;
        public static string RarePrefix => TuningConfig.Instance.UICustomization.RarityPrefixes.Rare;
        public static string UncommonPrefix => TuningConfig.Instance.UICustomization.RarityPrefixes.Uncommon;
        public static string CommonPrefix => TuningConfig.Instance.UICustomization.RarityPrefixes.Common;
        
        // Action Constants - Now configurable via TuningConfig.UICustomization
        public static string BasicAttackName => TuningConfig.Instance.UICustomization.ActionNames.BasicAttackName;
        public static string DefaultActionDescription => TuningConfig.Instance.UICustomization.ActionNames.DefaultActionDescription;
        
        // Error Messages - Now configurable via TuningConfig.UICustomization
        public static string FileNotFoundError => TuningConfig.Instance.UICustomization.ErrorMessages.FileNotFoundError;
        public static string JsonDeserializationError => TuningConfig.Instance.UICustomization.ErrorMessages.JsonDeserializationError;
        public static string InvalidDataError => TuningConfig.Instance.UICustomization.ErrorMessages.InvalidDataError;
        public static string SaveError => TuningConfig.Instance.UICustomization.ErrorMessages.SaveError;
        public static string LoadError => TuningConfig.Instance.UICustomization.ErrorMessages.LoadError;
        
        // Debug Messages - Now configurable via TuningConfig.UICustomization
        public static string DebugPrefix => TuningConfig.Instance.UICustomization.DebugMessages.DebugPrefix;
        public static string WarningPrefix => TuningConfig.Instance.UICustomization.DebugMessages.WarningPrefix;
        public static string ErrorPrefix => TuningConfig.Instance.UICustomization.DebugMessages.ErrorPrefix;
        public static string InfoPrefix => TuningConfig.Instance.UICustomization.DebugMessages.InfoPrefix;
        
        // Character Constants - Now configurable via TuningConfig.DungeonGeneration
        public static string DefaultCharacterName => TuningConfig.Instance.DungeonGeneration.DefaultCharacterName;
        public const int DefaultStrength = 10;
        public const int DefaultAgility = 10;
        public const int DefaultTechnique = 10;
        public const int DefaultIntelligence = 10;
        
        // Dungeon Constants - Now configurable via TuningConfig.DungeonGeneration
        public static string DefaultDungeonTheme => TuningConfig.Instance.DungeonGeneration.DefaultTheme;
        public static string DefaultRoomType => TuningConfig.Instance.DungeonGeneration.DefaultRoomType;
        public static int DefaultDungeonLevels => TuningConfig.Instance.DungeonGeneration.DefaultDungeonLevels;
        public static int DefaultRoomCount => TuningConfig.Instance.DungeonGeneration.DefaultRoomCount;
        
        // Equipment Slots - Now configurable via TuningConfig.DungeonGeneration
        public static string HeadSlot => TuningConfig.Instance.DungeonGeneration.EquipmentSlots[0];
        public static string ChestSlot => TuningConfig.Instance.DungeonGeneration.EquipmentSlots[1];
        public static string FeetSlot => TuningConfig.Instance.DungeonGeneration.EquipmentSlots[2];
        public static string WeaponSlot => TuningConfig.Instance.DungeonGeneration.EquipmentSlots[3];
        
        // Status Effects - Now configurable via TuningConfig.DungeonGeneration
        public static string BleedEffect => TuningConfig.Instance.DungeonGeneration.StatusEffectTypes[0];
        public static string PoisonEffect => TuningConfig.Instance.DungeonGeneration.StatusEffectTypes[1];
        public static string BurnEffect => TuningConfig.Instance.DungeonGeneration.StatusEffectTypes[2];
        public static string SlowEffect => TuningConfig.Instance.DungeonGeneration.StatusEffectTypes[3];
        public static string WeakenEffect => TuningConfig.Instance.DungeonGeneration.StatusEffectTypes[4];
        public static string StunEffect => TuningConfig.Instance.DungeonGeneration.StatusEffectTypes[5];
        
        // Utility Methods
        /// <summary>
        /// Gets a file path for a JSON file in the GameData directory
        /// </summary>
        /// <param name="fileName">The JSON file name</param>
        /// <returns>The full path to the file</returns>
        public static string GetGameDataFilePath(string fileName)
        {
            return Path.Combine(GameDataDirectory, fileName);
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
        /// Gets all possible paths for a file in the GameData directory
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>Array of possible file paths</returns>
        public static string[] GetPossibleGameDataFilePaths(string fileName)
        {
            var paths = new string[PossibleGameDataPaths.Length];
            for (int i = 0; i < PossibleGameDataPaths.Length; i++)
            {
                paths[i] = Path.Combine(PossibleGameDataPaths[i], fileName);
            }
            return paths;
        }
    }
}
