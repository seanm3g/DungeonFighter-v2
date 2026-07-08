using System;
using System.Threading;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Simulation-only flags for developer tuning runs (not used in live gameplay).
    /// </summary>
    public static class DeveloperSimMode
    {
        private const int DefaultNegativeHpFloor = -500;

        private static readonly AsyncLocal<int?> AsyncNegativeHpFloor = new();

        /// <summary>When true, combat may continue past 0 HP to measure loss severity.</summary>
        public static bool ContinuePastZeroHp { get; private set; }

        /// <summary>Stop the fight when player HP falls to or below this value (negative allowed).</summary>
        public static int NegativeHpFloor => AsyncNegativeHpFloor.Value ?? DefaultNegativeHpFloor;

        /// <summary>Hard cap on sim advance calls per encounter in developer mode.</summary>
        public static int MaxSimAdvanceCalls { get; set; } = 200;

        public static void SetContinuePastZeroHp(bool enabled) => ContinuePastZeroHp = enabled;

        public static IDisposable BeginScope(bool continuePastZeroHp, int? negativeHpFloor = null)
        {
            bool previousContinue = ContinuePastZeroHp;
            int? previousFloor = AsyncNegativeHpFloor.Value;
            ContinuePastZeroHp = continuePastZeroHp;
            if (negativeHpFloor.HasValue)
                AsyncNegativeHpFloor.Value = negativeHpFloor.Value;
            return new Scope(previousContinue, previousFloor);
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
            private readonly bool _previousContinue;
            private readonly int? _previousFloor;
            private bool _disposed;

            public Scope(bool previousContinue, int? previousFloor)
            {
                _previousContinue = previousContinue;
                _previousFloor = previousFloor;
            }

            public void Dispose()
            {
                if (_disposed) return;
                ContinuePastZeroHp = _previousContinue;
                AsyncNegativeHpFloor.Value = _previousFloor;
                _disposed = true;
            }
        }
    }
}
