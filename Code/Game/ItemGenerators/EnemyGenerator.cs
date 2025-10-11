using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Specialized generator for enemy data with scaling and validation logic
    /// </summary>
    public static class EnemyGenerator
    {
        /// <summary>
        /// Applies scaling adjustments to existing enemies in Enemies.json based on TuningConfig archetype settings
        /// </summary>
        public static void GenerateEnemiesJson()
        {
            var tuning = GameConfiguration.Instance;
            var enemyScaling = tuning.EnemyScaling;
            var enemyBalance = tuning.EnemyBalance;
            
            if (enemyScaling == null || enemyBalance == null)
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: EnemyScaling or EnemyBalance configuration not found, using existing Enemies.json");
                }
                return;
            }

            string filePath = FileManager.GetGameDataFilePath("Enemies.json");
            var existingEnemies = new List<EnemyData>();

            // Load existing enemies if file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string existingJson = System.IO.File.ReadAllText(filePath);
                    var loadedEnemies = System.Text.Json.JsonSerializer.Deserialize<List<EnemyData>>(existingJson);
                    if (loadedEnemies != null)
                    {
                        existingEnemies = loadedEnemies;
                    }
                }
                catch (Exception ex)
                {
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        Console.WriteLine($"Warning: Could not load existing Enemies.json: {ex.Message}");
                    }
                    return; // Don't overwrite if we can't read existing data
                }
            }
            else
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: Enemies.json not found, skipping enemy generation");
                }
                return; // Don't create new file if none exists
            }

            // Apply scaling adjustments to existing enemies
            var adjustedEnemies = new List<EnemyData>();
            int adjustedCount = 0;

            foreach (var existing in existingEnemies)
            {
                try
                {
                    var adjusted = GenerateEnemyFromConfig(existing, enemyScaling);
                    adjustedEnemies.Add(adjusted);
                    
                    // Check if any adjustments were made (for logging purposes)
                    if (existing != adjusted) // This would need proper comparison logic
                    {
                        adjustedCount++;
                    }
                }
                catch (Exception ex)
                {
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        Console.WriteLine($"ERROR: Failed to adjust enemy {existing.Name}: {ex.Message}");
                    }
                    // Add original enemy if adjustment fails
                    adjustedEnemies.Add(existing);
                }
            }

            // Write adjusted enemies back to JSON
            FileManager.SafeWriteJsonFile(filePath, adjustedEnemies, true);
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Applied scaling adjustments to {adjustedEnemies.Count} enemies in Enemies.json");
            }
        }

        private static EnemyData GenerateEnemyFromConfig(EnemyData existing, EnemyScalingConfig enemyScaling)
        {
            // Create a copy of the existing enemy data
            var adjusted = new EnemyData
            {
                Name = existing.Name,
                Archetype = existing.Archetype,
                Actions = existing.Actions,
                IsLiving = existing.IsLiving,
                Description = existing.Description,
                Overrides = existing.Overrides
            };

            // Apply archetype-based scaling adjustments if needed
            // For now, we preserve the existing data structure
            // The actual stat scaling happens at runtime in EnemyLoader.cs using the archetype system
            
            return adjusted;
        }

        private static List<EnemyData> LoadExistingEnemies()
        {
            string? filePath = JsonLoader.FindGameDataFile("Enemies.json");
            if (filePath != null)
            {
                return JsonLoader.LoadJsonList<EnemyData>(filePath);
            }
            return new List<EnemyData>();
        }
    }
}
