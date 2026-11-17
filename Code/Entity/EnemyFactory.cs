using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Factory pattern for creating different types of enemies
    /// Provides convenient methods for creating common enemy types
    /// </summary>
    public static class EnemyFactory
    {

        /// <summary>
        /// Creates an enemy with direct stats (new system)
        /// </summary>
        public static Enemy CreateWithDirectStats(string name, int level, int maxHealth, int damage, double attackSpeed, int armor = 0)
        {
            return EnemyBuilder.CreateWithDirectStats(name, level, maxHealth, damage, attackSpeed, armor);
        }

        /// <summary>
        /// Creates an enemy with traditional stats (legacy system)
        /// </summary>
        public static Enemy CreateWithStats(string name, int level, int maxHealth, int strength, int agility, int technique, int intelligence, int armor = 0)
        {
            return EnemyBuilder.CreateWithStats(name, level, maxHealth, strength, agility, technique, intelligence, armor);
        }
    }
}