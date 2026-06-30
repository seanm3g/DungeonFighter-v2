using System.Collections.Generic;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// Declares what to simulate and what validators/suggesters to run for a tuning pass.
    /// </summary>
    public sealed class BalanceTuningProfile
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public SimulationProfileConfig Simulation { get; set; } = new();
        public AnalysisProfileConfig Analysis { get; set; } = new();
    }

    public sealed class SimulationProfileConfig
    {
        /// <summary>
        /// multi_level_weapon_enemy | comprehensive_weapon_enemy
        /// </summary>
        public string Mode { get; set; } = "multi_level_weapon_enemy";

        public int BattlesPerCombination { get; set; } = 25;
        public int BattlesPerWeapon { get; set; } = 100;
        public int NumberOfBattles { get; set; } = 100;

        /// <summary>Same-level sweep points when mode is multi_level_weapon_enemy.</summary>
        public List<int>? Levels { get; set; }

        public int PlayerLevel { get; set; } = 10;
        public int EnemyLevel { get; set; } = 10;

        /// <summary>fundamentals_encounter: number of fights to run.</summary>
        public int EncounterCount { get; set; } = 500;

        /// <summary>fundamentals_encounter: loader enemy type; null = default test dummy.</summary>
        public string? EnemyType { get; set; }

        /// <summary>fundamentals_encounter: hero weapon type (Sword, Dagger, Mace, Wand).</summary>
        public string WeaponType { get; set; } = "Sword";

        /// <summary>fundamentals_encounter: catalog action forced on combo-eligible rolls; null = first strip action.</summary>
        public string? ForcedCatalogAction { get; set; }

        /// <summary>When true, fights continue past 0 HP to measure loss severity (simulation only).</summary>
        public bool ContinuePastZeroHp { get; set; }

        /// <summary>Stop developer sim when player HP falls to or below this value.</summary>
        public int NegativeHpFloor { get; set; } = -500;

        /// <summary>class_playthrough_batch: runs per weapon path.</summary>
        public int RunsPerClass { get; set; } = 10;

        /// <summary>class_playthrough_batch: max menu/combat actions before stopping a run.</summary>
        public int MaxActionsPerRun { get; set; } = 500;

        /// <summary>class_playthrough_batch: optional csv class filter.</summary>
        public string? Classes { get; set; }
    }

    public sealed class PlaythroughAnalysisTargets
    {
        public double MinMeanFinalLevel { get; set; } = 3.0;
        public double MinMeanDungeonsCompleted { get; set; } = 1.5;
        public double MaxMeanFinalLevel { get; set; } = 12.0;
        public double MaxLevelSpread { get; set; } = 1.5;
        public double MaxDungeonSpread { get; set; } = 1.0;
    }

    public sealed class FundamentalsAnalysisTargets
    {
        /// <summary>Target median hero actions per encounter (fundamentals sim).</summary>
        public double TargetMedianPlayerTurns { get; set; } = 12;
        /// <summary>Target median enemy actions per encounter (fundamentals sim).</summary>
        public double TargetMedianEnemyTurns { get; set; } = 12;
        /// <summary>Target median combined hero+enemy actions per encounter.</summary>
        public double TargetMedianCombinedActions { get; set; } = 24;
        /// <summary>Expected mean combined actions per encounter.</summary>
        public double MinAverageActions { get; set; } = 24;
        public double MaxAverageActions { get; set; } = 30;

        /// <summary>Mean count of completed combo+ chains (length ≥ 2) per encounter.</summary>
        public double MinAverageComboStreakRuns2Plus { get; set; } = 0.5;

        /// <summary>Mean longest uninterrupted combo+ chain per encounter.</summary>
        public double MinAverageMaxComboStreak { get; set; } = 2.0;

        /// <summary>± band for hero-turn tempo checks (default 1.5).</summary>
        public double TempoTolerance { get; set; } = 1.5;

        /// <summary>
        /// When true, only L1 knobs (player base HP, enemy base health scale) are adjusted until
        /// L1 combined tempo and level-curve win rate are both in band. Blocks tempo/shape/parity/health-per-level.
        /// </summary>
        public bool RequireL1AnchorBeforeScaling { get; set; }
    }

    public sealed class AnalysisProfileConfig
    {
        /// <summary>
        /// When false (default), win-rate validators/suggesters are skipped (level_curve, win_rate, comprehensive, global/player/enemy knobs).
        /// Set true in profile JSON to re-enable win-rate-driven tuning.
        /// </summary>
        public bool OptimizeWinRate { get; set; }

        /// <summary>
        /// Validator ids: level_curve, comprehensive, win_rate, combat_duration, weapon_variance, enemy_differentiation
        /// </summary>
        public List<string> Validators { get; set; } = new();

        /// <summary>
        /// Suggester ids: level_curve, global, player, enemy_baseline, weapon, enemy, duration
        /// </summary>
        public List<string> Suggesters { get; set; } = new();

        /// <summary>How many suggestions to keep (apply step uses the first).</summary>
        public int MaxSuggestions { get; set; } = 1;

        /// <summary>Optional targets when using fundamentals_* validators.</summary>
        public FundamentalsAnalysisTargets? FundamentalsTargets { get; set; }

        /// <summary>When true, route suggesters by classified balance dial (Power/Variance/Agency/Scaling).</summary>
        public bool EnableDialRouting { get; set; }

        /// <summary>Targets when using playthrough_* validators.</summary>
        public PlaythroughAnalysisTargets? PlaythroughTargets { get; set; }
    }

    public static class TuningSimulationModes
    {
        public const string MultiLevelWeaponEnemy = "multi_level_weapon_enemy";
        public const string ComprehensiveWeaponEnemy = "comprehensive_weapon_enemy";
        public const string FundamentalsEncounter = "fundamentals_encounter";
        public const string ClassBuildMatrix = "class_build_matrix";
        public const string DungeonScaling = "dungeon_scaling";
        public const string ClassPlaythroughBatch = "class_playthrough_batch";
    }

    public static class TuningValidatorIds
    {
        public const string LevelCurve = "level_curve";
        public const string Comprehensive = "comprehensive";
        public const string WinRate = "win_rate";
        public const string CombatDuration = "combat_duration";
        public const string WeaponVariance = "weapon_variance";
        public const string EnemyDifferentiation = "enemy_differentiation";
        public const string FundamentalsTempo = "fundamentals_tempo";
        public const string FundamentalsAnchors = "fundamentals_anchors";
        public const string FundamentalsComboStreaks = "fundamentals_combo_streaks";
        public const string DialVariance = "dial_variance";
        public const string DialAgency = "dial_agency";
        public const string DungeonScaling = "dungeon_scaling";
        public const string ClassBuildMatrix = "class_build_matrix";
        public const string PlaythroughProgression = "playthrough_progression";
        public const string PlaythroughClassParity = "playthrough_class_parity";
        public const string EnvironmentHazards = "environment_hazards";
        public const string GearProbability = "gear_probability";
        public const string EnemyRoster = "enemy_roster";
    }

    public static class TuningSuggesterIds
    {
        public const string LevelCurve = "level_curve";
        public const string Global = "global";
        public const string Player = "player";
        public const string EnemyBaseline = "enemy_baseline";
        public const string Weapon = "weapon";
        public const string Enemy = "enemy";
        public const string Duration = "duration";
        public const string DialRouted = "dial_routed";
        public const string Variance = "variance";
        public const string Agency = "agency";
        public const string DungeonScaling = "dungeon_scaling";
        public const string PlaythroughBalance = "playthrough_balance";
    }
}
