using RPGGame;

namespace RPGGame.Progression
{
    /// <summary>
    /// Centralized system for awarding XP from different sources
    /// Awards XP immediately when earned, allowing mid-dungeon level-ups
    /// </summary>
    public static class XPRewardSystem
    {
        /// <summary>
        /// Awards XP for killing an enemy
        /// Uses the enemy's XPReward property (1x multiplier)
        /// Falls back to recalculating XP if enemy.XPReward is 0
        /// </summary>
        public static void AwardEnemyKillXP(Character player, Enemy enemy)
        {
            if (player == null || enemy == null) return;

            // Use the enemy's pre-calculated XPReward (already scaled by level)
            int xpGained = enemy.XPReward;

            // Fallback: if enemy XPReward is 0, recalculate using same logic as Enemy constructor
            if (xpGained <= 0)
            {
                var tuning = GameConfiguration.Instance;
                int baseXP = tuning.Progression.EnemyXPBase;
                if (baseXP <= 0)
                {
                    baseXP = 25; // Fallback minimum if config is 0 or negative
                }
                int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
                if (xpPerLevel <= 0)
                {
                    xpPerLevel = 5; // Fallback to ensure higher level enemies give more XP
                }
                xpGained = baseXP + (enemy.Level * xpPerLevel);
            }

            if (xpGained > 0)
            {
                player.AddXP(xpGained);
            }
        }

        /// <summary>
        /// Awards XP for entering a room
        /// Awards 0.5x of base enemy XP for that level
        /// </summary>
        public static void AwardRoomEntryXP(Character player, int roomLevel)
        {
            if (player == null) return;

            var tuning = GameConfiguration.Instance;
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0)
            {
                baseXP = 25; // Fallback minimum if config is 0 or negative
            }
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0)
            {
                xpPerLevel = 5; // Fallback to ensure higher level rooms give more XP
            }

            // Calculate base XP for this level
            int baseLevelXP = baseXP + (roomLevel * xpPerLevel);
            
            // Room entry gives 0.5x of base enemy XP
            int xpGained = (int)(baseLevelXP * 0.5);

            if (xpGained > 0)
            {
                player.AddXP(xpGained);
            }
        }

        /// <summary>
        /// Awards XP for finding an item
        /// Awards 0.3x of base enemy XP for item level
        /// </summary>
        public static void AwardItemFoundXP(Character player, Item item)
        {
            if (player == null || item == null) return;

            var tuning = GameConfiguration.Instance;
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0)
            {
                baseXP = 25; // Fallback minimum if config is 0 or negative
            }
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0)
            {
                xpPerLevel = 5; // Fallback to ensure higher level items give more XP
            }

            // Calculate base XP for item's level
            int baseLevelXP = baseXP + (item.Level * xpPerLevel);
            
            // Item found gives 0.3x of base enemy XP
            int xpGained = (int)(baseLevelXP * 0.3);

            if (xpGained > 0)
            {
                player.AddXP(xpGained);
            }
        }

        /// <summary>
        /// Awards XP for completing a dungeon
        /// Awards 10x of base enemy XP for dungeon level (largest reward)
        /// </summary>
        public static void AwardDungeonCompletionXP(Character player, int dungeonLevel, bool isFirstDungeon = false)
        {
            if (player == null) return;

            var tuning = GameConfiguration.Instance;
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0)
            {
                baseXP = 25; // Fallback minimum if config is 0 or negative
            }
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0)
            {
                xpPerLevel = 5; // Fallback to ensure higher level dungeons give more XP
            }

            // Calculate XP based on dungeon level (not player level)
            int dungeonLevelXP = baseXP + (dungeonLevel * xpPerLevel);
            int xpReward = dungeonLevelXP * 10; // 10x multiplier for dungeon completion

            // Guarantee level-up after first dungeon (when level 1 and first dungeon completed)
            if (player.Level == 1 && isFirstDungeon)
            {
                // Calculate XP needed to level from 1->2
                int averageXPPerDungeonAtLevel1 = baseXP + 25;
                int xpNeededForLevel2 = 1 * 1 * averageXPPerDungeonAtLevel1; // Level^2 * base

                // Ensure we award at least enough XP to level up, with a small buffer
                if (xpReward < xpNeededForLevel2)
                {
                    xpReward = xpNeededForLevel2 + 5; // Add small buffer to ensure level up
                }
            }

            player.AddXP(xpReward);
            UIManager.WriteBlankLine(); // Blank line before loot

            if (player.Level > 1)
            {
                UIManager.WriteLine($"Level up! You are now level {player.Level}");
                UIManager.WriteBlankLine(); // Add line break after level up message
            }
        }
    }
}
