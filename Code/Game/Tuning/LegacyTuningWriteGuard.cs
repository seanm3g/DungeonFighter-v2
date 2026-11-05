using System;
using RPGGame.Utils;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Blocks the legacy automated tuning paths (single-fight win-rate suggesters, MCP save/load_patch,
    /// TUNEAPPLY, Lab Balance suggestion Apply, profile/import apply) from writing the config to disk.
    /// <see cref="GameConfiguration.SaveToFile"/> writes the active balance patch named in PatchProfile.json
    /// (a git-tracked GameData/Patches/Balance file), so those tools would silently overwrite a solved patch.
    /// Manual edits (Settings save, variable/status-effect editors, Lab knob save) are not guarded.
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
            return $"{source}: blocked. This legacy tuning tool would overwrite {path} (the active patch from PatchProfile.json). " +
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
