using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Combat;
using RPGGame.MCP;
using RPGGame.UI;
using RPGGame.UI.Avalonia;

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

        /// <summary>Longest wait for the center log to finish typing before pacing moves on anyway.</summary>
        public const int MaxLogRevealWaitMs = 15000;

        /// <summary>
        /// Room / encounter pacing that lets the center log finish typing first: waits until every queued line is
        /// shown, then holds at least <paramref name="holdAfterRevealMs"/>; the whole pause is never shorter than
        /// <paramref name="delayMs"/> (both scaled by combat speed). Headless runs skip it like any other delay.
        /// </summary>
        public static async Task DelayAfterLogRevealAsync(IUIManager? ui, int delayMs, Character? character = null, int holdAfterRevealMs = 700)
        {
            if (ShouldSkipDelays && !IsPacedScope)
                return;

            var clock = Stopwatch.StartNew();
            bool waitedForReveal = await WaitForLogRevealAsync(ui, character);
            int elapsed = (int)clock.ElapsedMilliseconds;
            int scaledDelayMs = DeveloperModeState.ScaleDelayMs(delayMs);
            int hold = waitedForReveal ? DeveloperModeState.ScaleDelayMs(holdAfterRevealMs) : 0;
            int remaining = Math.Max(scaledDelayMs - elapsed, hold);
            if (remaining > 0)
                await Task.Delay(remaining);
        }

        /// <summary>
        /// Waits until the center log has no line still typing in (canvas UI only; capped at
        /// <see cref="MaxLogRevealWaitMs"/>). True when it had to wait.
        /// </summary>
        public static async Task<bool> WaitForLogRevealAsync(IUIManager? ui, Character? character = null)
        {
            if (ui is not CanvasUICoordinator canvas || (ShouldSkipDelays && !IsPacedScope))
                return false;

            bool waited = false;
            var clock = Stopwatch.StartNew();
            while (canvas.IsLogRevealActive(character) && clock.ElapsedMilliseconds < MaxLogRevealWaitMs)
            {
                waited = true;
                await Task.Delay(40);
            }
            return waited;
        }
    }
}
