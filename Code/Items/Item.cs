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
        Wand,
        Staff,
        Axe,
        Bow
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
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
        public int Level { get; set; } = 1; // Item level based on dungeon level when generated
        public int ComboBonus { get; set; } = 0;
        public string Rarity { get; set; } = "Common";
        public List<StatBonus> StatBonuses { get; set; } = new List<StatBonus>();
        public List<ActionBonus> ActionBonuses { get; set; } = new List<ActionBonus>();
        public List<Modification> Modifications { get; set; } = new List<Modification>();
        public List<ArmorStatus> ArmorStatuses { get; set; } = new List<ArmorStatus>();
        public int BonusDamage { get; set; } = 0;
        public int BonusAttackSpeed { get; set; } = 0;
        public List<string> Tags { get; set; } = new List<string>();
        
        // Attribute requirements for this item (extensible for future secondary attributes)
        public AttributeRequirements AttributeRequirements { get; set; } = new AttributeRequirements();
        
        // The specific action this gear provides (assigned when created)
        public string? GearAction { get; set; } = null;
        
        // Check if this is a starter item (always gets actions)
        public bool IsStarterItem => Name.Contains("Starter");

        /// <summary>
        /// Checks if a character meets all attribute requirements for this item
        /// </summary>
        /// <param name="character">The character to check</param>
        /// <returns>True if character meets all requirements, false otherwise</returns>
        public bool MeetsRequirements(Character character)
        {
            if (character == null || character.Facade == null || !AttributeRequirements.HasRequirements)
            {
                return true; // No requirements or no character means always pass
            }

            // Get character's effective attributes
            int strength = character.Facade.GetEffectiveStrength();
            int agility = character.Facade.GetEffectiveAgility();
            int technique = character.Facade.GetEffectiveTechnique();
            int intelligence = character.Facade.GetEffectiveIntelligence();

            // Check each requirement
            foreach (var requirement in AttributeRequirements)
            {
                int characterValue = requirement.Key.ToLower() switch
                {
                    "strength" => strength,
                    "agility" => agility,
                    "technique" => technique,
                    "intelligence" => intelligence,
                    _ => 0 // Future secondary attributes will need to be added here
                };

                if (characterValue < requirement.Value)
                {
                    return false; // Character doesn't meet this requirement
                }
            }

            return true; // All requirements met
        }

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
            
            // Prevent integer overflow
            return Math.Max(int.MinValue + 1, Math.Min(int.MaxValue - 1, totalArmor));
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
            
            // Prevent integer overflow
            return Math.Max(int.MinValue + 1, Math.Min(int.MaxValue - 1, totalArmor));
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
            
            // Prevent integer overflow
            return Math.Max(int.MinValue + 1, Math.Min(int.MaxValue - 1, totalArmor));
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
        /// Gets the attack speed multiplier for the weapon
        /// </summary>
        /// <returns>Attack speed multiplier (0.9 = 10% faster, 1.1 = 10% slower)</returns>
        public double GetAttackSpeedMultiplier()
        {
            // Clamp the attack speed to reasonable multiplier values
            // The scaling system has corrupted BaseAttackSpeed, so we need to normalize it
            double normalizedSpeed = Math.Max(0.1, Math.Min(10.0, BaseAttackSpeed));
            
            // If the speed is extremely high (corrupted by scaling), use a reasonable default
            if (BaseAttackSpeed > 10.0)
            {
                // Use a reasonable default for corrupted values
                return 1.0; // Normal speed
            }
            
            return normalizedSpeed;
        }
    }
} 