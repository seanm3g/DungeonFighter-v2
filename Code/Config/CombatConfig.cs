using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Combat system configuration
    /// </summary>
    public class CombatConfig
    {
        /// <summary>Used when tuning JSON omits or zeroes base attack time (seconds per attack baseline before agility/weapon).</summary>
        public const double DefaultBaseAttackTimeSeconds = 8.0;

        /// <summary>Floor for final attack time when tuning omits or zeroes <see cref="MinimumAttackTime"/>.</summary>
        public const double DefaultMinimumAttackTimeSeconds = 0.1;

        public int CriticalHitThreshold { get; set; }
        public double CriticalHitMultiplier { get; set; }
        public int MinimumDamage { get; set; }
        public int PlayerBaseArmor { get; set; }
        public int MaximumDamageCap { get; set; } = 999;
        public double ArmorReductionFactor { get; set; } = 100.0;
        public double WeaponAttackTimeClampMin { get; set; } = 0.5;
        public double WeaponAttackTimeClampMax { get; set; } = 1.5;
        public double TutorialCombatDelayMultiplier { get; set; } = 2.0;
        public double LevelDifferenceDamageScaling { get; set; }
        public double EnemyEnrageThresholdPercent { get; set; } = 25.0;
        public double EnemyEnrageDamageMult { get; set; } = 1.25;
        public double ComboBreakPenaltyPercent { get; set; } = 10.0;
        public double StatusEffectBaseProcChance { get; set; } = 1.0;
        public double GlobalHealEffectiveness { get; set; } = 1.0;
        public double FirstStrikeBonusPercent { get; set; }
        public double OverkillDamagePercent { get; set; }
        public int AttributeSoftCap { get; set; } = 50;
        public double TechniqueMilestoneRollShift { get; set; } = 1.0;
        public double BaseAttackTime { get; set; }
        public double AgilitySpeedReduction { get; set; }
        public double MinimumAttackTime { get; set; }

        /// <summary>
        /// Replaces invalid combat timing fields (e.g. <c>baseAttackTime: 0</c> in TuningConfig.json) so attack speed and UI are not stuck at the hard floor.
        /// </summary>
        public void EnsureValidCombatTimingDefaults()
        {
            if (BaseAttackTime <= 0)
                BaseAttackTime = DefaultBaseAttackTimeSeconds;
            if (MinimumAttackTime <= 0)
                MinimumAttackTime = DefaultMinimumAttackTimeSeconds;
        }

        /// <summary>
        /// Aligns with threshold and damage safeguards used at combat runtime when tuning JSON zeros these fields.
        /// </summary>
        public void EnsureValidCombatCriticalAndDamageDefaults()
        {
            if (CriticalHitThreshold <= 0)
                CriticalHitThreshold = 20;
            if (MinimumDamage <= 0)
                MinimumDamage = 1;
            if (CriticalHitMultiplier <= 0)
                CriticalHitMultiplier = 2.0;
            if (MaximumDamageCap <= 0)
                MaximumDamageCap = 999;
            if (ArmorReductionFactor <= 0)
                ArmorReductionFactor = 100.0;
            if (WeaponAttackTimeClampMin <= 0)
                WeaponAttackTimeClampMin = 0.5;
            if (WeaponAttackTimeClampMax <= WeaponAttackTimeClampMin)
                WeaponAttackTimeClampMax = 1.5;
            if (TutorialCombatDelayMultiplier <= 0)
                TutorialCombatDelayMultiplier = 2.0;
            if (EnemyEnrageThresholdPercent <= 0)
                EnemyEnrageThresholdPercent = 25.0;
            if (EnemyEnrageDamageMult <= 0)
                EnemyEnrageDamageMult = 1.25;
            if (StatusEffectBaseProcChance <= 0)
                StatusEffectBaseProcChance = 1.0;
            if (GlobalHealEffectiveness <= 0)
                GlobalHealEffectiveness = 1.0;
            if (AttributeSoftCap <= 0)
                AttributeSoftCap = 50;
        }
        
        // Agility speed mapping parameters
        public int AgilityMin { get; set; } = 1;
        public int AgilityMax { get; set; } = 100;
        public double AgilityMinSpeedMultiplier { get; set; } = 0.99; // Speed multiplier at minimum agility (1% faster)
        public double AgilityMaxSpeedMultiplier { get; set; } = 0.01; // Speed multiplier at maximum agility (99% faster)
    }

    /// <summary>
    /// Combat balance configuration
    /// </summary>
    public class CombatBalanceConfig
    {
        public double CriticalHitChance { get; set; }
        public double CriticalHitDamageMultiplier { get; set; }
        public RollDamageMultipliersConfig RollDamageMultipliers { get; set; } = new();
        public StatusEffectScalingConfig StatusEffectScaling { get; set; } = new();
        public EnvironmentalEffectsConfig EnvironmentalEffects { get; set; } = new();
        public ActionMechanicsConfig ActionMechanics { get; set; } = new();
        public double CritMissComfortShift { get; set; } = -2.0;

        /// <summary>
        /// Master variance slider (0 = chaotic spikes, 1 = regular/smoothed). Stored for UI; sub-knobs remain editable.
        /// </summary>
        public double RollFeelVarianceCompression { get; set; } = 0.5;

        public string Description { get; set; } = "";

        /// <summary>
        /// Matches DamageCalculator safeguards that treat non-positive multipliers as 1.0 and crit damage multiplier as unusable when zero.
        /// </summary>
        public void EnsureValidRollDamageAndCritDefaults()
        {
            RollDamageMultipliers ??= new RollDamageMultipliersConfig();
            RollDamageMultipliers.EnsurePositiveMultipliers();
            if (CriticalHitDamageMultiplier <= 0)
                CriticalHitDamageMultiplier = 2.0;
        }
    }

    /// <summary>
    /// Roll damage multipliers configuration
    /// </summary>
    public class RollDamageMultipliersConfig
    {
        public double ComboRollDamageMultiplier { get; set; }
        public double BasicRollDamageMultiplier { get; set; }
        public double ComboAmplificationScalingMultiplier { get; set; }
        public double TierScalingFallbackMultiplier { get; set; }

        public void EnsurePositiveMultipliers()
        {
            // Defaults should preserve the classic "combo hits harder than basic" ladder
            // even when tuning JSON uses 0 as "unset".
            if (ComboRollDamageMultiplier <= 0)
                ComboRollDamageMultiplier = 1.5;
            if (BasicRollDamageMultiplier <= 0)
                BasicRollDamageMultiplier = 1.0;
            if (ComboAmplificationScalingMultiplier <= 0)
                ComboAmplificationScalingMultiplier = 1.0;
            if (TierScalingFallbackMultiplier <= 0)
                TierScalingFallbackMultiplier = 1.0;
        }
    }

    /// <summary>Action mechanic tag bonus percents (swift, bludgeon, etc.).</summary>
    public class ActionMechanicsConfig
    {
        public double SwiftSpeedPercent { get; set; } = 10.0;
        public double BludgeonDamagePercent { get; set; } = 15.0;

        public void EnsureValidDefaults()
        {
            if (SwiftSpeedPercent <= 0)
                SwiftSpeedPercent = 10.0;
            if (BludgeonDamagePercent <= 0)
                BludgeonDamagePercent = 15.0;
        }
    }

    /// <summary>
    /// Status effect scaling configuration
    /// </summary>
    public class StatusEffectScalingConfig
    {
        public double BleedDuration { get; set; }
        public double PoisonDuration { get; set; }
        public double StunDuration { get; set; }
        public double BurnDuration { get; set; }
        public double FreezeDuration { get; set; }
        public double StatusEffectDamageScaling { get; set; }
    }

    /// <summary>
    /// Environmental effects configuration
    /// </summary>
    public class EnvironmentalEffectsConfig
    {
        public bool EnableEnvironmentalEffects { get; set; }
        public double EnvironmentalDamageMultiplier { get; set; }
        public double EnvironmentalDebuffChance { get; set; }
        public double EnvironmentalBuffChance { get; set; }
    }

    /// <summary>
    /// Roll system configuration
    /// </summary>
    public class RollSystemConfig
    {
        public MinMaxConfig MissThreshold { get; set; } = new();
        public MinMaxConfig BasicAttackThreshold { get; set; } = new();
        public MinMaxConfig ComboThreshold { get; set; } = new();
        public int CriticalThreshold { get; set; }

        /// <summary>
        /// When key thresholds are missing or zero, applies the standard d20 miss/basic/combo ladder used as combat defaults.
        /// </summary>
        public void EnsureValidDefaultThresholdBands()
        {
            MissThreshold ??= new MinMaxConfig();
            BasicAttackThreshold ??= new MinMaxConfig();
            ComboThreshold ??= new MinMaxConfig();

            bool needsStandardBands = MissThreshold.Max <= 0
                || BasicAttackThreshold.Min <= 0
                || ComboThreshold.Min <= 0;

            if (needsStandardBands)
            {
                MissThreshold.Min = 1;
                MissThreshold.Max = 5;
                BasicAttackThreshold.Min = 6;
                BasicAttackThreshold.Max = 13;
                ComboThreshold.Min = 14;
                ComboThreshold.Max = 19;
            }

            if (CriticalThreshold <= 0)
                CriticalThreshold = 20;
        }
    }

    /// <summary>
    /// Combo system configuration
    /// </summary>
    public class ComboSystemConfig
    {
        /// <summary>Legacy tuning field; the current amplifier curve is the fixed designer log formula in <see cref="ComboAmplifierFromIntelligence"/>.</summary>
        public double ComboAmplifierAtTech5 { get; set; }

        /// <summary>Legacy tuning field retained for old configs; current INT AMP no longer clamps to a configured maximum.</summary>
        public double ComboAmplifierMax { get; set; }

        /// <summary>Legacy tuning field retained for old configs; current INT AMP has no configured max-stat breakpoint.</summary>
        public int ComboAmplifierMaxTech { get; set; }

        /// <summary>
        /// Legacy tuning field retained for old configs; current INT AMP uses a fixed logarithmic curve.
        /// </summary>
        public double ComboAmplifierCurveExponent { get; set; }

        /// <summary>
        /// Repairs legacy tuning values for older configs. The current INT AMP formula does not read these fields,
        /// but settings and saved tuning profiles may still display them.
        /// </summary>
        public void EnsureValidComboAmplifierDefaults()
        {
            if (ComboAmplifierMax <= 0)
                ComboAmplifierMax = Utils.GameConstants.COMBO_AMPLIFIER_MAX;
            if (ComboAmplifierMaxTech <= 0)
                ComboAmplifierMaxTech = Utils.GameConstants.COMBO_AMPLIFIER_MAX_TECH;
        }
    }

    /// <summary>
    /// Maps Intelligence to base combo amplifier using the designer formula <c>1 + 0.5 * log10(INT + 1)</c>.
    /// </summary>
    public static class ComboAmplifierFromIntelligence
    {
        public static double Compute(int intelligence, ComboSystemConfig combo)
        {
            int clamped = Math.Max(0, intelligence);
            return 1.0 + 0.5 * Math.Log10(clamped + 1.0);
        }
    }

    /// <summary>
    /// Poison configuration
    /// </summary>
    public class PoisonConfig
    {
        public double TickInterval { get; set; }
        public int DamagePerTick { get; set; }
        public int StacksPerApplication { get; set; }
    }

    /// <summary>
    /// Status effects configuration - now supports dynamic status effects
    /// </summary>
    public class StatusEffectsConfig
    {
        /// <summary>
        /// Dictionary of status effects by name (e.g., "Bleed", "Burn", "Stun")
        /// Allows dynamic addition/removal of status effects
        /// </summary>
        public Dictionary<string, StatusEffectConfig> Effects { get; set; } = new();
        
        /// <summary>
        /// Description of the status effects configuration
        /// </summary>
        public string Description { get; set; } = "";
        
        /// <summary>
        /// Legacy properties for backward compatibility - these now read/write to/from the Effects dictionary
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public StatusEffectConfig Bleed
        {
            get => Effects.TryGetValue("Bleed", out var effect) ? effect : new StatusEffectConfig();
            set => Effects["Bleed"] = value;
        }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public StatusEffectConfig Burn
        {
            get => Effects.TryGetValue("Burn", out var effect) ? effect : new StatusEffectConfig();
            set => Effects["Burn"] = value;
        }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public StatusEffectConfig Freeze
        {
            get => Effects.TryGetValue("Freeze", out var effect) ? effect : new StatusEffectConfig();
            set => Effects["Freeze"] = value;
        }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public StatusEffectConfig Stun
        {
            get => Effects.TryGetValue("Stun", out var effect) ? effect : new StatusEffectConfig();
            set => Effects["Stun"] = value;
        }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public StatusEffectConfig Poison
        {
            get => Effects.TryGetValue("Poison", out var effect) ? effect : new StatusEffectConfig();
            set => Effects["Poison"] = value;
        }
        
        /// <summary>
        /// Initialize default status effects if the dictionary is empty
        /// Also normalizes dictionary keys to PascalCase after JSON deserialization
        /// </summary>
        public void InitializeDefaults()
        {
            // Normalize dictionary keys from camelCase (JSON) to PascalCase
            var normalizedEffects = new Dictionary<string, StatusEffectConfig>();
            foreach (var kvp in Effects)
            {
                string normalizedKey = NormalizeKey(kvp.Key);
                normalizedEffects[normalizedKey] = kvp.Value;
            }
            Effects = normalizedEffects;
            
            // Ensure default effects exist
            if (!Effects.ContainsKey("Bleed"))
                Effects["Bleed"] = new StatusEffectConfig();
            if (!Effects.ContainsKey("Burn"))
                Effects["Burn"] = new StatusEffectConfig();
            if (!Effects.ContainsKey("Freeze"))
                Effects["Freeze"] = new StatusEffectConfig();
            if (!Effects.ContainsKey("Stun"))
                Effects["Stun"] = new StatusEffectConfig();
            if (!Effects.ContainsKey("Poison"))
                Effects["Poison"] = new StatusEffectConfig();
        }
        
        /// <summary>
        /// Normalize a key from camelCase to PascalCase
        /// </summary>
        private string NormalizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;
            
            // If already PascalCase, return as-is
            if (char.IsUpper(key[0]))
                return key;
            
            // Convert camelCase to PascalCase
            return char.ToUpper(key[0]) + key.Substring(1);
        }
        
        /// <summary>
        /// Get a status effect by name (case-insensitive)
        /// </summary>
        public StatusEffectConfig? GetEffect(string name)
        {
            InitializeDefaults();
            var key = Effects.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            return key != null ? Effects[key] : null;
        }
        
        /// <summary>
        /// Add or update a status effect
        /// </summary>
        public void SetEffect(string name, StatusEffectConfig config)
        {
            InitializeDefaults();
            var key = Effects.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (key != null)
            {
                Effects[key] = config;
            }
            else
            {
                Effects[name] = config;
            }
        }
        
        /// <summary>
        /// Remove a status effect (cannot remove default effects)
        /// </summary>
        public bool RemoveEffect(string name)
        {
            InitializeDefaults();
            var key = Effects.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (key != null && !IsDefaultEffect(key))
            {
                return Effects.Remove(key);
            }
            return false;
        }
        
        /// <summary>
        /// Check if an effect is a default effect (cannot be removed)
        /// </summary>
        public bool IsDefaultEffect(string name)
        {
            return name.Equals("Bleed", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Burn", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Freeze", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Stun", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Poison", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Get all status effect names
        /// </summary>
        public List<string> GetEffectNames()
        {
            InitializeDefaults();
            return Effects.Keys.ToList();
        }
    }

    /// <summary>
    /// Individual status effect configuration
    /// </summary>
    public class StatusEffectConfig
    {
        public int DamagePerTick { get; set; }
        public double TickInterval { get; set; }
        public int MaxStacks { get; set; }
        public int StacksPerApplication { get; set; }
        public double SpeedReduction { get; set; }
        public double Duration { get; set; }
        public int SkipTurns { get; set; }
    }

    /// <summary>
    /// Min/Max configuration helper
    /// </summary>
    public class MinMaxConfig
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }
}
