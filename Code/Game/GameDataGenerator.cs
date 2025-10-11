using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Legacy wrapper for GameDataGenerator - delegates to the new orchestrator architecture
    /// This maintains backward compatibility while using the new decomposed system
    /// </summary>
    public static class GameDataGenerator
    {
        /// <summary>
        /// Generates/updates all game data JSON files based on TuningConfig
        /// ONLY when explicitly called - no automatic generation
        /// </summary>
        public static GenerationResult GenerateAllGameData(bool forceOverwrite = false)
        {
            return GameDataGenerationOrchestrator.GenerateAllGameData(forceOverwrite);
        }

        // Legacy result classes - now defined in GenerationResult.cs
        // These are kept for backward compatibility

        /// <summary>
        /// Manual generation method for development use - provides full control over generation
        /// </summary>
        public static GenerationResult GenerateGameDataManually(bool forceOverwrite = false, bool createBackups = true)
        {
            return GameDataGenerationOrchestrator.GenerateGameDataManually(forceOverwrite, createBackups);
        }

        /// <summary>
        /// Test method to demonstrate the dynamic generation system
        /// </summary>
        public static void TestDynamicGeneration()
        {
            GameDataGenerationOrchestrator.TestDynamicGeneration();
        }

        /// <summary>
        /// Applies scaling adjustments to existing enemies in Enemies.json based on TuningConfig archetype settings
        /// </summary>
        public static void GenerateEnemiesJson()
        {
            EnemyGenerator.GenerateEnemiesJson();
        }

        /// <summary>
        /// Generates Armor.json based on ItemScaling configurations with comprehensive safety checks
        /// </summary>
        public static FileGenerationResult GenerateArmorJson(bool forceOverwrite = false)
        {
            return ArmorGenerator.GenerateArmorJson(forceOverwrite);
        }

        /// <summary>
        /// Generates Weapons.json based on ItemScaling configurations with comprehensive safety checks
        /// </summary>
        public static FileGenerationResult GenerateWeaponsJson(bool forceOverwrite = false)
        {
            return WeaponGenerator.GenerateWeaponsJson(forceOverwrite);
        }

        // All private methods have been moved to specialized classes:
        // - EnemyGenerator.GenerateEnemyFromConfig()
        // - ArmorGenerator.GenerateArmorFromConfig() and GetBaseArmorForTierAndSlot()
        // - WeaponGenerator.GenerateWeaponFromConfig()
        // - FileManager.SafeWriteJsonFile() and GetGameDataFilePath()
    }
}