using System.Text.Json;

namespace RPGGame
{
    public static class GameDataGenerator
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Generates/updates all game data JSON files based on TuningConfig
        /// </summary>
        public static void GenerateAllGameData()
        {
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine("Generating game data files based on TuningConfig...");
            }
            
            GenerateEnemiesJson();
            GenerateArmorJson();
            GenerateWeaponsJson();
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine("Game data generation complete!");
            }
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
                GenerateArmorJson();
                Console.WriteLine("✓ Armor.json generation successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Armor.json generation failed: {ex.Message}");
            }
            
            try
            {
                GenerateWeaponsJson();
                Console.WriteLine("✓ Weapons.json generation successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Weapons.json generation failed: {ex.Message}");
            }
            
            try
            {
                GenerateEnemiesJson();
                Console.WriteLine("✓ Enemies.json generation successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Enemies.json generation failed: {ex.Message}");
            }
            
            Console.WriteLine("\nDynamic generation system test complete!");
        }

        /// <summary>
        /// Generates Enemies.json based on base enemy configurations in TuningConfig
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

            // Generate enemies from base configurations in TuningConfig
            var generatedEnemies = new List<EnemyData>();

            foreach (var baseEnemyConfig in enemyBalance.BaseEnemyConfigs)
            {
                try
                {
                    // Create a minimal existing enemy data structure for compatibility
                    var existingEnemy = new EnemyData
                    {
                        Name = baseEnemyConfig.Key,
                        Archetype = "Berserker", // Default archetype
                        Actions = baseEnemyConfig.Value.Actions,
                        IsLiving = baseEnemyConfig.Value.IsLiving
                    };
                    
                    var generatedEnemy = GenerateEnemyFromConfig(existingEnemy, enemyScaling);
                    generatedEnemies.Add(generatedEnemy);
                }
                catch (Exception ex)
                {
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        Console.WriteLine($"ERROR: Failed to generate enemy {baseEnemyConfig.Key}: {ex.Message}");
                    }
                }
            }

            // Write updated enemies to JSON
            string json = JsonSerializer.Serialize(generatedEnemies, _jsonOptions);
            string filePath = GetGameDataFilePath("Enemies.json");
            File.WriteAllText(filePath, json);
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Generated {generatedEnemies.Count} enemies in Enemies.json from TuningConfig base configurations");
            }
        }

        /// <summary>
        /// Generates Armor.json based on ItemScaling configurations
        /// </summary>
        public static void GenerateArmorJson()
        {
            var tuning = GameConfiguration.Instance;
            var itemScaling = tuning.ItemScaling;
            
            if (itemScaling == null)
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: ItemScaling configuration not found, using existing Armor.json");
                }
                return;
            }

            // Load existing armor data to preserve names and other properties
            var existingArmor = LoadExistingArmor();
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Loaded {existingArmor?.Count ?? 0} existing armor pieces");
            }
            
            // If no valid armor data exists, skip generation entirely - DO NOT OVERWRITE FILE
            if (existingArmor == null || existingArmor.Count == 0)
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("No existing armor data found, skipping armor generation to preserve file");
                }
                return;
            }

            var generatedArmor = new List<ArmorData>();
            int validEntries = 0;
            int skippedEntries = 0;
            int changedEntries = 0;

            foreach (var existing in existingArmor)
            {
                // Skip empty/corrupted entries completely - don't include them in output
                if (string.IsNullOrEmpty(existing.Slot) || string.IsNullOrEmpty(existing.Name) || existing.Tier <= 0)
                {
                    skippedEntries++;
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        Console.WriteLine($"Skipping invalid armor entry: Slot='{existing.Slot}', Name='{existing.Name}', Tier={existing.Tier}");
                    }
                    continue; // Skip this entry entirely
                }

                validEntries++;
                var generated = GenerateArmorFromConfig(existing, itemScaling);
                generatedArmor.Add(generated);
                
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    // Only show items that actually changed
                    if (existing.Armor != generated.Armor)
                    {
                        Console.WriteLine($"Updated: {existing.Name} (Tier {existing.Tier}) - Armor: {existing.Armor} -> {generated.Armor}");
                        changedEntries++;
                    }
                }
            }

            // Only write if we have valid entries to process - NEVER write empty data
            if (validEntries > 0)
            {
                // Write updated armor to JSON
                string json = JsonSerializer.Serialize(generatedArmor, _jsonOptions);
                string filePath = GetGameDataFilePath("Armor.json");
                File.WriteAllText(filePath, json);
                
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    // Only show summary if there were actual changes
                    if (changedEntries > 0)
                    {
                        Console.WriteLine($"Updated {changedEntries} armor pieces in Armor.json");
                    }
                    if (skippedEntries > 0)
                    {
                        Console.WriteLine($"Skipped {skippedEntries} invalid entries");
                    }
                }
            }
            else
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine($"No valid armor entries found (skipped {skippedEntries} invalid entries), skipping armor generation to preserve existing data");
                }
            }
        }

        /// <summary>
        /// Generates Weapons.json based on ItemScaling configurations
        /// </summary>
        public static void GenerateWeaponsJson()
        {
            var tuning = GameConfiguration.Instance;
            var itemScaling = tuning.ItemScaling;
            
            if (itemScaling == null)
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: ItemScaling configuration not found, using existing Weapons.json");
                }
                return;
            }

            // Load existing weapon data to preserve names and other properties
            var existingWeapons = LoadExistingWeapons();
            var generatedWeapons = new List<WeaponData>();

            // Safety check: if no weapons exist, don't overwrite the file
            if (existingWeapons.Count == 0)
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: No weapons found in Weapons.json, skipping generation to prevent data loss");
                }
                return;
            }

            // Filter out corrupted entries (empty type or name) and only process valid weapons
            var validWeapons = existingWeapons.Where(w => !string.IsNullOrEmpty(w.Type) && !string.IsNullOrEmpty(w.Name)).ToList();
            
            // Safety check: if all weapons are corrupted, don't overwrite the file
            if (validWeapons.Count == 0)
            {
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: All weapons are corrupted, skipping generation to prevent data loss");
                }
                return;
            }
            
            foreach (var existing in validWeapons)
            {
                var generated = GenerateWeaponFromConfig(existing, itemScaling);
                generatedWeapons.Add(generated);
            }
            
            if (validWeapons.Count != existingWeapons.Count && GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Filtered out {existingWeapons.Count - validWeapons.Count} corrupted weapon entries");
            }

            // Write updated weapons to JSON
            string json = JsonSerializer.Serialize(generatedWeapons, _jsonOptions);
            string filePath = GetGameDataFilePath("Weapons.json");
            File.WriteAllText(filePath, json);
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Generated {generatedWeapons.Count} weapons in Weapons.json");
            }
        }

        private static EnemyData GenerateEnemyFromConfig(EnemyData existing, EnemyScalingConfig enemyScaling)
        {
            // For the new archetype system, we just return the existing enemy data
            // The actual stat generation happens in EnemyLoader.cs using the archetype system
            return existing;
        }

        private static ArmorData GenerateArmorFromConfig(ArmorData existing, ItemScalingConfig itemScaling)
        {
            // Create a copy preserving all original values exactly
            var generated = new ArmorData
            {
                Slot = existing.Slot,
                Name = existing.Name,
                Tier = existing.Tier,
                Armor = 1 // Start with base value, not existing corrupted value
            };

            // Generate proper base armor values based on tier and slot
            int baseArmor = GetBaseArmorForTierAndSlot(existing.Tier, existing.Slot);
            generated.Armor = baseArmor;

            return generated;
        }

        private static int GetBaseArmorForTierAndSlot(int tier, string slot)
        {
            // Define base armor values by tier and slot (not amplifying existing values)
            var baseValues = new Dictionary<int, Dictionary<string, int>>
            {
                [1] = new Dictionary<string, int> { ["head"] = 2, ["chest"] = 4, ["feet"] = 2 },
                [2] = new Dictionary<string, int> { ["head"] = 4, ["chest"] = 8, ["feet"] = 4 },
                [3] = new Dictionary<string, int> { ["head"] = 6, ["chest"] = 12, ["feet"] = 6 },
                [4] = new Dictionary<string, int> { ["head"] = 8, ["chest"] = 16, ["feet"] = 8 },
                [5] = new Dictionary<string, int> { ["head"] = 10, ["chest"] = 20, ["feet"] = 10 }
            };

            if (baseValues.TryGetValue(tier, out var tierValues) && 
                tierValues.TryGetValue(slot, out int armor))
            {
                return armor;
            }

            // Fallback: simple tier-based calculation
            return Math.Max(1, tier * 2);
        }

        private static WeaponData GenerateWeaponFromConfig(WeaponData existing, ItemScalingConfig itemScaling)
        {
            // Skip weapons with empty type or name - these are corrupted entries
            if (string.IsNullOrEmpty(existing.Type) || string.IsNullOrEmpty(existing.Name))
            {
                return existing; // Return as-is, don't process corrupted entries
            }

            var generated = new WeaponData
            {
                Type = existing.Type,        // Always preserve original type
                Name = existing.Name,        // Always preserve original name  
                BaseDamage = existing.BaseDamage,
                AttackSpeed = existing.AttackSpeed,
                Tier = existing.Tier         // Always preserve original tier
            };

            // Apply simplified weapon scaling based on tier and global multiplier
            double weaponDamagePerTier = itemScaling.WeaponDamagePerTier;
            double tierMultiplier = 1.0 + (existing.Tier - 1) * 0.3; // 30% increase per tier
            double globalMultiplier = itemScaling.GlobalDamageMultiplier;
            
            generated.BaseDamage = Math.Max(1, (int)Math.Round(existing.BaseDamage * tierMultiplier * globalMultiplier));
            
            // Apply speed bonus per tier
            double speedBonusPerTier = itemScaling.SpeedBonusPerTier;
            generated.AttackSpeed = Math.Max(0.1, existing.AttackSpeed + (existing.Tier - 1) * speedBonusPerTier);

            return generated;
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

        private static List<ArmorData> LoadExistingArmor()
        {
            string? filePath = JsonLoader.FindGameDataFile("Armor.json");
            if (filePath != null)
            {
                return JsonLoader.LoadJsonList<ArmorData>(filePath);
            }
            return new List<ArmorData>();
        }

        private static List<WeaponData> LoadExistingWeapons()
        {
            string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
            if (filePath != null)
            {
                return JsonLoader.LoadJsonList<WeaponData>(filePath);
            }
            return new List<WeaponData>();
        }

        private static string GetGameDataFilePath(string fileName)
        {
            // First try to find an existing GameData directory
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
                Path.Combine(currentDir, "..", "GameData"),
                Path.Combine(executableDir, "..", "GameData"),
                Path.Combine(currentDir, "..", "..", "GameData"),
                Path.Combine(executableDir, "..", "..", "GameData"),
                // Then try other locations
                Path.Combine(executableDir, "GameData"),
                Path.Combine(currentDir, "GameData"),
                Path.Combine("GameData")
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
            return Path.Combine("GameData", fileName);
        }

        private static string? FindGameDataDirectory()
        {
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            
            string[] possibleGameDataDirs = {
                // Prioritize the root GameData directory (project root level)
                Path.Combine(currentDir, "..", "GameData"),
                Path.Combine(executableDir, "..", "GameData"),
                Path.Combine(currentDir, "..", "..", "GameData"),
                Path.Combine(executableDir, "..", "..", "GameData"),
                
                // Relative to executable directory
                Path.Combine(executableDir, "GameData"),
                Path.Combine(executableDir, "..", "..", "GameData"),
                
                // Relative to current working directory
                Path.Combine(currentDir, "GameData"),
                Path.Combine(currentDir, "..", "..", "GameData"),
                
                // Common project structure variations
                Path.Combine(executableDir, "..", "..", "..", "GameData"),
                Path.Combine(currentDir, "..", "..", "..", "GameData"),
                
                // Legacy paths for backward compatibility
                Path.Combine("GameData"),
                Path.Combine("..", "GameData"),
                Path.Combine("..", "..", "GameData"),
                Path.Combine("DF4 - CONSOLE", "GameData"),
                Path.Combine("..", "DF4 - CONSOLE", "GameData")
            };

            foreach (string dir in possibleGameDataDirs)
            {
                if (Directory.Exists(dir))
                {
                    return dir;
                }
            }

            return null;
        }
    }
}