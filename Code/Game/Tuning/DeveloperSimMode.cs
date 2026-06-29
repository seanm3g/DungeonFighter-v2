using System;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Simulation-only flags for developer tuning runs (not used in live gameplay).
    /// </summary>
    public static class DeveloperSimMode
    {
        /// <summary>When true, combat may continue past 0 HP to measure loss severity.</summary>
        public static bool ContinuePastZeroHp { get; private set; }

        /// <summary>Stop the fight when player HP falls to or below this value (negative allowed).</summary>
        public static int NegativeHpFloor { get; set; } = -500;

        /// <summary>Hard cap on sim advance calls per encounter in developer mode.</summary>
        public static int MaxSimAdvanceCalls { get; set; } = 200;

        public static void SetContinuePastZeroHp(bool enabled) => ContinuePastZeroHp = enabled;

        public static IDisposable BeginScope(bool continuePastZeroHp)
        {
            bool previous = ContinuePastZeroHp;
            ContinuePastZeroHp = continuePastZeroHp;
            return new Scope(previous);
        }

        public static bool ShouldContinueEncounter(int playerHealth, int enemyHealth, bool enemyIsAlive)
        {
            if (!enemyIsAlive)
                return false;
            if (!ContinuePastZeroHp)
                return playerHealth > 0;
            return playerHealth > NegativeHpFloor;
        }

        private sealed class Scope : IDisposable
        {
            private readonly bool _previous;
            private bool _disposed;

            public Scope(bool previous) => _previous = previous;

            public void Dispose()
            {
                if (_disposed) return;
                ContinuePastZeroHp = _previous;
                _disposed = true;
            }
        }
    }
}
