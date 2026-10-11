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

        private const int TuningMaxBaseHealth = 1000000;
        private const int TuningMaxHealthPerLevel = 100000;

        /// <summary>Level-1 intercepts: hero/enemy bases, damage equation, tempo, roll ladder, starting gear.</summary>
        private static void BuildStartingParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            const CombatTuningTab tab = CombatTuningTab.Starting;

            // Hero bases
            list.Add(IntParam("playerBaseStrength", tab, CombatTuningLayer.WinRate, "Hero bases",
                "Player base strength", "Melee damage baseline (before scale)", 1, 50,
                () => cfg().Attributes.PlayerBaseAttributes.Strength, v => cfg().Attributes.PlayerBaseAttributes.Strength = v));
            list.Add(IntParam("playerBaseAgility", tab, CombatTuningLayer.WinRate, "Hero bases",
                "Player base agility", "Attack speed baseline", 1, 50,
                () => cfg().Attributes.PlayerBaseAttributes.Agility, v => cfg().Attributes.PlayerBaseAttributes.Agility = v));
            list.Add(IntParam("playerBaseTechnique", tab, CombatTuningLayer.WinRate, "Hero bases",
                "Player base technique", "Combo AMP + HIT/COMBO/CRIT milestones", 1, 50,
                () => cfg().Attributes.PlayerBaseAttributes.Technique, v => cfg().Attributes.PlayerBaseAttributes.Technique = v));
            list.Add(IntParam("playerBaseIntelligence", tab, CombatTuningLayer.WinRate, "Hero bases",
                "Player base intelligence", "Combo strip order gate baseline", 1, 50,
                () => cfg().Attributes.PlayerBaseAttributes.Intelligence, v => cfg().Attributes.PlayerBaseAttributes.Intelligence = v));
            list.Add(IntParam("playerBaseHealth", tab, CombatTuningLayer.Duration, "Hero bases",
                "Hero base health", "Hero max HP at level 1", 20, TuningMaxBaseHealth,
                () => cfg().Character.PlayerBaseHealth, v => cfg().Character.PlayerBaseHealth = v));
            list.Add(IntParam("playerArmorBaseline", tab, CombatTuningLayer.RollFeel, "Hero bases",
                "Hero base armor", "Flat Defense rating baseline for hero", 0, 50,
                () => cfg().Combat.PlayerBaseArmor, v => cfg().Combat.PlayerBaseArmor = v));

            // Damage equation: attrDmg = floor((STR + Primary) × scale)
            list.Add(DoubleParam("attributeDamageScale", tab, CombatTuningLayer.WinRate, "Damage equation",
                "Attribute damage scale", "attrDmg = floor((STR + Primary) × scale); 0.05 → 1 at base 10+10", 0.01, 2.0, 0.01,
                () => cfg().Combat.AttributeDamageScale, v => cfg().Combat.AttributeDamageScale = v));
            list.Add(IntParam("minimumDamage", tab, CombatTuningLayer.RollFeel, "Damage equation",
                "Minimum damage floor", "Lowest damage after armor", 1, 10,
                () => cfg().Combat.MinimumDamage, v => cfg().Combat.MinimumDamage = v));
            list.Add(IntParam("maximumDamageCap", tab, CombatTuningLayer.RollFeel, "Damage equation",
                "Maximum damage cap", "Hard cap on single hit damage", 100, 9999,
                () => cfg().Combat.MaximumDamageCap, v => cfg().Combat.MaximumDamageCap = v));
            list.Add(DoubleParam("armorReductionFactor", tab, CombatTuningLayer.RollFeel, "Damage equation",
                "Armor reduction factor (K)", "DR = effective / (effective + K)", 10, 500, 5,
                () => cfg().Combat.ArmorReductionFactor, v => cfg().Combat.ArmorReductionFactor = v));

            // Enemy baselines
            list.Add(IntParam("enemyBaselineHealth", tab, CombatTuningLayer.Duration, "Enemy baselines",
                "Enemy base health", "Global enemy HP anchor at level 1", 10, TuningMaxBaseHealth,
                () => cfg().EnemySystem.BaselineStats.Health, v => cfg().EnemySystem.BaselineStats.Health = v));
            list.Add(IntParam("enemyBaselineStrength", tab, CombatTuningLayer.WinRate, "Enemy baselines",
                "Enemy baseline strength", "Enemy damage baseline", 1, 50,
                () => cfg().EnemySystem.BaselineStats.Strength, v => cfg().EnemySystem.BaselineStats.Strength = v));
            list.Add(IntParam("enemyBaselineAgility", tab, CombatTuningLayer.Duration, "Enemy baselines",
                "Enemy baseline agility", "Enemy AGI anchor at level 1", 1, 50,
                () => cfg().EnemySystem.BaselineStats.Agility, v => cfg().EnemySystem.BaselineStats.Agility = v));
            list.Add(IntParam("enemyBaselineTechnique", tab, CombatTuningLayer.Duration, "Enemy baselines",
                "Enemy baseline technique", "Enemy TEC anchor at level 1", 1, 50,
                () => cfg().EnemySystem.BaselineStats.Technique, v => cfg().EnemySystem.BaselineStats.Technique = v));
            list.Add(IntParam("enemyBaselineIntelligence", tab, CombatTuningLayer.Duration, "Enemy baselines",
                "Enemy baseline intelligence", "Enemy INT anchor at level 1", 1, 50,
                () => cfg().EnemySystem.BaselineStats.Intelligence, v => cfg().EnemySystem.BaselineStats.Intelligence = v));
            list.Add(IntParam("enemyBaselineArmor", tab, CombatTuningLayer.WinRate, "Enemy baselines",
                "Enemy baseline armor", "Flat Defense on enemies", 0, 50,
                () => cfg().EnemySystem.BaselineStats.Armor, v => cfg().EnemySystem.BaselineStats.Armor = v));
            list.Add(IntParam("enemyAttributePoolBase", tab, CombatTuningLayer.Duration, "Enemy baselines",
                "Attribute pool base (L1)", "Shared attribute budget at level 1", 1, 200,
                () => cfg().EnemyBalance.AttributePool.BasePointsAtLevel1,
                v => cfg().EnemyBalance.AttributePool.BasePointsAtLevel1 = (int)v));
            list.Add(IntParam("enemySustainPoolBase", tab, CombatTuningLayer.Duration, "Enemy baselines",
                "Sustain pool base (L1)", "Health/armor budget at level 1", 1, 500,
                () => cfg().EnemyBalance.SustainPool.BasePointsAtLevel1,
                v => cfg().EnemyBalance.SustainPool.BasePointsAtLevel1 = (int)v));

            // Tempo (L1)
            list.Add(DoubleParam("baseAttackTime", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Base attack time (s)", "Baseline seconds per attack before agility", 1, 20, 0.5,
                () => cfg().Combat.BaseAttackTime, v => cfg().Combat.BaseAttackTime = v));
            list.Add(DoubleParam("minimumAttackTime", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Minimum attack time (s)", "Floor for final attack time", 0.01, 2, 0.01,
                () => cfg().Combat.MinimumAttackTime, v => cfg().Combat.MinimumAttackTime = v));
            list.Add(IntParam("agilityMin", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Agility min", "Minimum agility for speed curve", 1, 50,
                () => cfg().Combat.AgilityMin, v => cfg().Combat.AgilityMin = v));
            list.Add(IntParam("agilityMax", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Agility max", "Maximum agility for speed curve", 50, 200,
                () => cfg().Combat.AgilityMax, v => cfg().Combat.AgilityMax = v));
            list.Add(DoubleParam("agilityMinSpeedMult", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Agility min speed mult", "Speed multiplier at minimum agility", 0.5, 1.5, 0.01,
                () => cfg().Combat.AgilityMinSpeedMultiplier, v => cfg().Combat.AgilityMinSpeedMultiplier = v));
            list.Add(DoubleParam("agilityMaxSpeedMult", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Agility max speed mult", "Speed multiplier at maximum agility", 0.01, 1.0, 0.01,
                () => cfg().Combat.AgilityMaxSpeedMultiplier, v => cfg().Combat.AgilityMaxSpeedMultiplier = v));
            list.Add(DoubleParam("weaponAttackTimeClampMin", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Weapon attack time clamp min", "Minimum weapon speed multiplier", 0.1, 1.0, 0.05,
                () => cfg().Combat.WeaponAttackTimeClampMin, v => cfg().Combat.WeaponAttackTimeClampMin = v));
            list.Add(DoubleParam("weaponAttackTimeClampMax", tab, CombatTuningLayer.RollFeel, "Tempo",
                "Weapon attack time clamp max", "Maximum weapon speed multiplier", 1.0, 3.0, 0.05,
                () => cfg().Combat.WeaponAttackTimeClampMax, v => cfg().Combat.WeaponAttackTimeClampMax = v));

            // Combo start
            list.Add(IntParam("comboSequenceBaseMax", tab, CombatTuningLayer.ComboAffordance, "Combo start",
                "Combo sequence base max", "Default combo strip length before gear", 1, 8,
                () => cfg().LootSystem.ComboSequenceBaseMax, v => cfg().LootSystem.ComboSequenceBaseMax = v));
            list.Add(IntParam("comboIntelligenceThreshold", tab, CombatTuningLayer.ComboAffordance, "Combo start",
                "Combo INT threshold", "INT for ordered strip + ComboRouter", 1, 30,
                () => cfg().LootSystem.ComboSequenceIntelligenceThreshold,
                v => cfg().LootSystem.ComboSequenceIntelligenceThreshold = v));

            // Starting weapons
            list.Add(IntParam("startingDamageMace", tab, CombatTuningLayer.WinRate, "Starting weapons",
                "Starting damage — Mace", "Level 1 mace base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Mace ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Mace = v; }));
            list.Add(IntParam("startingDamageSword", tab, CombatTuningLayer.WinRate, "Starting weapons",
                "Starting damage — Sword", "Level 1 sword base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Sword ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Sword = v; }));
            list.Add(IntParam("startingDamageDagger", tab, CombatTuningLayer.WinRate, "Starting weapons",
                "Starting damage — Dagger", "Level 1 dagger base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Dagger ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Dagger = v; }));
            list.Add(IntParam("startingDamageWand", tab, CombatTuningLayer.WinRate, "Starting weapons",
                "Starting damage — Wand", "Level 1 wand base damage", 1, 30,
                () => cfg().WeaponScaling?.StartingWeaponDamage.Wand ?? 0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.StartingWeaponDamage.Wand = v; }));

            // Roll ladder
            list.Add(IntParam("missThresholdMin", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Miss threshold min", "Lowest roll that counts as miss", 1, 10,
                () => cfg().RollSystem.MissThreshold.Min, v => cfg().RollSystem.MissThreshold.Min = v));
            list.Add(IntParam("missThresholdMax", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Miss threshold max", "Miss band upper bound", 1, 10,
                () => cfg().RollSystem.MissThreshold.Max, v => cfg().RollSystem.MissThreshold.Max = v));
            list.Add(IntParam("basicAttackMin", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Basic attack roll min", "Normal hit band lower bound", 1, 20,
                () => cfg().RollSystem.BasicAttackThreshold.Min, v => cfg().RollSystem.BasicAttackThreshold.Min = v));
            list.Add(IntParam("basicAttackMax", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Basic attack roll max", "Normal hit band upper bound", 1, 20,
                () => cfg().RollSystem.BasicAttackThreshold.Max, v => cfg().RollSystem.BasicAttackThreshold.Max = v));
            list.Add(IntParam("comboThresholdMin", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Combo roll min", "Combo-tier band lower bound", 1, 20,
                () => cfg().RollSystem.ComboThreshold.Min, v => cfg().RollSystem.ComboThreshold.Min = v));
            list.Add(IntParam("comboThresholdMax", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Combo roll max", "Combo-tier band upper bound", 1, 20,
                () => cfg().RollSystem.ComboThreshold.Max, v => cfg().RollSystem.ComboThreshold.Max = v));
            list.Add(IntParam("criticalThreshold", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Critical threshold (roll system)", "Natural crit on d20 ladder", 1, 20,
                () => cfg().RollSystem.CriticalThreshold, v => cfg().RollSystem.CriticalThreshold = v));
            list.Add(IntParam("criticalHitThreshold", tab, CombatTuningLayer.RollFeel, "Roll ladder",
                "Critical hit threshold (combat)", "Crit eval threshold in damage calc", 1, 20,
                () => cfg().Combat.CriticalHitThreshold, v => cfg().Combat.CriticalHitThreshold = v));
        }

        /// <summary>Growth with level/dungeon, progression curve, class mults, goals.</summary>
        private static void BuildScalingParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            var gs = () => GameSettings.Instance;
            var goals = () => cfg().BalanceTuningGoals;
            const CombatTuningTab tab = CombatTuningTab.Scaling;

            // Level growth
            list.Add(IntParam("playerAttributesPerLevel", tab, CombatTuningLayer.WinRate, "Level growth",
                "Player attributes per level", "Legacy stat growth per level-up", 0, 5,
                () => cfg().Attributes.PlayerAttributesPerLevel, v => cfg().Attributes.PlayerAttributesPerLevel = v));
            list.Add(IntParam("playerPrimaryStatPerLevel", tab, CombatTuningLayer.WinRate, "Level growth",
                "Primary stat per level", "+primary stat on weapon-based level-up", 1, 10,
                () => cfg().Attributes.PlayerPrimaryStatPerLevel, v => cfg().Attributes.PlayerPrimaryStatPerLevel = v));
            list.Add(IntParam("playerSecondaryStatPerLevel", tab, CombatTuningLayer.WinRate, "Level growth",
                "Secondary stat per level", "+other stats on weapon-based level-up", 0, 5,
                () => cfg().Attributes.PlayerSecondaryStatPerLevel, v => cfg().Attributes.PlayerSecondaryStatPerLevel = v));
            list.Add(IntParam("playerHealthPerLevel", tab, CombatTuningLayer.Duration, "Level growth",
                "Player health per level", "Hero HP slope per level", 0, TuningMaxHealthPerLevel,
                () => cfg().Character.HealthPerLevel, v => cfg().Character.HealthPerLevel = v));
            list.Add(IntParam("enemyHealthPerLevel", tab, CombatTuningLayer.Duration, "Level growth",
                "Enemy health per level", "Enemy HP growth per level (legacy slope)", 0, TuningMaxHealthPerLevel,
                () => cfg().EnemySystem.ScalingPerLevel.Health, v => cfg().EnemySystem.ScalingPerLevel.Health = v));
            list.Add(DoubleParam("enemyAttributesPerLevel", tab, CombatTuningLayer.WinRate, "Level growth",
                "Enemy attributes per level", "Global enemy attribute growth per level", 0, 10, 0.1,
                () => cfg().EnemySystem.ScalingPerLevel.Attributes,
                v => cfg().EnemySystem.ScalingPerLevel.Attributes = v));
            list.Add(DoubleParam("enemyArmorPerLevel", tab, CombatTuningLayer.WinRate, "Level growth",
                "Enemy armor per level", "Enemy armor growth per level", 0, 5, 0.05,
                () => cfg().EnemySystem.ScalingPerLevel.Armor,
                v => cfg().EnemySystem.ScalingPerLevel.Armor = v));
            list.Add(DoubleParam("enemyAttributeGrowthBudget", tab, CombatTuningLayer.WinRate, "Level growth",
                "Enemy attribute growth budget", "STR+AGI+TEC+INT growth sum per level", 1, 20, 0.5,
                () => cfg().EnemySystem.AttributeGrowthBudgetPerLevel,
                v => cfg().EnemySystem.AttributeGrowthBudgetPerLevel = v));
            list.Add(IntParam("enemyAttributePoolPerLevel", tab, CombatTuningLayer.Duration, "Level growth",
                "Attribute pool per level", "Attribute points added per level to the shared pool", 0, 50,
                () => cfg().EnemyBalance.AttributePool.PointsPerLevel,
                v => cfg().EnemyBalance.AttributePool.PointsPerLevel = (int)v));
            list.Add(IntParam("enemySustainPoolPerLevel", tab, CombatTuningLayer.Duration, "Level growth",
                "Sustain pool per level", "Sustain budget points per level", 0, 100,
                () => cfg().EnemyBalance.SustainPool.PointsPerLevel,
                v => cfg().EnemyBalance.SustainPool.PointsPerLevel = (int)v));
            list.Add(IntParam("enemyPrimaryAttributeBonus", tab, CombatTuningLayer.WinRate, "Level growth",
                "Enemy primary attribute bonus", "Bonus to enemy primary stat", 0, 10,
                () => cfg().Attributes.EnemyPrimaryAttributeBonus, v => cfg().Attributes.EnemyPrimaryAttributeBonus = v));
            list.Add(IntParam("wandComboSlotBonus", tab, CombatTuningLayer.WinRate, "Level growth",
                "Wand combo slot bonus", "Extra combo slots when wand equipped", 0, 5,
                () => cfg().ClassPresentation.WandEquippedComboSlotBonus,
                v => cfg().ClassPresentation.WandEquippedComboSlotBonus = v));

            // Progression curve
            list.Add(DoubleParam("combatTempoScale", tab, CombatTuningLayer.Duration, "Progression curve",
                "Combat tempo scale", "Stretches all enemy HP uniformly", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.CombatTempoScale,
                v => cfg().EnemySystem.ProgressionScales.CombatTempoScale = v));
            list.Add(DoubleParam("progressionShape", tab, CombatTuningLayer.Duration, "Progression curve",
                "Progression shape", "0 = early/base-dominated; 1 = growth earlier at high levels", 0, 1, 0.05,
                () => cfg().EnemySystem.ProgressionScales.ProgressionShape,
                v => cfg().EnemySystem.ProgressionScales.ProgressionShape = v));
            list.Add(DoubleParam("playerEnemyParity", tab, CombatTuningLayer.WinRate, "Progression curve",
                "Player/enemy parity", "+ toward hero survivability; − toward enemies", -1, 1, 0.05,
                () => cfg().EnemySystem.ProgressionScales.PlayerEnemyParity,
                v => cfg().EnemySystem.ProgressionScales.PlayerEnemyParity = v));
            list.Add(IntParam("progressionPivotLevel", tab, CombatTuningLayer.Duration, "Progression curve",
                "Progression pivot level", "Level where growth weight reaches full strength", 10, 100,
                () => cfg().EnemySystem.ProgressionScales.ProgressionPivotLevel,
                v => cfg().EnemySystem.ProgressionScales.ProgressionPivotLevel = v));
            list.Add(DoubleParam("baseHealthScale", tab, CombatTuningLayer.WinRate, "Progression curve",
                "Enemy base health scale", "Multiplies enemy base HP at all levels", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.BaseHealthScale,
                v => cfg().EnemySystem.ProgressionScales.BaseHealthScale = v));
            list.Add(DoubleParam("healthGrowthScale", tab, CombatTuningLayer.WinRate, "Progression curve",
                "Enemy health growth scale", "Multiplies per-level enemy HP growth", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.HealthGrowthScale,
                v => cfg().EnemySystem.ProgressionScales.HealthGrowthScale = v));
            list.Add(DoubleParam("attributeGrowthScale", tab, CombatTuningLayer.WinRate, "Progression curve",
                "Attribute growth scale", "Enemy STR/AGI/TEC/INT growth", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.ProgressionScales.AttributeGrowthScale,
                v => cfg().EnemySystem.ProgressionScales.AttributeGrowthScale = v));

            // Global multipliers (scale whole fight)
            list.Add(DoubleParam("globalEnemyHealthMult", tab, CombatTuningLayer.Duration, "Global multipliers",
                "Global enemy health multiplier", "All spawned enemy max HP", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.HealthMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.HealthMultiplier = v));
            list.Add(DoubleParam("globalEnemyDamageMult", tab, CombatTuningLayer.WinRate, "Global multipliers",
                "Global enemy damage multiplier", "Enemy STR/TEC/INT scaling at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.DamageMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.DamageMultiplier = v));
            list.Add(DoubleParam("globalEnemySpeedMult", tab, CombatTuningLayer.WinRate, "Global multipliers",
                "Global enemy speed multiplier", "Enemy AGI scaling at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.SpeedMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.SpeedMultiplier = v));
            list.Add(DoubleParam("globalEnemyArmorMult", tab, CombatTuningLayer.RollFeel, "Global multipliers",
                "Global enemy armor multiplier", "Enemy armor at spawn", 0.25, 3.0, 0.05,
                () => cfg().EnemySystem.GlobalMultipliers.ArmorMultiplier,
                v => cfg().EnemySystem.GlobalMultipliers.ArmorMultiplier = v));
            list.Add(DoubleParam("globalWeaponDamageMult", tab, CombatTuningLayer.WinRate, "Global multipliers",
                "Global weapon damage multiplier", "All weapon base damage scaling", 0.25, 3.0, 0.05,
                () => cfg().WeaponScaling?.GlobalDamageMultiplier ?? 1.0,
                v => { var ws = cfg().WeaponScaling; if (ws != null) ws.GlobalDamageMultiplier = v; }));

            // Combo growth
            list.Add(IntParam("comboSequenceAbsoluteMax", tab, CombatTuningLayer.ComboAffordance, "Combo growth",
                "Combo sequence absolute max", "Hard cap on combo strip length", 1, 12,
                () => cfg().LootSystem.ComboSequenceAbsoluteMax, v => cfg().LootSystem.ComboSequenceAbsoluteMax = v));
            list.Add(DoubleParam("comboAmplifierMax", tab, CombatTuningLayer.ComboAffordance, "Combo growth",
                "Combo amplifier max", "Base AMP multiplier at max TECH", 1, 5, 0.1,
                () => cfg().ComboSystem.ComboAmplifierMax, v => cfg().ComboSystem.ComboAmplifierMax = v));
            list.Add(IntParam("comboAmplifierMaxTech", tab, CombatTuningLayer.ComboAffordance, "Combo growth",
                "Combo amplifier max TECH", "Effective TECH where base AMP reaches max", 20, 200,
                () => cfg().ComboSystem.ComboAmplifierMaxTech, v => cfg().ComboSystem.ComboAmplifierMaxTech = v));
            list.Add(DoubleParam("comboAmplifierCurveExponent", tab, CombatTuningLayer.ComboAffordance, "Combo growth",
                "Combo amplifier curve exponent", "Early/mid TECH power-ramp exponent", 0.5, 2.5, 0.05,
                () => cfg().ComboSystem.ComboAmplifierCurveExponent,
                v => cfg().ComboSystem.ComboAmplifierCurveExponent = v));

            // Class multipliers
            AddClassMultipliers(list, tab, CombatTuningLayer.WinRate, "Barbarian (Mace)", "barbarian",
                () => cfg().ClassBalance.Barbarian);
            AddClassMultipliers(list, tab, CombatTuningLayer.WinRate, "Warrior (Sword)", "warrior",
                () => cfg().ClassBalance.Warrior);
            AddClassMultipliers(list, tab, CombatTuningLayer.WinRate, "Rogue (Dagger)", "rogue",
                () => cfg().ClassBalance.Rogue);
            AddClassMultipliers(list, tab, CombatTuningLayer.WinRate, "Wizard (Wand)", "wizard",
                () => cfg().ClassBalance.Wizard);

            // Runtime difficulty
            list.Add(DoubleParam("runtimePlayerHealthMult", tab, CombatTuningLayer.WinRate, "Runtime difficulty",
                "Runtime player health multiplier", "Difficulty overlay on current player HP", 0.5, 3.0, 0.1,
                () => gs().PlayerHealthMultiplier, v => gs().PlayerHealthMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimePlayerDamageMult", tab, CombatTuningLayer.WinRate, "Runtime difficulty",
                "Runtime player damage multiplier", "Difficulty overlay on player damage", 0.5, 3.0, 0.1,
                () => gs().PlayerDamageMultiplier, v => gs().PlayerDamageMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimeEnemyHealthMult", tab, CombatTuningLayer.WinRate, "Runtime difficulty",
                "Runtime enemy health multiplier", "Difficulty overlay on spawned enemy HP", 0.5, 3.0, 0.1,
                () => gs().EnemyHealthMultiplier, v => gs().EnemyHealthMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimeEnemyDamageMult", tab, CombatTuningLayer.WinRate, "Runtime difficulty",
                "Runtime enemy damage multiplier", "Difficulty overlay on enemy damage", 0.5, 3.0, 0.1,
                () => gs().EnemyDamageMultiplier, v => gs().EnemyDamageMultiplier = v, usesGameSettings: true));
            list.Add(DoubleParam("runtimeCombatSpeed", tab, CombatTuningLayer.RollFeel, "Runtime difficulty",
                "Runtime combat speed", "UI/combat pacing multiplier", 0.5, 2.0, 0.1,
                () => gs().CombatSpeed, v => gs().CombatSpeed = v, usesGameSettings: true));

            // Goals (targets for sim / quality)
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
            list.Add(IntParam("simulationsPerMatchup", tab, CombatTuningLayer.Goals, "Goals",
                "Simulations per matchup", "Battles per weapon/enemy pair in analysis", 10, 1000,
                () => cfg().BalanceAnalysis.SimulationsPerMatchup,
                v => cfg().BalanceAnalysis.SimulationsPerMatchup = v));
        }

        /// <summary>Items, actions, archetypes, status, loot, and combat feel fine-tuning.</summary>
        private static void BuildOutfitParameters(List<CombatTuningParameter> list)
        {
            var cfg = () => GameConfiguration.Instance;
            var gs = () => GameSettings.Instance;
            const CombatTuningTab tab = CombatTuningTab.Outfit;

            BuildVarianceCompressionParameters(list, cfg, tab);

            // Weapons & gear
            list.Add(IntParam("weaponDamagePerTier", tab, CombatTuningLayer.WinRate, "Weapons & gear",
                "Weapon damage per tier", "Damage added per equipment tier", 1, 20,
                () => cfg().EquipmentScaling.WeaponDamagePerTier,
                v => cfg().EquipmentScaling.WeaponDamagePerTier = v));
            list.Add(IntParam("armorValuePerTier", tab, CombatTuningLayer.WinRate, "Weapons & gear",
                "Armor value per tier", "Armor added per equipment tier", 1, 20,
                () => cfg().EquipmentScaling.ArmorValuePerTier,
                v => cfg().EquipmentScaling.ArmorValuePerTier = v));
            list.Add(DoubleParam("speedBonusPerTier", tab, CombatTuningLayer.WinRate, "Weapons & gear",
                "Speed bonus per tier", "Attack speed bonus per tier", 0, 0.5, 0.01,
                () => cfg().EquipmentScaling.SpeedBonusPerTier,
                v => cfg().EquipmentScaling.SpeedBonusPerTier = v));
            list.Add(IntParam("equipmentMaxTier", tab, CombatTuningLayer.WinRate, "Weapons & gear",
                "Equipment max tier", "Maximum equipment tier", 1, 20,
                () => cfg().EquipmentScaling.MaxTier, v => cfg().EquipmentScaling.MaxTier = v));

            // Actions & feel
            list.Add(DoubleParam("swiftActionSpeedPercent", tab, CombatTuningLayer.RollFeel, "Actions & feel",
                "Swift speed bonus (%)", "SPEED_MOD from swift tag", 0, 50, 1,
                () => cfg().CombatBalance.ActionMechanics.SwiftSpeedPercent,
                v => cfg().CombatBalance.ActionMechanics.SwiftSpeedPercent = v));
            list.Add(DoubleParam("bludgeonDamagePercent", tab, CombatTuningLayer.RollFeel, "Actions & feel",
                "Bludgeon damage bonus (%)", "DAMAGE_MOD from bludgeon tag", 0, 50, 1,
                () => cfg().CombatBalance.ActionMechanics.BludgeonDamagePercent,
                v => cfg().CombatBalance.ActionMechanics.BludgeonDamagePercent = v));
            list.Add(DoubleParam("criticalHitChance", tab, CombatTuningLayer.RollFeel, "Actions & feel",
                "Critical hit chance", "Base critical hit chance modifier", 0, 1, 0.01,
                () => cfg().CombatBalance.CriticalHitChance,
                v => cfg().CombatBalance.CriticalHitChance = v));
            list.Add(DoubleParam("comboAmplificationScalingMult", tab, CombatTuningLayer.RollFeel, "Actions & feel",
                "Combo amplification scaling", "AMP scaling in roll damage path", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.ComboAmplificationScalingMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.ComboAmplificationScalingMultiplier = v));
            list.Add(DoubleParam("tierScalingFallbackMult", tab, CombatTuningLayer.RollFeel, "Actions & feel",
                "Tier scaling fallback mult", "Fallback when tier scaling missing", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.TierScalingFallbackMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.TierScalingFallbackMultiplier = v));
            list.Add(DoubleParam("critMissComfortShift", tab, CombatTuningLayer.RollFeel, "Actions & feel",
                "Crit/miss comfort shift", "Roll band comfort adjustment", -5, 5, 0.5,
                () => cfg().CombatBalance.CritMissComfortShift,
                v => cfg().CombatBalance.CritMissComfortShift = v));
            list.Add(IntParam("enemyLevelVariance", tab, CombatTuningLayer.WinRate, "Actions & feel",
                "Enemy level variance", "Spawn level jitter ±N", 0, 10,
                () => cfg().EnemySystem.LevelVariance, v => cfg().EnemySystem.LevelVariance = v));

            // Enemy pool conversion (fine outfit of enemy generation)
            list.Add(DoubleParam("statConversionStrength", tab, CombatTuningLayer.WinRate, "Enemy conversion",
                "STR per point", "Enemy strength conversion from attribute pool", 0.1, 3.0, 0.05,
                () => cfg().EnemyBalance.StatConversionRates.StrengthPerPoint,
                v => cfg().EnemyBalance.StatConversionRates.StrengthPerPoint = v));
            list.Add(DoubleParam("statConversionAgility", tab, CombatTuningLayer.Duration, "Enemy conversion",
                "AGI per point", "Enemy agility conversion (attack speed)", 0.1, 3.0, 0.05,
                () => cfg().EnemyBalance.StatConversionRates.AgilityPerPoint,
                v => cfg().EnemyBalance.StatConversionRates.AgilityPerPoint = v));
            list.Add(DoubleParam("statConversionTechnique", tab, CombatTuningLayer.RollFeel, "Enemy conversion",
                "TEC per point", "Enemy technique conversion", 0.1, 3.0, 0.05,
                () => cfg().EnemyBalance.StatConversionRates.TechniquePerPoint,
                v => cfg().EnemyBalance.StatConversionRates.TechniquePerPoint = v));
            list.Add(DoubleParam("statConversionIntelligence", tab, CombatTuningLayer.ComboAffordance, "Enemy conversion",
                "INT per point", "Enemy intelligence conversion", 0.1, 3.0, 0.05,
                () => cfg().EnemyBalance.StatConversionRates.IntelligencePerPoint,
                v => cfg().EnemyBalance.StatConversionRates.IntelligencePerPoint = v));
            list.Add(DoubleParam("statConversionHealth", tab, CombatTuningLayer.Duration, "Enemy conversion",
                "Health per point", "Enemy health conversion from sustain pool", 0.1, 3.0, 0.05,
                () => cfg().EnemyBalance.StatConversionRates.HealthPerPoint,
                v => cfg().EnemyBalance.StatConversionRates.HealthPerPoint = v));
            list.Add(DoubleParam("statConversionArmor", tab, CombatTuningLayer.WinRate, "Enemy conversion",
                "Armor per point", "Enemy armor conversion from sustain pool", 0.1, 3.0, 0.05,
                () => cfg().EnemyBalance.StatConversionRates.ArmorPerPoint,
                v => cfg().EnemyBalance.StatConversionRates.ArmorPerPoint = v));

            // Rewards & loot
            list.Add(IntParam("enemyXPBase", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Enemy XP base", "Base XP from defeating an enemy", 0, 200,
                () => cfg().Progression.EnemyXPBase, v => cfg().Progression.EnemyXPBase = v));
            list.Add(IntParam("enemyXPPerLevel", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Enemy XP per level", "XP bonus per enemy level", 0, 50,
                () => cfg().Progression.EnemyXPPerLevel, v => cfg().Progression.EnemyXPPerLevel = v));
            list.Add(IntParam("enemyGoldBase", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Enemy gold base", "Base gold from defeating an enemy", 0, 200,
                () => cfg().Progression.EnemyGoldBase, v => cfg().Progression.EnemyGoldBase = v));
            list.Add(IntParam("enemyGoldPerLevel", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Enemy gold per level", "Gold bonus per enemy level", 0, 50,
                () => cfg().Progression.EnemyGoldPerLevel, v => cfg().Progression.EnemyGoldPerLevel = v));
            list.Add(DoubleParam("baseDropChance", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Base drop chance", "Base item drop probability", 0, 1, 0.01,
                () => cfg().LootSystem.BaseDropChance, v => cfg().LootSystem.BaseDropChance = v));
            list.Add(DoubleParam("maxDropChance", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Max drop chance", "Maximum item drop probability", 0, 1, 0.01,
                () => cfg().LootSystem.MaxDropChance, v => cfg().LootSystem.MaxDropChance = v));
            list.Add(DoubleParam("goldDropMultiplier", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Gold drop multiplier", "Scales gold drops from combat", 0.25, 3.0, 0.05,
                () => cfg().LootSystem.GoldDropMultiplier, v => cfg().LootSystem.GoldDropMultiplier = v));
            list.Add(IntParam("enableComboSystem", tab, CombatTuningLayer.Duration, "Rewards & loot",
                "Enable combo system", "1 = enabled, 0 = disabled", 0, 1,
                () => gs().EnableComboSystem ? 1 : 0, v => gs().EnableComboSystem = v >= 1,
                usesGameSettings: true));

            // Status effects (global + per-effect via FilterKey)
            var scaling = () => cfg().CombatBalance.StatusEffectScaling;
            list.Add(DoubleParam("statusBleedDuration", tab, CombatTuningLayer.RollFeel, "Status — global",
                "Bleed duration scale", "Global bleed duration multiplier", 0, 10, 0.1,
                () => scaling().BleedDuration, v => scaling().BleedDuration = v));
            list.Add(DoubleParam("statusPoisonDuration", tab, CombatTuningLayer.RollFeel, "Status — global",
                "Poison duration scale", "Global poison duration multiplier", 0, 10, 0.1,
                () => scaling().PoisonDuration, v => scaling().PoisonDuration = v));
            list.Add(DoubleParam("statusStunDuration", tab, CombatTuningLayer.RollFeel, "Status — global",
                "Stun duration scale", "Global stun duration multiplier", 0, 10, 0.1,
                () => scaling().StunDuration, v => scaling().StunDuration = v));
            list.Add(DoubleParam("statusBurnDuration", tab, CombatTuningLayer.RollFeel, "Status — global",
                "Burn duration scale", "Global burn duration multiplier", 0, 10, 0.1,
                () => scaling().BurnDuration, v => scaling().BurnDuration = v));
            list.Add(DoubleParam("statusFreezeDuration", tab, CombatTuningLayer.RollFeel, "Status — global",
                "Freeze duration scale", "Global freeze duration multiplier", 0, 10, 0.1,
                () => scaling().FreezeDuration, v => scaling().FreezeDuration = v));
            list.Add(DoubleParam("statusEffectDamageScaling", tab, CombatTuningLayer.RollFeel, "Status — global",
                "Status effect damage scaling", "Global DoT damage scaling", 0, 5, 0.1,
                () => scaling().StatusEffectDamageScaling, v => scaling().StatusEffectDamageScaling = v));

            foreach (var effect in StatusEffectNames)
            {
                list.Add(IntParam($"fx_{effect}_damagePerTick", tab, CombatTuningLayer.RollFeel, "Status — per effect",
                    "Damage per tick", $"{effect} damage per tick", 0, 50,
                    () => GetOrCreateEffect(cfg(), effect).DamagePerTick,
                    v => GetOrCreateEffect(cfg(), effect).DamagePerTick = v,
                    filterKey: effect));
                list.Add(DoubleParam($"fx_{effect}_tickInterval", tab, CombatTuningLayer.RollFeel, "Status — per effect",
                    "Tick interval (s)", $"{effect} seconds between ticks", 0, 10, 0.1,
                    () => GetOrCreateEffect(cfg(), effect).TickInterval,
                    v => GetOrCreateEffect(cfg(), effect).TickInterval = v,
                    filterKey: effect));
                list.Add(IntParam($"fx_{effect}_maxStacks", tab, CombatTuningLayer.RollFeel, "Status — per effect",
                    "Max stacks", $"{effect} maximum stack count", 0, 20,
                    () => GetOrCreateEffect(cfg(), effect).MaxStacks,
                    v => GetOrCreateEffect(cfg(), effect).MaxStacks = v,
                    filterKey: effect));
                list.Add(DoubleParam($"fx_{effect}_duration", tab, CombatTuningLayer.RollFeel, "Status — per effect",
                    "Duration (s)", $"{effect} effect duration", 0, 60, 0.5,
                    () => GetOrCreateEffect(cfg(), effect).Duration,
                    v => GetOrCreateEffect(cfg(), effect).Duration = v,
                    filterKey: effect));
            }

            // Archetypes (FilterKey selects which set the Outfit UI shows)
            foreach (var archetype in ArchetypeNames)
            {
                string key = archetype;
                list.Add(DoubleParam($"arch_{key}_health", tab, CombatTuningLayer.WinRate, "Archetypes",
                    "Health multiplier", "Scales archetype base health", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Health,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Health = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_strength", tab, CombatTuningLayer.WinRate, "Archetypes",
                    "Strength multiplier", "Scales archetype STR", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Strength,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Strength = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_agility", tab, CombatTuningLayer.WinRate, "Archetypes",
                    "Agility multiplier", "Scales archetype AGI", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Agility,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Agility = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_technique", tab, CombatTuningLayer.WinRate, "Archetypes",
                    "Technique multiplier", "Scales archetype TEC", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Technique,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Technique = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_intelligence", tab, CombatTuningLayer.WinRate, "Archetypes",
                    "Intelligence multiplier", "Scales archetype INT", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Intelligence,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Intelligence = v,
                    filterKey: key));
                list.Add(DoubleParam($"arch_{key}_armor", tab, CombatTuningLayer.WinRate, "Archetypes",
                    "Armor multiplier", "Scales archetype armor", 0.1, 3.0, 0.05,
                    () => GetOrCreateArchetype(cfg().EnemySystem, key).Armor,
                    v => GetOrCreateArchetype(cfg().EnemySystem, key).Armor = v,
                    filterKey: key));
            }
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
                "Combo roll damage multiplier", "Raw damage on combo-tier rolls", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier = v));

            list.Add(DoubleParam("basicRollDamageMult", tab, layer, group,
                "Basic roll damage multiplier", "Damage on normal hit band", 0.5, 3.0, 0.05,
                () => cfg().CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier,
                v => cfg().CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier = v));
        }
    }
}
