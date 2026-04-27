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
            heroLevel = Math.Clamp(heroLevel, Utils.GameConstants.MIN_CHARACTER_LEVEL, Utils.GameConstants.MAX_CHARACTER_LEVEL);
            int dungeon = heroLevel + dungeonDelta;
            return Math.Clamp(dungeon, Utils.GameConstants.MIN_DUNGEON_LEVEL, Utils.GameConstants.MAX_DUNGEON_LEVEL);
        }

        /// <summary>
        /// Clamp deltas so they cannot push beyond the configured dungeon level range.
        /// </summary>
        public static int ClampDungeonDelta(int heroLevel, int dungeonDelta)
        {
            heroLevel = Math.Clamp(heroLevel, Utils.GameConstants.MIN_CHARACTER_LEVEL, Utils.GameConstants.MAX_CHARACTER_LEVEL);
            int minDelta = Utils.GameConstants.MIN_DUNGEON_LEVEL - heroLevel;
            int maxDelta = Utils.GameConstants.MAX_DUNGEON_LEVEL - heroLevel;
            return Math.Clamp(dungeonDelta, minDelta, maxDelta);
        }
    }
}

