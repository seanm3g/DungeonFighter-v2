using System;
using System.Diagnostics;
using System.Threading;

namespace RPGGame.Diagnostics
{
    /// <summary>
    /// Opt-in wall-clock sampling for headless combat benchmarks.
    /// When disabled, overhead is a single branch check at entry points.
    /// </summary>
    public static class CombatHotPathMetrics
    {
        private static int enabled;

        public static bool IsEnabled
        {
            get => Volatile.Read(ref enabled) != 0;
            set => Volatile.Write(ref enabled, value ? 1 : 0);
        }

        private static long actionExecutionFlowTicks;
        private static long damageCalculatorTicks;
        private static int actionExecutionSamples;
        private static int damageCalculatorSamples;

        public static void Reset()
        {
            Interlocked.Exchange(ref actionExecutionFlowTicks, 0);
            Interlocked.Exchange(ref damageCalculatorTicks, 0);
            Interlocked.Exchange(ref actionExecutionSamples, 0);
            Interlocked.Exchange(ref damageCalculatorSamples, 0);
        }

        internal static void RecordActionExecutionFlow(TimeSpan elapsed)
        {
            if (!IsEnabled) return;
            Interlocked.Add(ref actionExecutionFlowTicks, elapsed.Ticks);
            Interlocked.Increment(ref actionExecutionSamples);
        }

        internal static void RecordDamageCalculator(TimeSpan elapsed)
        {
            if (!IsEnabled) return;
            Interlocked.Add(ref damageCalculatorTicks, elapsed.Ticks);
            Interlocked.Increment(ref damageCalculatorSamples);
        }

        /// <summary>
        /// Share of wall time spent in <see cref="Combat.Calculators.DamageCalculator.CalculateDamage"/>
        /// relative to time spent in <see cref="Actions.Execution.ActionExecutionFlow.Execute"/> (subset).
        /// </summary>
        public static double DamageCalculatorShareOfExecution =>
            actionExecutionFlowTicks <= 0 ? 0 : (double)damageCalculatorTicks / actionExecutionFlowTicks;

        public static string FormatReport()
        {
            double execMs = TimeSpan.FromTicks(actionExecutionFlowTicks).TotalMilliseconds;
            double dmgMs = TimeSpan.FromTicks(damageCalculatorTicks).TotalMilliseconds;
            double pct = DamageCalculatorShareOfExecution * 100.0;
            return
                "COMBAT HOT PATH (wall-clock, headless)\n" +
                $"  ActionExecutionFlow.Execute: {execMs:F2} ms over {actionExecutionSamples} samples\n" +
                $"  DamageCalculator.CalculateDamage: {dmgMs:F2} ms over {damageCalculatorSamples} calls\n" +
                $"  Damage calc ≈ {pct:F1}% of execution-flow time (nested subset)\n";
        }
    }
}
