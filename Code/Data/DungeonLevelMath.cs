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
        /// Dungeon-selection menu center level: a stored custom anchor when valid, otherwise the hero's level.
        /// </summary>
        public static int ResolveSelectionAnchorLevel(int heroLevel, int? customAnchor)
        {
            if (customAnchor is int anchor
                && anchor >= Utils.GameConstants.MIN_DUNGEON_LEVEL
                && anchor <= Utils.GameConstants.MAX_DUNGEON_LEVEL)
            {
                return anchor;
            }

            return Math.Clamp(heroLevel, Utils.GameConstants.MIN_DUNGEON_LEVEL, Utils.GameConstants.MAX_DUNGEON_LEVEL);
        }

        /// <summary>
        /// Offered dungeon level relative to a selection anchor (typically -1 / 0 / +1), clamped to dungeon bounds.
        /// </summary>
        public static int ResolveDungeonLevelAroundAnchor(int anchorLevel, int dungeonDelta)
        {
            anchorLevel = Math.Clamp(anchorLevel, Utils.GameConstants.MIN_DUNGEON_LEVEL, Utils.GameConstants.MAX_DUNGEON_LEVEL);
            return Math.Clamp(anchorLevel + dungeonDelta, Utils.GameConstants.MIN_DUNGEON_LEVEL, Utils.GameConstants.MAX_DUNGEON_LEVEL);
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

