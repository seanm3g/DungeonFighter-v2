using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Specialized generator for weapon data with scaling and validation logic
    /// </summary>
    public static class WeaponGenerator
    {
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
                string filePath = FileManager.GetGameDataFilePath("Weapons.json");
                
                // Create backup before writing
                if (FileManager.CreateBackup(filePath) && !forceOverwrite)
                {
                    result.AddMessage($"Created backup: {filePath}.backup");
                }
                
                FileManager.SafeWriteJsonFile(filePath, generatedWeapons, false);
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
            // Use base damage from tier 1 weapons to prevent accumulation corruption
            double weaponDamagePerTier = itemScaling.WeaponDamagePerTier;
            double baseDamage = GetBaseDamageForWeaponType(existing.Type);
            double tierMultiplier = 1.0 + (existing.Tier - 1) * 0.3; // 30% increase per tier
            double globalMultiplier = itemScaling.GlobalDamageMultiplier;
            
            generated.BaseDamage = Math.Max(1, (int)Math.Round(baseDamage * tierMultiplier * globalMultiplier));
            
            // Apply speed bonus per tier - use base speed from tier 1 weapons to prevent corruption
            double speedBonusPerTier = itemScaling.SpeedBonusPerTier;
            
            // Get base speed for this weapon type from tier 1 weapons to prevent accumulation corruption
            double baseSpeed = GetBaseSpeedForWeaponType(existing.Type);
            generated.AttackSpeed = Math.Max(0.1, baseSpeed + (existing.Tier - 1) * speedBonusPerTier);

            return generated;
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
        /// Gets the base attack speed for a weapon type from tier 1 weapons to prevent corruption
        /// </summary>
        private static double GetBaseSpeedForWeaponType(string weaponType)
        {
            // Define base speeds for each weapon type based on StartingGear.json and reasonable defaults
            return weaponType.ToLower() switch
            {
                "sword" => 1.0,    // Balanced speed
                "mace" => 0.8,     // Slower, heavier weapon
                "dagger" => 1.4,   // Fast, light weapon
                "wand" => 1.1,     // Slightly faster than sword
                _ => 1.0           // Default fallback
            };
        }

        /// <summary>
        /// Gets the base damage for a weapon type from tier 1 weapons to prevent corruption
        /// </summary>
        private static double GetBaseDamageForWeaponType(string weaponType)
        {
            // Define base damage for each weapon type based on StartingGear.json and reasonable defaults
            return weaponType.ToLower() switch
            {
                "sword" => 6.0,    // Balanced damage
                "mace" => 7.5,     // Higher damage, slower speed
                "dagger" => 4.3,   // Lower damage, faster speed
                "wand" => 5.5,     // Moderate damage
                _ => 6.0           // Default fallback
            };
        }
    }
}
