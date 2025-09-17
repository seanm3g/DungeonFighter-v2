namespace RPGGame
{
    public enum ItemType
    {
        Head,
        Feet,
        Chest,
        Weapon
    }

    public enum WeaponType
    {
        Sword,
        Dagger,
        Mace,
        Wand
    }

    public class StatBonus
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double Value { get; set; } = 0;
        public int Weight { get; set; } = 0;
        public string StatType { get; set; } = ""; // Which stat this affects (STR, AGI, TEC, INT, Health, Armor, etc.)
    }

    public class ActionBonus
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Weight { get; set; } = 0;
    }

    public class Modification
    {
        public int DiceResult { get; set; } = 0;
        public string ItemRank { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Effect { get; set; } = "";
        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 0;
    }

    public class ArmorStatus
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Effect { get; set; } = "";
        public double Value { get; set; } = 0;
        public bool IsPassive { get; set; } = true;
    }

    public class Item
    {
        public string Name { get; set; } = "";
        public ItemType Type { get; set; }
        public int Tier { get; set; } = 1;
        public int ComboBonus { get; set; } = 0;
        public string Rarity { get; set; } = "Common";
        public List<StatBonus> StatBonuses { get; set; } = new List<StatBonus>();
        public List<ActionBonus> ActionBonuses { get; set; } = new List<ActionBonus>();
        public List<Modification> Modifications { get; set; } = new List<Modification>();
        public List<ArmorStatus> ArmorStatuses { get; set; } = new List<ArmorStatus>();
        public int BonusDamage { get; set; } = 0;
        public int BonusAttackSpeed { get; set; } = 0;
        
        // The specific action this gear provides (assigned when created)
        public string? GearAction { get; set; } = null;
        
        // Check if this is a starter item (always gets actions)
        public bool IsStarterItem => Name.Contains("Starter");

        public Item(ItemType type, string? name = null, int tier = 1, int comboBonus = 0)
        {
            Type = type;
            Name = name ?? (type == ItemType.Weapon ? "Unknown Weapon" : "Unknown Armor");
            Tier = tier;
            ComboBonus = comboBonus;
        }
    }

    public class HeadItem : Item
    {
        public int Armor { get; set; }
        public HeadItem(string? name = null, int tier = 1, int armor = 5)
            : base(ItemType.Head, name, tier)
        {
            Armor = armor;
        }

        public int GetTotalArmor()
        {
            int totalArmor = Armor;
            
            // Add armor bonuses from stat bonuses
            foreach (var statBonus in StatBonuses)
            {
                if (statBonus.StatType == "Armor")
                {
                    totalArmor += (int)statBonus.Value;
                }
            }
            
            // Add armor bonuses from modifications
            foreach (var modification in Modifications)
            {
                if (modification.Effect.Contains("armor") || modification.Effect.Contains("Armor"))
                {
                    totalArmor += (int)modification.MaxValue; // Use MaxValue as the bonus amount
                }
            }
            
            return totalArmor;
        }
    }

    public class FeetItem : Item
    {
        public int Armor { get; set; }
        public FeetItem(string? name = null, int tier = 1, int armor = 3)
            : base(ItemType.Feet, name, tier)
        {
            Armor = armor;
        }

        public int GetTotalArmor()
        {
            int totalArmor = Armor;
            
            // Add armor bonuses from stat bonuses
            foreach (var statBonus in StatBonuses)
            {
                if (statBonus.StatType == "Armor")
                {
                    totalArmor += (int)statBonus.Value;
                }
            }
            
            // Add armor bonuses from modifications
            foreach (var modification in Modifications)
            {
                if (modification.Effect.Contains("armor") || modification.Effect.Contains("Armor"))
                {
                    totalArmor += (int)modification.MaxValue; // Use MaxValue as the bonus amount
                }
            }
            
            return totalArmor;
        }
    }

    public class ChestItem : Item
    {
        public int Armor { get; set; }
        public ChestItem(string? name = null, int tier = 1, int armor = 8)
            : base(ItemType.Chest, name, tier)
        {
            Armor = armor;
        }

        public int GetTotalArmor()
        {
            int totalArmor = Armor;
            
            // Add armor bonuses from stat bonuses
            foreach (var statBonus in StatBonuses)
            {
                if (statBonus.StatType == "Armor")
                {
                    totalArmor += (int)statBonus.Value;
                }
            }
            
            // Add armor bonuses from modifications
            foreach (var modification in Modifications)
            {
                if (modification.Effect.Contains("armor") || modification.Effect.Contains("Armor"))
                {
                    totalArmor += (int)modification.MaxValue; // Use MaxValue as the bonus amount
                }
            }
            
            return totalArmor;
        }
    }

    public class WeaponItem : Item
    {
        public int BaseDamage { get; set; }
        public double BaseAttackSpeed { get; set; } = 0.05;
        public WeaponType WeaponType { get; set; } = WeaponType.Sword;
        
        public WeaponItem(string? name = null, int tier = 1, int baseDamage = 10, double baseAttackSpeed = 0.05, WeaponType weaponType = WeaponType.Sword)
            : base(ItemType.Weapon, name, tier)
        {
            BaseDamage = baseDamage;
            BaseAttackSpeed = baseAttackSpeed;
            WeaponType = weaponType;
        }

        public int GetTotalDamage()
        {
            return BaseDamage + BonusDamage;
        }

        public double GetTotalAttackSpeed()
        {
            return BaseAttackSpeed + (BonusAttackSpeed * 0.1);
        }
        
        /// <summary>
        /// Gets the attack speed modifier for the new system (-2s to +10s range)
        /// </summary>
        /// <returns>Attack speed modifier in seconds</returns>
        public double GetAttackSpeedModifier()
        {
            // Convert the old attack speed system to the new modifier system
            // Old system: lower values = faster, new system: negative = faster, positive = slower
            // BaseAttackSpeed of 0.05 (fast) becomes -2s modifier
            // BaseAttackSpeed of 0.15 (slow) becomes +10s modifier
            
            double modifier = (BaseAttackSpeed - 0.05) * 100; // Scale to -2 to +10 range
            return Math.Max(-2.0, Math.Min(10.0, modifier)); // Clamp to -2s to +10s range
        }
    }
} 