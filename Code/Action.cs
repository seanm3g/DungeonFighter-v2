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
        public bool CausesStun { get; set; }
        public bool IsComboAction { get; set; }
        public int ComboBonusAmount { get; set; }
        public int ComboBonusDuration { get; set; }

        // New properties for advanced mechanics
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
        public List<string> Tags { get; set; } = new List<string>();
        public double SelfAttackChance { get; set; } = 0.0;
        public bool ResetEnemyCombo { get; set; } = false;
        public bool StunEnemy { get; set; } = false;
        public int StunDuration { get; set; } = 0;
        public bool ReduceLengthNextActions { get; set; } = false;
        public double LengthReduction { get; set; } = 0.0;
        public int LengthReductionDuration { get; set; } = 0;

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
                MultiHitCount = 3;
                MultiHitDamagePercent = 0.35;
            }

            // Self-damage (Deal with the Devil)
            if (Name.ToUpper() == "DEAL WITH THE DEVIL" && desc.Contains("5% damage to yourself"))
            {
                SelfDamagePercent = 5;
            }

            // Roll bonuses/penalties
            if (Name.ToUpper() == "LUCKY STRIKE" && desc.Contains("+1 to next roll"))
            {
                RollBonus = 1;
            }
            else if (Name.ToUpper() == "LAST GRASP" && desc.Contains("+10 to roll"))
            {
                RollBonus = 10;
            }
            else if (Name.ToUpper() == "DRUNKEN BRAWLER" && desc.Contains("-5 to your next roll"))
            {
                RollBonus = -5;
            }

            // Stat bonuses
            if (desc.Contains("gain 1 str"))
            {
                StatBonus = 1;
                StatBonusType = "STR";
                StatBonusDuration = 999; // Duration of dungeon
            }

            // Turn skipping (True Strike)
            if (Name.ToUpper() == "TRUE STRIKE" && desc.Contains("skip turn"))
            {
                SkipNextTurn = true;
                GuaranteeNextSuccess = true;
            }

            // Healing (Second Wind)
            if (Name.ToUpper() == "SECOND WIND" && desc.Contains("heal for 5 health"))
            {
                HealAmount = 5;
            }

            // Health thresholds
            if (desc.Contains("below 25% health") || desc.Contains("health is below 25%"))
            {
                HealthThreshold = 0.25;
            }
            else if (desc.Contains("below 5%") || desc.Contains("health is below 5%"))
            {
                HealthThreshold = 0.05;
            }
            else if (desc.Contains("full health") || desc.Contains("at full health"))
            {
                HealthThreshold = 1.0;
            }
            else if (desc.Contains("1 health") || desc.Contains("at 1 health"))
            {
                HealthThreshold = 0.01;
            }

            // Stat thresholds
            if (desc.Contains("str ≥ 10"))
            {
                StatThreshold = 10.0;
                StatThresholdType = "STR";
            }

            // Conditional damage multipliers
            if (desc.Contains("double damage"))
            {
                ConditionalDamageMultiplier = 2.0;
            }
            else if (desc.Contains("quadrable damage"))
            {
                ConditionalDamageMultiplier = 4.0;
            }
            else if (desc.Contains("add 50% damage"))
            {
                ConditionalDamageMultiplier = 1.5;
            }

            // Action repetition (Deja Vu)
            if (Name.ToUpper() == "DEJA VU" && desc.Contains("repeat the previous action"))
            {
                RepeatLastAction = true;
            }

            // Extra attacks
            if (desc.Contains("add 1 attack to next action") || desc.Contains("+1 attack to next action"))
            {
                ExtraAttacks = 1;
            }

            // Combo amplifier multiplier (Pretty Boy Swag)
            if (Name.ToUpper() == "PRETTY BOY SWAG" && desc.Contains("double combo amp"))
            {
                ComboAmplifierMultiplier = 2.0;
            }

            // Enemy roll penalties
            if (Name.ToUpper() == "QUICK REFLEXES" && desc.Contains("-5 to next enemies roll"))
            {
                EnemyRollPenalty = 5;
            }
            else if (Name.ToUpper() == "DRUNKEN BRAWLER" && desc.Contains("-5 to enemies next roll"))
            {
                EnemyRollPenalty = 5;
            }

            // Extra damage (Opening Volley)
            if (Name.ToUpper() == "OPENING VOLLEY" && desc.Contains("10 extra damage"))
            {
                ExtraDamage = 10;
                ExtraDamageDecay = 1;
            }

            // Damage reduction (Sharp Edge)
            if (Name.ToUpper() == "SHARP EDGE" && desc.Contains("reduce damage by 50%"))
            {
                DamageReduction = 0.5;
                DamageReductionDecay = 1;
            }

            // Self-attack chance (Swing for the Fences)
            if (Name.ToUpper() == "SWING FOR THE FENCES" && desc.Contains("50% chance to attack yourself"))
            {
                SelfAttackChance = 0.5;
            }

            // Enemy combo reset (Jab)
            if (Name.ToUpper() == "JAB" && desc.Contains("reset enemy combo"))
            {
                ResetEnemyCombo = true;
            }

            // Stun effects (Stun)
            if (Name.ToUpper() == "STUN" && desc.Contains("stuns the enemy"))
            {
                StunEnemy = true;
                StunDuration = 5;
            }

            // Length reduction (Taunt)
            if (Name.ToUpper() == "TAUNT" && desc.Contains("50% length for next 2 actions"))
            {
                ReduceLengthNextActions = true;
                LengthReduction = 0.5;
                LengthReductionDuration = 2;
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