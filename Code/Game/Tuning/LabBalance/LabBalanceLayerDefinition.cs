using System;
using System.Collections.Generic;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning.LabBalance
{
    public enum LabBalanceStatsMode
    {
        Encounter,
        MultiAnchorEncounter,
        WeaponMatrixEncounter,
        Dungeon,
        DeepLinkOnly
    }

    /// <summary>Which PASS/FAIL checks a Balance Layers process step evaluates.</summary>
    [Flags]
    public enum LabBalanceCheckFlags
    {
        None = 0,
        /// <summary>Median/mean combined and hero/enemy turn bands from <see cref="LabBalanceLayerDefinition.Targets"/>.</summary>
        Duration = 1 << 0,
        /// <summary>Combo streak floors from Targets.</summary>
        Combo = 1 << 1,
        /// <summary>Turn std-dev ceiling (Feel).</summary>
        FeelVariance = 1 << 2,
        /// <summary>Fixed win-rate band (WinRateMin–WinRateMax).</summary>
        WinRateFixed = 1 << 3,
        /// <summary>Win rate vs <see cref="LevelWinRateCurve"/> at the scenario/anchor level.</summary>
        WinRateCurve = 1 << 4,
        /// <summary>Best−worst weapon WR spread ≤ MaxWeaponWinRateSpread.</summary>
        WeaponSpread = 1 << 5,
        /// <summary>Level curve: evaluate every anchor, not only the last report.</summary>
        AllAnchors = 1 << 6,
        /// <summary>Dungeon clear-rate band.</summary>
        DungeonClear = 1 << 7
    }

    /// <summary>Static recipe for one process layer: scenario, knobs, targets, stats mode.</summary>
    public sealed class LabBalanceLayerDefinition
    {
        public LabBalanceProcessLayer Layer { get; init; }
        public string DisplayName { get; init; } = "";
        public string Description { get; init; } = "";
        /// <summary>One-line: what “OK” means for this process step.</summary>
        public string GoalIntent { get; set; } = "";
        public LabBalanceStatsMode StatsMode { get; init; }
        public IReadOnlyList<CombatTuningLayer> RegistryLayers { get; init; } = Array.Empty<CombatTuningLayer>();
        public string? WorkbenchProfileId { get; init; }
        /// <summary>Duration / combo numeric bands (also fed to duration suggesters). Editable in Balance Layers UI.</summary>
        public FundamentalsAnalysisTargets Targets { get; set; } = new();
        public LabBalanceCheckFlags CheckFlags { get; init; }
        public double MaxTurnStdDev { get; set; } = 8.0;
        public double WinRateMin { get; set; } = 0.85;
        public double WinRateMax { get; set; } = 0.98;
        public double MaxWeaponWinRateSpread { get; set; } = 0.12;
        public double MinDungeonClearRate { get; set; } = 0.70;
        public double MaxDungeonClearRate { get; set; } = 0.95;
        public bool StripNonWeaponGear { get; init; } = true;
        public bool ContinuePastZeroHp { get; init; }
        public bool RequireL1AnchorBeforeScaling { get; init; }
        public string DefaultWeaponType { get; init; } = "Sword";
        public string DefaultEnemyType { get; init; } = FundamentalsCombatSetup.DefaultFundamentalsEnemyType;
        public int DefaultPlayerLevel { get; init; } = 1;
        public int DefaultEnemyLevel { get; init; } = 1;
        public IReadOnlyList<int> LevelAnchors { get; init; } = Array.Empty<int>();
        public IReadOnlyList<string> WeaponTypes { get; init; } = Array.Empty<string>();

        public bool HasFlag(LabBalanceCheckFlags flag) => (CheckFlags & flag) != 0;

        public static IReadOnlyList<LabBalanceLayerDefinition> All { get; } = BuildAll();

        public static LabBalanceLayerDefinition Get(LabBalanceProcessLayer layer) =>
            All[(int)layer];

        private static IReadOnlyList<LabBalanceLayerDefinition> BuildAll()
        {
            var list = new LabBalanceLayerDefinition[Enum.GetValues<LabBalanceProcessLayer>().Length];

            list[(int)LabBalanceProcessLayer.CombatEquation] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.CombatEquation,
                DisplayName = "1. Combat equation",
                Description = "Weapon-only L1 vs Goblin. Tune global HP/damage until fight length is right.",
                GoalIntent = "L1 weapon-only tempo: fights land near 27 combined actions (±1.5).",
                StatsMode = LabBalanceStatsMode.Encounter,
                RegistryLayers = new[] { CombatTuningLayer.Duration },
                WorkbenchProfileId = "combat-dials",
                StripNonWeaponGear = true,
                DefaultPlayerLevel = 1,
                DefaultEnemyLevel = 1,
                RequireL1AnchorBeforeScaling = true,
                Targets = CreateDurationTargets(level: 1, requireL1: true, includeCombo: false),
                CheckFlags = LabBalanceCheckFlags.Duration
            };

            list[(int)LabBalanceProcessLayer.Feel] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.Feel,
                DisplayName = "2. Feel (variance / agency)",
                Description = "Same equation matchup. Tune roll feel and combo affordance.",
                GoalIntent = "L1 feel: keep tempo, combo streaks alive, turn std-dev ≤ 8.",
                StatsMode = LabBalanceStatsMode.Encounter,
                RegistryLayers = new[] { CombatTuningLayer.RollFeel, CombatTuningLayer.ComboAffordance },
                WorkbenchProfileId = "combat-dials",
                StripNonWeaponGear = true,
                ContinuePastZeroHp = true,
                DefaultPlayerLevel = 1,
                DefaultEnemyLevel = 1,
                Targets = CreateDurationTargets(level: 1, requireL1: true, includeCombo: true),
                CheckFlags = LabBalanceCheckFlags.Duration
                             | LabBalanceCheckFlags.Combo
                             | LabBalanceCheckFlags.FeelVariance,
                MaxTurnStdDev = 8.0
            };

            list[(int)LabBalanceProcessLayer.LevelCurve] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.LevelCurve,
                DisplayName = "3. Level curve",
                Description = "Same matchup at level anchors. Tune progression / growth knobs.",
                GoalIntent = "Every anchor: WR on level curve + tempo near 27 (wider ± at high levels).",
                StatsMode = LabBalanceStatsMode.MultiAnchorEncounter,
                RegistryLayers = new[] { CombatTuningLayer.WinRate, CombatTuningLayer.Goals },
                WorkbenchProfileId = "level-curve",
                StripNonWeaponGear = true,
                LevelAnchors = new[] { 1, 10, 25, 50, 75, 100 },
                DefaultPlayerLevel = 1,
                DefaultEnemyLevel = 1,
                // Targets used as the L1 reference; per-anchor eval widens tolerance by level.
                Targets = CreateDurationTargets(level: 1, requireL1: false, includeCombo: false),
                CheckFlags = LabBalanceCheckFlags.Duration
                             | LabBalanceCheckFlags.WinRateCurve
                             | LabBalanceCheckFlags.AllAnchors
            };

            list[(int)LabBalanceProcessLayer.WeaponParity] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.WeaponParity,
                DisplayName = "4. Weapon parity",
                Description = "Matrix of weapon paths vs reference enemy at a fixed level.",
                GoalIntent = "L10 weapon paths: each WR on curve, best−worst spread ≤ 12pp, tempo held.",
                StatsMode = LabBalanceStatsMode.WeaponMatrixEncounter,
                RegistryLayers = new[] { CombatTuningLayer.WinRate },
                WorkbenchProfileId = "weapon-focus",
                StripNonWeaponGear = true,
                DefaultPlayerLevel = 10,
                DefaultEnemyLevel = 10,
                WeaponTypes = new[] { "Mace", "Sword", "Dagger", "Wand" },
                Targets = CreateDurationTargets(level: 10, requireL1: false, includeCombo: false),
                CheckFlags = LabBalanceCheckFlags.Duration
                             | LabBalanceCheckFlags.WinRateCurve
                             | LabBalanceCheckFlags.WeaponSpread,
                MaxWeaponWinRateSpread = 0.12
            };

            list[(int)LabBalanceProcessLayer.EnemyRoster] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.EnemyRoster,
                DisplayName = "5. Enemy roster",
                Description = "Keep hero; switch foes in catalog. Tune archetypes / enemy baseline.",
                GoalIntent = "L25 vs catalog foe: WR on curve + midgame tempo (27 ± 2.5).",
                StatsMode = LabBalanceStatsMode.Encounter,
                RegistryLayers = new[] { CombatTuningLayer.WinRate, CombatTuningLayer.Duration },
                WorkbenchProfileId = "enemy-roster",
                StripNonWeaponGear = true,
                DefaultPlayerLevel = 25,
                DefaultEnemyLevel = 25,
                Targets = CreateDurationTargets(level: 25, requireL1: false, includeCombo: false),
                CheckFlags = LabBalanceCheckFlags.Duration | LabBalanceCheckFlags.WinRateCurve
            };

            list[(int)LabBalanceProcessLayer.GearInjection] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.GearInjection,
                DisplayName = "6. Gear injection",
                Description = "Keep / equip lab gear. Compare power vs naked equation baseline.",
                GoalIntent = "L10 with gear: WR 90–99% (power spike) but fights not trivial (mean ≥ 18).",
                StatsMode = LabBalanceStatsMode.Encounter,
                RegistryLayers = new[] { CombatTuningLayer.WinRate },
                WorkbenchProfileId = "midgame-balance",
                StripNonWeaponGear = false,
                DefaultPlayerLevel = 10,
                DefaultEnemyLevel = 10,
                Targets = CreateGearInjectionTargets(),
                CheckFlags = LabBalanceCheckFlags.Duration | LabBalanceCheckFlags.WinRateFixed,
                WinRateMin = 0.90,
                WinRateMax = 0.99
            };

            list[(int)LabBalanceProcessLayer.DungeonAttrition] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.DungeonAttrition,
                DisplayName = "7. Dungeon attrition",
                Description = "Seeded dungeon clear batches. Tune density via Workbench if knobs are sparse.",
                GoalIntent = "Dungeon clear rate 70–95% (attrition without brick walls).",
                StatsMode = LabBalanceStatsMode.Dungeon,
                RegistryLayers = new[] { CombatTuningLayer.Goals },
                WorkbenchProfileId = "dungeon-scaling",
                StripNonWeaponGear = false,
                DefaultPlayerLevel = 10,
                DefaultEnemyLevel = 10,
                Targets = CreateDurationTargets(level: 10, requireL1: false, includeCombo: false),
                CheckFlags = LabBalanceCheckFlags.DungeonClear,
                MinDungeonClearRate = 0.70,
                MaxDungeonClearRate = 0.95
            };

            list[(int)LabBalanceProcessLayer.PlaythroughCheck] = new LabBalanceLayerDefinition
            {
                Layer = LabBalanceProcessLayer.PlaythroughCheck,
                DisplayName = "8. Playthrough check",
                Description = "Full class playthrough lives on Balance Tuning Workbench — open profile from here.",
                GoalIntent = "Full class playthrough validation on Workbench (no Lab checklist).",
                StatsMode = LabBalanceStatsMode.DeepLinkOnly,
                RegistryLayers = Array.Empty<CombatTuningLayer>(),
                WorkbenchProfileId = "class-playthrough-balance",
                StripNonWeaponGear = false,
                Targets = CreateDurationTargets(level: 1, requireL1: false, includeCombo: false),
                CheckFlags = LabBalanceCheckFlags.None
            };

            return list;
        }

        /// <summary>
        /// Tempo stays near 27 combined across levels (fight-length parity); tolerance widens with level.
        /// </summary>
        public static FundamentalsAnalysisTargets CreateDurationTargets(
            int level, bool requireL1, bool includeCombo)
        {
            double tol = TempoToleranceForLevel(level);
            double meanPad = Math.Max(0, tol - 1.5);
            return new FundamentalsAnalysisTargets
            {
                TargetMedianPlayerTurns = 12,
                TargetMedianEnemyTurns = 12,
                TargetMedianCombinedActions = 27,
                MinAverageActions = 24 - meanPad,
                MaxAverageActions = 30 + meanPad,
                MinAverageComboStreakRuns2Plus = includeCombo ? 0.5 : 0,
                MinAverageMaxComboStreak = includeCombo ? 2.0 : 0,
                TempoTolerance = tol,
                RequireL1AnchorBeforeScaling = requireL1
            };
        }

        public static FundamentalsAnalysisTargets CreateGearInjectionTargets() =>
            new()
            {
                // Gear may shorten fights; floor mean so power spikes do not trivialize.
                TargetMedianPlayerTurns = 10,
                TargetMedianEnemyTurns = 10,
                TargetMedianCombinedActions = 22,
                MinAverageActions = 18,
                MaxAverageActions = 30,
                MinAverageComboStreakRuns2Plus = 0,
                MinAverageMaxComboStreak = 0,
                TempoTolerance = 3.0,
                RequireL1AnchorBeforeScaling = false
            };

        public static double TempoToleranceForLevel(int level) =>
            level <= 1 ? 1.5 :
            level <= 10 ? 2.0 :
            level <= 25 ? 2.5 :
            3.0;

        /// <summary>Restores factory checklist bands for a layer (Balance Layers “Reset targets”).</summary>
        public static void ResetEditableTargets(LabBalanceProcessLayer layer)
        {
            var def = Get(layer);
            switch (layer)
            {
                case LabBalanceProcessLayer.CombatEquation:
                    def.Targets = CreateDurationTargets(1, requireL1: true, includeCombo: false);
                    break;
                case LabBalanceProcessLayer.Feel:
                    def.Targets = CreateDurationTargets(1, requireL1: true, includeCombo: true);
                    def.MaxTurnStdDev = 8.0;
                    break;
                case LabBalanceProcessLayer.LevelCurve:
                    def.Targets = CreateDurationTargets(1, requireL1: false, includeCombo: false);
                    break;
                case LabBalanceProcessLayer.WeaponParity:
                    def.Targets = CreateDurationTargets(10, requireL1: false, includeCombo: false);
                    def.MaxWeaponWinRateSpread = 0.12;
                    break;
                case LabBalanceProcessLayer.EnemyRoster:
                    def.Targets = CreateDurationTargets(25, requireL1: false, includeCombo: false);
                    break;
                case LabBalanceProcessLayer.GearInjection:
                    def.Targets = CreateGearInjectionTargets();
                    def.WinRateMin = 0.90;
                    def.WinRateMax = 0.99;
                    break;
                case LabBalanceProcessLayer.DungeonAttrition:
                    def.Targets = CreateDurationTargets(10, requireL1: false, includeCombo: false);
                    def.MinDungeonClearRate = 0.70;
                    def.MaxDungeonClearRate = 0.95;
                    break;
                default:
                    def.Targets = CreateDurationTargets(1, requireL1: false, includeCombo: false);
                    break;
            }
        }

        public static FundamentalsAnalysisTargets CloneTargets(FundamentalsAnalysisTargets src) =>
            new()
            {
                TargetMedianPlayerTurns = src.TargetMedianPlayerTurns,
                TargetMedianEnemyTurns = src.TargetMedianEnemyTurns,
                TargetMedianCombinedActions = src.TargetMedianCombinedActions,
                MinAverageActions = src.MinAverageActions,
                MaxAverageActions = src.MaxAverageActions,
                MinAverageComboStreakRuns2Plus = src.MinAverageComboStreakRuns2Plus,
                MinAverageMaxComboStreak = src.MinAverageMaxComboStreak,
                TempoTolerance = src.TempoTolerance,
                RequireL1AnchorBeforeScaling = src.RequireL1AnchorBeforeScaling
            };
    }
}
