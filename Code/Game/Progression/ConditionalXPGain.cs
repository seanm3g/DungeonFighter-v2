using RPGGame;
using RPGGame.Combat.Events;

namespace RPGGame.Progression
{
    /// <summary>
    /// Handles conditional XP gain based on various criteria
    /// </summary>
    public static class ConditionalXPGain
    {
        /// <summary>
        /// Grants XP based on combat event
        /// </summary>
        public static void GrantXPFromEvent(CombatEvent evt, Character character)
        {
            int xpGained = 0;

            switch (evt.Type)
            {
                case CombatEventType.EnemyDied:
                    xpGained = CalculateEnemyKillXP(evt);
                    break;
                case CombatEventType.ActionCritical:
                    xpGained = CalculateCritXP(evt);
                    break;
                // Add more cases as needed
            }

            if (xpGained > 0 && character != null)
            {
                character.AddXP(xpGained);
            }
        }

        private static int CalculateEnemyKillXP(CombatEvent evt)
        {
            // Base XP for killing an enemy
            // Could scale based on enemy level, difficulty, etc.
            return 10; // Default value
        }

        private static int CalculateCritXP(CombatEvent evt)
        {
            // Small XP bonus for critical hits
            return 1;
        }
    }
}

