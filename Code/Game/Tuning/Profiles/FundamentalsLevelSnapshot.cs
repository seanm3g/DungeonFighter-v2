using System;

namespace RPGGame.Tuning.Profiles
{
    public sealed class FundamentalsLevelSnapshot
    {
        public int Level { get; init; }
        public int EncounterCount { get; init; }
        public int SuccessfulEncounters { get; init; }
        public double MedianCombinedActions { get; init; }
        public double AverageCombinedActions { get; init; }
        public double MedianPlayerTurns { get; init; }
        public double MedianEnemyTurns { get; init; }
        public double WinRate { get; init; }

        public static FundamentalsLevelSnapshot FromResult(FundamentalsSimulationResult result) =>
            new()
            {
                Level = result.PlayerLevel,
                EncounterCount = result.EncounterCount,
                SuccessfulEncounters = result.SuccessfulEncounters,
                MedianCombinedActions = result.MedianActionsPerEncounter,
                AverageCombinedActions = result.AverageActionsPerEncounter,
                MedianPlayerTurns = result.MedianPlayerTurnsPerEncounter,
                MedianEnemyTurns = result.MedianEnemyTurnsPerEncounter,
                WinRate = result.WinRate
            };
    }
}
