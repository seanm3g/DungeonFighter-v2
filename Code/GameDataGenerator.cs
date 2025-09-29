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
            if (TuningConfig.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine("Generating game data files based on TuningConfig...");
            }
            
            GenerateEnemiesJson();
            GenerateArmorJson();
            GenerateWeaponsJson();
            
            if (TuningConfig.Instance.GameData.ShowGenerationMessages)
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
            
            var tuning = TuningConfig.Instance;
            
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
        /// Generates Enemies.json based on EnemyScaling configurations
        /// </summary>
        public static void GenerateEnemiesJson()
        {
            var tuning = TuningConfig.Instance;
            var enemyScaling = tuning.EnemyScaling;
            
            if (enemyScaling == null)
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: EnemyScaling configuration not found, using existing Enemies.json");
                }
                return;
            }

            // Load existing enemy data to preserve names, actions, and other properties
            var existingEnemies = LoadExistingEnemies();
            var generatedEnemies = new List<EnemyData>();

            foreach (var existingEnemy in existingEnemies)
            {
                var generatedEnemy = GenerateEnemyFromConfig(existingEnemy, enemyScaling);
                generatedEnemies.Add(generatedEnemy);
            }

            // Write updated enemies to JSON
            string json = JsonSerializer.Serialize(generatedEnemies, _jsonOptions);
            string filePath = GetGameDataFilePath("Enemies.json");
            File.WriteAllText(filePath, json);
            
            if (TuningConfig.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Generated {generatedEnemies.Count} enemies in Enemies.json");
            }
        }

        /// <summary>
        /// Generates Armor.json based on ItemScaling configurations
        /// </summary>
        public static void GenerateArmorJson()
        {
            var tuning = TuningConfig.Instance;
            var itemScaling = tuning.ItemScaling;
            
            if (itemScaling == null)
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: ItemScaling configuration not found, using existing Armor.json");
                }
                return;
            }

            // Load existing armor data to preserve names and other properties
            var existingArmor = LoadExistingArmor();
            
            if (TuningConfig.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Loaded {existingArmor?.Count ?? 0} existing armor pieces");
            }
            
            // If no valid armor data exists, skip generation entirely - DO NOT OVERWRITE FILE
            if (existingArmor == null || existingArmor.Count == 0)
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("No existing armor data found, skipping armor generation to preserve file");
                }
                return;
            }

            var generatedArmor = new List<ArmorData>();
            int validEntries = 0;
            int skippedEntries = 0;

            foreach (var existing in existingArmor)
            {
                // Skip empty/corrupted entries completely - don't include them in output
                if (string.IsNullOrEmpty(existing.Slot) || string.IsNullOrEmpty(existing.Name) || existing.Tier <= 0)
                {
                    skippedEntries++;
                    if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                    {
                        Console.WriteLine($"Skipping invalid armor entry: Slot='{existing.Slot}', Name='{existing.Name}', Tier={existing.Tier}");
                    }
                    continue; // Skip this entry entirely
                }

                validEntries++;
                var generated = GenerateArmorFromConfig(existing, itemScaling);
                generatedArmor.Add(generated);
                
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine($"Processed: {existing.Name} (Tier {existing.Tier}) - Armor: {existing.Armor} -> {generated.Armor}");
                }
            }

            // Only write if we have valid entries to process - NEVER write empty data
            if (validEntries > 0)
            {
                // Write updated armor to JSON
                string json = JsonSerializer.Serialize(generatedArmor, _jsonOptions);
                string filePath = GetGameDataFilePath("Armor.json");
                File.WriteAllText(filePath, json);
                
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine($"Updated armor values for {generatedArmor.Count} armor pieces in Armor.json");
                    Console.WriteLine($"Skipped {skippedEntries} invalid entries");
                }
            }
            else
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
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
            var tuning = TuningConfig.Instance;
            var itemScaling = tuning.ItemScaling;
            
            if (itemScaling == null)
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
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
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
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
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
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
            
            if (validWeapons.Count != existingWeapons.Count && TuningConfig.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Filtered out {existingWeapons.Count - validWeapons.Count} corrupted weapon entries");
            }

            // Write updated weapons to JSON
            string json = JsonSerializer.Serialize(generatedWeapons, _jsonOptions);
            string filePath = GetGameDataFilePath("Weapons.json");
            File.WriteAllText(filePath, json);
            
            if (TuningConfig.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Generated {generatedWeapons.Count} weapons in Weapons.json");
            }
        }

        private static EnemyData GenerateEnemyFromConfig(EnemyData existing, EnemyScalingConfig enemyScaling)
        {
            // Preserve all identity and behavior properties
            // Reset to proper base values first (prevent attribute multiplication on already high values)
            var baseStats = GetBaseEnemyStats(existing.Name, existing.PrimaryAttribute);
            
            var generated = new EnemyData
            {
                Name = existing.Name,                    // Keep original name
                BaseLevel = existing.BaseLevel,          // Keep original level
                PrimaryAttribute = existing.PrimaryAttribute, // Keep original primary attribute
                Actions = existing.Actions,              // Keep original actions
                IsLiving = existing.IsLiving,            // Keep original living status
                
                // These will be recalculated based on TuningConfig
                BaseHealth = existing.BaseHealth,
                BaseStats = new EnemyStats
                {
                    Strength = baseStats.Strength,
                    Agility = baseStats.Agility,
                    Technique = baseStats.Technique,
                    Intelligence = baseStats.Intelligence
                },
                BaseArmor = existing.BaseArmor
            };

            // Apply simplified enemy scaling based on EnemyScaling config
            // Calculate health based on level and health multiplier
            generated.BaseHealth = Math.Max(1, (int)Math.Round(existing.BaseHealth * enemyScaling.EnemyHealthMultiplier));
            
            // Apply damage scaling to stats
            double damageMultiplier = enemyScaling.EnemyDamageMultiplier;
            generated.BaseStats.Strength = Math.Max(1, (int)Math.Round(generated.BaseStats.Strength * damageMultiplier));
            generated.BaseStats.Agility = Math.Max(1, (int)Math.Round(generated.BaseStats.Agility * damageMultiplier));
            generated.BaseStats.Technique = Math.Max(1, (int)Math.Round(generated.BaseStats.Technique * damageMultiplier));
            generated.BaseStats.Intelligence = Math.Max(1, (int)Math.Round(generated.BaseStats.Intelligence * damageMultiplier));
            
            // Apply primary attribute bonus
            switch (existing.PrimaryAttribute.ToLower())
            {
                case "strength":
                    generated.BaseStats.Strength = (int)Math.Round(generated.BaseStats.Strength * 1.2);
                    break;
                case "agility":
                    generated.BaseStats.Agility = (int)Math.Round(generated.BaseStats.Agility * 1.2);
                    break;
                case "technique":
                    generated.BaseStats.Technique = (int)Math.Round(generated.BaseStats.Technique * 1.2);
                    break;
                case "intelligence":
                    generated.BaseStats.Intelligence = (int)Math.Round(generated.BaseStats.Intelligence * 1.2);
                    break;
            }
            
            // Simple armor scaling based on health
            double armorRatio = 10.0; // 10:1 health to armor ratio
            generated.BaseArmor = Math.Max(0, (int)Math.Round(generated.BaseHealth / armorRatio));

            return generated;
        }

        private static string DetermineEnemyArchetype(EnemyData enemy)
        {
            // Simple archetype determination based on primary attribute and stats
            // This can be enhanced with more sophisticated logic
            switch (enemy.PrimaryAttribute.ToLower())
            {
                case "strength":
                    if (enemy.BaseStats.Agility < enemy.BaseStats.Strength * 0.7)
                        return "Juggernaut";
                    else if (enemy.BaseStats.Agility > enemy.BaseStats.Strength * 1.2)
                        return "Berserker";
                    else
                        return "Brute";
                        
                case "agility":
                    if (enemy.BaseStats.Strength > enemy.BaseStats.Agility * 1.2)
                        return "Assassin";
                    else
                        return "Berserker";
                        
                case "technique":
                    return "Warrior";
                    
                case "intelligence":
                    return "Warrior"; // Default for intelligence-based enemies
                    
                default:
                    return "Warrior";
            }
        }

        private static ArmorData GenerateArmorFromConfig(ArmorData existing, ItemScalingConfig itemScaling)
        {
            // Create a copy preserving all original values exactly
            var generated = new ArmorData
            {
                Slot = existing.Slot,
                Name = existing.Name,
                Tier = existing.Tier,
                Armor = existing.Armor // Start with original armor value
            };

            // Apply simplified armor scaling based on tier
            double armorPerTier = itemScaling.ArmorValuePerTier;
            double tierMultiplier = 1.0 + (existing.Tier - 1) * 0.2; // 20% increase per tier
            generated.Armor = Math.Max(1, (int)Math.Round(existing.Armor * tierMultiplier));

            return generated;
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

        private static EnemyStats GetBaseEnemyStats(string enemyName, string primaryAttribute)
        {
            // Determine archetype based on enemy name and primary attribute
            string archetype = DetermineEnemyArchetype(new EnemyData 
            { 
                Name = enemyName, 
                PrimaryAttribute = primaryAttribute 
            });
            
            // Get archetype ratios (hardcoded for now until TuningConfig classes are properly set up)
            var archetypeRatios = GetArchetypeRatios(archetype);
            
            // Base attribute pool (total points to distribute)
            int baseAttributePool = 10; // Total attribute points to distribute
            
            // Apply archetype ratios to distribute the pool
            var baseStats = DistributeAttributePool(baseAttributePool, archetypeRatios);
            
            return baseStats;
        }

        private static EnemyStats DistributeAttributePool(int totalPool, Dictionary<string, double> archetypeRatios)
        {
            // Calculate total ratio weight
            double totalRatio = archetypeRatios.Values.Sum();
            
            // Distribute pool based on ratios
            var stats = new EnemyStats();
            
            // Get ratios for each attribute (default to 1.0 if not specified)
            double strengthRatio = archetypeRatios.GetValueOrDefault("Strength", 1.0);
            double agilityRatio = archetypeRatios.GetValueOrDefault("Agility", 1.0);
            double techniqueRatio = archetypeRatios.GetValueOrDefault("Technique", 1.0);
            double intelligenceRatio = archetypeRatios.GetValueOrDefault("Intelligence", 1.0);
            
            // Calculate attribute values
            stats.Strength = Math.Max(1, (int)Math.Round((strengthRatio / totalRatio) * totalPool));
            stats.Agility = Math.Max(1, (int)Math.Round((agilityRatio / totalRatio) * totalPool));
            stats.Technique = Math.Max(1, (int)Math.Round((techniqueRatio / totalRatio) * totalPool));
            stats.Intelligence = Math.Max(1, (int)Math.Round((intelligenceRatio / totalRatio) * totalPool));
            
            // Ensure minimum of 1 for each attribute
            stats.Strength = Math.Max(1, stats.Strength);
            stats.Agility = Math.Max(1, stats.Agility);
            stats.Technique = Math.Max(1, stats.Technique);
            stats.Intelligence = Math.Max(1, stats.Intelligence);
            
            return stats;
        }

        private static Dictionary<string, double> GetArchetypeRatios(string archetype)
        {
            // Hardcoded archetype ratios for now (can be moved to TuningConfig later)
            switch (archetype.ToLower())
            {
                case "berserker":
                    return new Dictionary<string, double>
                    {
                        { "Strength", 0.6 },
                        { "Agility", 2.0 },
                        { "Technique", 1.0 },
                        { "Intelligence", 1.0 }
                    };
                case "assassin":
                    return new Dictionary<string, double>
                    {
                        { "Strength", 0.8 },
                        { "Agility", 1.5 },
                        { "Technique", 1.3 },
                        { "Intelligence", 1.0 }
                    };
                case "warrior":
                    return new Dictionary<string, double>
                    {
                        { "Strength", 1.2 },
                        { "Agility", 1.0 },
                        { "Technique", 1.0 },
                        { "Intelligence", 1.0 }
                    };
                case "brute":
                    return new Dictionary<string, double>
                    {
                        { "Strength", 2.0 },
                        { "Agility", 0.6 },
                        { "Technique", 1.0 },
                        { "Intelligence", 1.0 }
                    };
                case "juggernaut":
                    return new Dictionary<string, double>
                    {
                        { "Strength", 2.5 },
                        { "Agility", 0.4 },
                        { "Technique", 1.2 },
                        { "Intelligence", 1.0 }
                    };
                case "mage":
                    return new Dictionary<string, double>
                    {
                        { "Strength", 0.5 },
                        { "Agility", 0.5 },
                        { "Technique", 1.0 },
                        { "Intelligence", 2.0 }
                    };
                default:
                    return new Dictionary<string, double>
                    {
                        { "Strength", 1.0 },
                        { "Agility", 1.0 },
                        { "Technique", 1.0 },
                        { "Intelligence", 1.0 }
                    };
            }
        }
    }

    // Note: EnemyData and EnemyStats classes are defined in EnemyLoader.cs
}
