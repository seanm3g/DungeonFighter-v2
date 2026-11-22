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

        /// <summary>
        /// The boss damage multiplier ramps in by boss level: ×1 at level 1, the full <see cref="BossDamageMultiplier"/>
        /// at this level and above (playtest 11-21: a full ×2.5 merged Demon one-shot level-1 heroes; 30% died in the first
        /// dungeon). 1 or less = no ramp.
        /// </summary>
        public int BossDamageFullAtLevel { get; set; } = 5;

        /// <summary>
        /// Room enemies before the Demon (every enemy but the final boss) start at this share of their HP at level 1,
        /// ramping to full HP at <see cref="RoomEnemyHealthFullAtLevel"/> (playtest 11-21: shorter early fights so the
        /// hero reaches the Demon with more HP). 1 = off.
        /// </summary>
        public double RoomEnemyHealthAtLevel1 { get; set; } = 0.6;

        public int RoomEnemyHealthFullAtLevel { get; set; } = 5;

        /// <summary>The HP share for a room enemy (not the Demon) of <paramref name="level"/>.</summary>
        public double RoomEnemyHealthAt(int level)
        {
            double start = Math.Clamp(RoomEnemyHealthAtLevel1, 0.05, 1.0);
            if (RoomEnemyHealthFullAtLevel <= 1 || level >= RoomEnemyHealthFullAtLevel)
                return 1.0;
            double t = Math.Clamp((Math.Max(1, level) - 1) / (double)(RoomEnemyHealthFullAtLevel - 1), 0, 1);
            return start + (1.0 - start) * t;
        }

        /// <summary>The boss damage multiplier for a boss of <paramref name="level"/> (see <see cref="BossDamageFullAtLevel"/>).</summary>
        public double BossDamageMultiplierAt(int level)
        {
            double full = BossDamageMultiplier > 0 ? BossDamageMultiplier : 1.0;
            if (BossDamageFullAtLevel <= 1 || level >= BossDamageFullAtLevel)
                return full;
            double t = Math.Clamp((Math.Max(1, level) - 1) / (double)(BossDamageFullAtLevel - 1), 0, 1);
            return 1.0 + (full - 1.0) * t;
        }
        public double TrapRoomChance { get; set; } = 0.2;
        public double TreasureRoomChance { get; set; } = 0.15;
        public string Description { get; set; } = "";

        /// <summary>
        /// The two room counts a dungeon at <paramref name="level"/> can roll and the chance of the larger one.
        /// Rooms grow by <see cref="RoomCountPerLevel"/> per level; the fraction is a chance of one more room, so the
        /// average grows smoothly instead of stepping (0.5/level: L5 rolls 2 or 3 rooms, L6 always 3, L7 3 or 4).
        /// Never below <see cref="RoomCountBase"/> (or 1), never above <see cref="MaxRoomCount"/> when set.
        /// </summary>
        public (int Low, int High, double HighChance) GetRoomCountRange(int level)
        {
            double exact = Math.Max(0, level) * RoomCountPerLevel;
            int low = (int)Math.Floor(exact);
            double frac = exact - low;
            int floor = Math.Max(1, RoomCountBase);
            int Clamp(int n)
            {
                n = Math.Max(floor, n);
                return MaxRoomCount > 0 ? Math.Min(n, MaxRoomCount) : n;
            }
            int lo = Clamp(low), hi = Clamp(low + 1);
            if (frac < 1e-9 || lo == hi)
                return (lo, lo, 0);
            return (lo, hi, frac);
        }

        /// <summary>Rolls a dungeon's room count from <see cref="GetRoomCountRange"/>.</summary>
        public int RollRoomCount(int level, Random random)
        {
            var (low, high, chance) = GetRoomCountRange(level);
            return chance > 0 && random.NextDouble() < chance ? high : low;
        }

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
