using System;
using System.Collections.Generic;

namespace RPGGame
{
    public enum ActionType
    {
        Attack,
        Heal,
        Buff,
        Debuff,
        Interact,
        Move,
        UseItem,
        Spell
    }

    public enum TargetType
    {
        Self,
        SingleTarget,
        AreaOfEffect,
        Environment
    }

    /// <summary>
    /// Roll modification properties for actions
    /// </summary>
    public class RollModificationProperties
    {
        private int _additive = 0;
        private double _multiplier = 1.0;
        private int _min = 1;
        private int _max = 20;
        private double _rerollChance = 0.0;
        private int _explodingDiceThreshold = 20;
        private int _multipleDiceCount = 1;
        private int _criticalHitThresholdOverride = 0;
        private int _comboThresholdOverride = 0;
        private int _hitThresholdOverride = 0;

        public int Additive
        {
            get => _additive;
            set => _additive = value; // No range restriction for additive
        }

        public double Multiplier
        {
            get => _multiplier;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Multiplier), "Multiplier cannot be negative");
                _multiplier = value;
            }
        }

        public int Min
        {
            get => _min;
            set
            {
                if (value < 1 || value > 20)
                    throw new ArgumentOutOfRangeException(nameof(Min), "Min must be between 1 and 20");
                if (value > _max)
                    throw new ArgumentException("Min cannot be greater than Max");
                _min = value;
            }
        }

        public int Max
        {
            get => _max;
            set
            {
                if (value < 1 || value > 20)
                    throw new ArgumentOutOfRangeException(nameof(Max), "Max must be between 1 and 20");
                if (value < _min)
                    throw new ArgumentException("Max cannot be less than Min");
                _max = value;
            }
        }

        public bool AllowReroll { get; set; } = false;

        public double RerollChance
        {
            get => _rerollChance;
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(RerollChance), "RerollChance must be between 0.0 and 1.0");
                _rerollChance = value;
            }
        }

        public bool ExplodingDice { get; set; } = false;

        public int ExplodingDiceThreshold
        {
            get => _explodingDiceThreshold;
            set
            {
                if (value < 1 || value > 20)
                    throw new ArgumentOutOfRangeException(nameof(ExplodingDiceThreshold), "ExplodingDiceThreshold must be between 1 and 20");
                _explodingDiceThreshold = value;
            }
        }

        public int MultipleDiceCount
        {
            get => _multipleDiceCount;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MultipleDiceCount), "MultipleDiceCount must be at least 1");
                _multipleDiceCount = value;
            }
        }

        public string MultipleDiceMode { get; set; } = "Sum";

        public int CriticalHitThresholdOverride
        {
            get => _criticalHitThresholdOverride;
            set
            {
                if (value != 0 && (value < 1 || value > 20))
                    throw new ArgumentOutOfRangeException(nameof(CriticalHitThresholdOverride), "CriticalHitThresholdOverride must be 0 (use default) or between 1 and 20");
                _criticalHitThresholdOverride = value;
            }
        }

        public int ComboThresholdOverride
        {
            get => _comboThresholdOverride;
            set
            {
                if (value != 0 && (value < 1 || value > 20))
                    throw new ArgumentOutOfRangeException(nameof(ComboThresholdOverride), "ComboThresholdOverride must be 0 (use default) or between 1 and 20");
                _comboThresholdOverride = value;
            }
        }

        public int HitThresholdOverride
        {
            get => _hitThresholdOverride;
            set
            {
                if (value != 0 && (value < 1 || value > 20))
                    throw new ArgumentOutOfRangeException(nameof(HitThresholdOverride), "HitThresholdOverride must be 0 (use default) or between 1 and 20");
                _hitThresholdOverride = value;
            }
        }

        /// <summary>
        /// Validates all properties for consistency
        /// </summary>
        public void Validate()
        {
            if (_min > _max)
                throw new ArgumentException("Min cannot be greater than Max");
            if (_rerollChance > 0 && !AllowReroll)
                throw new ArgumentException("RerollChance is set but AllowReroll is false");
        }
    }

    /// <summary>
    /// Conditional trigger properties for actions
    /// </summary>
    public class ConditionalTriggerProperties
    {
        public List<string> TriggerConditions { get; set; } = new List<string>(); // List of condition types
        public int ExactRollTriggerValue { get; set; } = 0; // Exact roll value to trigger (0 = disabled)
        public string? RequiredTag { get; set; } = null; // Required tag for trigger
    }

    /// <summary>
    /// Combo routing properties for actions
    /// </summary>
    public class ComboRoutingProperties
    {
        private int _jumpToSlot = 0;
        private int _triggerOnlyInSlot = 0;

        public int JumpToSlot
        {
            get => _jumpToSlot;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(JumpToSlot), "JumpToSlot must be 0 (disabled) or positive");
                _jumpToSlot = value;
            }
        }

        public bool SkipNext { get; set; } = false;
        public bool RepeatPrevious { get; set; } = false;
        public bool LoopToStart { get; set; } = false;
        public bool StopEarly { get; set; } = false;
        public bool DisableSlot { get; set; } = false;
        public bool RandomAction { get; set; } = false;

        public int TriggerOnlyInSlot
        {
            get => _triggerOnlyInSlot;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(TriggerOnlyInSlot), "TriggerOnlyInSlot must be 0 (always) or positive");
                _triggerOnlyInSlot = value;
            }
        }

        /// <summary>
        /// Validates routing properties for consistency
        /// </summary>
        public void Validate()
        {
            // Only one routing action should be active at a time
            int activeRoutingCount = 0;
            if (JumpToSlot > 0) activeRoutingCount++;
            if (SkipNext) activeRoutingCount++;
            if (RepeatPrevious) activeRoutingCount++;
            if (LoopToStart) activeRoutingCount++;
            if (StopEarly) activeRoutingCount++;
            if (RandomAction) activeRoutingCount++;

            if (activeRoutingCount > 1)
                throw new ArgumentException("Only one combo routing action can be active at a time");
        }
    }

    /// <summary>
    /// Advanced mechanics properties for actions
    /// </summary>
    public class AdvancedMechanicsProperties
    {
        public int MultiHitCount { get; set; } = 1;
        public double MultiHitDamagePercent { get; set; } = 1.0;
        public int SelfDamagePercent { get; set; } = 0;
        public int RollBonus { get; set; } = 0;
        public int StatBonus { get; set; } = 0;
        public string StatBonusType { get; set; } = "";
        public int StatBonusDuration { get; set; } = 0;
        public bool SkipNextTurn { get; set; } = false;
        public bool GuaranteeNextSuccess { get; set; } = false;
        public int HealAmount { get; set; } = 0;
        public double HealthThreshold { get; set; } = 0.0;
        public double StatThreshold { get; set; } = 0.0;
        public string StatThresholdType { get; set; } = "";
        public double ConditionalDamageMultiplier { get; set; } = 1.0;
        public bool RepeatLastAction { get; set; } = false;
        public int ExtraAttacks { get; set; } = 0;
        public double ComboAmplifierMultiplier { get; set; } = 1.0;
        public int EnemyRollPenalty { get; set; } = 0;
        public int ExtraDamage { get; set; } = 0;
        public int ExtraDamageDecay { get; set; } = 0;
        public double DamageReduction { get; set; } = 0.0;
        public int DamageReductionDecay { get; set; } = 0;
        public double SelfAttackChance { get; set; } = 0.0;
        public bool ResetEnemyCombo { get; set; } = false;
        public bool StunEnemy { get; set; } = false;
        public int StunDuration { get; set; } = 0;
        public bool ReduceLengthNextActions { get; set; } = false;
        public double LengthReduction { get; set; } = 0.0;
        public int LengthReductionDuration { get; set; } = 0;
    }

    public class Action
    {
        public string Name { get; set; } = "";
        public ActionType Type { get; set; }
        public TargetType Target { get; set; }
        public int BaseValue { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public string Description { get; set; } = "";
        public int ComboOrder { get; set; }
        public double DamageMultiplier { get; set; }
        public double Length { get; set; }
        public bool CausesBleed { get; set; }
        public bool CausesWeaken { get; set; }
        public bool CausesSlow { get; set; }
        public bool CausesPoison { get; set; }
        public bool CausesBurn { get; set; }
        public bool CausesStun { get; set; }
        
        // Advanced status effects (Phase 2)
        public bool CausesVulnerability { get; set; }
        public bool CausesHarden { get; set; }
        public bool CausesFortify { get; set; }
        public bool CausesFocus { get; set; }
        public bool CausesExpose { get; set; }
        public bool CausesHPRegen { get; set; }
        public bool CausesArmorBreak { get; set; }
        public bool CausesPierce { get; set; }
        public bool CausesReflect { get; set; }
        public bool CausesSilence { get; set; }
        public bool CausesStatDrain { get; set; }
        public bool CausesAbsorb { get; set; }
        public bool CausesTemporaryHP { get; set; }
        public bool CausesConfusion { get; set; }
        public bool CausesCleanse { get; set; }
        public bool CausesMark { get; set; }
        public bool CausesDisrupt { get; set; }
        
        public bool IsComboAction { get; set; }
        public int ComboBonusAmount { get; set; }
        public int ComboBonusDuration { get; set; }

        // Grouped advanced mechanics properties
        public RollModificationProperties RollMods { get; set; } = new RollModificationProperties();
        public ConditionalTriggerProperties Triggers { get; set; } = new ConditionalTriggerProperties();
        public ComboRoutingProperties ComboRouting { get; set; } = new ComboRoutingProperties();
        public AdvancedMechanicsProperties Advanced { get; set; } = new AdvancedMechanicsProperties();
        public List<string> Tags { get; set; } = new List<string>();
        
        // Outcome handlers - list of outcome handler type names (e.g., "conditional", "xpGain")
        public List<string> OutcomeHandlers { get; set; } = new List<string>();

        // Backward compatibility properties - delegate to nested classes
        // These will be removed after all usages are updated
        [Obsolete("Use RollMods.Additive instead")]
        public int RollModifierAdditive { get => RollMods.Additive; set => RollMods.Additive = value; }
        [Obsolete("Use RollMods.Multiplier instead")]
        public double RollModifierMultiplier { get => RollMods.Multiplier; set => RollMods.Multiplier = value; }
        [Obsolete("Use RollMods.Min instead")]
        public int RollModifierMin { get => RollMods.Min; set => RollMods.Min = value; }
        [Obsolete("Use RollMods.Max instead")]
        public int RollModifierMax { get => RollMods.Max; set => RollMods.Max = value; }
        [Obsolete("Use RollMods.AllowReroll instead")]
        public bool AllowReroll { get => RollMods.AllowReroll; set => RollMods.AllowReroll = value; }
        [Obsolete("Use RollMods.RerollChance instead")]
        public double RerollChance { get => RollMods.RerollChance; set => RollMods.RerollChance = value; }
        [Obsolete("Use RollMods.ExplodingDice instead")]
        public bool ExplodingDice { get => RollMods.ExplodingDice; set => RollMods.ExplodingDice = value; }
        [Obsolete("Use RollMods.ExplodingDiceThreshold instead")]
        public int ExplodingDiceThreshold { get => RollMods.ExplodingDiceThreshold; set => RollMods.ExplodingDiceThreshold = value; }
        [Obsolete("Use RollMods.MultipleDiceCount instead")]
        public int MultipleDiceCount { get => RollMods.MultipleDiceCount; set => RollMods.MultipleDiceCount = value; }
        [Obsolete("Use RollMods.MultipleDiceMode instead")]
        public string MultipleDiceMode { get => RollMods.MultipleDiceMode; set => RollMods.MultipleDiceMode = value; }
        [Obsolete("Use RollMods.CriticalHitThresholdOverride instead")]
        public int CriticalHitThresholdOverride { get => RollMods.CriticalHitThresholdOverride; set => RollMods.CriticalHitThresholdOverride = value; }
        [Obsolete("Use RollMods.ComboThresholdOverride instead")]
        public int ComboThresholdOverride { get => RollMods.ComboThresholdOverride; set => RollMods.ComboThresholdOverride = value; }
        [Obsolete("Use RollMods.HitThresholdOverride instead")]
        public int HitThresholdOverride { get => RollMods.HitThresholdOverride; set => RollMods.HitThresholdOverride = value; }
        [Obsolete("Use Triggers.TriggerConditions instead")]
        public List<string> TriggerConditions { get => Triggers.TriggerConditions; set => Triggers.TriggerConditions = value; }
        [Obsolete("Use Triggers.ExactRollTriggerValue instead")]
        public int ExactRollTriggerValue { get => Triggers.ExactRollTriggerValue; set => Triggers.ExactRollTriggerValue = value; }
        [Obsolete("Use Triggers.RequiredTag instead")]
        public string? RequiredTag { get => Triggers.RequiredTag; set => Triggers.RequiredTag = value; }
        [Obsolete("Use ComboRouting.JumpToSlot instead")]
        public int ComboJumpToSlot { get => ComboRouting.JumpToSlot; set => ComboRouting.JumpToSlot = value; }
        [Obsolete("Use ComboRouting.SkipNext instead")]
        public bool ComboSkipNext { get => ComboRouting.SkipNext; set => ComboRouting.SkipNext = value; }
        [Obsolete("Use ComboRouting.RepeatPrevious instead")]
        public bool ComboRepeatPrevious { get => ComboRouting.RepeatPrevious; set => ComboRouting.RepeatPrevious = value; }
        [Obsolete("Use ComboRouting.LoopToStart instead")]
        public bool ComboLoopToStart { get => ComboRouting.LoopToStart; set => ComboRouting.LoopToStart = value; }
        [Obsolete("Use ComboRouting.StopEarly instead")]
        public bool ComboStopEarly { get => ComboRouting.StopEarly; set => ComboRouting.StopEarly = value; }
        [Obsolete("Use ComboRouting.DisableSlot instead")]
        public bool ComboDisableSlot { get => ComboRouting.DisableSlot; set => ComboRouting.DisableSlot = value; }
        [Obsolete("Use ComboRouting.RandomAction instead")]
        public bool ComboRandomAction { get => ComboRouting.RandomAction; set => ComboRouting.RandomAction = value; }
        [Obsolete("Use ComboRouting.TriggerOnlyInSlot instead")]
        public int ComboTriggerOnlyInSlot { get => ComboRouting.TriggerOnlyInSlot; set => ComboRouting.TriggerOnlyInSlot = value; }
        [Obsolete("Use Advanced.MultiHitCount instead")]
        public int MultiHitCount { get => Advanced.MultiHitCount; set => Advanced.MultiHitCount = value; }
        [Obsolete("Use Advanced.MultiHitDamagePercent instead")]
        public double MultiHitDamagePercent { get => Advanced.MultiHitDamagePercent; set => Advanced.MultiHitDamagePercent = value; }
        [Obsolete("Use Advanced.SelfDamagePercent instead")]
        public int SelfDamagePercent { get => Advanced.SelfDamagePercent; set => Advanced.SelfDamagePercent = value; }
        [Obsolete("Use Advanced.RollBonus instead")]
        public int RollBonus { get => Advanced.RollBonus; set => Advanced.RollBonus = value; }
        [Obsolete("Use Advanced.StatBonus instead")]
        public int StatBonus { get => Advanced.StatBonus; set => Advanced.StatBonus = value; }
        [Obsolete("Use Advanced.StatBonusType instead")]
        public string StatBonusType { get => Advanced.StatBonusType; set => Advanced.StatBonusType = value; }
        [Obsolete("Use Advanced.StatBonusDuration instead")]
        public int StatBonusDuration { get => Advanced.StatBonusDuration; set => Advanced.StatBonusDuration = value; }
        [Obsolete("Use Advanced.SkipNextTurn instead")]
        public bool SkipNextTurn { get => Advanced.SkipNextTurn; set => Advanced.SkipNextTurn = value; }
        [Obsolete("Use Advanced.GuaranteeNextSuccess instead")]
        public bool GuaranteeNextSuccess { get => Advanced.GuaranteeNextSuccess; set => Advanced.GuaranteeNextSuccess = value; }
        [Obsolete("Use Advanced.HealAmount instead")]
        public int HealAmount { get => Advanced.HealAmount; set => Advanced.HealAmount = value; }
        [Obsolete("Use Advanced.HealthThreshold instead")]
        public double HealthThreshold { get => Advanced.HealthThreshold; set => Advanced.HealthThreshold = value; }
        [Obsolete("Use Advanced.StatThreshold instead")]
        public double StatThreshold { get => Advanced.StatThreshold; set => Advanced.StatThreshold = value; }
        [Obsolete("Use Advanced.StatThresholdType instead")]
        public string StatThresholdType { get => Advanced.StatThresholdType; set => Advanced.StatThresholdType = value; }
        [Obsolete("Use Advanced.ConditionalDamageMultiplier instead")]
        public double ConditionalDamageMultiplier { get => Advanced.ConditionalDamageMultiplier; set => Advanced.ConditionalDamageMultiplier = value; }
        [Obsolete("Use Advanced.RepeatLastAction instead")]
        public bool RepeatLastAction { get => Advanced.RepeatLastAction; set => Advanced.RepeatLastAction = value; }
        [Obsolete("Use Advanced.ExtraAttacks instead")]
        public int ExtraAttacks { get => Advanced.ExtraAttacks; set => Advanced.ExtraAttacks = value; }
        [Obsolete("Use Advanced.ComboAmplifierMultiplier instead")]
        public double ComboAmplifierMultiplier { get => Advanced.ComboAmplifierMultiplier; set => Advanced.ComboAmplifierMultiplier = value; }
        [Obsolete("Use Advanced.EnemyRollPenalty instead")]
        public int EnemyRollPenalty { get => Advanced.EnemyRollPenalty; set => Advanced.EnemyRollPenalty = value; }
        [Obsolete("Use Advanced.ExtraDamage instead")]
        public int ExtraDamage { get => Advanced.ExtraDamage; set => Advanced.ExtraDamage = value; }
        [Obsolete("Use Advanced.ExtraDamageDecay instead")]
        public int ExtraDamageDecay { get => Advanced.ExtraDamageDecay; set => Advanced.ExtraDamageDecay = value; }
        [Obsolete("Use Advanced.DamageReduction instead")]
        public double DamageReduction { get => Advanced.DamageReduction; set => Advanced.DamageReduction = value; }
        [Obsolete("Use Advanced.DamageReductionDecay instead")]
        public int DamageReductionDecay { get => Advanced.DamageReductionDecay; set => Advanced.DamageReductionDecay = value; }
        [Obsolete("Use Advanced.SelfAttackChance instead")]
        public double SelfAttackChance { get => Advanced.SelfAttackChance; set => Advanced.SelfAttackChance = value; }
        [Obsolete("Use Advanced.ResetEnemyCombo instead")]
        public bool ResetEnemyCombo { get => Advanced.ResetEnemyCombo; set => Advanced.ResetEnemyCombo = value; }
        [Obsolete("Use Advanced.StunEnemy instead")]
        public bool StunEnemy { get => Advanced.StunEnemy; set => Advanced.StunEnemy = value; }
        [Obsolete("Use Advanced.StunDuration instead")]
        public int StunDuration { get => Advanced.StunDuration; set => Advanced.StunDuration = value; }
        [Obsolete("Use Advanced.ReduceLengthNextActions instead")]
        public bool ReduceLengthNextActions { get => Advanced.ReduceLengthNextActions; set => Advanced.ReduceLengthNextActions = value; }
        [Obsolete("Use Advanced.LengthReduction instead")]
        public double LengthReduction { get => Advanced.LengthReduction; set => Advanced.LengthReduction = value; }
        [Obsolete("Use Advanced.LengthReductionDuration instead")]
        public int LengthReductionDuration { get => Advanced.LengthReductionDuration; set => Advanced.LengthReductionDuration = value; }

        // Parameterless constructor for JSON deserialization
        public Action()
        {
            Name = "";
            Type = ActionType.Attack;
            Target = TargetType.SingleTarget;
            BaseValue = 0;
            Range = 1;
            Cooldown = 0;
            Description = "";
            ComboOrder = -1;
            DamageMultiplier = 1.0;
            Length = 1.0;
            CausesBleed = false;
            CausesWeaken = false;
            CausesStun = false;
            IsComboAction = false;
            ComboBonusAmount = 0;
            ComboBonusDuration = 0;
            
            // Initialize nested property objects
            RollMods = new RollModificationProperties();
            Triggers = new ConditionalTriggerProperties();
            ComboRouting = new ComboRoutingProperties();
            Advanced = new AdvancedMechanicsProperties();
            Tags = new List<string>();
        }

        public Action(string? name = null, ActionType type = ActionType.Attack, TargetType targetType = TargetType.SingleTarget,
                     int baseValue = 0, int range = 1, int cooldown = 0, string? description = "",
                     int comboOrder = -1, double damageMultiplier = 1.0, double length = 1.0,
                     bool causesBleed = false, bool causesWeaken = false, bool causesPoison = false, bool causesStun = false, bool isComboAction = false,
                     int comboBonusAmount = 0, int comboBonusDuration = 0)
        {
            Name = name ?? GetDefaultName(type);
            Type = type;
            Target = targetType;
            BaseValue = baseValue;
            Range = range;
            Cooldown = cooldown;
            CurrentCooldown = 0;
            Description = description ?? "";
            ComboOrder = comboOrder;
            DamageMultiplier = damageMultiplier;
            Length = length;
            CausesBleed = causesBleed;
            CausesWeaken = causesWeaken;
            CausesPoison = causesPoison;
            CausesStun = causesStun;
            IsComboAction = isComboAction;
            ComboBonusAmount = comboBonusAmount;
            ComboBonusDuration = comboBonusDuration;

            // Parse description to set advanced properties
            ParseDescriptionForProperties();
        }

        private void ParseDescriptionForProperties()
        {
            if (string.IsNullOrEmpty(Description)) return;

            string desc = Description.ToLower();

            // Multi-hit attacks (Cleave)
            if (Name.ToUpper() == "CLEAVE" && desc.Contains("3x35%"))
            {
                Advanced.MultiHitCount = 3;
                Advanced.MultiHitDamagePercent = 0.35;
            }

            // Self-damage (Deal with the Devil)
            if (Name.ToUpper() == "DEAL WITH THE DEVIL" && desc.Contains("5% damage to yourself"))
            {
                Advanced.SelfDamagePercent = 5;
            }

            // Roll bonuses/penalties
            if (Name.ToUpper() == "LUCKY STRIKE" && desc.Contains("+1 to next roll"))
            {
                Advanced.RollBonus = 1;
            }
            else if (Name.ToUpper() == "LAST GRASP" && desc.Contains("+10 to roll"))
            {
                Advanced.RollBonus = 10;
            }
            else if (Name.ToUpper() == "DRUNKEN BRAWLER" && desc.Contains("-5 to your next roll"))
            {
                Advanced.RollBonus = -5;
            }

            // Stat bonuses
            if (desc.Contains("gain 1 str"))
            {
                Advanced.StatBonus = 1;
                Advanced.StatBonusType = "STR";
                Advanced.StatBonusDuration = 999; // Duration of dungeon
            }

            // Turn skipping (True Strike)
            if (Name.ToUpper() == "TRUE STRIKE" && desc.Contains("skip turn"))
            {
                Advanced.SkipNextTurn = true;
                Advanced.GuaranteeNextSuccess = true;
            }

            // Healing (Second Wind)
            if (Name.ToUpper() == "SECOND WIND" && desc.Contains("heal for 5 health"))
            {
                Advanced.HealAmount = 5;
            }

            // Health thresholds
            if (desc.Contains("below 25% health") || desc.Contains("health is below 25%"))
            {
                Advanced.HealthThreshold = 0.25;
            }
            else if (desc.Contains("below 5%") || desc.Contains("health is below 5%"))
            {
                Advanced.HealthThreshold = 0.05;
            }
            else if (desc.Contains("full health") || desc.Contains("at full health"))
            {
                Advanced.HealthThreshold = 1.0;
            }
            else if (desc.Contains("1 health") || desc.Contains("at 1 health"))
            {
                Advanced.HealthThreshold = 0.01;
            }

            // Stat thresholds
            if (desc.Contains("str ≥ 10"))
            {
                Advanced.StatThreshold = 10.0;
                Advanced.StatThresholdType = "STR";
            }

            // Conditional damage multipliers
            if (desc.Contains("double damage"))
            {
                Advanced.ConditionalDamageMultiplier = 2.0;
            }
            else if (desc.Contains("quadrable damage"))
            {
                Advanced.ConditionalDamageMultiplier = 4.0;
            }
            else if (desc.Contains("add 50% damage"))
            {
                Advanced.ConditionalDamageMultiplier = 1.5;
            }

            // Action repetition (Deja Vu)
            if (Name.ToUpper() == "DEJA VU" && desc.Contains("repeat the previous action"))
            {
                Advanced.RepeatLastAction = true;
            }

            // Extra attacks
            if (desc.Contains("add 1 attack to next action") || desc.Contains("+1 attack to next action"))
            {
                Advanced.ExtraAttacks = 1;
            }

            // Combo amplifier multiplier (Pretty Boy Swag)
            if (Name.ToUpper() == "PRETTY BOY SWAG" && desc.Contains("double combo amp"))
            {
                Advanced.ComboAmplifierMultiplier = 2.0;
            }

            // Enemy roll penalties
            if (Name.ToUpper() == "QUICK REFLEXES" && desc.Contains("-5 to next enemies roll"))
            {
                Advanced.EnemyRollPenalty = 5;
            }
            else if (Name.ToUpper() == "DRUNKEN BRAWLER" && desc.Contains("-5 to enemies next roll"))
            {
                Advanced.EnemyRollPenalty = 5;
            }

            // Extra damage (Opening Volley)
            if (Name.ToUpper() == "OPENING VOLLEY" && desc.Contains("10 extra damage"))
            {
                Advanced.ExtraDamage = 10;
                Advanced.ExtraDamageDecay = 1;
            }

            // Damage reduction (Sharp Edge)
            if (Name.ToUpper() == "SHARP EDGE" && desc.Contains("reduce damage by 50%"))
            {
                Advanced.DamageReduction = 0.5;
                Advanced.DamageReductionDecay = 1;
            }

            // Self-attack chance (Swing for the Fences)
            if (Name.ToUpper() == "SWING FOR THE FENCES" && desc.Contains("50% chance to attack yourself"))
            {
                Advanced.SelfAttackChance = 0.5;
            }

            // Enemy combo reset (Jab)
            if (Name.ToUpper() == "JAB" && desc.Contains("reset enemy combo"))
            {
                Advanced.ResetEnemyCombo = true;
            }

            // Stun effects (Stun)
            if (Name.ToUpper() == "STUN" && desc.Contains("stuns the enemy"))
            {
                Advanced.StunEnemy = true;
                Advanced.StunDuration = 5;
            }

            // Length reduction (Taunt)
            if (Name.ToUpper() == "TAUNT" && desc.Contains("50% length for next 2 actions"))
            {
                Advanced.ReduceLengthNextActions = true;
                Advanced.LengthReduction = 0.5;
                Advanced.LengthReductionDuration = 2;
            }
        }

        private string GetDefaultName(ActionType type)
        {
            return ActionLoader.GetRandomActionNameByType(type);
        }

        public bool IsOnCooldown => CurrentCooldown > 0;

        public void UpdateCooldown()
        {
            if (CurrentCooldown > 0)
                CurrentCooldown--;
        }

        public void ResetCooldown()
        {
            CurrentCooldown = Cooldown;
        }

        public int CalculateEffect(Character source, Character? target = null)
        {
            int modifier = 0;

            switch (Type)
            {
                case ActionType.Attack:
                    modifier = source.Strength;
                    break;
                case ActionType.Heal:
                    modifier = source.Technique;
                    break;
                case ActionType.Buff:
                case ActionType.Debuff:
                    modifier = source.Technique;
                    break;
                default:
                    modifier = 0;
                    break;
            }

            return BaseValue + modifier;
        }

        public override string ToString()
        {
            return $"{Name} ({Type}) - {Description}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Action other) return false;
            return Name == other.Name && ComboOrder == other.ComboOrder;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, ComboOrder);
        }
    }
} 