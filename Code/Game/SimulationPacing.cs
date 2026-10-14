using System.Threading;
using System.Threading.Tasks;
using RPGGame.Combat;
using RPGGame.MCP;
using RPGGame.UI;

namespace RPGGame
{
    /// <summary>
    /// Disables combat/UI pacing for headless simulations, MCP, tuning CLI, and tests.
    /// </summary>
    public static class SimulationPacing
    {
        private static int _fastModeEnabled;

        public static bool IsFastModeEnabled => Volatile.Read(ref _fastModeEnabled) != 0;

        public static bool ShouldSkipDelays =>
            IsFastModeEnabled
            || DeveloperModeState.IsCombatLogInstant
            || CombatManager.DisableCombatUIOutput
            || !UIManager.EnableDelays
            || MCPMode.IsActive
            || GameSettings.Instance.FastCombat;

        /// <summary>
        /// Zero combat log, dungeon room, and UI pacing delays for batch runs.
        /// </summary>
        public static void EnableFastMode()
        {
            Volatile.Write(ref _fastModeEnabled, 1);
            DeveloperModeState.SetCombatLogInstant(true);
            UIManager.EnableDelays = false;
            CombatManager.DisableCombatUIOutput = true;
            MCPMode.IsActive = true;

            var settings = GameSettings.Instance;
            settings.EnableTextDisplayDelays = false;
            settings.FastCombat = true;
            settings.CombatSpeed = 2.0;
        }

        /// <summary>Tests only: clears the fast-mode latch (the other flags EnableFastMode sets are restored by the caller).</summary>
        internal static void ResetFastModeLatchForTests() => Volatile.Write(ref _fastModeEnabled, 0);

        public static async Task DelayForCombatSpeedAsync(int delayMs)
        {
            if (ShouldSkipDelays)
                return;

            int scaledDelayMs = DeveloperModeState.ScaleDelayMs(delayMs);
            if (scaledDelayMs > 0)
                await Task.Delay(scaledDelayMs);
        }
    }
}
