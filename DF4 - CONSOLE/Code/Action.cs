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
        public string Name { get; private set; }
        public ActionType Type { get; private set; }
        public TargetType Target { get; private set; }
        public int BaseValue { get; private set; }
        public int Range { get; private set; }
        public int Cooldown { get; private set; }
        public int CurrentCooldown { get; private set; }
        public string Description { get; private set; }
        public int ComboOrder { get; set; }
        public double DamageMultiplier { get; set; }
        public double Length { get; set; }
        public bool CausesBleed { get; set; }
        public bool CausesWeaken { get; set; }
        public bool IsComboAction { get; set; }
        public int ComboBonusAmount { get; private set; }
        public int ComboBonusDuration { get; private set; }

        public Action(string? name = null, ActionType type = ActionType.Attack, TargetType targetType = TargetType.SingleTarget,
                     int baseValue = 0, int range = 1, int cooldown = 0, string? description = "",
                     int comboOrder = -1, double damageMultiplier = 1.0, double length = 1.0,
                     bool causesBleed = false, bool causesWeaken = false, bool isComboAction = false,
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
            IsComboAction = isComboAction;
            ComboBonusAmount = comboBonusAmount;
            ComboBonusDuration = comboBonusDuration;
        }

        private string GetDefaultName(ActionType type)
        {
            return type switch
            {
                ActionType.Attack => FlavorText.GetRandomName(FlavorText.Actions.AttackNames),
                ActionType.Heal => "Heal",
                ActionType.Buff => FlavorText.GetRandomName(FlavorText.Actions.BuffNames),
                ActionType.Debuff => FlavorText.GetRandomName(FlavorText.Actions.DebuffNames),
                ActionType.Spell => FlavorText.GetRandomName(FlavorText.Actions.SpellNames),
                ActionType.Interact => "Interact",
                ActionType.Move => "Move",
                ActionType.UseItem => "Use Item",
                _ => "Unknown Action"
            };
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