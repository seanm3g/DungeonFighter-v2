using System;
using System.Collections.Generic;
using RPGGame.Actions.Parsing;

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
        Environment,
        SelfAndTarget
    }

    public class Action
    {
        public string Name { get; set; } = "";
        public ActionType Type { get; set; }
        public TargetType Target { get; set; }
        public int Cooldown { get; set; }
        
        private int _currentCooldown = 0;
        public int CurrentCooldown 
        { 
            get => _currentCooldown;
            set => _currentCooldown = value < 0 ? 0 : value;
        }
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
                     int cooldown = 0, string? description = "",
                     int comboOrder = -1, double damageMultiplier = 1.0, double length = 1.0,
                     bool causesBleed = false, bool causesWeaken = false, bool causesPoison = false, bool causesStun = false, bool isComboAction = false,
                     int comboBonusAmount = 0, int comboBonusDuration = 0)
        {
            Name = name ?? GetDefaultName(type);
            Type = type;
            Target = targetType;
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
            ActionDescriptionParser.ParseDescriptionForProperties(this);
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

            return modifier;
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