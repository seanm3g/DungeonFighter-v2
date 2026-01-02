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
        public int CriticalHitThreshold { get; set; }
        public double CriticalHitMultiplier { get; set; }
        public int MinimumDamage { get; set; }
        public double BaseAttackTime { get; set; }
        public double AgilitySpeedReduction { get; set; }
        public double MinimumAttackTime { get; set; }
        
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
        public string Description { get; set; } = "";
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
    }

    /// <summary>
    /// Combo system configuration
    /// </summary>
    public class ComboSystemConfig
    {
        public double ComboAmplifierAtTech5 { get; set; }
        public double ComboAmplifierMax { get; set; }
        public int ComboAmplifierMaxTech { get; set; }
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
