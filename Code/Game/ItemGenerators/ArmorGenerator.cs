using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Specialized generator for armor data with scaling and validation logic
    /// </summary>
    public static class ArmorGenerator
    {
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
                    string filePath = FileManager.GetGameDataFilePath("Armor.json");
                    
                    // Create backup before writing
                    if (FileManager.CreateBackup(filePath) && !forceOverwrite)
                    {
                        result.AddMessage($"Created backup: {filePath}.backup");
                    }
                    
                    FileManager.SafeWriteJsonFile(filePath, generatedArmor, false);
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

        private static List<ArmorData> LoadExistingArmor()
        {
            string? filePath = JsonLoader.FindGameDataFile("Armor.json");
            if (filePath != null)
            {
                return JsonLoader.LoadJsonList<ArmorData>(filePath);
            }
            return new List<ArmorData>();
        }
    }
}
