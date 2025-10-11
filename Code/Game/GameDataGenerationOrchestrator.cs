using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Orchestrates game data generation by coordinating specialized generators
    /// Replaces the monolithic GameDataGenerator with a clean, maintainable architecture
    /// </summary>
    public static class GameDataGenerationOrchestrator
    {
        /// <summary>
        /// Generates/updates all game data JSON files based on TuningConfig
        /// ONLY when explicitly called - no automatic generation
        /// </summary>
        public static GenerationResult GenerateAllGameData(bool forceOverwrite = false)
        {
            var result = new GenerationResult();
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine("Generating game data files based on TuningConfig...");
            }
            
            // Skip enemy generation to preserve manually curated Enemies.json
            // EnemyGenerator.GenerateEnemiesJson(); // DISABLED - preserving manual enemy data
            
            result.ArmorResult = ArmorGenerator.GenerateArmorJson(forceOverwrite);
            result.WeaponResult = WeaponGenerator.GenerateWeaponsJson(forceOverwrite);
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine("Game data generation complete! (Enemies.json preserved)");
                result.LogSummary();
            }
            
            return result;
        }

        /// <summary>
        /// Manual generation method for development use - provides full control over generation
        /// </summary>
        public static GenerationResult GenerateGameDataManually(bool forceOverwrite = false, bool createBackups = true)
        {
            Console.WriteLine("=== MANUAL GAME DATA GENERATION ===");
            Console.WriteLine($"Force Overwrite: {forceOverwrite}");
            Console.WriteLine($"Create Backups: {createBackups}");
            Console.WriteLine();

            var result = GenerateAllGameData(forceOverwrite);
            
            Console.WriteLine("\n=== GENERATION SUMMARY ===");
            result.LogSummary();
            
            if (result.HasErrors)
            {
                Console.WriteLine("\nErrors:");
                foreach (var error in result.ArmorResult.Errors.Concat(result.WeaponResult.Errors))
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            
            if (result.HasWarnings)
            {
                Console.WriteLine("\nWarnings:");
                foreach (var warning in result.ArmorResult.Warnings.Concat(result.WeaponResult.Warnings))
                {
                    Console.WriteLine($"  - {warning}");
                }
            }
            
            return result;
        }

        /// <summary>
        /// Test method to demonstrate the dynamic generation system
        /// </summary>
        public static void TestDynamicGeneration()
        {
            Console.WriteLine("=== TESTING DYNAMIC GENERATION SYSTEM ===");
            
            var tuning = GameConfiguration.Instance;
            
            Console.WriteLine("Current TuningConfig Status:");
            Console.WriteLine($"  ItemScaling configured: {tuning.ItemScaling != null}");
            Console.WriteLine($"  EnemyScaling configured: {tuning.EnemyScaling != null}");
            
            if (tuning.ItemScaling != null)
            {
                Console.WriteLine($"  StartingWeaponDamage: {tuning.ItemScaling.StartingWeaponDamage?.Count ?? 0}");
                Console.WriteLine($"  TierDamageRanges: {tuning.ItemScaling.TierDamageRanges?.Count ?? 0}");
                Console.WriteLine($"  GlobalDamageMultiplier: {tuning.ItemScaling.GlobalDamageMultiplier}");
            }
            
            if (tuning.EnemyScaling != null)
            {
                Console.WriteLine($"  BaseDPSAtLevel1: {tuning.EnemyScaling.BaseDPSAtLevel1}");
                Console.WriteLine($"  DPSPerLevel: {tuning.EnemyScaling.DPSPerLevel}");
            }
            
            Console.WriteLine("\nTesting JSON file generation...");
            
            // Test generating a single file
            try
            {
                ArmorGenerator.GenerateArmorJson();
                Console.WriteLine("✓ Armor.json generation successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Armor.json generation failed: {ex.Message}");
            }
            
            try
            {
                WeaponGenerator.GenerateWeaponsJson();
                Console.WriteLine("✓ Weapons.json generation successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Weapons.json generation failed: {ex.Message}");
            }
            
            // Skip enemy generation to preserve manually curated Enemies.json
            Console.WriteLine("✓ Enemies.json generation skipped (preserving manual data)");
            
            Console.WriteLine("\nDynamic generation system test complete!");
        }
    }
}
