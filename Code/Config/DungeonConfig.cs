using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Dungeon scaling configuration
    /// </summary>
    public class DungeonScalingConfig
    {
        public int RoomCountBase { get; set; }
        public double RoomCountPerLevel { get; set; }
        public int EnemyCountPerRoom { get; set; }
        public double BossRoomChance { get; set; }
        public double TrapRoomChance { get; set; }
        public double TreasureRoomChance { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Dungeon generation configuration
    /// </summary>
    public class DungeonGenerationConfig
    {
        public int minRooms { get; set; } = 2;
        public double roomCountScaling { get; set; } = 0.5;
        public double hostileRoomChance { get; set; } = 0.8;
        public string bossRoomName { get; set; } = "Boss";
        public string DefaultTheme { get; set; } = "Forest";
        public string DefaultRoomType { get; set; } = "Chamber";
        public int DefaultDungeonLevels { get; set; } = 5;
        public int DefaultRoomCount { get; set; } = 3;
        public List<string> EquipmentSlots { get; set; } = new() { "Head", "Chest", "Feet", "Weapon" };
        public List<string> StatusEffectTypes { get; set; } = new() { "bleed", "poison", "burn", "slow", "weaken", "stun" };
        public string DefaultCharacterName { get; set; } = "Unknown";
        public string Description { get; set; } = "Dungeon generation defaults and configuration";
    }
}
