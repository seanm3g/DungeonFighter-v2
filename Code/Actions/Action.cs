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
        /// <summary>Cadence (Action, Ability, Chain, Fight, Dungeon) used for stat bonus duration and timing.</summary>
        public string Cadence { get; set; } = "";

        // Grouped advanced mechanics properties
        public RollModificationProperties RollMods { get; set; } = new RollModificationProperties();
        public ConditionalTriggerProperties Triggers { get; set; } = new ConditionalTriggerProperties();
        public ComboRoutingProperties ComboRouting { get; set; } = new ComboRoutingProperties();
        public AdvancedMechanicsProperties Advanced { get; set; } = new AdvancedMechanicsProperties();
        public List<string> Tags { get; set; } = new List<string>();
        
        // ACTION/ATTACK keyword bonuses
        public Data.ActionAttackBonuses? ActionAttackBonuses { get; set; }

        /// <summary>Speed modifier for next action/ability (%). Positive = faster. Applied only to next based on cadence.</summary>
        public string SpeedMod { get; set; } = "";
        /// <summary>Damage modifier for next action/ability (%). Applied only to next based on cadence.</summary>
        public string DamageMod { get; set; } = "";
        /// <summary>Multi-hit modifier for next action/ability (raw value). Applied only to next based on cadence.</summary>
        public string MultiHitMod { get; set; } = "";
        /// <summary>Amp modifier for next action/ability (%). Multiply. Applied only to next based on cadence.</summary>
        public string AmpMod { get; set; } = "";
        
        // Outcome handlers - list of outcome handler type names (e.g., "conditional", "xpGain")
        public List<string> OutcomeHandlers { get; set; } = new List<string>();

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
            IsComboAction = true;
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
                     bool causesBleed = false, bool causesWeaken = false, bool causesPoison = false, bool causesStun = false, bool isComboAction = true,
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