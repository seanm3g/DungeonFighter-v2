using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Dungeon scaling configuration
    /// </summary>
    public class DungeonScalingConfig
    {
        public int RoomCountBase { get; set; } = 2;
        public double RoomCountPerLevel { get; set; } = 0.5;
        /// <summary>Upper bound on rooms per dungeon (0 = no cap). Keeps fights-per-dungeon inside the health budget.</summary>
        public int MaxRoomCount { get; set; }
        /// <summary>
        /// Upper bound on enemies per hostile room (0 = legacy ceil(roomLevel / 2), which grows without limit — about
        /// 25 fights per dungeon at level 10 and 2500 at level 100 once multiplied by room count).
        /// </summary>
        public int MaxEnemiesPerRoom { get; set; }
        /// <summary>Percent of effective max HP restored after each won fight inside a dungeon (0 = none, legacy).</summary>
        public double PostFightHealPercent { get; set; }
        public int EnemyCountPerRoom { get; set; } = 2;
        public double BossRoomChance { get; set; } = 0.1;
        /// <summary>
        /// When true, the final room's enemies merge into one boss: every enemy's actions in one sequence (the dungeon's
        /// longest), the room's summed HP and rewards. Off = the final room fights like any other (legacy).
        /// </summary>
        public bool BossMergesRoom { get; set; }
        /// <summary>Damage scale on the merged boss's swings (1 = one room enemy's hit; the merged fight keeps the
        /// hero's combo running, so a boss at 1 costs less HP than the room it replaced).</summary>
        public double BossDamageMultiplier { get; set; } = 1.0;
        public double TrapRoomChance { get; set; } = 0.2;
        public double TreasureRoomChance { get; set; } = 0.15;
        public string Description { get; set; } = "";

        public void EnsureSensibleDefaults()
        {
            if (RoomCountBase <= 0)
                RoomCountBase = 2;
            if (RoomCountPerLevel <= 0)
                RoomCountPerLevel = 0.5;
            if (EnemyCountPerRoom <= 0)
                EnemyCountPerRoom = 2;
            if (BossRoomChance < 0)
                BossRoomChance = 0;
            if (TrapRoomChance < 0)
                TrapRoomChance = 0;
            if (TreasureRoomChance < 0)
                TreasureRoomChance = 0;
        }
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
