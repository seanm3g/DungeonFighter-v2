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
        public double RolledValue { get; set; } = 0; // The actual rolled value between MinValue and MaxValue
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
                    totalArmor += (int)modification.RolledValue; // Use RolledValue as the bonus amount
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
                    totalArmor += (int)modification.RolledValue; // Use RolledValue as the bonus amount
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
                    totalArmor += (int)modification.RolledValue; // Use RolledValue as the bonus amount
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
            // New system: BaseAttackSpeed is in seconds, BonusAttackSpeed is a modifier
            // BonusAttackSpeed should reduce the attack time (negative modifier = faster)
            return BaseAttackSpeed - (BonusAttackSpeed * 0.1);
        }
        
        /// <summary>
        /// Gets the attack speed modifier for the new system
        /// </summary>
        /// <returns>Attack speed modifier in seconds (positive = slower, negative = faster)</returns>
        public double GetAttackSpeedModifier()
        {
            // New system: BaseAttackSpeed is already in seconds
            // We need to convert this to a modifier relative to a base attack time
            // Base attack time is 10 seconds (from TuningConfig), so:
            // - If weapon has 3s attack speed, modifier should be -7s (3-10 = -7)
            // - If weapon has 6s attack speed, modifier should be -4s (6-10 = -4)
            // - If weapon has 10s attack speed, modifier should be 0s (10-10 = 0)
            
            double baseAttackTime = 10.0; // This should match TuningConfig.Combat.BaseAttackTime
            return BaseAttackSpeed - baseAttackTime;
        }
    }
} 