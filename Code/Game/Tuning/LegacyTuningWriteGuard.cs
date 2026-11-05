using System;
using RPGGame.Utils;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Blocks automated writers (the MCP save_variable_changes tool; before 2026-11-04 also the removed single-fight
    /// win-rate tuning stack) from writing the config to disk.
    /// <see cref="GameConfiguration.SaveToFile"/> writes the active balance patch named in PatchProfile.json
    /// (a git-tracked GameData/Patches/Balance file), so those tools would silently overwrite a solved patch.
    /// Manual edits in the game UI (Settings save, variable/status-effect editors, Lab knob save) are not guarded.
    /// Set DF_ALLOW_LEGACY_TUNING_WRITES=1 to allow these writes anyway.
    /// </summary>
    public static class LegacyTuningWriteGuard
    {
        public const string AllowEnvVar = "DF_ALLOW_LEGACY_TUNING_WRITES";

        public static bool WritesAllowed =>
            System.Environment.GetEnvironmentVariable(AllowEnvVar) == "1";

        public static string BlockedMessage(string source)
        {
            string path = GameConfiguration.TryGetExistingTuningConfigFilePath() ?? "the active balance patch";
            return $"{source}: blocked. This tool would overwrite {path} (the active patch from PatchProfile.json). " +
                   "Balance work now goes through Scripts/balance-scan.py, bo-solve.py and dungeon-sim.py, with a new patch JSON " +
                   "in GameData/Patches/Balance (see Documentation/BALANCE_PROCESS.md). " +
                   $"Set {AllowEnvVar}=1 to allow this write anyway.";
        }

        /// <summary>Returns true if the write may proceed; otherwise logs why and returns false.</summary>
        public static bool Check(string source)
        {
            if (WritesAllowed)
                return true;
            ScrollDebugLogger.Log(BlockedMessage(source));
            return false;
        }

        /// <summary>Saves the config if allowed. Returns false (and logs) when blocked or the save fails.</summary>
        public static bool TrySave(string source) =>
            Check(source) && GameConfiguration.Instance.SaveToFile();
    }
}
