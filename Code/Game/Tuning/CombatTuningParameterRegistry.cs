using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning
{
    public enum CombatTuningLayer
    {
        Duration,
        WinRate,
        RollFeel,
        ComboAffordance,
        Goals
    }

    public enum CombatTuningTab
    {
        Core,
        HeroClasses,
        SpeedDefense,
        Equipment,
        EnemyStats,
        ProgressionCurve,
        Archetypes,
        StatusEffects,
        RewardsLoot,
        GoalsAnalysis
    }

    public enum CombatTuningValueKind
    {
        Integer,
        Double
    }

    /// <summary>
    /// Single curated combat balance knob wired to the config path combat systems read.
    /// </summary>
    public sealed class CombatTuningParameter
    {
        public string Id { get; init; } = "";
        public CombatTuningTab Tab { get; init; }
        public CombatTuningLayer Layer { get; init; }
        public string SubGroup { get; init; } = "";
        public string Label { get; init; } = "";
        public string Affects { get; init; } = "";
        public double Minimum { get; init; }
        public double Maximum { get; init; }
        public double TickFrequency { get; init; } = 1.0;
        public CombatTuningValueKind ValueKind { get; init; } = CombatTuningValueKind.Double;
        public bool UsesGameSettings { get; init; }

        /// <summary>False when the value is stored in config but no gameplay system reads it yet.</summary>
        public bool IsImplemented { get; init; } = true;

        /// <summary>Optional filter key for archetype or status-effect selector tabs.</summary>
        public string? FilterKey { get; init; }

        private readonly Func<double> getter;
        private readonly Action<double> setter;

        public CombatTuningParameter(
            string id,
            CombatTuningTab tab,
            CombatTuningLayer layer,
            string subGroup,
            string label,
            string affects,
            double minimum,
            double maximum,
            Func<double> getter,
            Action<double> setter,
            double tickFrequency = 1.0,
            CombatTuningValueKind valueKind = CombatTuningValueKind.Double,
            bool usesGameSettings = false,
            string? filterKey = null,
            bool isImplemented = true)
        {
            Id = id;
            Tab = tab;
            Layer = layer;
            SubGroup = subGroup;
            Label = label;
            Affects = affects;
            Minimum = minimum;
            Maximum = maximum;
            TickFrequency = tickFrequency;
            ValueKind = valueKind;
            UsesGameSettings = usesGameSettings;
            IsImplemented = isImplemented;
            FilterKey = filterKey;
            this.getter = getter;
            this.setter = setter;
        }

        public double GetValue() => getter();

        public void SetValue(double value)
        {
            if (ValueKind == CombatTuningValueKind.Integer)
                setter(Math.Round(value));
            else
                setter(value);
        }
    }

    /// <summary>
    /// Curated combat tuning parameters grouped by UI tab and balance layer.
    /// </summary>
    public static partial class CombatTuningParameterRegistry
    {
        private static readonly Lazy<IReadOnlyList<CombatTuningParameter>> Parameters =
            new Lazy<IReadOnlyList<CombatTuningParameter>>(BuildParameters);

        public static IReadOnlyList<CombatTuningParameter> All => Parameters.Value;

        public static CombatTuningParameter? GetById(string id) =>
            All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        public static IReadOnlyList<CombatTuningParameter> GetByLayer(CombatTuningLayer layer) =>
            All.Where(p => p.Layer == layer).ToList();

        public static IReadOnlyList<CombatTuningParameter> GetByTab(CombatTuningTab tab) =>
            All.Where(p => p.Tab == tab).ToList();

        public static BalanceDial GetDialForLayer(CombatTuningLayer layer) => layer switch
        {
            CombatTuningLayer.Duration => BalanceDial.Power,
            CombatTuningLayer.WinRate => BalanceDial.Power,
            CombatTuningLayer.RollFeel => BalanceDial.Variance,
            CombatTuningLayer.ComboAffordance => BalanceDial.Agency,
            CombatTuningLayer.Goals => BalanceDial.Scaling,
            _ => BalanceDial.Power
        };

        public static IReadOnlyList<string> GetSubGroupsForTab(CombatTuningTab tab) =>
            All.Where(p => p.Tab == tab)
                .Select(p => p.SubGroup)
                .Distinct()
                .ToList();

        public static void EnsureSanitizedDefaults()
        {
            var cfg = GameConfiguration.Instance;
            cfg.EnemySystem?.EnsureSanitizedDefaults();
            cfg.RollSystem?.EnsureValidDefaultThresholdBands();
            cfg.Combat?.EnsureValidCombatTimingDefaults();
            cfg.Combat?.EnsureValidCombatCriticalAndDamageDefaults();
            cfg.CombatBalance?.EnsureValidRollDamageAndCritDefaults();
            cfg.CombatBalance?.ActionMechanics?.EnsureValidDefaults();
            cfg.ComboSystem?.EnsureValidComboAmplifierDefaults();
            cfg.Attributes?.EnsureValidPlayerBaseStatDefaults();
            cfg.ClassBalance?.EnsureNonDegenerateClassMultipliers();
            cfg.WeaponScaling?.EnsureSanitizedDefaults();
            GameSettings.Instance.ValidateAndFix();
        }

        private static IReadOnlyList<CombatTuningParameter> BuildParameters()
        {
            var list = new List<CombatTuningParameter>();
            BuildCoreParameters(list);
            BuildHeroClassParameters(list);
            BuildSpeedDefenseParameters(list);
            BuildEquipmentParameters(list);
            BuildEnemyStatsParameters(list);
            BuildProgressionCurveParameters(list);
            BuildArchetypeParameters(list);
            BuildStatusEffectParameters(list);
            BuildRewardsLootParameters(list);
            BuildGoalsAnalysisParameters(list);
            return list;
        }

        internal static CombatTuningParameter IntParam(
            string id, CombatTuningTab tab, CombatTuningLayer layer, string subGroup,
            string label, string affects, int min, int max,
            Func<int> getter, Action<int> setter,
            bool usesGameSettings = false, string? filterKey = null, bool isImplemented = true) =>
            new(id, tab, layer, subGroup, label, affects, min, max,
                () => getter(), v => setter((int)v),
                tickFrequency: 1, valueKind: CombatTuningValueKind.Integer,
                usesGameSettings: usesGameSettings, filterKey: filterKey, isImplemented: isImplemented);

        internal static CombatTuningParameter DoubleParam(
            string id, CombatTuningTab tab, CombatTuningLayer layer, string subGroup,
            string label, string affects, double min, double max, double tick,
            Func<double> getter, Action<double> setter,
            bool usesGameSettings = false, string? filterKey = null, bool isImplemented = true) =>
            new(id, tab, layer, subGroup, label, affects, min, max, getter, setter, tick,
                CombatTuningValueKind.Double, usesGameSettings, filterKey, isImplemented);
    }
}
