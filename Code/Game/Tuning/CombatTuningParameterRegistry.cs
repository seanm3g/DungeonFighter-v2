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
        public CombatTuningLayer Layer { get; init; }
        public string Label { get; init; } = "";
        public string Affects { get; init; } = "";
        public double Minimum { get; init; }
        public double Maximum { get; init; }
        public double TickFrequency { get; init; } = 1.0;
        public CombatTuningValueKind ValueKind { get; init; } = CombatTuningValueKind.Double;
        public bool UsesGameSettings { get; init; }

        private readonly Func<double> getter;
        private readonly Action<double> setter;

        public CombatTuningParameter(
            string id,
            CombatTuningLayer layer,
            string label,
            string affects,
            double minimum,
            double maximum,
            Func<double> getter,
            Action<double> setter,
            double tickFrequency = 1.0,
            CombatTuningValueKind valueKind = CombatTuningValueKind.Double,
            bool usesGameSettings = false)
        {
            Id = id;
            Layer = layer;
            Label = label;
            Affects = affects;
            Minimum = minimum;
            Maximum = maximum;
            TickFrequency = tickFrequency;
            ValueKind = valueKind;
            UsesGameSettings = usesGameSettings;
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
    /// Curated combat tuning parameters grouped by balance layer (duration → win rate → roll → combo → goals).
    /// </summary>
    public static class CombatTuningParameterRegistry
    {
        private static readonly Lazy<IReadOnlyList<CombatTuningParameter>> Parameters =
            new Lazy<IReadOnlyList<CombatTuningParameter>>(BuildParameters);

        public static IReadOnlyList<CombatTuningParameter> All => Parameters.Value;

        public static CombatTuningParameter? GetById(string id) =>
            All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        public static IReadOnlyList<CombatTuningParameter> GetByLayer(CombatTuningLayer layer) =>
            All.Where(p => p.Layer == layer).ToList();

        public static void EnsureSanitizedDefaults()
        {
            GameConfiguration.Instance.EnemySystem?.EnsureSanitizedDefaults();
            GameConfiguration.Instance.RollSystem?.EnsureValidDefaultThresholdBands();
            GameSettings.Instance.ValidateAndFix();
        }

        private static IReadOnlyList<CombatTuningParameter> BuildParameters()
        {
            var list = new List<CombatTuningParameter>();
            var cfg = () => GameConfiguration.Instance;
            var gs = () => GameSettings.Instance;

            // Layer 1 — Combat duration
            list.Add(IntParam("playerBaseHealth", CombatTuningLayer.Duration, "Player base health",
                "Turns to kill / player survivability at level 1", 20, 200,
                () => cfg().Character.PlayerBaseHealth, v => cfg().Character.PlayerBaseHealth = (int)v));
            list.Add(IntParam("playerHealthPerLevel", CombatTuningLayer.Duration, "Player health per level",
                "Combat length scaling with player level", 0, 20,
                () => cfg().Character.HealthPerLevel, v => cfg().Character.HealthPerLevel = (int)v));
            list.Add(IntParam("enemyBaselineHealth", CombatTuningLayer.Duration, "Enemy baseline health",
                "Base enemy HP before archetype and level growth", 10, 200,
                () => cfg().EnemySystem.BaselineStats.Health, v => cfg().EnemySystem.BaselineStats.Health = (int)v));
            list.Add(IntParam("enemyHealthPerLevel", CombatTuningLayer.Duration, "Enemy health per level",
                "Enemy HP growth per level", 0, 30,
                () => cfg().EnemySystem.ScalingPerLevel.Health, v => cfg().EnemySystem.ScalingPerLevel.Health = (int)v));
            list.Add(DoubleParam("globalEnemyHealthMult", CombatTuningLayer.Duration, "Global enemy health multiplier",
                "All spawned enemy max HP", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.HealthMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.HealthMultiplier = v));

            // Layer 2 — Win rate
            list.Add(IntParam("playerBaseStrength", CombatTuningLayer.WinRate, "Player base strength",
                "Melee damage baseline", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Strength, v => cfg().Attributes.PlayerBaseAttributes.Strength = (int)v));
            list.Add(IntParam("playerBaseAgility", CombatTuningLayer.WinRate, "Player base agility",
                "Attack speed baseline", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Agility, v => cfg().Attributes.PlayerBaseAttributes.Agility = (int)v));
            list.Add(IntParam("playerBaseTechnique", CombatTuningLayer.WinRate, "Player base technique",
                "Combo threshold milestones", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Technique, v => cfg().Attributes.PlayerBaseAttributes.Technique = (int)v));
            list.Add(IntParam("playerBaseIntelligence", CombatTuningLayer.WinRate, "Player base intelligence",
                "Combo amplification baseline", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Intelligence, v => cfg().Attributes.PlayerBaseAttributes.Intelligence = (int)v));
            list.Add(IntParam("playerAttributesPerLevel", CombatTuningLayer.WinRate, "Player attributes per level",
                "Stat growth per level-up", 0, 5,
                () => cfg().Attributes.PlayerAttributesPerLevel, v => cfg().Attributes.PlayerAttributesPerLevel = (int)v));
            list.Add(IntParam("enemyBaselineStrength", CombatTuningLayer.WinRate, "Enemy baseline strength",
                "Enemy damage baseline", 1, 30,
                () => cfg().EnemySystem.BaselineStats.Strength, v => cfg().EnemySystem.BaselineStats.Strength = (int)v));
            list.Add(IntParam("enemyBaselineArmor", CombatTuningLayer.WinRate, "Enemy baseline armor",
                "Flat damage reduction on enemies", 0, 30,
                () => cfg().EnemySystem.BaselineStats.Armor, v => cfg().EnemySystem.BaselineStats.Armor = (int)v));
            list.Add(DoubleParam("globalEnemyDamageMult", CombatTuningLayer.WinRate, "Global enemy damage multiplier",
                "Enemy STR/TEC/INT scaling at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.DamageMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.DamageMultiplier = v));
            list.Add(DoubleParam("globalEnemyArmorMult", CombatTuningLayer.WinRate, "Global enemy armor multiplier",
                "Enemy armor at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.ArmorMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.ArmorMultiplier = v));

            list.Add(DoubleParam("runtimePlayerHealthMult", CombatTuningLayer.WinRate, "Runtime player health multiplier",
                "Difficulty overlay on current player HP", 0.5, 3.0, 0.1,
                () => gs().PlayerHealthMultiplier, v => gs().PlayerHealthMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimePlayerDamageMult", CombatTuningLayer.WinRate, "Runtime player damage multiplier",
                "Difficulty overlay on player damage", 0.5, 3.0, 0.1,
                () => gs().PlayerDamageMultiplier, v => gs().PlayerDamageMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimeEnemyHealthMult", CombatTuningLayer.WinRate, "Runtime enemy health multiplier",
                "Difficulty overlay on spawned enemy HP", 0.5, 3.0, 0.1,
                () => gs().EnemyHealthMultiplier, v => gs().EnemyHealthMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimeEnemyDamageMult", CombatTuningLayer.WinRate, "Runtime enemy damage multiplier",
                "Difficulty overlay on enemy damage per swing", 0.5, 3.0, 0.1,
                () => gs().EnemyDamageMultiplier, v => gs().EnemyDamageMultiplier = v, usesGameSettings: true));

            // Layer 3 — Roll feel
            list.Add(IntParam("missThresholdMax", CombatTuningLayer.RollFeel, "Miss threshold max (1–N = miss)",
                "Miss chance band width", 1, 10,
                () => cfg().RollSystem.MissThreshold.Max, v => cfg().RollSystem.MissThreshold.Max = (int)v));
            list.Add(IntParam("basicAttackMin", CombatTuningLayer.RollFeel, "Basic attack roll min",
                "Normal hit band lower bound", 1, 20,
                () => cfg().RollSystem.BasicAttackThreshold.Min, v => cfg().RollSystem.BasicAttackThreshold.Min = (int)v));
            list.Add(IntParam("basicAttackMax", CombatTuningLayer.RollFeel, "Basic attack roll max",
                "Normal hit band upper bound", 1, 20,
                () => cfg().RollSystem.BasicAttackThreshold.Max, v => cfg().RollSystem.BasicAttackThreshold.Max = (int)v));
            list.Add(IntParam("comboThresholdMin", CombatTuningLayer.RollFeel, "Combo roll min",
                "Combo-tier band lower bound", 1, 20,
                () => cfg().RollSystem.ComboThreshold.Min, v => cfg().RollSystem.ComboThreshold.Min = (int)v));
            list.Add(IntParam("comboThresholdMax", CombatTuningLayer.RollFeel, "Combo roll max",
                "Combo-tier band upper bound", 1, 20,
                () => cfg().RollSystem.ComboThreshold.Max, v => cfg().RollSystem.ComboThreshold.Max = (int)v));
            list.Add(DoubleParam("basicRollDamageMult", CombatTuningLayer.RollFeel, "Basic roll damage multiplier",
                "Damage on normal hit band", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier = v));
            list.Add(DoubleParam("comboRollDamageMult", CombatTuningLayer.RollFeel, "Combo roll damage multiplier",
                "Damage on combo-tier rolls", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier = v));
            list.Add(DoubleParam("criticalHitDamageMult", CombatTuningLayer.RollFeel, "Critical hit damage multiplier",
                "Damage on natural crit rolls", 1.0, 5.0, 0.1,
                () => cfg().CombatBalance.CriticalHitDamageMultiplier,
                v => cfg().CombatBalance.CriticalHitDamageMultiplier = v));
            list.Add(IntParam("minimumDamage", CombatTuningLayer.RollFeel, "Minimum damage floor",
                "Lowest damage after armor", 1, 10,
                () => cfg().Combat.MinimumDamage, v => cfg().Combat.MinimumDamage = (int)v));

            // Layer 4 — Combo affordance
            list.Add(IntParam("comboSequenceBaseMax", CombatTuningLayer.ComboAffordance, "Combo sequence base max",
                "Default combo strip length before gear", 1, 8,
                () => cfg().LootSystem.ComboSequenceBaseMax, v => cfg().LootSystem.ComboSequenceBaseMax = (int)v));
            list.Add(IntParam("comboSequenceAbsoluteMax", CombatTuningLayer.ComboAffordance, "Combo sequence absolute max",
                "Hard cap on combo strip length", 1, 12,
                () => cfg().LootSystem.ComboSequenceAbsoluteMax, v => cfg().LootSystem.ComboSequenceAbsoluteMax = (int)v));

            // Layer 5 — Goals
            var goals = () => cfg().BalanceTuningGoals;
            list.Add(DoubleParam("winRateMinTarget", CombatTuningLayer.Goals, "Win rate min target (%)",
                "Simulation validation lower bound", 50, 100, 1,
                () => goals().WinRate.MinTarget, v => goals().WinRate.MinTarget = v));
            list.Add(DoubleParam("winRateMaxTarget", CombatTuningLayer.Goals, "Win rate max target (%)",
                "Simulation validation upper bound", 50, 100, 1,
                () => goals().WinRate.MaxTarget, v => goals().WinRate.MaxTarget = v));
            list.Add(DoubleParam("winRateOptimalMin", CombatTuningLayer.Goals, "Win rate optimal min (%)",
                "Quality score sweet spot lower", 50, 100, 1,
                () => goals().WinRate.OptimalMin, v => goals().WinRate.OptimalMin = v));
            list.Add(DoubleParam("winRateOptimalMax", CombatTuningLayer.Goals, "Win rate optimal max (%)",
                "Quality score sweet spot upper", 50, 100, 1,
                () => goals().WinRate.OptimalMax, v => goals().WinRate.OptimalMax = v));
            list.Add(DoubleParam("durationMinTarget", CombatTuningLayer.Goals, "Combat duration min (turns)",
                "Too-short fight threshold", 3, 20, 1,
                () => goals().CombatDuration.MinTarget, v => goals().CombatDuration.MinTarget = v));
            list.Add(DoubleParam("durationMaxTarget", CombatTuningLayer.Goals, "Combat duration max (turns)",
                "Too-long fight threshold", 5, 30, 1,
                () => goals().CombatDuration.MaxTarget, v => goals().CombatDuration.MaxTarget = v));
            list.Add(DoubleParam("durationOptimalMin", CombatTuningLayer.Goals, "Combat duration optimal min",
                "Target fight length lower", 3, 20, 1,
                () => goals().CombatDuration.OptimalMin, v => goals().CombatDuration.OptimalMin = v));
            list.Add(DoubleParam("durationOptimalMax", CombatTuningLayer.Goals, "Combat duration optimal max",
                "Target fight length upper", 5, 30, 1,
                () => goals().CombatDuration.OptimalMax, v => goals().CombatDuration.OptimalMax = v));

            return list;
        }

        private static CombatTuningParameter IntParam(
            string id, CombatTuningLayer layer, string label, string affects,
            int min, int max, Func<int> getter, Action<int> setter) =>
            new(id, layer, label, affects, min, max,
                () => getter(), v => setter((int)v),
                tickFrequency: 1, valueKind: CombatTuningValueKind.Integer);

        private static CombatTuningParameter DoubleParam(
            string id, CombatTuningLayer layer, string label, string affects,
            double min, double max, double tick,
            Func<double> getter, Action<double> setter, bool usesGameSettings = false) =>
            new(id, layer, label, affects, min, max, getter, setter, tick,
                CombatTuningValueKind.Double, usesGameSettings);
    }
}
