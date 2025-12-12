using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame.Config
{
    /// <summary>
    /// Manages balance patches - shareable balance configurations
    /// </summary>
    public class BalancePatchManager
    {
        private static readonly string PatchesDirectory = Path.Combine("GameData", "BalancePatches");
        private const string GameVersion = "6.2";

        /// <summary>
        /// Patch metadata structure
        /// </summary>
        public class PatchMetadata
        {
            public string PatchId { get; set; } = "";
            public string Name { get; set; } = "";
            public string Author { get; set; } = "";
            public string Description { get; set; } = "";
            public string Version { get; set; } = "1.0";
            public string CreatedDate { get; set; } = "";
            public string CompatibleGameVersion { get; set; } = GameVersion;
            public List<string> Tags { get; set; } = new();
            public TestResults? TestResults { get; set; }
        }

        /// <summary>
        /// Test results embedded in patch
        /// </summary>
        public class TestResults
        {
            public double AverageWinRate { get; set; }
            public string TestDate { get; set; } = "";
            public int BattlesTested { get; set; }
        }

        /// <summary>
        /// Complete patch structure
        /// </summary>
        public class BalancePatch
        {
            public PatchMetadata PatchMetadata { get; set; } = new();
            public GameConfiguration TuningConfig { get; set; } = new();
        }

        /// <summary>
        /// Validation result
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
        }

        /// <summary>
        /// Ensure patches directory exists
        /// </summary>
        private static void EnsurePatchesDirectory()
        {
            if (!Directory.Exists(PatchesDirectory))
            {
                Directory.CreateDirectory(PatchesDirectory);
            }
        }

        /// <summary>
        /// Create a patch from current configuration
        /// </summary>
        public static BalancePatch CreatePatch(string name, string author, string description, 
            string version = "1.0", List<string>? tags = null, TestResults? testResults = null)
        {
            var patch = new BalancePatch
            {
                PatchMetadata = new PatchMetadata
                {
                    PatchId = GeneratePatchId(name, version),
                    Name = name,
                    Author = author,
                    Description = description,
                    Version = version,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    CompatibleGameVersion = GameVersion,
                    Tags = tags ?? new List<string>(),
                    TestResults = testResults
                },
                TuningConfig = GameConfiguration.Instance
            };

            return patch;
        }

        /// <summary>
        /// Save patch to patches directory
        /// </summary>
        public static bool SavePatch(BalancePatch patch)
        {
            try
            {
                EnsurePatchesDirectory();
                
                string fileName = $"{patch.PatchMetadata.PatchId}.json";
                string filePath = Path.Combine(PatchesDirectory, fileName);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(patch, options);
                File.WriteAllText(filePath, json);

                ScrollDebugLogger.Log($"BalancePatchManager: Saved patch '{patch.PatchMetadata.Name}' to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalancePatchManager: Error saving patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export patch to external location
        /// </summary>
        public static bool ExportPatch(BalancePatch patch, string exportPath)
        {
            try
            {
                string fileName = $"{patch.PatchMetadata.PatchId}.json";
                string filePath = Path.Combine(exportPath, fileName);

                // Ensure export directory exists
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(patch, options);
                File.WriteAllText(filePath, json);

                ScrollDebugLogger.Log($"BalancePatchManager: Exported patch '{patch.PatchMetadata.Name}' to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalancePatchManager: Error exporting patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load patch from file path
        /// </summary>
        public static BalancePatch? LoadPatch(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ScrollDebugLogger.Log($"BalancePatchManager: Patch file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var patch = JsonSerializer.Deserialize<BalancePatch>(json, options);
                
                if (patch != null)
                {
                    ScrollDebugLogger.Log($"BalancePatchManager: Loaded patch '{patch.PatchMetadata.Name}' from {filePath}");
                }

                return patch;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalancePatchManager: Error loading patch: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Import patch from external location and copy to patches directory
        /// </summary>
        public static bool ImportPatch(string sourcePath)
        {
            try
            {
                var patch = LoadPatch(sourcePath);
                if (patch == null)
                {
                    return false;
                }

                // Validate before importing
                var validation = ValidatePatch(patch);
                if (!validation.IsValid)
                {
                    ScrollDebugLogger.Log($"BalancePatchManager: Patch validation failed: {string.Join(", ", validation.Errors)}");
                    return false;
                }

                // Copy to patches directory
                EnsurePatchesDirectory();
                string fileName = $"{patch.PatchMetadata.PatchId}.json";
                string destPath = Path.Combine(PatchesDirectory, fileName);

                // If patch already exists, add timestamp to avoid overwrite
                if (File.Exists(destPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    fileName = $"{patch.PatchMetadata.PatchId}_{timestamp}.json";
                    destPath = Path.Combine(PatchesDirectory, fileName);
                }

                File.Copy(sourcePath, destPath, false);
                ScrollDebugLogger.Log($"BalancePatchManager: Imported patch '{patch.PatchMetadata.Name}' to {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalancePatchManager: Error importing patch: {ex.Message}");
                return false;
            }
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
            var result = new ValidationResult { IsValid = true };

            // Check metadata
            if (string.IsNullOrWhiteSpace(patch.PatchMetadata.Name))
            {
                result.Errors.Add("Patch name is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(patch.PatchMetadata.Author))
            {
                result.Warnings.Add("Patch author is not specified");
            }

            if (string.IsNullOrWhiteSpace(patch.PatchMetadata.Description))
            {
                result.Warnings.Add("Patch description is not specified");
            }

            // Check game version compatibility
            if (!string.IsNullOrWhiteSpace(patch.PatchMetadata.CompatibleGameVersion))
            {
                if (patch.PatchMetadata.CompatibleGameVersion != GameVersion)
                {
                    result.Warnings.Add($"Patch was created for game version {patch.PatchMetadata.CompatibleGameVersion}, current version is {GameVersion}");
                }
            }

            // Check tuning config exists
            if (patch.TuningConfig == null)
            {
                result.Errors.Add("Tuning configuration is missing");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// List all available patches
        /// </summary>
        public static List<BalancePatch> ListPatches()
        {
            var patches = new List<BalancePatch>();
            
            try
            {
                EnsurePatchesDirectory();

                if (!Directory.Exists(PatchesDirectory))
                {
                    return patches;
                }

                var files = Directory.GetFiles(PatchesDirectory, "*.json");
                foreach (var file in files)
                {
                    var patch = LoadPatch(file);
                    if (patch != null)
                    {
                        patches.Add(patch);
                    }
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalancePatchManager: Error listing patches: {ex.Message}");
            }

            return patches;
        }

        /// <summary>
        /// Get patch by ID
        /// </summary>
        public static BalancePatch? GetPatch(string patchId)
        {
            try
            {
                EnsurePatchesDirectory();
                string filePath = Path.Combine(PatchesDirectory, $"{patchId}.json");
                return LoadPatch(filePath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generate unique patch ID
        /// </summary>
        private static string GeneratePatchId(string name, string version)
        {
            string sanitized = name.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_");
            
            // Remove special characters
            sanitized = new string(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            
            return $"{sanitized}_v{version}_{DateTime.Now:yyyyMMdd}";
        }
    }
}

