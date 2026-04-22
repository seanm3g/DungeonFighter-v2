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
                xpGained = CharacterProgression.GetBaseXpForContentLevel(enemy.Level);
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

            int baseLevelXP = CharacterProgression.GetBaseXpForContentLevel(roomLevel);

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

            int baseLevelXP = CharacterProgression.GetBaseXpForContentLevel(item.Level);

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

            int xpReward = CharacterProgression.GetStandardDungeonCompletionXpForLevel(dungeonLevel);

            // Guarantee level-up after first dungeon (when level 1 and first dungeon completed)
            if (player.Level == 1 && isFirstDungeon)
            {
                int xpNeededForLevel2 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(1);

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
