using System;

namespace RPGGame
{
    public static class DungeonLevelMath
    {
        /// <summary>
        /// Converts a dungeon delta (relative to hero) into an effective dungeon level.
        /// Example: hero 10 + delta +5 => dungeon 15.
        /// </summary>
        public static int ResolveEffectiveDungeonLevel(int heroLevel, int dungeonDelta)
        {
            heroLevel = Math.Clamp(heroLevel, 1, 99);
            int dungeon = heroLevel + dungeonDelta;
            return Math.Clamp(dungeon, 1, 99);
        }

        /// <summary>
        /// Clamp deltas so they cannot push beyond the 1–99 dungeon range.
        /// </summary>
        public static int ClampDungeonDelta(int heroLevel, int dungeonDelta)
        {
            heroLevel = Math.Clamp(heroLevel, 1, 99);
            int minDelta = 1 - heroLevel;
            int maxDelta = 99 - heroLevel;
            return Math.Clamp(dungeonDelta, minDelta, maxDelta);
        }
    }
}

