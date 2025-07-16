namespace RPGGame
{
    public enum ItemType
    {
        Head,
        Feet,
        Chest,
        Weapon
    }

    public class Item
    {
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public int Durability { get; set; }
        public double Weight { get; set; }
        public int ComboBonus { get; set; } = 0;

        public Item(ItemType type, string? name = null, int durability = 100, double weight = 1.0, int comboBonus = 0)
        {
            Type = type;
            Name = name ?? (type == ItemType.Weapon ? FlavorText.GenerateWeaponName() : FlavorText.GenerateArmorName());
            Durability = durability;
            Weight = weight;
            ComboBonus = comboBonus;
        }
    }

    public class HeadItem : Item
    {
        public int Armor { get; set; }
        public HeadItem(string? name = null, int durability = 100, int armor = 5, double weight = 1.0)
            : base(ItemType.Head, name, durability, weight)
        {
            Armor = armor;
        }
    }

    public class FeetItem : Item
    {
        public int Armor { get; set; }
        public FeetItem(string? name = null, int durability = 100, int armor = 3, double weight = 1.0)
            : base(ItemType.Feet, name, durability, weight)
        {
            Armor = armor;
        }
    }

    public class ChestItem : Item
    {
        public int Armor { get; set; }
        public ChestItem(string? name = null, int durability = 100, int armor = 8, double weight = 1.0)
            : base(ItemType.Chest, name, durability, weight)
        {
            Armor = armor;
        }
    }

    public class WeaponItem : Item
    {
        public int Damage { get; set; }
        public WeaponItem(string? name = null, int durability = 100, int damage = 10, double weight = 1.0)
            : base(ItemType.Weapon, name, durability, weight)
        {
            Damage = damage;
        }
    }
} 