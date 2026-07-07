using System;
using System.Collections.Generic;
using RPGGame.Config;

namespace RPGGame.Tuning
{
    public static partial class CombatTuningParameterRegistry
    {
        private static readonly string[] ArchetypeNames =
            { "Berserker", "Guardian", "Assassin", "Brute", "Mage", "Acrobat" };

        private static readonly string[] StatusEffectNames =
            { "Bleed", "Burn", "Freeze", "Stun", "Poison" };

        private static void BuildCoreParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            var gs = () => GameSettings.Instance;
            var goals = () => cfg().BalanceTuningGoals;
            const CombatTuningTab tab = CombatTuningTab.Core;

            const int tuningMaxBaseHealth = 1000000;
            const int tuningMaxHealthPerLevel = 100000;

            list.Add(IntParam("enemyBaselineHealth", tab, CombatTuningLayer.Duration, "Duration",
                "Enemy base health", "Global enemy HP anchor at level 1", 10, tuningMaxBaseHealth,
                () => cfg().EnemySystem.BaselineStats.Health, v => cfg().EnemySystem.BaselineStats.Health = v));
            list.Add(IntParam("enemyHealthPerLevel", tab, CombatTuningLayer.Duration, "Duration",
                "Enemy health per level", "Enemy HP growth per level", 0, tuningMaxHealthPerLevel,
                () => cfg().EnemySystem.ScalingPerLevel.Health, v => cfg().EnemySystem.ScalingPerLevel.Health = v));
            list.Add(DoubleParam("globalEnemyHealthMult", tab, CombatTuningLayer.Duration, "Duration",
                "Global enemy health multiplier", "All spawned enemy max HP", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.HealthMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.HealthMultiplier = v));
            list.Add(IntParam("enemyBaselineAgility", tab, CombatTuningLayer.Duration, "Duration",
                "Enemy baseline agility", "Enemy AGI anchor at level 1", 1, 30,
                () => cfg().EnemySystem.BaselineStats.Agility, v => cfg().EnemySystem.BaselineStats.Agility = v));
            list.Add(IntParam("enemyBaselineTechnique", tab, CombatTuningLayer.Duration, "Duration",
                "Enemy baseline technique", "Enemy TEC anchor at level 1", 1, 30,
                () => cfg().EnemySystem.BaselineStats.Technique, v => cfg().EnemySystem.BaselineStats.Technique = v));
            list.Add(IntParam("enemyBaselineIntelligence", tab, CombatTuningLayer.Duration, "Duration",
                "Enemy baseline intelligence", "Enemy INT anchor at level 1", 1, 30,
                () => cfg().EnemySystem.BaselineStats.Intelligence, v => cfg().EnemySystem.BaselineStats.Intelligence = v));
            // Win rate
            list.Add(IntParam("playerBaseStrength", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Player base strength", "Melee damage baseline", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Strength, v => cfg().Attributes.PlayerBaseAttributes.Strength = v));
            list.Add(IntParam("playerBaseAgility", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Player base agility", "Attack speed baseline", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Agility, v => cfg().Attributes.PlayerBaseAttributes.Agility = v));
            list.Add(IntParam("playerBaseTechnique", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Player base technique", "Combo threshold milestones", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Technique, v => cfg().Attributes.PlayerBaseAttributes.Technique = v));
            list.Add(IntParam("playerBaseIntelligence", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Player base intelligence", "Combo amplification baseline", 1, 30,
                () => cfg().Attributes.PlayerBaseAttributes.Intelligence, v => cfg().Attributes.PlayerBaseAttributes.Intelligence = v));
            list.Add(IntParam("playerAttributesPerLevel", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Player attributes per level", "Legacy stat growth per level-up", 0, 5,
                () => cfg().Attributes.PlayerAttributesPerLevel, v => cfg().Attributes.PlayerAttributesPerLevel = v));
            list.Add(IntParam("enemyBaselineStrength", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy baseline strength", "Enemy damage baseline", 1, 30,
                () => cfg().EnemySystem.BaselineStats.Strength, v => cfg().EnemySystem.BaselineStats.Strength = v));
            list.Add(IntParam("enemyBaselineArmor", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy baseline armor", "Flat damage reduction on enemies", 0, 30,
                () => cfg().EnemySystem.BaselineStats.Armor, v => cfg().EnemySystem.BaselineStats.Armor = v));
            list.Add(DoubleParam("globalEnemyDamageMult", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Global enemy damage multiplier", "Enemy STR/TEC/INT scaling at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.DamageMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.DamageMultiplier = v));
            list.Add(DoubleParam("enemyAttributesPerLevel", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy attributes per level", "Global enemy attribute growth per level", 0, 10, 0.1,
                () => cfg().EnemySystem.ScalingPerLevel.Attributes,
                v => cfg().EnemySystem.ScalingPerLevel.Attributes = v));
            list.Add(DoubleParam("enemyArmorPerLevel", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy armor per level", "Enemy armor growth per level", 0, 5, 0.05,
                () => cfg().EnemySystem.ScalingPerLevel.Armor,
                v => cfg().EnemySystem.ScalingPerLevel.Armor = v));
            list.Add(DoubleParam("globalEnemySpeedMult", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Global enemy speed multiplier", "Enemy AGI scaling at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.SpeedMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.SpeedMultiplier = v));
            list.Add(DoubleParam("globalWeaponDamageMult", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Global weapon damage multiplier", "All weapon base damage scaling", 0.25, 3.0, 0.05,
                () => cfg().WeaponScaling?.GlobalDamageMultiplier ?? 1.0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.GlobalDamageMultiplier = v; }));
            list.Add(DoubleParam("enemyAttributeGrowthBudget", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy attribute growth budget", "STR+AGI+TEC+INT growth sum per level", 1, 20, 0.5,
                () => cfg().EnemySystem.AttributeGrowthBudgetPerLevel,
                v => cfg().EnemySystem.AttributeGrowthBudgetPerLevel = v));
            list.Add(IntParam("enemyPrimaryAttributeBonus", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy primary attribute bonus", "Bonus to enemy primary stat", 0, 10,
                () => cfg().Attributes.EnemyPrimaryAttributeBonus, v => cfg().Attributes.EnemyPrimaryAttributeBonus = v));
            list.Add(DoubleParam("levelDifferenceDamageScaling", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Level difference damage scaling", "Damage bonus per level delta vs enemy", 0, 0.2, 0.01,
                () => cfg().Combat.LevelDifferenceDamageScaling,
                v => cfg().Combat.LevelDifferenceDamageScaling = v, isImplemented: false));
            list.Add(DoubleParam("enemyEnrageThresholdPercent", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy enrage threshold (%)", "HP% below which enrage applies", 0, 50, 1,
                () => cfg().Combat.EnemyEnrageThresholdPercent,
                v => cfg().Combat.EnemyEnrageThresholdPercent = v, isImplemented: false));
            list.Add(DoubleParam("enemyEnrageDamageMult", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Enemy enrage damage mult", "Damage multiplier when enraged", 1, 3, 0.05,
                () => cfg().Combat.EnemyEnrageDamageMult,
                v => cfg().Combat.EnemyEnrageDamageMult = v, isImplemented: false));
            list.Add(DoubleParam("firstStrikeBonusPercent", tab, CombatTuningLayer.WinRate, "Win Rate",
                "First strike bonus (%)", "Bonus damage on first hit", 0, 50, 1,
                () => cfg().Combat.FirstStrikeBonusPercent,
                v => cfg().Combat.FirstStrikeBonusPercent = v, isImplemented: false));
            list.Add(IntParam("attributeSoftCap", tab, CombatTuningLayer.WinRate, "Win Rate",
                "Attribute soft cap", "Diminishing returns threshold", 10, 200,
                () => cfg().Combat.AttributeSoftCap, v => cfg().Combat.AttributeSoftCap = v, isImplemented: false));

            list.Add(DoubleParam("runtimePlayerHealthMult", tab, CombatTuningLayer.WinRate, "Runtime Difficulty",
                "Runtime player health multiplier", "Difficulty overlay on current player HP", 0.5, 3.0, 0.1,
                () => gs().PlayerHealthMultiplier, v => gs().PlayerHealthMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimePlayerDamageMult", tab, CombatTuningLayer.WinRate, "Runtime Difficulty",
                "Runtime player damage multiplier", "Difficulty overlay on player damage", 0.5, 3.0, 0.1,
                () => gs().PlayerDamageMultiplier, v => gs().PlayerDamageMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimeEnemyHealthMult", tab, CombatTuningLayer.WinRate, "Runtime Difficulty",
                "Runtime enemy health multiplier", "Difficulty overlay on spawned enemy HP", 0.5, 3.0, 0.1,
                () => gs().EnemyHealthMultiplier, v => gs().EnemyHealthMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimeEnemyDamageMult", tab, CombatTuningLayer.WinRate, "Runtime Difficulty",
                "Runtime enemy damage multiplier", "Difficulty overlay on enemy damage per swing", 0.5, 3.0, 0.1,
                () => gs().EnemyDamageMultiplier, v => gs().EnemyDamageMultiplier = v, usesGameSettings: true));

            BuildVarianceCompressionParameters(list, cfg, tab);

            // Roll feel — threshold and band knobs (not driven by variance compression master)
            list.Add(IntParam("missThresholdMin", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Miss threshold min", "Lowest roll that counts as miss", 1, 10,
                () => cfg().RollSystem.MissThreshold.Min, v => cfg().RollSystem.MissThreshold.Min = v));
            list.Add(IntParam("missThresholdMax", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Miss threshold max (1–N = miss)", "Miss chance band width", 1, 10,
                () => cfg().RollSystem.MissThreshold.Max, v => cfg().RollSystem.MissThreshold.Max = v));
            list.Add(IntParam("basicAttackMin", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Basic attack roll min", "Normal hit band lower bound", 1, 20,
                () => cfg().RollSystem.BasicAttackThreshold.Min, v => cfg().RollSystem.BasicAttackThreshold.Min = v));
            list.Add(IntParam("basicAttackMax", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Basic attack roll max", "Normal hit band upper bound", 1, 20,
                () => cfg().RollSystem.BasicAttackThreshold.Max, v => cfg().RollSystem.BasicAttackThreshold.Max = v));
            list.Add(IntParam("comboThresholdMin", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Combo roll min", "Combo-tier band lower bound", 1, 20,
                () => cfg().RollSystem.ComboThreshold.Min, v => cfg().RollSystem.ComboThreshold.Min = v));
            list.Add(IntParam("comboThresholdMax", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Combo roll max", "Combo-tier band upper bound", 1, 20,
                () => cfg().RollSystem.ComboThreshold.Max, v => cfg().RollSystem.ComboThreshold.Max = v));
            list.Add(IntParam("criticalThreshold", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Critical threshold (roll system)", "Natural crit on d20 ladder", 1, 20,
                () => cfg().RollSystem.CriticalThreshold, v => cfg().RollSystem.CriticalThreshold = v));
            list.Add(IntParam("criticalHitThreshold", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Critical hit threshold (combat)", "Crit eval threshold in damage calc", 1, 20,
                () => cfg().Combat.CriticalHitThreshold, v => cfg().Combat.CriticalHitThreshold = v));
            list.Add(DoubleParam("criticalHitChance", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Critical hit chance", "Base critical hit chance modifier", 0, 1, 0.01,
                () => cfg().CombatBalance.CriticalHitChance,
                v => cfg().CombatBalance.CriticalHitChance = v));
            list.Add(DoubleParam("comboAmplificationScalingMult", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Combo amplification scaling", "INT amp scaling in roll damage", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.ComboAmplificationScalingMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.ComboAmplificationScalingMultiplier = v));
            list.Add(DoubleParam("tierScalingFallbackMult", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Tier scaling fallback mult", "Fallback when tier scaling missing", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.TierScalingFallbackMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.TierScalingFallbackMultiplier = v));
            list.Add(DoubleParam("critMissComfortShift", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "Crit/miss comfort shift", "Roll band comfort adjustment", -5, 5, 0.5,
                () => cfg().CombatBalance.CritMissComfortShift,
                v => cfg().CombatBalance.CritMissComfortShift = v));
            list.Add(DoubleParam("techniqueMilestoneRollShift", tab, CombatTuningLayer.RollFeel, "Roll Feel",
                "TECH milestone roll shift", "Roll threshold shift per TECH milestone", 0, 5, 0.1,
                () => cfg().Combat.TechniqueMilestoneRollShift,
                v => cfg().Combat.TechniqueMilestoneRollShift = v, isImplemented: false));

            // Combo affordance
            list.Add(IntParam("comboSequenceBaseMax", tab, CombatTuningLayer.ComboAffordance, "Combo Affordance",
                "Combo sequence base max", "Default combo strip length before gear", 1, 8,
                () => cfg().LootSystem.ComboSequenceBaseMax, v => cfg().LootSystem.ComboSequenceBaseMax = v));
            list.Add(IntParam("comboSequenceAbsoluteMax", tab, CombatTuningLayer.ComboAffordance, "Combo Affordance",
                "Combo sequence absolute max", "Hard cap on combo strip length", 1, 12,
                () => cfg().LootSystem.ComboSequenceAbsoluteMax, v => cfg().LootSystem.ComboSequenceAbsoluteMax = v));
            list.Add(DoubleParam("comboAmplifierMax", tab, CombatTuningLayer.ComboAffordance, "Combo Affordance",
                "Combo amplifier max", "Base AMP multiplier at max TECH", 1, 5, 0.1,
                () => cfg().ComboSystem.ComboAmplifierMax, v => cfg().ComboSystem.ComboAmplifierMax = v));
            list.Add(IntParam("comboAmplifierMaxTech", tab, CombatTuningLayer.ComboAffordance, "Combo Affordance",
                "Combo amplifier max TECH", "Effective TECH where base AMP reaches max", 20, 200,
                () => cfg().ComboSystem.ComboAmplifierMaxTech, v => cfg().ComboSystem.ComboAmplifierMaxTech = v));
            list.Add(DoubleParam("comboAmplifierCurveExponent", tab, CombatTuningLayer.ComboAffordance, "Combo Affordance",
                "Combo amplifier curve exponent", "Early/mid TECH power-ramp exponent (0 → knee)", 0.5, 2.5, 0.05,
                () => cfg().ComboSystem.ComboAmplifierCurveExponent,
                v => cfg().ComboSystem.ComboAmplifierCurveExponent = v));
            list.Add(IntParam("comboIntelligenceThreshold", tab, CombatTuningLayer.ComboAffordance, "Combo Affordance",
                "Combo INT threshold", "INT required for combo sequence unlocks", 1, 30,
                () => cfg().LootSystem.ComboSequenceIntelligenceThreshold,
                v => cfg().LootSystem.ComboSequenceIntelligenceThreshold = v));
            list.Add(DoubleParam("comboBreakPenaltyPercent", tab, CombatTuningLayer.ComboAffordance, "Combo Affordance",
                "Combo break penalty (%)", "Damage loss when combo breaks", 0, 50, 1,
                () => cfg().Combat.ComboBreakPenaltyPercent,
                v => cfg().Combat.ComboBreakPenaltyPercent = v, isImplemented: false));

            // Goals (core targets only — extended in GoalsAnalysis tab)
            list.Add(DoubleParam("winRateMinTarget", tab, CombatTuningLayer.Goals, "Goals",
                "Win rate min target (%)", "Simulation validation lower bound", 50, 100, 1,
                () => goals().WinRate.MinTarget, v => goals().WinRate.MinTarget = v));
            list.Add(DoubleParam("winRateMaxTarget", tab, CombatTuningLayer.Goals, "Goals",
                "Win rate max target (%)", "Simulation validation upper bound", 50, 100, 1,
                () => goals().WinRate.MaxTarget, v => goals().WinRate.MaxTarget = v));
            list.Add(DoubleParam("winRateOptimalMin", tab, CombatTuningLayer.Goals, "Goals",
                "Win rate optimal min (%)", "Quality score sweet spot lower", 50, 100, 1,
                () => goals().WinRate.OptimalMin, v => goals().WinRate.OptimalMin = v));
            list.Add(DoubleParam("winRateOptimalMax", tab, CombatTuningLayer.Goals, "Goals",
                "Win rate optimal max (%)", "Quality score sweet spot upper", 50, 100, 1,
                () => goals().WinRate.OptimalMax, v => goals().WinRate.OptimalMax = v));
            list.Add(DoubleParam("durationMinTarget", tab, CombatTuningLayer.Goals, "Goals",
                "Combat duration min (turns)", "Too-short fight threshold", 3, 20, 1,
                () => goals().CombatDuration.MinTarget, v => goals().CombatDuration.MinTarget = v));
            list.Add(DoubleParam("durationMaxTarget", tab, CombatTuningLayer.Goals, "Goals",
                "Combat duration max (turns)", "Too-long fight threshold", 5, 30, 1,
                () => goals().CombatDuration.MaxTarget, v => goals().CombatDuration.MaxTarget = v));
            list.Add(DoubleParam("durationOptimalMin", tab, CombatTuningLayer.Goals, "Goals",
                "Combat duration optimal min", "Target fight length lower", 3, 20, 1,
                () => goals().CombatDuration.OptimalMin, v => goals().CombatDuration.OptimalMin = v));
            list.Add(DoubleParam("durationOptimalMax", tab, CombatTuningLayer.Goals, "Goals",
                "Combat duration optimal max", "Target fight length upper", 5, 30, 1,
                () => goals().CombatDuration.OptimalMax, v => goals().CombatDuration.OptimalMax = v));
        }

        private static void BuildHeroClassParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            const CombatTuningTab tab = CombatTuningTab.HeroClasses;
            const CombatTuningLayer layer = CombatTuningLayer.WinRate;

            list.Add(IntParam("playerPrimaryStatPerLevel", tab, layer, "Level-Up Growth",
                "Primary stat per level", "+primary stat on weapon-based level-up", 1, 10,
                () => cfg().Attributes.PlayerPrimaryStatPerLevel, v => cfg().Attributes.PlayerPrimaryStatPerLevel = v));
            list.Add(IntParam("playerSecondaryStatPerLevel", tab, layer, "Level-Up Growth",
                "Secondary stat per level", "+other stats on weapon-based level-up", 0, 5,
                () => cfg().Attributes.PlayerSecondaryStatPerLevel, v => cfg().Attributes.PlayerSecondaryStatPerLevel = v));
            list.Add(IntParam("wandComboSlotBonus", tab, layer, "Level-Up Growth",
                "Wand combo slot bonus", "Extra combo slots when wand equipped", 0, 5,
                () => cfg().ClassPresentation.WandEquippedComboSlotBonus,
                v => cfg().ClassPresentation.WandEquippedComboSlotBonus = v));

            AddClassMultipliers(list, tab, layer, "Barbarian (Mace)", "barbarian",
                () => cfg().ClassBalance.Barbarian);
            AddClassMultipliers(list, tab, layer, "Warrior (Sword)", "warrior",
                () => cfg().ClassBalance.Warrior);
            AddClassMultipliers(list, tab, layer, "Rogue (Dagger)", "rogue",
                () => cfg().ClassBalance.Rogue);
            AddClassMultipliers(list, tab, layer, "Wizard (Wand)", "wizard",
                () => cfg().ClassBalance.Wizard);
        }

        private static void AddClassMultipliers(
            List<CombatTuningParameter> list, CombatTuningTab tab, CombatTuningLayer layer,
            string classLabel, string classKey, Func<ClassMultipliers> getClass)
        {
            list.Add(DoubleParam($"{classKey}HealthMult", tab, layer, classLabel,
                $"{classLabel} health mult", "Level-up HP scaling for this class", 0.25, 3.0, 0.05,
                () => getClass().HealthMultiplier, v => getClass().HealthMultiplier = v));
            list.Add(DoubleParam($"{classKey}DamageMult", tab, layer, classLabel,
                $"{classLabel} damage mult", "Combat damage scaling for this class", 0.25, 3.0, 0.05,
                () => getClass().DamageMultiplier, v => getClass().DamageMultiplier = v));
            list.Add(DoubleParam($"{classKey}SpeedMult", tab, layer, classLabel,
                $"{classLabel} speed mult", "Attack speed scaling for this class", 0.25, 3.0, 0.05,
                () => getClass().SpeedMultiplier, v => getClass().SpeedMultiplier = v));
        }

        private static void BuildSpeedDefenseParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            var gs = () => GameSettings.Instance;
            const CombatTuningTab tab = CombatTuningTab.SpeedDefense;
            const CombatTuningLayer layer = CombatTuningLayer.RollFeel;

            list.Add(DoubleParam("baseAttackTime", tab, layer, "Attack Timing",
                "Base attack time (s)", "Baseline seconds per attack before agility", 1, 20, 0.5,
                () => cfg().Combat.BaseAttackTime, v => cfg().Combat.BaseAttackTime = v));
            list.Add(DoubleParam("minimumAttackTime", tab, layer, "Attack Timing",
                "Minimum attack time (s)", "Floor for final attack time", 0.01, 2, 0.01,
                () => cfg().Combat.MinimumAttackTime, v => cfg().Combat.MinimumAttackTime = v));
            list.Add(IntParam("agilityMin", tab, layer, "Agility Curve",
                "Agility min", "Minimum agility for speed curve", 1, 50,
                () => cfg().Combat.AgilityMin, v => cfg().Combat.AgilityMin = v));
            list.Add(IntParam("agilityMax", tab, layer, "Agility Curve",
                "Agility max", "Maximum agility for speed curve", 50, 200,
                () => cfg().Combat.AgilityMax, v => cfg().Combat.AgilityMax = v));
            list.Add(DoubleParam("agilityMinSpeedMult", tab, layer, "Agility Curve",
                "Agility min speed mult", "Speed multiplier at minimum agility", 0.5, 1.5, 0.01,
                () => cfg().Combat.AgilityMinSpeedMultiplier, v => cfg().Combat.AgilityMinSpeedMultiplier = v));
            list.Add(DoubleParam("agilityMaxSpeedMult", tab, layer, "Agility Curve",
                "Agility max speed mult", "Speed multiplier at maximum agility", 0.01, 1.0, 0.01,
                () => cfg().Combat.AgilityMaxSpeedMultiplier, v => cfg().Combat.AgilityMaxSpeedMultiplier = v));
            list.Add(DoubleParam("weaponAttackTimeClampMin", tab, layer, "Attack Timing",
                "Weapon attack time clamp min", "Minimum weapon speed multiplier", 0.1, 1.0, 0.05,
                () => cfg().Combat.WeaponAttackTimeClampMin, v => cfg().Combat.WeaponAttackTimeClampMin = v));
            list.Add(DoubleParam("weaponAttackTimeClampMax", tab, layer, "Attack Timing",
                "Weapon attack time clamp max", "Maximum weapon speed multiplier", 1.0, 3.0, 0.05,
                () => cfg().Combat.WeaponAttackTimeClampMax, v => cfg().Combat.WeaponAttackTimeClampMax = v));
            list.Add(DoubleParam("runtimeCombatSpeed", tab, layer, "Pacing",
                "Runtime combat speed", "UI/combat pacing multiplier", 0.5, 2.0, 0.1,
                () => gs().CombatSpeed, v => gs().CombatSpeed = v, usesGameSettings: true));
            list.Add(DoubleParam("swiftActionSpeedPercent", tab, layer, "Action Mechanics",
                "Swift speed bonus (%)", "SPEED_MOD from swift tag", 0, 50, 1,
                () => cfg().CombatBalance.ActionMechanics.SwiftSpeedPercent,
                v => cfg().CombatBalance.ActionMechanics.SwiftSpeedPercent = v));
            list.Add(DoubleParam("bludgeonDamagePercent", tab, layer, "Action Mechanics",
                "Bludgeon damage bonus (%)", "DAMAGE_MOD from bludgeon tag", 0, 50, 1,
                () => cfg().CombatBalance.ActionMechanics.BludgeonDamagePercent,
                v => cfg().CombatBalance.ActionMechanics.BludgeonDamagePercent = v));
            list.Add(DoubleParam("globalHealEffectiveness", tab, layer, "Defense",
                "Global heal effectiveness", "Heal amount multiplier", 0.25, 3.0, 0.05,
                () => cfg().Combat.GlobalHealEffectiveness, v => cfg().Combat.GlobalHealEffectiveness = v,
                isImplemented: false));
            list.Add(DoubleParam("overkillDamagePercent", tab, layer, "Defense",
                "Overkill damage (%)", "Overkill splash transfer", 0, 100, 1,
                () => cfg().Combat.OverkillDamagePercent, v => cfg().Combat.OverkillDamagePercent = v,
                isImplemented: false));
        }

        private static void BuildEquipmentParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            const CombatTuningTab tab = CombatTuningTab.Equipment;
            const CombatTuningLayer layer = CombatTuningLayer.WinRate;

            list.Add(IntParam("weaponDamagePerTier", tab, layer, "Tier Scaling",
                "Weapon damage per tier", "Damage added per equipment tier", 1, 20,
                () => cfg().EquipmentScaling.WeaponDamagePerTier,
                v => cfg().EquipmentScaling.WeaponDamagePerTier = v));
            list.Add(IntParam("armorValuePerTier", tab, layer, "Tier Scaling",
                "Armor value per tier", "Armor added per equipment tier", 1, 20,
                () => cfg().EquipmentScaling.ArmorValuePerTier,
                v => cfg().EquipmentScaling.ArmorValuePerTier = v));
            list.Add(DoubleParam("speedBonusPerTier", tab, layer, "Tier Scaling",
                "Speed bonus per tier", "Attack speed bonus per tier", 0, 0.5, 0.01,
                () => cfg().EquipmentScaling.SpeedBonusPerTier,
                v => cfg().EquipmentScaling.SpeedBonusPerTier = v));
            list.Add(IntParam("equipmentMaxTier", tab, layer, "Tier Scaling",
                "Equipment max tier", "Maximum equipment tier", 1, 20,
                () => cfg().EquipmentScaling.MaxTier, v => cfg().EquipmentScaling.MaxTier = v));
            list.Add(DoubleParam("enchantmentChance", tab, layer, "Tier Scaling",
                "Enchantment chance", "Base enchantment roll chance", 0, 1, 0.01,
                () => cfg().EquipmentScaling.EnchantmentChance,
                v => cfg().EquipmentScaling.EnchantmentChance = v, isImplemented: false));
            list.Add(IntParam("startingDamageMace", tab, layer, "Starting Weapon Damage",
                "Starting damage — Mace", "Level 1 mace base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Mace ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Mace = v; }));
            list.Add(IntParam("startingDamageSword", tab, layer, "Starting Weapon Damage",
                "Starting damage — Sword", "Level 1 sword base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Sword ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Sword = v; }));
            list.Add(IntParam("startingDamageDagger", tab, layer, "Starting Weapon Damage",
                "Starting damage — Dagger", "Level 1 dagger base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Dagger ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Dagger = v; }));
            list.Add(IntParam("startingDamageWand", tab, layer, "Starting Weapon Damage",
                "Starting damage — Wand", "Level 1 wand base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Wand ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Wand = v; }));
        }

        private static void BuildEnemyStatsParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            const CombatTuningTab tab = CombatTuningTab.EnemyStats;
            const CombatTuningLayer layer = CombatTuningLayer.WinRate;

            list.Add(IntParam("enemyLevelVariance", tab, layer, "Spawn",
                "Enemy level variance", "Spawn level jitter ±N", 0, 10,
                () => cfg().EnemySystem.LevelVariance, v => cfg().EnemySystem.LevelVariance = v));
            list.Add(IntParam("legacyEnemyAttributesPerLevel", tab, layer, "Legacy",
                "Legacy enemy attributes per level", "Legacy global enemy attr growth", 0, 10,
                () => cfg().Attributes.EnemyAttributesPerLevel, v => cfg().Attributes.EnemyAttributesPerLevel = v));
            list.Add(IntParam("intelligenceRollBonusPer", tab, layer, "Legacy",
                "INT roll bonus per point", "Legacy flat INT roll bonus", 0, 30,
                () => cfg().Attributes.IntelligenceRollBonusPer, v => cfg().Attributes.IntelligenceRollBonusPer = v,
                isImplemented: false));

            AddDifficultyPreset(list, tab, layer, "Easy", "easy", () => cfg().DifficultySettings.Easy);
            AddDifficultyPreset(list, tab, layer, "Normal", "normal", () => cfg().DifficultySettings.Normal);
            AddDifficultyPreset(list, tab, layer, "Hard", "hard", () => cfg().DifficultySettings.Hard);
        }

        private static void BuildProgressionCurveParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            const CombatTuningTab tab = CombatTuningTab.ProgressionCurve;
            const CombatTuningLayer layer = CombatTuningLayer.Duration;
            const int tuningMaxBaseHealth = 1000000;
            const int tuningMaxHealthPerLevel = 100000;

            list.Add(DoubleParam("combatTempoScale", tab, layer, "Progression Curve (Broad Knobs)",
                "Combat tempo scale",
                "Stretches all enemy HP uniformly — longer fights at every level when increased",
                0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.CombatTempoScale,
                v => cfg().EnemySystem.ProgressionScales.CombatTempoScale = v));
            list.Add(DoubleParam("progressionShape", tab, layer, "Progression Curve (Broad Knobs)",
                "Progression shape",
                "0 = early/base-dominated (L10–L30 stay closer to base HP); 1 = growth kicks in earlier at high levels",
                0, 1, 0.05,
                () => cfg().EnemySystem.ProgressionScales.ProgressionShape,
                v => cfg().EnemySystem.ProgressionScales.ProgressionShape = v));
            list.Add(DoubleParam("playerEnemyParity", tab, CombatTuningLayer.WinRate, "Progression Curve (Broad Knobs)",
                "Player/enemy parity",
                "+ shifts survivability toward the hero; − toward enemies — does not change curve shape",
                -1, 1, 0.05,
                () => cfg().EnemySystem.ProgressionScales.PlayerEnemyParity,
                v => cfg().EnemySystem.ProgressionScales.PlayerEnemyParity = v));
            list.Add(IntParam("progressionPivotLevel", tab, layer, "Progression Curve (Broad Knobs)",
                "Progression pivot level",
                "Level where growth weight reaches full strength (design constant; rarely changed)",
                10, 100,
                () => cfg().EnemySystem.ProgressionScales.ProgressionPivotLevel,
                v => cfg().EnemySystem.ProgressionScales.ProgressionPivotLevel = v));

            list.Add(IntParam("playerBaseHealth", tab, layer, "Curve Inputs (Fine)",
                "Hero base health",
                "Hero max HP at level 1 — intercept of the player curve",
                20, tuningMaxBaseHealth,
                () => cfg().Character.PlayerBaseHealth, v => cfg().Character.PlayerBaseHealth = v));
            list.Add(IntParam("playerHealthPerLevel", tab, layer, "Curve Inputs (Fine)",
                "Player health per level",
                "Hero HP slope per level — midgame survivability when parity alone is not enough",
                0, tuningMaxHealthPerLevel,
                () => cfg().Character.HealthPerLevel, v => cfg().Character.HealthPerLevel = v));
            list.Add(DoubleParam("baseHealthScale", tab, CombatTuningLayer.WinRate, "Curve Inputs (Fine)",
                "Enemy base health scale",
                "Multiplies enemy base HP at all levels — early-game intercept",
                0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.BaseHealthScale,
                v => cfg().EnemySystem.ProgressionScales.BaseHealthScale = v));
            list.Add(DoubleParam("healthGrowthScale", tab, CombatTuningLayer.WinRate, "Curve Inputs (Fine)",
                "Enemy health growth scale",
                "Multiplies per-level enemy HP growth — late-game slope input",
                0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.HealthGrowthScale,
                v => cfg().EnemySystem.ProgressionScales.HealthGrowthScale = v));
            list.Add(DoubleParam("attributeGrowthScale", tab, CombatTuningLayer.WinRate, "Curve Inputs (Fine)",
                "Attribute growth scale",
                "Enemy STR/AGI/TEC/INT growth (damage/speed, not HP curve)",
                0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.AttributeGrowthScale,
                v => cfg().EnemySystem.ProgressionScales.AttributeGrowthScale = v));
        }

        private static void AddDifficultyPreset(
            List<CombatTuningParameter> list, CombatTuningTab tab, CombatTuningLayer layer,
            string label, string key, Func<DifficultyLevel> getLevel)
        {
            list.Add(DoubleParam($"{key}EnemyHealthMult", tab, layer, $"Difficulty — {label}",
                $"{label} enemy health mult", "Preset enemy HP multiplier (no difficulty picker applies this yet)", 0.25, 3.0, 0.05,
                () => getLevel().EnemyHealthMultiplier, v => getLevel().EnemyHealthMultiplier = v,
                isImplemented: false));
            list.Add(DoubleParam($"{key}EnemyDamageMult", tab, layer, $"Difficulty — {label}",
                $"{label} enemy damage mult", "Preset enemy damage multiplier (no difficulty picker applies this yet)", 0.25, 3.0, 0.05,
                () => getLevel().EnemyDamageMultiplier, v => getLevel().EnemyDamageMultiplier = v,
                isImplemented: false));
            list.Add(DoubleParam($"{key}XpMult", tab, layer, $"Difficulty — {label}",
                $"{label} XP mult", "Preset XP reward multiplier (XP system does not read this yet)", 0.25, 3.0, 0.05,
                () => getLevel().XPMultiplier, v => getLevel().XPMultiplier = v,
                isImplemented: false));
            list.Add(DoubleParam($"{key}LootMult", tab, layer, $"Difficulty — {label}",
                $"{label} loot mult", "Preset loot drop multiplier (loot system does not read this yet)", 0.25, 3.0, 0.05,
                () => getLevel().LootMultiplier, v => getLevel().LootMultiplier = v,
                isImplemented: false));
        }

        private static void BuildArchetypeParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            const CombatTuningTab tab = CombatTuningTab.Archetypes;
            const CombatTuningLayer layer = CombatTuningLayer.WinRate;

            foreach (var archetype in ArchetypeNames)
            {
                string key = archetype;
                list.Add(DoubleParam($"arch_{key}_health", tab, layer, archetype,
                    "Health multiplier", "Scales archetype base health", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Health,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Health = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_strength", tab, layer, archetype,
                    "Strength multiplier", "Scales archetype STR", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Strength,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Strength = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_agility", tab, layer, archetype,
                    "Agility multiplier", "Scales archetype AGI", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Agility,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Agility = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_technique", tab, layer, archetype,
                    "Technique multiplier", "Scales archetype TEC", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Technique,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Technique = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_intelligence", tab, layer, archetype,
                    "Intelligence multiplier", "Scales archetype INT", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Intelligence,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Intelligence = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_armor", tab, layer, archetype,
                    "Armor multiplier", "Scales archetype armor", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Armor,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Armor = v,
                    filterKey: key));
            }
        }

        private static ArchetypeMultipliersConfig GetOrCreateArchetype(EnemySystemConfig system, string name)
        {
            system.Archetypes ??= new Dictionary<string, ArchetypeMultipliersConfig>();
            if (!system.Archetypes.TryGetValue(name, out var arch))
            {
                arch = new ArchetypeMultipliersConfig();
                system.Archetypes[name] = arch;
            }
            return arch;
        }

        private static void BuildStatusEffectParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            const CombatTuningTab tab = CombatTuningTab.StatusEffects;
            const CombatTuningLayer layer = CombatTuningLayer.RollFeel;
            var scaling = () => cfg().CombatBalance.StatusEffectScaling;

            list.Add(DoubleParam("statusBleedDuration", tab, layer, "Global Scaling",
                "Bleed duration scale", "Global bleed duration multiplier", 0, 10, 0.1,
                () => scaling().BleedDuration, v => scaling().BleedDuration = v));
            list.Add(DoubleParam("statusPoisonDuration", tab, layer, "Global Scaling",
                "Poison duration scale", "Global poison duration multiplier", 0, 10, 0.1,
                () => scaling().PoisonDuration, v => scaling().PoisonDuration = v));
            list.Add(DoubleParam("statusStunDuration", tab, layer, "Global Scaling",
                "Stun duration scale", "Global stun duration multiplier", 0, 10, 0.1,
                () => scaling().StunDuration, v => scaling().StunDuration = v));
            list.Add(DoubleParam("statusBurnDuration", tab, layer, "Global Scaling",
                "Burn duration scale", "Global burn duration multiplier", 0, 10, 0.1,
                () => scaling().BurnDuration, v => scaling().BurnDuration = v));
            list.Add(DoubleParam("statusFreezeDuration", tab, layer, "Global Scaling",
                "Freeze duration scale", "Global freeze duration multiplier", 0, 10, 0.1,
                () => scaling().FreezeDuration, v => scaling().FreezeDuration = v));
            list.Add(DoubleParam("statusEffectDamageScaling", tab, layer, "Global Scaling",
                "Status effect damage scaling", "Global DoT damage scaling", 0, 5, 0.1,
                () => scaling().StatusEffectDamageScaling, v => scaling().StatusEffectDamageScaling = v));
            list.Add(DoubleParam("statusEffectBaseProcChance", tab, layer, "Global Scaling",
                "Status proc chance mult", "Global proc chance modifier", 0, 3, 0.05,
                () => cfg().Combat.StatusEffectBaseProcChance,
                v => cfg().Combat.StatusEffectBaseProcChance = v, isImplemented: false));

            foreach (var effect in StatusEffectNames)
            {
                list.Add(IntParam($"fx_{effect}_damagePerTick", tab, layer, effect,
                    "Damage per tick", $"{effect} damage per tick", 0, 50,
                    () => GetOrCreateEffect(cfg(), effect).DamagePerTick,
                    v => GetOrCreateEffect(cfg(), effect).DamagePerTick = v,
                    filterKey: effect));
                list.Add(DoubleParam($"fx_{effect}_tickInterval", tab, layer, effect,
                    "Tick interval (s)", $"{effect} seconds between ticks", 0, 10, 0.1,
                    () => GetOrCreateEffect(cfg(), effect).TickInterval,
                    v => GetOrCreateEffect(cfg(), effect).TickInterval = v,
                    filterKey: effect));
                list.Add(IntParam($"fx_{effect}_maxStacks", tab, layer, effect,
                    "Max stacks", $"{effect} maximum stack count", 0, 20,
                    () => GetOrCreateEffect(cfg(), effect).MaxStacks,
                    v => GetOrCreateEffect(cfg(), effect).MaxStacks = v,
                    filterKey: effect));
                list.Add(DoubleParam($"fx_{effect}_duration", tab, layer, effect,
                    "Duration (s)", $"{effect} effect duration", 0, 60, 0.5,
                    () => GetOrCreateEffect(cfg(), effect).Duration,
                    v => GetOrCreateEffect(cfg(), effect).Duration = v,
                    filterKey: effect));
            }
        }

        private static StatusEffectConfig GetOrCreateEffect(GameConfiguration cfg, string name)
        {
            cfg.StatusEffects ??= new StatusEffectsConfig();
            cfg.StatusEffects.InitializeDefaults();
            var effect = cfg.StatusEffects.GetEffect(name);
            if (effect == null)
            {
                effect = new StatusEffectConfig();
                cfg.StatusEffects.SetEffect(name, effect);
            }
            return effect;
        }

        private static void BuildRewardsLootParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            var gs = () => GameSettings.Instance;
            const CombatTuningTab tab = CombatTuningTab.RewardsLoot;
            const CombatTuningLayer layer = CombatTuningLayer.Duration;

            list.Add(IntParam("enemyXPBase", tab, layer, "Combat Rewards",
                "Enemy XP base", "Base XP from defeating an enemy", 0, 200,
                () => cfg().Progression.EnemyXPBase, v => cfg().Progression.EnemyXPBase = v));
            list.Add(IntParam("enemyXPPerLevel", tab, layer, "Combat Rewards",
                "Enemy XP per level", "XP bonus per enemy level", 0, 50,
                () => cfg().Progression.EnemyXPPerLevel, v => cfg().Progression.EnemyXPPerLevel = v));
            list.Add(IntParam("enemyGoldBase", tab, layer, "Combat Rewards",
                "Enemy gold base", "Base gold from defeating an enemy", 0, 200,
                () => cfg().Progression.EnemyGoldBase, v => cfg().Progression.EnemyGoldBase = v));
            list.Add(IntParam("enemyGoldPerLevel", tab, layer, "Combat Rewards",
                "Enemy gold per level", "Gold bonus per enemy level", 0, 50,
                () => cfg().Progression.EnemyGoldPerLevel, v => cfg().Progression.EnemyGoldPerLevel = v));
            list.Add(DoubleParam("baseDropChance", tab, layer, "Loot",
                "Base drop chance", "Base item drop probability", 0, 1, 0.01,
                () => cfg().LootSystem.BaseDropChance, v => cfg().LootSystem.BaseDropChance = v));
            list.Add(DoubleParam("maxDropChance", tab, layer, "Loot",
                "Max drop chance", "Maximum item drop probability", 0, 1, 0.01,
                () => cfg().LootSystem.MaxDropChance, v => cfg().LootSystem.MaxDropChance = v));
            list.Add(DoubleParam("goldDropMultiplier", tab, layer, "Loot",
                "Gold drop multiplier", "Scales gold drops from combat", 0.25, 3.0, 0.05,
                () => cfg().LootSystem.GoldDropMultiplier, v => cfg().LootSystem.GoldDropMultiplier = v));
            list.Add(IntParam("enableComboSystem", tab, layer, "Combat Systems",
                "Enable combo system", "1 = enabled, 0 = disabled", 0, 1,
                () => gs().EnableComboSystem ? 1 : 0, v => gs().EnableComboSystem = v >= 1,
                usesGameSettings: true));
        }

        private static void BuildGoalsAnalysisParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            var goals = () => cfg().BalanceTuningGoals;
            const CombatTuningTab tab = CombatTuningTab.GoalsAnalysis;
            const CombatTuningLayer layer = CombatTuningLayer.Goals;

            list.Add(DoubleParam("winRateCriticalLow", tab, layer, "Win Rate Thresholds",
                "Win rate critical low (%)", "Critical underperformance threshold", 50, 100, 1,
                () => goals().WinRate.CriticalLow, v => goals().WinRate.CriticalLow = v));
            list.Add(DoubleParam("winRateWarningLow", tab, layer, "Win Rate Thresholds",
                "Win rate warning low (%)", "Warning underperformance threshold", 50, 100, 1,
                () => goals().WinRate.WarningLow, v => goals().WinRate.WarningLow = v));
            list.Add(DoubleParam("winRateWarningHigh", tab, layer, "Win Rate Thresholds",
                "Win rate warning high (%)", "Warning overperformance threshold", 50, 100, 1,
                () => goals().WinRate.WarningHigh, v => goals().WinRate.WarningHigh = v));
            list.Add(DoubleParam("winRateCriticalHigh", tab, layer, "Win Rate Thresholds",
                "Win rate critical high (%)", "Critical overperformance threshold", 50, 100, 1,
                () => goals().WinRate.CriticalHigh, v => goals().WinRate.CriticalHigh = v));

            list.Add(DoubleParam("durationCriticalShort", tab, layer, "Duration Thresholds",
                "Duration critical short", "Critically too-short fights (turns)", 1, 15, 1,
                () => goals().CombatDuration.CriticalShort, v => goals().CombatDuration.CriticalShort = v));
            list.Add(DoubleParam("durationWarningShort", tab, layer, "Duration Thresholds",
                "Duration warning short", "Warning too-short fights (turns)", 1, 20, 1,
                () => goals().CombatDuration.WarningShort, v => goals().CombatDuration.WarningShort = v));
            list.Add(DoubleParam("durationWarningLong", tab, layer, "Duration Thresholds",
                "Duration warning long", "Warning too-long fights (turns)", 5, 30, 1,
                () => goals().CombatDuration.WarningLong, v => goals().CombatDuration.WarningLong = v));
            list.Add(DoubleParam("durationCriticalLong", tab, layer, "Duration Thresholds",
                "Duration critical long", "Critically too-long fights (turns)", 5, 40, 1,
                () => goals().CombatDuration.CriticalLong, v => goals().CombatDuration.CriticalLong = v));

            list.Add(DoubleParam("weaponBalanceMaxVariance", tab, layer, "Weapon Balance",
                "Weapon balance max variance", "Maximum acceptable weapon DPS variance", 1, 30, 1,
                () => goals().WeaponBalance.MaxVariance, v => goals().WeaponBalance.MaxVariance = v));
            list.Add(DoubleParam("weaponBalanceOptimalVariance", tab, layer, "Weapon Balance",
                "Weapon balance optimal variance", "Target weapon DPS variance", 1, 20, 1,
                () => goals().WeaponBalance.OptimalVariance, v => goals().WeaponBalance.OptimalVariance = v));
            list.Add(DoubleParam("weaponBalanceCriticalVariance", tab, layer, "Weapon Balance",
                "Weapon balance critical variance", "Critical weapon imbalance threshold", 1, 40, 1,
                () => goals().WeaponBalance.CriticalVariance, v => goals().WeaponBalance.CriticalVariance = v));

            list.Add(DoubleParam("enemyDiffMinVariance", tab, layer, "Enemy Differentiation",
                "Enemy diff min variance", "Minimum enemy differentiation score", 0, 10, 0.5,
                () => goals().EnemyDifferentiation.MinVariance, v => goals().EnemyDifferentiation.MinVariance = v));
            list.Add(DoubleParam("enemyDiffOptimalVariance", tab, layer, "Enemy Differentiation",
                "Enemy diff optimal variance", "Target enemy differentiation", 0, 10, 0.5,
                () => goals().EnemyDifferentiation.OptimalVariance, v => goals().EnemyDifferentiation.OptimalVariance = v));
            list.Add(DoubleParam("enemyDiffCriticalVariance", tab, layer, "Enemy Differentiation",
                "Enemy diff critical variance", "Critical lack of differentiation", 0, 10, 0.5,
                () => goals().EnemyDifferentiation.CriticalVariance, v => goals().EnemyDifferentiation.CriticalVariance = v));

            list.Add(DoubleParam("qualityWinRateWeight", tab, layer, "Quality Weights",
                "Win rate weight", "Quality score weight for win rate", 0, 1, 0.05,
                () => goals().QualityWeights.WinRateWeight, v => goals().QualityWeights.WinRateWeight = v));
            list.Add(DoubleParam("qualityDurationWeight", tab, layer, "Quality Weights",
                "Duration weight", "Quality score weight for duration", 0, 1, 0.05,
                () => goals().QualityWeights.DurationWeight, v => goals().QualityWeights.DurationWeight = v));
            list.Add(DoubleParam("qualityWeaponWeight", tab, layer, "Quality Weights",
                "Weapon balance weight", "Quality score weight for weapon balance", 0, 1, 0.05,
                () => goals().QualityWeights.WeaponBalanceWeight, v => goals().QualityWeights.WeaponBalanceWeight = v));
            list.Add(DoubleParam("qualityEnemyDiffWeight", tab, layer, "Quality Weights",
                "Enemy diff weight", "Quality score weight for enemy differentiation", 0, 1, 0.05,
                () => goals().QualityWeights.EnemyDiffWeight, v => goals().QualityWeights.EnemyDiffWeight = v));

            list.Add(IntParam("simulationsPerMatchup", tab, layer, "Simulation",
                "Simulations per matchup", "Battles run per weapon/enemy pair in analysis", 10, 1000,
                () => cfg().BalanceAnalysis.SimulationsPerMatchup,
                v => cfg().BalanceAnalysis.SimulationsPerMatchup = v));
            list.Add(DoubleParam("damageCalcAverageMult", tab, layer, "Simulation",
                "Damage calc average mult", "Average damage multiplier in balance analysis", 0.1, 2.0, 0.05,
                () => cfg().BalanceAnalysis.DamageCalculation.AverageDamageMultiplier,
                v => cfg().BalanceAnalysis.DamageCalculation.AverageDamageMultiplier = v));
            list.Add(DoubleParam("dpsCalcBaseDamageMult", tab, layer, "Simulation",
                "DPS calc base damage mult", "Base damage multiplier in DPS calc", 0.1, 2.0, 0.05,
                () => cfg().BalanceAnalysis.DPSCalculation.BaseDamageMultiplier,
                v => cfg().BalanceAnalysis.DPSCalculation.BaseDamageMultiplier = v));
            list.Add(DoubleParam("dpsCalcAttackSpeedMult", tab, layer, "Simulation",
                "DPS calc attack speed mult", "Attack speed multiplier in DPS calc", 0.01, 1.0, 0.01,
                () => cfg().BalanceAnalysis.DPSCalculation.AttackSpeedMultiplier,
                v => cfg().BalanceAnalysis.DPSCalculation.AttackSpeedMultiplier = v));
            list.Add(DoubleParam("balanceTooEasyThreshold", tab, layer, "Simulation",
                "Too easy threshold", "Win rate above which content is too easy", 0.5, 1.0, 0.01,
                () => cfg().BalanceAnalysis.DifficultyThresholds.TooEasy,
                v => cfg().BalanceAnalysis.DifficultyThresholds.TooEasy = v));
            list.Add(DoubleParam("balanceModerateThreshold", tab, layer, "Simulation",
                "Moderate threshold", "Win rate for moderate difficulty", 0.1, 1.0, 0.01,
                () => cfg().BalanceAnalysis.DifficultyThresholds.Moderate,
                v => cfg().BalanceAnalysis.DifficultyThresholds.Moderate = v));
            list.Add(DoubleParam("balanceHardThreshold", tab, layer, "Simulation",
                "Hard threshold", "Win rate below which content is hard", 0.1, 1.0, 0.01,
                () => cfg().BalanceAnalysis.DifficultyThresholds.Hard,
                v => cfg().BalanceAnalysis.DifficultyThresholds.Hard = v));
            list.Add(DoubleParam("tutorialCombatDelayMult", tab, layer, "Simulation",
                "Tutorial combat delay mult", "Combat delay multiplier during tutorial", 1, 5, 0.1,
                () => cfg().Combat.TutorialCombatDelayMultiplier,
                v => cfg().Combat.TutorialCombatDelayMultiplier = v));
        }
        private static void BuildVarianceCompressionParameters(
            List<CombatTuningParameter> list,
            Func<GameConfiguration> cfg,
            CombatTuningTab tab)
        {
            const CombatTuningLayer layer = CombatTuningLayer.RollFeel;
            const string group = RollFeelVarianceCompression.SubGroupName;

            list.Add(DoubleParam(RollFeelVarianceCompression.MasterParameterId, tab, layer, group,
                "Variance compression master", "0 = chaotic spikes, 1 = regular/smoothed damage", 0, 1, 0.05,
                () => cfg().CombatBalance.RollFeelVarianceCompression,
                v => RollFeelVarianceCompression.Apply(v)));

            list.Add(DoubleParam("criticalHitDamageMult", tab, layer, group,
                "Critical hit damage multiplier", "Damage on natural crit rolls", 1.0, 5.0, 0.1,
                () => cfg().CombatBalance.CriticalHitDamageMultiplier,
                v => cfg().CombatBalance.CriticalHitDamageMultiplier = v));

            list.Add(DoubleParam("comboRollDamageMult", tab, layer, group,
                "Combo roll damage multiplier", "Damage on combo-tier rolls", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier = v));

            list.Add(DoubleParam("basicRollDamageMult", tab, layer, group,
                "Basic roll damage multiplier", "Damage on normal hit band", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier = v));

            list.Add(IntParam("minimumDamage", tab, layer, group,
                "Minimum damage floor", "Lowest damage after armor", 1, 10,
                () => cfg().Combat.MinimumDamage, v => cfg().Combat.MinimumDamage = v));

            list.Add(IntParam("maximumDamageCap", tab, layer, group,
                "Maximum damage cap", "Hard cap on single hit damage", 100, 9999,
                () => cfg().Combat.MaximumDamageCap, v => cfg().Combat.MaximumDamageCap = v));

            list.Add(IntParam("playerArmorBaseline", tab, layer, group,
                "Hero base armor", "Flat armor pool baseline for hero", 0, 50,
                () => cfg().Combat.PlayerBaseArmor, v => cfg().Combat.PlayerBaseArmor = v));

            list.Add(DoubleParam("globalEnemyArmorMult", tab, layer, group,
                "Global enemy armor multiplier", "Enemy armor at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.ArmorMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.ArmorMultiplier = v));

            list.Add(DoubleParam("armorReductionFactor", tab, layer, group,
                "Armor reduction factor", "Denominator in armor mitigation formula (lower = more absorption)", 10, 500, 5,
                () => cfg().Combat.ArmorReductionFactor, v => cfg().Combat.ArmorReductionFactor = v));
        }
    }
}
