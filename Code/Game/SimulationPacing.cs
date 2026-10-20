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

        // Stable fighters: combat UI is muted (which normally skips every delay), but the run should still take
        // as long as a watched fight. Inside this scope the action and room delays run anyway.
        private static readonly AsyncLocal<bool> _pacedScope = new();

        /// <summary>True inside <see cref="BeginPacedScope"/>: muted, but paced like a visible fight.</summary>
        public static bool IsPacedScope => _pacedScope.Value;

        /// <summary>For the current async flow, keep combat and room pacing even while combat UI is muted. Dispose to restore.</summary>
        public static System.IDisposable BeginPacedScope()
        {
            bool previous = _pacedScope.Value;
            _pacedScope.Value = true;
            return new PacedScopeRestore(previous);
        }

        private sealed class PacedScopeRestore : System.IDisposable
        {
            private readonly bool _previous;
            public PacedScopeRestore(bool previous) => _previous = previous;
            public void Dispose() => _pacedScope.Value = _previous;
        }

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
            if (ShouldSkipDelays && !IsPacedScope)
                return;

            int scaledDelayMs = DeveloperModeState.ScaleDelayMs(delayMs);
            if (scaledDelayMs > 0)
                await Task.Delay(scaledDelayMs);
        }
    }
}
