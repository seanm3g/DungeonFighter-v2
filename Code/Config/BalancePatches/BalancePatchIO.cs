using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame.Config.BalancePatches
{
    /// <summary>
    /// Handles file I/O operations for balance patches
    /// Extracted from BalancePatchManager to separate I/O logic
    /// </summary>
    public static class BalancePatchIO
    {
        private static readonly string PatchesDirectory = Path.Combine("GameData", "BalancePatches");

        /// <summary>
        /// Ensure patches directory exists
        /// </summary>
        public static void EnsurePatchesDirectory()
        {
            if (!Directory.Exists(PatchesDirectory))
            {
                Directory.CreateDirectory(PatchesDirectory);
            }
        }

        /// <summary>
        /// Save patch to patches directory
        /// </summary>
        public static bool SavePatch(BalancePatchMetadata.BalancePatch patch)
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
        public static bool ExportPatch(BalancePatchMetadata.BalancePatch patch, string exportPath)
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
        public static BalancePatchMetadata.BalancePatch? LoadPatch(string filePath)
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

                var patch = JsonSerializer.Deserialize<BalancePatchMetadata.BalancePatch>(json, options);
                
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
                var validation = BalancePatchValidator.ValidatePatch(patch);
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
        /// List all available patches
        /// </summary>
        public static List<BalancePatchMetadata.BalancePatch> ListPatches()
        {
            var patches = new List<BalancePatchMetadata.BalancePatch>();
            
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
        public static BalancePatchMetadata.BalancePatch? GetPatch(string patchId)
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
    }
}

