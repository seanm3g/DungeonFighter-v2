using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>Decade evaluation points for fundamentals tuning (L1 base HP + enemy scaling curve).</summary>
    public static class FundamentalsLevelAnchors
    {
        public static IReadOnlyList<int> GetDecadeLevels() =>
            new[] { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        public static IReadOnlyList<int> ResolveEvaluationLevels(
            SimulationProfileConfig sim,
            SimulationRunOverrides? overrides)
        {
            if (overrides?.Levels != null && overrides.Levels.Count > 0)
                return overrides.Levels.Distinct().OrderBy(l => l).ToList();

            if (sim.Levels != null && sim.Levels.Count > 0)
                return sim.Levels.Distinct().OrderBy(l => l).ToList();

            if (overrides?.PlayerLevel != null)
                return new[] { Math.Clamp(overrides.PlayerLevel.Value, 1, 99) };

            return GetDecadeLevels();
        }

        public static bool IsDecadeAnchor(int level) => GetDecadeLevels().Contains(level);
    }
}
