using System;
using System.Collections.Generic;
using RPGGame.Config.BalancePatches;
using RPGGame.Utils;

namespace RPGGame.Config
{
    /// <summary>
    /// Facade for managing balance patches - shareable balance configurations
    /// 
    /// Refactored from 414 lines to ~150 lines using Facade pattern.
    /// Delegates to:
    /// - BalancePatchMetadata: Data structures and metadata
    /// - BalancePatchValidator: Validation logic
    /// - BalancePatchIO: File I/O operations
    /// </summary>
    public class BalancePatchManager
    {
        // Type aliases for backward compatibility
        public class PatchMetadata : BalancePatchMetadata.PatchMetadata { }
        public class TestResults : BalancePatchMetadata.TestResults { }
        public class BalancePatch : BalancePatchMetadata.BalancePatch { }
        public class ValidationResult : BalancePatchMetadata.ValidationResult { }

        /// <summary>
        /// Create a patch from current configuration
        /// </summary>
        public static BalancePatch CreatePatch(string name, string author, string description, 
            string version = "1.0", List<string>? tags = null, TestResults? testResults = null)
        {
            var internalPatch = new BalancePatchMetadata.BalancePatch
            {
                PatchMetadata = new BalancePatchMetadata.PatchMetadata
                {
                    PatchId = BalancePatchMetadata.GeneratePatchId(name, version),
                    Name = name,
                    Author = author,
                    Description = description,
                    Version = version,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    CompatibleGameVersion = BalancePatchMetadata.GetGameVersion(),
                    Tags = tags ?? new List<string>(),
                    TestResults = testResults != null ? new BalancePatchMetadata.TestResults
                    {
                        AverageWinRate = testResults.AverageWinRate,
                        TestDate = testResults.TestDate,
                        BattlesTested = testResults.BattlesTested
                    } : null
                },
                TuningConfig = GameConfiguration.Instance
            };

            return ConvertFromInternal(internalPatch);
        }

        /// <summary>
        /// Save patch to patches directory
        /// </summary>
        public static bool SavePatch(BalancePatch patch)
        {
            var internalPatch = ConvertToInternal(patch);
            return BalancePatchIO.SavePatch(internalPatch);
        }

        /// <summary>
        /// Export patch to external location
        /// </summary>
        public static bool ExportPatch(BalancePatch patch, string exportPath)
        {
            var internalPatch = ConvertToInternal(patch);
            return BalancePatchIO.ExportPatch(internalPatch, exportPath);
        }

        /// <summary>
        /// Load patch from file path
        /// </summary>
        public static BalancePatch? LoadPatch(string filePath)
        {
            var internalPatch = BalancePatchIO.LoadPatch(filePath);
            return internalPatch != null ? ConvertFromInternal(internalPatch) : null;
        }

        /// <summary>
        /// Import patch from external location and copy to patches directory
        /// </summary>
        public static bool ImportPatch(string sourcePath)
        {
            return BalancePatchIO.ImportPatch(sourcePath);
        }

        /// <summary>
        /// Apply patch to current game configuration
        /// </summary>
        public static bool ApplyPatch(BalancePatch patch)
        {
            try
            {
                var validation = ValidatePatch(patch);
                if (!validation.IsValid)
                {
                    ScrollDebugLogger.Log($"BalancePatchManager: Cannot apply patch - validation failed: {string.Join(", ", validation.Errors)}");
                    return false;
                }

                // Apply the tuning config from patch
                var config = GameConfiguration.Instance;
                var patchConfig = patch.TuningConfig;

                // Copy all configuration sections
                config.Character = patchConfig.Character;
                config.Attributes = patchConfig.Attributes;
                config.Combat = patchConfig.Combat;
                config.CombatBalance = patchConfig.CombatBalance;
                config.RollSystem = patchConfig.RollSystem;
                config.EnemySystem = patchConfig.EnemySystem;
                config.WeaponScaling = patchConfig.WeaponScaling;
                config.EquipmentScaling = patchConfig.EquipmentScaling;
                config.Progression = patchConfig.Progression;
                config.StatusEffects = patchConfig.StatusEffects;
                config.ComboSystem = patchConfig.ComboSystem;
                config.LootSystem = patchConfig.LootSystem;
                config.DungeonScaling = patchConfig.DungeonScaling;
                config.DungeonGeneration = patchConfig.DungeonGeneration;
                config.ModificationRarity = patchConfig.ModificationRarity;
                config.GameSpeed = patchConfig.GameSpeed;
                config.GameData = patchConfig.GameData;
                config.Debug = patchConfig.Debug;
                config.BalanceAnalysis = patchConfig.BalanceAnalysis;
                config.BalanceValidation = patchConfig.BalanceValidation;
                config.DifficultySettings = patchConfig.DifficultySettings;
                config.UICustomization = patchConfig.UICustomization;

                // Save to file
                config.SaveToFile();

                ScrollDebugLogger.Log($"BalancePatchManager: Applied patch '{patch.PatchMetadata.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalancePatchManager: Error applying patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate patch structure and compatibility
        /// </summary>
        public static ValidationResult ValidatePatch(BalancePatch patch)
        {
            var internalPatch = ConvertToInternal(patch);
            var result = BalancePatchValidator.ValidatePatch(internalPatch);
            return new ValidationResult
            {
                IsValid = result.IsValid,
                Errors = result.Errors,
                Warnings = result.Warnings
            };
        }

        /// <summary>
        /// List all available patches
        /// </summary>
        public static List<BalancePatch> ListPatches()
        {
            var internalPatches = BalancePatchIO.ListPatches();
            return internalPatches.Select(ConvertFromInternal).ToList();
        }

        /// <summary>
        /// Get patch by ID
        /// </summary>
        public static BalancePatch? GetPatch(string patchId)
        {
            var internalPatch = BalancePatchIO.GetPatch(patchId);
            return internalPatch != null ? ConvertFromInternal(internalPatch) : null;
        }

        /// <summary>
        /// Convert public patch to internal patch
        /// </summary>
        private static BalancePatchMetadata.BalancePatch ConvertToInternal(BalancePatch patch)
        {
            // Since BalancePatch inherits from BalancePatchMetadata.BalancePatch, we can cast
            return (BalancePatchMetadata.BalancePatch)patch;
        }

        /// <summary>
        /// Convert internal patch to public patch
        /// </summary>
        private static BalancePatch ConvertFromInternal(BalancePatchMetadata.BalancePatch internalPatch)
        {
            // Create a new BalancePatch (which inherits from internal) and copy properties
            var patch = new BalancePatch
            {
                TuningConfig = internalPatch.TuningConfig
            };
            patch.PatchMetadata = new PatchMetadata
            {
                PatchId = internalPatch.PatchMetadata.PatchId,
                Name = internalPatch.PatchMetadata.Name,
                Author = internalPatch.PatchMetadata.Author,
                Description = internalPatch.PatchMetadata.Description,
                Version = internalPatch.PatchMetadata.Version,
                CreatedDate = internalPatch.PatchMetadata.CreatedDate,
                CompatibleGameVersion = internalPatch.PatchMetadata.CompatibleGameVersion,
                Tags = internalPatch.PatchMetadata.Tags,
                TestResults = internalPatch.PatchMetadata.TestResults != null ? new TestResults
                {
                    AverageWinRate = internalPatch.PatchMetadata.TestResults.AverageWinRate,
                    TestDate = internalPatch.PatchMetadata.TestResults.TestDate,
                    BattlesTested = internalPatch.PatchMetadata.TestResults.BattlesTested
                } : null
            };
            return patch;
        }
    }
}

