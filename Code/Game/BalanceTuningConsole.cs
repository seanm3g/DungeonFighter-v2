using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.Tuning;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Interactive console for real-time balance tuning
    /// Facade coordinating adjustment execution, undo/redo, and patch/profile management
    /// </summary>
    public static class BalanceTuningConsole
    {
        // ========== Adjustment Methods ==========

        public static bool AdjustGlobalEnemyMultiplier(string multiplierName, double value)
        {
            return AdjustmentExecutor.AdjustGlobalEnemyMultiplier(multiplierName, value);
        }

        public static bool AdjustArchetype(string archetypeName, string statName, double value)
        {
            return AdjustmentExecutor.AdjustArchetype(archetypeName, statName, value);
        }

        public static bool AdjustEnemyOverride(string enemyName, string statName, double? value)
        {
            ScrollDebugLogger.Log($"BalanceTuningConsole: Enemy overrides must be adjusted in Enemies.json for '{enemyName}'");
            return false;
        }

        public static bool AdjustWeaponScaling(string weaponType, string parameter, double value)
        {
            return AdjustmentExecutor.AdjustWeaponScaling(weaponType, parameter, value);
        }

        public static bool AdjustPlayerBaseAttribute(string attributeName, int value)
        {
            return AdjustmentExecutor.AdjustPlayerBaseAttribute(attributeName, value);
        }

        public static bool AdjustPlayerAttributesPerLevel(int value)
        {
            return AdjustmentExecutor.AdjustPlayerAttributesPerLevel(value);
        }

        public static bool AdjustPlayerBaseHealth(int value)
        {
            return AdjustmentExecutor.AdjustPlayerBaseHealth(value);
        }

        public static bool AdjustPlayerHealthPerLevel(int value)
        {
            return AdjustmentExecutor.AdjustPlayerHealthPerLevel(value);
        }

        public static bool AdjustEnemyBaselineStat(string statName, double value)
        {
            return AdjustmentExecutor.AdjustEnemyBaselineStat(statName, value);
        }

        public static bool AdjustEnemyScalingPerLevel(string statName, double value)
        {
            return AdjustmentExecutor.AdjustEnemyScalingPerLevel(statName, value);
        }

        public static bool ApplyPreset(string presetName)
        {
            return AdjustmentExecutor.ApplyPreset(presetName);
        }

        // ========== Patch Management ==========

        public static bool SavePatch(string patchName, string author, string description, 
            string version = "1.0", List<string>? tags = null, 
            BalancePatchManager.TestResults? testResults = null)
        {
            try
            {
                var patch = BalancePatchManager.CreatePatch(patchName, author, description, version, tags, testResults);
                return BalancePatchManager.SavePatch(patch);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error saving patch: {ex.Message}");
                return false;
            }
        }

        public static bool ExportPatch(string patchName, string exportPath)
        {
            try
            {
                var patch = BalancePatchManager.GetPatch(patchName);
                if (patch == null)
                {
                    var patches = BalancePatchManager.ListPatches();
                    patch = patches.FirstOrDefault(p => p.PatchMetadata.Name == patchName);
                }

                if (patch == null)
                {
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Patch '{patchName}' not found");
                    return false;
                }

                return BalancePatchManager.ExportPatch(patch, exportPath);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error exporting patch: {ex.Message}");
                return false;
            }
        }

        public static bool ImportPatch(string filePath)
        {
            try
            {
                return BalancePatchManager.ImportPatch(filePath);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error importing patch: {ex.Message}");
                return false;
            }
        }

        public static bool LoadPatch(string patchId)
        {
            try
            {
                UndoRedoManager.SaveState();
                var patch = BalancePatchManager.GetPatch(patchId);
                if (patch == null)
                {
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Patch '{patchId}' not found");
                    return false;
                }

                return BalancePatchManager.ApplyPatch(patch);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error loading patch: {ex.Message}");
                return false;
            }
        }

        // ========== Profile Management ==========

        public static bool SaveProfile(string profileName, string description = "", string notes = "")
        {
            try
            {
                var profile = TuningProfileManager.CreateProfile(profileName, description, notes);
                return TuningProfileManager.SaveProfile(profile);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error saving profile: {ex.Message}");
                return false;
            }
        }

        public static bool LoadProfile(string profileName)
        {
            try
            {
                UndoRedoManager.SaveState();
                var profile = TuningProfileManager.LoadProfile(profileName);
                if (profile == null)
                {
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Profile '{profileName}' not found");
                    return false;
                }

                return TuningProfileManager.ApplyProfile(profile);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error loading profile: {ex.Message}");
                return false;
            }
        }

        // ========== Undo/Redo ==========

        public static bool Undo()
        {
            return UndoRedoManager.Undo();
        }

        public static bool Redo()
        {
            return UndoRedoManager.Redo();
        }

        // ========== Configuration Management ==========

        public static bool SaveConfiguration()
        {
            try
            {
                return GameConfiguration.Instance.SaveToFile();
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error saving configuration: {ex.Message}");
                return false;
            }
        }
    }
}
