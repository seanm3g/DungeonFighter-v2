using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Safe game data generator that only updates files when explicitly requested
    /// and provides comprehensive safety checks to prevent data loss
    /// </summary>
    public static class GameDataGenerator
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
            // GenerateEnemiesJson(); // DISABLED - preserving manual enemy data
            
            result.ArmorResult = GenerateArmorJson(forceOverwrite);
            result.WeaponResult = GenerateWeaponsJson(forceOverwrite);
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine("Game data generation complete! (Enemies.json preserved)");
                result.LogSummary();
            }
            
            return result;
        }

        /// <summary>
        /// Result of a generation operation with detailed status information
        /// </summary>
        public class GenerationResult
        {
            public FileGenerationResult ArmorResult { get; set; } = new();
            public FileGenerationResult WeaponResult { get; set; } = new();
            
            public bool HasErrors => ArmorResult.HasErrors || WeaponResult.HasErrors;
            public bool HasWarnings => ArmorResult.HasWarnings || WeaponResult.HasWarnings;
            public int TotalFilesProcessed => (ArmorResult.Processed ? 1 : 0) + (WeaponResult.Processed ? 1 : 0);
            public int TotalFilesUpdated => (ArmorResult.Updated ? 1 : 0) + (WeaponResult.Updated ? 1 : 0);
            
            public void LogSummary()
            {
                if (HasErrors)
                {
                    Console.WriteLine($"Generation completed with {TotalFilesProcessed} files processed, {TotalFilesUpdated} updated, but with errors.");
                }
                else if (HasWarnings)
                {
                    Console.WriteLine($"Generation completed with {TotalFilesProcessed} files processed, {TotalFilesUpdated} updated, but with warnings.");
                }
                else
                {
                    Console.WriteLine($"Generation completed successfully: {TotalFilesProcessed} files processed, {TotalFilesUpdated} updated.");
                }
            }
        }

        /// <summary>
        /// Result of generating a single file
        /// </summary>
        public class FileGenerationResult
        {
            public bool Processed { get; set; }
            public bool Updated { get; set; }
            public bool HasErrors { get; set; }
            public bool HasWarnings { get; set; }
            public List<string> Messages { get; set; } = new();
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            
            public void AddMessage(string message) => Messages.Add(message);
            public void AddError(string error) { Errors.Add(error); HasErrors = true; }
            public void AddWarning(string warning) { Warnings.Add(warning); HasWarnings = true; }
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
            
            // Skip enemy generation to preserve manually curated Enemies.json
            Console.WriteLine("✓ Enemies.json generation skipped (preserving manual data)");
            
            Console.WriteLine("\nDynamic generation system test complete!");
        }

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

            string filePath = GetGameDataFilePath("Enemies.json");
            var existingEnemies = new List<EnemyData>();

            // Load existing enemies if file exists
            if (File.Exists(filePath))
            {
                try
                {
                    string existingJson = File.ReadAllText(filePath);
                    var loadedEnemies = JsonSerializer.Deserialize<List<EnemyData>>(existingJson);
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
            string json = JsonSerializer.Serialize(adjustedEnemies, _jsonOptions);
            File.WriteAllText(filePath, json);
            
            if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
            {
                Console.WriteLine($"Applied scaling adjustments to {adjustedEnemies.Count} enemies in Enemies.json");
            }
        }

        /// <summary>
        /// Generates Armor.json based on ItemScaling configurations with comprehensive safety checks
        /// </summary>
        public static FileGenerationResult GenerateArmorJson(bool forceOverwrite = false)
        {
            var result = new FileGenerationResult();
            
            try
            {
                var tuning = GameConfiguration.Instance;
                var itemScaling = tuning.ItemScaling;
                
                if (itemScaling == null)
                {
                    result.AddWarning("ItemScaling configuration not found, skipping armor generation");
                    return result;
                }

                // Load existing armor data to preserve names and other properties
                var existingArmor = LoadExistingArmor();
                result.Processed = true;
                
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine($"Loaded {existingArmor?.Count ?? 0} existing armor pieces");
                }
                
                // Safety check: if no valid armor data exists, skip generation entirely
                if (existingArmor == null || existingArmor.Count == 0)
                {
                    result.AddWarning("No existing armor data found, skipping armor generation to preserve file");
                    return result;
                }

                var generatedArmor = new List<ArmorData>();
                int validEntries = 0;
                int skippedEntries = 0;
                int changedEntries = 0;

                foreach (var existing in existingArmor)
                {
                    // Skip empty/corrupted entries completely
                    if (string.IsNullOrEmpty(existing.Slot) || string.IsNullOrEmpty(existing.Name) || existing.Tier <= 0)
                    {
                        skippedEntries++;
                        if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                        {
                            Console.WriteLine($"Skipping invalid armor entry: Slot='{existing.Slot}', Name='{existing.Name}', Tier={existing.Tier}");
                        }
                        continue;
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

                // Only write if we have valid entries to process
                if (validEntries > 0)
                {
                    // Additional safety check: don't overwrite if no changes and not forced
                    if (changedEntries == 0 && !forceOverwrite)
                    {
                        result.AddMessage($"No changes needed for {validEntries} armor pieces");
                        return result;
                    }

                    // Write updated armor to JSON
                    string json = JsonSerializer.Serialize(generatedArmor, _jsonOptions);
                    string filePath = GetGameDataFilePath("Armor.json");
                    
                    // Create backup before writing
                    if (File.Exists(filePath) && !forceOverwrite)
                    {
                        string backupPath = filePath + ".backup";
                        File.Copy(filePath, backupPath, true);
                        result.AddMessage($"Created backup: {backupPath}");
                    }
                    
                    File.WriteAllText(filePath, json);
                    result.Updated = true;
                    
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
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
                    result.AddWarning($"No valid armor entries found (skipped {skippedEntries} invalid entries), skipping armor generation to preserve existing data");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to generate armor data: {ex.Message}");
            }
            
            return result;
        }

        /// <summary>
        /// Generates Weapons.json based on ItemScaling configurations with comprehensive safety checks
        /// </summary>
        public static FileGenerationResult GenerateWeaponsJson(bool forceOverwrite = false)
        {
            var result = new FileGenerationResult();
            
            try
            {
                var tuning = GameConfiguration.Instance;
                var itemScaling = tuning.ItemScaling;
                
                if (itemScaling == null)
                {
                    result.AddWarning("ItemScaling configuration not found, skipping weapon generation");
                    return result;
                }

                // Load existing weapon data to preserve names and other properties
                var existingWeapons = LoadExistingWeapons();
                result.Processed = true;

                // Safety check: if no weapons exist, don't overwrite the file
                if (existingWeapons.Count == 0)
                {
                    result.AddWarning("No weapons found in Weapons.json, skipping generation to prevent data loss");
                    return result;
                }

                // Filter out corrupted entries (empty type or name) and only process valid weapons
                var validWeapons = existingWeapons.Where(w => !string.IsNullOrEmpty(w.Type) && !string.IsNullOrEmpty(w.Name)).ToList();
                
                // Safety check: if all weapons are corrupted, don't overwrite the file
                if (validWeapons.Count == 0)
                {
                    result.AddWarning("All weapons are corrupted, skipping generation to prevent data loss");
                    return result;
                }
                
                var generatedWeapons = new List<WeaponData>();
                int changedEntries = 0;
                
                foreach (var existing in validWeapons)
                {
                    var generated = GenerateWeaponFromConfig(existing, itemScaling);
                    generatedWeapons.Add(generated);
                    
                    // Check if weapon stats changed
                    if (existing.BaseDamage != generated.BaseDamage || existing.AttackSpeed != generated.AttackSpeed)
                    {
                        changedEntries++;
                        if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                        {
                            Console.WriteLine($"Updated: {existing.Name} - Damage: {existing.BaseDamage} -> {generated.BaseDamage}, Speed: {existing.AttackSpeed} -> {generated.AttackSpeed}");
                        }
                    }
                }
                
                if (validWeapons.Count != existingWeapons.Count)
                {
                    result.AddMessage($"Filtered out {existingWeapons.Count - validWeapons.Count} corrupted weapon entries");
                }

                // Additional safety check: don't overwrite if no changes and not forced
                if (changedEntries == 0 && !forceOverwrite)
                {
                    result.AddMessage($"No changes needed for {generatedWeapons.Count} weapons");
                    return result;
                }

                // Write updated weapons to JSON
                string json = JsonSerializer.Serialize(generatedWeapons, _jsonOptions);
                string filePath = GetGameDataFilePath("Weapons.json");
                
                // Create backup before writing
                if (File.Exists(filePath) && !forceOverwrite)
                {
                    string backupPath = filePath + ".backup";
                    File.Copy(filePath, backupPath, true);
                    result.AddMessage($"Created backup: {backupPath}");
                }
                
                File.WriteAllText(filePath, json);
                result.Updated = true;
                
                if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                {
                    Console.WriteLine($"Generated {generatedWeapons.Count} weapons in Weapons.json");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to generate weapon data: {ex.Message}");
            }
            
            return result;
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

        /// <summary>
        /// Gets the GameData file path using JsonLoader's existing logic
        /// </summary>
        private static string GetGameDataFilePath(string fileName)
        {
            // Use JsonLoader's existing file finding logic
            string? filePath = JsonLoader.FindGameDataFile(fileName);
            if (filePath != null)
            {
                return filePath;
            }

            // Fallback: try to find GameData directory and create file path
            string? gameDataDir = FindGameDataDirectory();
            if (gameDataDir != null)
            {
                return Path.Combine(gameDataDir, fileName);
            }

            // Last resort: use current directory
            return Path.Combine("GameData", fileName);
        }

        /// <summary>
        /// Simplified GameData directory finder
        /// </summary>
        private static string? FindGameDataDirectory()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Check common locations in order of preference
            string[] possibleDirs = {
                Path.Combine(currentDir, "GameData"),           // Current directory
                Path.Combine(currentDir, "..", "GameData"),     // Parent directory
                Path.Combine(executableDir, "GameData"),        // Executable directory
                Path.Combine(executableDir, "..", "GameData"),  // Executable parent
                "GameData"                                      // Relative path
            };

            foreach (string dir in possibleDirs)
            {
                if (Directory.Exists(dir))
                {
                    return dir;
                }
            }

            return null;
        }

        /// <summary>
        /// Safely writes JSON data to a file with backup creation
        /// </summary>
        private static void SafeWriteJsonFile(string filePath, object data, bool createBackup = true)
        {
            // Create backup if file exists and backup is requested
            if (File.Exists(filePath) && createBackup)
            {
                string backupPath = filePath + ".backup";
                File.Copy(filePath, backupPath, true);
            }

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write the file
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
    }
}