using System.Collections.Generic;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>CLI overrides for the fundamentals sims (FUNDMATRIX / FIGHTLOG / SEQLAB / value runners).</summary>
    public sealed class SimulationRunOverrides
    {
        public int? BattlesPerCombination { get; init; }
        public int? BattlesPerWeapon { get; init; }
        public int? NumberOfBattles { get; init; }
        public IReadOnlyList<int>? Levels { get; init; }
        public int? PlayerLevel { get; init; }
        public int? EnemyLevel { get; init; }
        public int? EncounterCount { get; init; }
        public string? WeaponType { get; init; }
        public string? EnemyType { get; init; }
        public string? ForcedCatalogAction { get; init; }
        public bool? ContinuePastZeroHp { get; init; }
        public int? NegativeHpFloor { get; init; }
        public int? RunsPerClass { get; init; }
        public int? MaxActionsPerRun { get; init; }
        public string? Classes { get; init; }
        /// <summary>Fixed RNG seed so before/after comparisons replay the same dice.</summary>
        public int? Seed { get; init; }
    }
}
