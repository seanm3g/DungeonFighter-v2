using System;
using System.Collections.Generic;

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
    /// Status effects configuration
    /// </summary>
    public class StatusEffectsConfig
    {
        public StatusEffectConfig Bleed { get; set; } = new();
        public StatusEffectConfig Burn { get; set; } = new();
        public StatusEffectConfig Freeze { get; set; } = new();
        public StatusEffectConfig Stun { get; set; } = new();
        public StatusEffectConfig Poison { get; set; } = new();
        public string Description { get; set; } = "";
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
