using System;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Derives level-specific enemy HP from broad progression knobs (tempo, shape, base/growth scales).
    /// </summary>
    public static class EnemyProgressionCurveEvaluator
    {
        public const double ParityPlayerHpFactor = 0.25;

        public sealed class ProgressionSample
        {
            public int Level { get; init; }
            public double GrowthWeight { get; init; }
            public int EnemyHealth { get; init; }
            public int PlayerMaxHealth { get; init; }
            public double? SimulatedMedianCombined { get; init; }
            public double? SimulatedWinRate { get; init; }
        }

        public static double ComputeGrowthWeight(int level, EnemyProgressionScalesConfig prog)
        {
            prog.EnsurePositiveScales();
            int lv = Math.Max(0, level - 1);
            int pivot = Math.Clamp(prog.ProgressionPivotLevel, 2, 100);
            double t = pivot > 1 ? Math.Clamp(lv / (double)(pivot - 1), 0, 1) : 1;
            double exponent = Lerp(3.0, 0.35, prog.ProgressionShape);
            return Math.Pow(t, exponent);
        }

        public static int ComputeLevelScaledHealth(
            double baseHealth,
            double growthHealthPerLevel,
            int level,
            EnemyProgressionScalesConfig prog)
        {
            prog.EnsurePositiveScales();
            int lv = Math.Max(0, level - 1);
            double growthWeight = ComputeGrowthWeight(level, prog);
            double scaled = baseHealth * prog.BaseHealthScale
                            + lv * growthHealthPerLevel * prog.HealthGrowthScale * growthWeight;
            return FloorToInt(scaled);
        }

        public static int ApplyFinalHealthMultipliers(int levelScaledHealth, EnemySystemConfig enemySystem)
        {
            var global = enemySystem.GlobalMultipliers;
            var prog = enemySystem.ProgressionScales ?? new EnemyProgressionScalesConfig();
            prog.EnsurePositiveScales();

            double runtime = GameSettings.Instance?.EnemyHealthMultiplier ?? 1.0;
            if (runtime <= 0)
                runtime = 1.0;

            double scaled = levelScaledHealth * global.HealthMultiplier * runtime * prog.CombatTempoScale;
            return Math.Max(1, FloorToInt(scaled));
        }

        public static int EstimatePlayerMaxHealth(int level)
        {
            var tuning = GameConfiguration.Instance;
            tuning.Character.EnsureValidPlayerHealthDefaults();
            var prog = tuning.EnemySystem.ProgressionScales ?? new EnemyProgressionScalesConfig();
            prog.EnsurePositiveScales();

            int baseHealth = tuning.Character.PlayerBaseHealth;
            int perLevel = tuning.Character.HealthPerLevel;
            int lv = Math.Max(0, level - 1);
            double raw = baseHealth + lv * (double)perLevel;
            raw *= 1 + prog.PlayerEnemyParity * ParityPlayerHpFactor;
            return Math.Max(1, (int)Math.Round(raw));
        }

        public static ProgressionSample GetDerivedSample(int level, EnemyData? enemyData = null)
        {
            var tuning = GameConfiguration.Instance;
            var enemySystem = tuning.EnemySystem;
            enemySystem.EnsureSanitizedDefaults();
            var prog = enemySystem.ProgressionScales;
            prog.EnsurePositiveScales();

            enemyData ??= ResolveSampleEnemy();
            var baseline = enemySystem.BaselineStats;
            var scaling = enemySystem.ScalingPerLevel;
            var archetype = enemySystem.Archetypes.GetValueOrDefault(enemyData.Archetype)
                ?? enemySystem.Archetypes.GetValueOrDefault("Berserker")
                ?? new ArchetypeMultipliersConfig();

            double baselineHp = baseline.Health;
            double healthPercent = enemyData.HealthPercent ?? (100.0 * archetype.Health);
            double baseHealth = baselineHp * (healthPercent / 100.0);
            double growthPercent = enemyData.HealthGrowthPercent
                ?? (baselineHp > 0 ? (scaling.Health / baselineHp) * 100.0 : 0.0);
            double growthHealth = baselineHp * (growthPercent / 100.0);

            int levelScaled = ComputeLevelScaledHealth(baseHealth, growthHealth, level, prog);
            int finalHp = ApplyFinalHealthMultipliers(levelScaled, enemySystem);

            return new ProgressionSample
            {
                Level = level,
                GrowthWeight = ComputeGrowthWeight(level, prog),
                EnemyHealth = finalHp,
                PlayerMaxHealth = EstimatePlayerMaxHealth(level)
            };
        }

        public static IReadOnlyList<int> GetPreviewSampleLevels() =>
            Enumerable.Range(1, 20).Select(i => i * 5).Prepend(1).Distinct().OrderBy(l => l).ToList();

        public static IReadOnlyList<int> GetDecadeAnchorLevels() =>
            new[] { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        private static EnemyData ResolveSampleEnemy()
        {
            EnemyLoader.LoadEnemies();
            var goblin = EnemyLoader.GetEnemyData("Goblin");
            if (goblin != null)
                return goblin;
            return new EnemyData { Archetype = "Berserker", HealthPercent = 100 };
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * Math.Clamp(t, 0, 1);

        private static int FloorToInt(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;
            return (int)Math.Floor(value);
        }
    }
}
