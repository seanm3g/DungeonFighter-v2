using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Starting gear configuration (weapons and armor)
    /// </summary>
    public class StartingGearConfig
    {
        public List<StartingWeaponConfig> Weapons { get; set; } = new();
        public List<StartingArmorConfig> Armor { get; set; } = new();
        public string Description { get; set; } = "Initial equipment for new characters";
    }

    /// <summary>
    /// Starting weapon configuration
    /// </summary>
    public class StartingWeaponConfig
    {
        public string Name { get; set; } = "";
        public double Damage { get; set; }
        public double AttackSpeed { get; set; }
        public double Weight { get; set; } = 0.0;
    }

    /// <summary>
    /// Starting armor configuration
    /// </summary>
    public class StartingArmorConfig
    {
        public string Slot { get; set; } = "";
        public string Name { get; set; } = "";
        public int Armor { get; set; }
        public double Weight { get; set; } = 0.0;
    }
}
