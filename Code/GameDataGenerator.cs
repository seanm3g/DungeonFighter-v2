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
            Console.WriteLine($"  SustainBalance configured: {tuning.EnemyDPS?.SustainBalance != null}");
            
            if (tuning.ItemScaling != null)
            {
                Console.WriteLine($"  WeaponTypes: {tuning.ItemScaling.WeaponTypes?.Count ?? 0}");
                Console.WriteLine($"  ArmorTypes: {tuning.ItemScaling.ArmorTypes?.Count ?? 0}");
                Console.WriteLine($"  RarityModifiers: {tuning.ItemScaling.RarityModifiers?.Count ?? 0}");
            }
            
            if (tuning.EnemyDPS?.SustainBalance != null)
            {
                var sustain = tuning.EnemyDPS.SustainBalance;
                Console.WriteLine($"  TargetActionsToKill: {sustain.TargetActionsToKill != null}");
                Console.WriteLine($"  DPSToSustainRatio: {sustain.DPSToSustainRatio != null}");
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
        /// Generates Enemies.json based on SustainBalance configurations
        /// </summary>
        public static void GenerateEnemiesJson()
        {
            var tuning = TuningConfig.Instance;
            var sustainBalance = tuning.EnemyDPS?.SustainBalance;
            
            if (sustainBalance == null)
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: SustainBalance configuration not found, using existing Enemies.json");
                }
                return;
            }

            // Load existing enemy data to preserve names, actions, and other properties
            var existingEnemies = LoadExistingEnemies();
            var generatedEnemies = new List<EnemyData>();

            foreach (var existingEnemy in existingEnemies)
            {
                var generatedEnemy = GenerateEnemyFromConfig(existingEnemy, sustainBalance);
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
            
            if (itemScaling?.ArmorTypes == null)
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: ItemScaling.ArmorTypes configuration not found, using existing Armor.json");
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
            
            if (itemScaling?.WeaponTypes == null)
            {
                if (TuningConfig.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine("Warning: ItemScaling.WeaponTypes configuration not found, using existing Weapons.json");
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

        private static EnemyData GenerateEnemyFromConfig(EnemyData existing, SustainBalanceConfig sustainBalance)
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

            // Apply SustainBalance health scaling based on target actions to kill
            if (sustainBalance.TargetActionsToKill != null)
            {
                var variables = new Dictionary<string, double>
                {
                    ["BaseActions"] = sustainBalance.TargetActionsToKill.Level1,
                    ["Level"] = existing.BaseLevel,
                    ["ActionsPerLevel"] = (sustainBalance.TargetActionsToKill.Level10 - sustainBalance.TargetActionsToKill.Level1) / 9.0,
                    ["QuadraticFactor"] = 0.1
                };

                if (!string.IsNullOrEmpty(sustainBalance.TargetActionsToKill.Formula))
                {
                    double targetActions = FormulaEvaluator.Evaluate(sustainBalance.TargetActionsToKill.Formula, variables);
                    
                    // Calculate health based on target actions and DPS balance
                    double baseDPS = 1.5; // Default DPS
                    if (TuningConfig.Instance.EnemyDPS != null)
                    {
                        baseDPS = TuningConfig.Instance.EnemyDPS.BaseDPSAtLevel1 + (existing.BaseLevel * TuningConfig.Instance.EnemyDPS.DPSPerLevel);
                    }
                    double estimatedHealth = targetActions * baseDPS;
                    generated.BaseHealth = Math.Max(1, (int)Math.Round(estimatedHealth));
                }
            }

            // Apply SustainBalance attribute scaling
            if (sustainBalance.AttributeGainRatio != null)
            {
                double attributeMultiplier = sustainBalance.AttributeGainRatio.BaseRatio;
                
                // Apply primary attribute bonus
                if (sustainBalance.AttributeGainRatio.PrimaryAttributeBonus > 0)
                {
                    switch (existing.PrimaryAttribute.ToLower())
                    {
                        case "strength":
                            generated.BaseStats.Strength = (int)Math.Round(generated.BaseStats.Strength * sustainBalance.AttributeGainRatio.PrimaryAttributeBonus);
                            break;
                        case "agility":
                            generated.BaseStats.Agility = (int)Math.Round(generated.BaseStats.Agility * sustainBalance.AttributeGainRatio.PrimaryAttributeBonus);
                            break;
                        case "technique":
                            generated.BaseStats.Technique = (int)Math.Round(generated.BaseStats.Technique * sustainBalance.AttributeGainRatio.PrimaryAttributeBonus);
                            break;
                        case "intelligence":
                            generated.BaseStats.Intelligence = (int)Math.Round(generated.BaseStats.Intelligence * sustainBalance.AttributeGainRatio.PrimaryAttributeBonus);
                            break;
                    }
                }
                
                // Apply secondary attribute scaling
                double secondaryMultiplier = sustainBalance.AttributeGainRatio.SecondaryAttributeBonus;
                generated.BaseStats.Strength = (int)Math.Round(generated.BaseStats.Strength * secondaryMultiplier);
                generated.BaseStats.Agility = (int)Math.Round(generated.BaseStats.Agility * secondaryMultiplier);
                generated.BaseStats.Technique = (int)Math.Round(generated.BaseStats.Technique * secondaryMultiplier);
                generated.BaseStats.Intelligence = (int)Math.Round(generated.BaseStats.Intelligence * secondaryMultiplier);
            }

            // Apply SustainBalance armor scaling based on health-to-armor ratio
            if (sustainBalance.HealthToArmorRatio != null)
            {
                double baseRatio = sustainBalance.HealthToArmorRatio.BaseRatio;
                double archetypeModifier = 1.0;
                
                // Apply archetype-specific modifiers if available
                if (sustainBalance.HealthToArmorRatio.ArchetypeModifiers != null)
                {
                    // Try to determine archetype from enemy name or stats
                    string archetype = DetermineEnemyArchetype(existing);
                    if (sustainBalance.HealthToArmorRatio.ArchetypeModifiers.ContainsKey(archetype))
                    {
                        archetypeModifier = sustainBalance.HealthToArmorRatio.ArchetypeModifiers[archetype];
                    }
                }
                
                double targetArmor = generated.BaseHealth / (baseRatio * archetypeModifier);
                generated.BaseArmor = Math.Max(0, (int)Math.Round(targetArmor));
            }

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

            // Apply armor type specific scaling - only modify armor value
            if (itemScaling.ArmorTypes.TryGetValue(existing.Slot, out var armorConfig))
            {
                var variables = new Dictionary<string, double>
                {
                    ["BaseArmor"] = existing.Armor,
                    ["Tier"] = existing.Tier,
                    ["Level"] = 1, // Base level
                    ["BaseChance"] = 0.1
                };

                if (!string.IsNullOrEmpty(armorConfig.ArmorFormula))
                {
                    double scaledArmor = FormulaEvaluator.Evaluate(armorConfig.ArmorFormula, variables);
                    generated.Armor = (int)Math.Round(scaledArmor);
                }
            }

            // Apply level scaling caps - only modify armor value
            if (itemScaling.LevelScalingCaps != null)
            {
                generated.Armor = Math.Max(itemScaling.LevelScalingCaps.MinimumValues.Armor, generated.Armor);
                double maxArmor = existing.Armor * itemScaling.LevelScalingCaps.MaxArmorScaling;
                generated.Armor = Math.Min(generated.Armor, (int)Math.Round(maxArmor));
            }

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

            // Apply weapon type specific scaling
            if (itemScaling.WeaponTypes.TryGetValue(existing.Type, out var weaponConfig))
            {
                var variables = new Dictionary<string, double>
                {
                    ["BaseDamage"] = existing.BaseDamage,
                    ["Tier"] = existing.Tier,
                    ["Level"] = 1, // Base level
                    ["BaseSpeed"] = existing.AttackSpeed
                };

                if (!string.IsNullOrEmpty(weaponConfig.DamageFormula))
                {
                    double scaledDamage = FormulaEvaluator.Evaluate(weaponConfig.DamageFormula, variables);
                    generated.BaseDamage = (int)Math.Round(scaledDamage);
                }

                if (!string.IsNullOrEmpty(weaponConfig.SpeedFormula))
                {
                    double scaledSpeed = FormulaEvaluator.Evaluate(weaponConfig.SpeedFormula, variables);
                    generated.AttackSpeed = scaledSpeed;
                }
            }

            // Apply level scaling caps
            if (itemScaling.LevelScalingCaps != null)
            {
                generated.BaseDamage = Math.Max(itemScaling.LevelScalingCaps.MinimumValues.Damage, generated.BaseDamage);
                double maxDamage = existing.BaseDamage * itemScaling.LevelScalingCaps.MaxDamageScaling;
                generated.BaseDamage = Math.Min(generated.BaseDamage, (int)Math.Round(maxDamage));

                generated.AttackSpeed = Math.Max(itemScaling.LevelScalingCaps.MinimumValues.Speed, generated.AttackSpeed);
                double maxSpeed = existing.AttackSpeed * itemScaling.LevelScalingCaps.MaxSpeedScaling;
                generated.AttackSpeed = Math.Min(generated.AttackSpeed, maxSpeed);
            }

            return generated;
        }


        private static List<EnemyData> LoadExistingEnemies()
        {
            try
            {
                string? filePath = FindGameDataFile("Enemies.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var enemies = JsonSerializer.Deserialize<List<EnemyData>>(json) ?? new List<EnemyData>();
                    return enemies;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading existing enemies: {ex.Message}");
            }
            return new List<EnemyData>();
        }

        private static List<ArmorData> LoadExistingArmor()
        {
            try
            {
                string? filePath = FindGameDataFile("Armor.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var armor = JsonSerializer.Deserialize<List<ArmorData>>(json) ?? new List<ArmorData>();
                    return armor;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading existing armor: {ex.Message}");
            }
            return new List<ArmorData>();
        }

        private static List<WeaponData> LoadExistingWeapons()
        {
            try
            {
                string? filePath = FindGameDataFile("Weapons.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var weapons = JsonSerializer.Deserialize<List<WeaponData>>(json) ?? new List<WeaponData>();
                    return weapons;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading existing weapons: {ex.Message}");
            }
            return new List<WeaponData>();
        }

        private static string? FindGameDataFile(string fileName)
        {
            // Get the directory where the executable is located
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            
            // List of possible GameData directory locations relative to different starting points
            string[] possibleGameDataDirs = {
                // Relative to executable directory
                Path.Combine(executableDir, "GameData"),
                Path.Combine(executableDir, "..", "GameData"),
                Path.Combine(executableDir, "..", "..", "GameData"),
                
                // Relative to current working directory
                Path.Combine(currentDir, "GameData"),
                Path.Combine(currentDir, "..", "GameData"),
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

            // Try to find the GameData directory first
            string? gameDataDir = null;
            foreach (string dir in possibleGameDataDirs)
            {
                if (Directory.Exists(dir))
                {
                    gameDataDir = dir;
                    break;
                }
            }

            // If we found a GameData directory, use it
            if (gameDataDir != null)
            {
                string filePath = Path.Combine(gameDataDir, fileName);
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            // Fallback: try direct file paths
            string[] directPaths = {
                Path.Combine(executableDir, "GameData", fileName),
                Path.Combine(currentDir, "GameData", fileName),
                Path.Combine("GameData", fileName),
                Path.Combine("..", "GameData", fileName),
                Path.Combine("..", "..", "GameData", fileName)
            };

            foreach (string path in directPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
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
            
            // Try to create GameData directory relative to executable first
            string[] preferredLocations = {
                Path.Combine(executableDir, "GameData"),
                Path.Combine(executableDir, "..", "GameData"),
                Path.Combine(currentDir, "GameData"),
                Path.Combine(currentDir, "..", "GameData"),
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
                // Relative to executable directory
                Path.Combine(executableDir, "GameData"),
                Path.Combine(executableDir, "..", "GameData"),
                Path.Combine(executableDir, "..", "..", "GameData"),
                
                // Relative to current working directory
                Path.Combine(currentDir, "GameData"),
                Path.Combine(currentDir, "..", "GameData"),
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
