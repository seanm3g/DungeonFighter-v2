namespace RPGGame
{
    public interface IComboMemory
    {
        int LastComboActionIdx { get; set; }
    }

    public class Character : Entity, IComboMemory
    {
        // Attributes
        public int Strength { get; protected set; }
        public int Agility { get; protected set; }
        public int Technique { get; protected set; }

        // Health
        public int CurrentHealth { get; protected set; }
        public int MaxHealth { get; protected set; }

        // Level and XP
        public int Level { get; protected set; }
        public int XP { get; protected set; }

        // Inventory
        public List<Item> Inventory { get; private set; }

        // Item slots
        public Item? Head { get; private set; }
        public Item? Body { get; private set; }
        public Item? Weapon { get; private set; }
        public Item? Feet { get; private set; }

        // Combo system state
        public int ComboStep { get; set; } = 0;
        public double ComboAmplifier { get; set; } = 1.0;
        public int ComboBonus { get; set; } = 0; // For loot integration
        // Temporary bonus for Taunt
        public int TempComboBonus { get; set; } = 0;
        public int TempComboBonusTurns { get; set; } = 0;
        public int LastComboActionIdx { get; set; } = -1;
        // New combo mode tracking
        public bool ComboModeActive { get; set; } = false;

        public Character(string? name = null, int level = 1)
            : base(name ?? FlavorText.GenerateCharacterName())
        {
            Level = level;
            // Initialize attributes - boosted for better balance
            Strength = 25;
            Agility = 20;
            Technique = 18;

            // Initialize health
            MaxHealth = 100+level*3;
            CurrentHealth = MaxHealth;

            // Initialize level and XP
            XP = 0;

            // Initialize inventory
            Inventory = new List<Item>();

            // Initialize item slots
            Head = null;
            Body = null;
            Weapon = null;
            Feet = null;

            // Add default actions
            AddDefaultActions();
            AddComboActions();
        }

        private void AddDefaultActions()
        {
            // No default actions; only combo actions will be used.
        }

        private void AddComboActions()
        {
            // Taunt
            var taunt = new Action(
                name: "Taunt",
                type: ActionType.Debuff,
                targetType: TargetType.SingleTarget,
                baseValue: 0,
                range: 1,
                cooldown: 0,
                description: "increases chance of next action",
                comboOrder: 0,
                damageMultiplier: 0,
                length: 2.0,
                isComboAction: true,
                comboBonusAmount: 2,
                comboBonusDuration: 2
            );
            // Jab
            var jab = new Action(
                name: "Jab",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 1, // Will be multiplied by 150%
                range: 1,
                cooldown: 0,
                description: "Interrupts enemies combo",
                comboOrder: 1,
                damageMultiplier: .5,
                length: 0.5,
                isComboAction: true
            );
            // Stun
            var stun = new Action(
                name: "Stun",
                type: ActionType.Debuff,
                targetType: TargetType.SingleTarget,
                baseValue: 0, // Will be handled as 300% damage
                range: 1,
                cooldown: 0,
                description: "Stuns the enemy for 5s and weakens",
                comboOrder: 2,
                damageMultiplier: 1.1,
                length: 4.0,
                causesWeaken: true,
                isComboAction: true
            );
            // Crit
            var crit = new Action(
                name: "Crit",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 0, // Will be handled as 800% damage
                range: 1,
                cooldown: 0,
                description: "Do 800% damage and bleeds",
                comboOrder: 3,
                damageMultiplier: 1.5,
                length: 2.0,
                causesBleed: true,
                isComboAction: true
            );
            AddAction(taunt, 1.0);
            AddAction(jab, 0.0);
            AddAction(stun, 0.0);
            AddAction(crit, 0.0);
        }

        // Methods to add/remove items from inventory
        public void AddToInventory(Item item) => Inventory.Add(item);
        public bool RemoveFromInventory(Item item) => Inventory.Remove(item);

        // Methods to equip/unequip items
        public Item? EquipItem(Item item, string slot)
        {
            Item? previousItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    previousItem = Head;
                    Head = item; 
                    break;
                case "body": 
                    previousItem = Body;
                    Body = item; 
                    break;
                case "weapon": 
                    previousItem = Weapon;
                    Weapon = item;
                    // Stat-leveling logic for weapons
                    if (item is WeaponItem)
                    {
                        int weaponLevel = 1; // Placeholder, can be extended for weapon leveling
                        if (item.Name.ToLower().Contains("sword"))
                            Agility += Agility * (2 * weaponLevel) + Level;
                        else if (item.Name.ToLower().Contains("axe"))
                            Strength += Strength * (2 * weaponLevel) + Level;
                        else if (item.Name.ToLower().Contains("dagger"))
                            Technique += Technique * (2 * weaponLevel) + Level;
                    }
                    break;
                case "feet": 
                    previousItem = Feet;
                    Feet = item; 
                    break;
            }
            return previousItem;
        }

        public Item? UnequipItem(string slot)
        {
            Item? unequippedItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    unequippedItem = Head;
                    Head = null; 
                    break;
                case "body": 
                    unequippedItem = Body;
                    Body = null; 
                    break;
                case "weapon": 
                    unequippedItem = Weapon;
                    Weapon = null; 
                    break;
                case "feet": 
                    unequippedItem = Feet;
                    Feet = null; 
                    break;
            }
            return unequippedItem;
        }

        // Methods to add XP and handle leveling up
        public void AddXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel())
            {
                XP -= XPToNextLevel();
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            MaxHealth += 10;
            CurrentHealth = MaxHealth;
            Strength += 2;
            Agility += 2;
            Technique += 2;
        }

        private int XPToNextLevel()
        {
            return 100 * Level;
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
        }

        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public bool IsAlive => CurrentHealth > 0;

        public override string GetDescription()
        {
            return $"Level {Level} (Health: {CurrentHealth}/{MaxHealth}) (STR: {Strength}, AGI: {Agility}, TEC: {Technique})";
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public void UpdateComboBonus()
        {
            int bonus = 0;
            // Equipped items
            if (Head != null) bonus += Head.ComboBonus;
            if (Body != null) bonus += Body.ComboBonus;
            if (Weapon != null) bonus += Weapon.ComboBonus;
            if (Feet != null) bonus += Feet.ComboBonus;
            // Inventory items (optional: only if you want inventory to count)
            // foreach (var item in Inventory) bonus += item.ComboBonus;
            ComboBonus = bonus;
        }

        public List<Action> GetComboActions()
        {
            var comboActions = new List<Action>();
            foreach (var entry in ActionPool)
            {
                if (entry.action.IsComboAction)
                    comboActions.Add(entry.action);
            }
            comboActions.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            return comboActions;
        }

        public void SetTempComboBonus(int bonus, int turns)
        {
            TempComboBonus = bonus;
            TempComboBonusTurns = turns;
        }

        public int ConsumeTempComboBonus()
        {
            int bonus = TempComboBonus;
            if (TempComboBonusTurns > 0)
            {
                TempComboBonusTurns--;
                if (TempComboBonusTurns == 0)
                    TempComboBonus = 0;
            }
            return bonus;
        }

        public void ApplyHealthMultiplier(double multiplier)
        {
            MaxHealth = (int)(MaxHealth * multiplier);
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Activates combo mode when a combo attack is triggered
        /// </summary>
        public void ActivateComboMode()
        {
            ComboModeActive = true;
        }

        /// <summary>
        /// Deactivates combo mode when combo fails or completes
        /// </summary>
        public void DeactivateComboMode()
        {
            ComboModeActive = false;
        }

        /// <summary>
        /// Resets combo state including combo mode
        /// </summary>
        public void ResetCombo()
        {
            ComboStep = 0;
            ComboAmplifier = 1.0;
            LastComboActionIdx = -1;
            ComboModeActive = false;
        }
    }
} 