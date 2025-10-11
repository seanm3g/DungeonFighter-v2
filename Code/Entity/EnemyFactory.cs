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
        /// Creates a basic enemy with standard stats
        /// </summary>
        public static Enemy CreateBasicEnemy(string name, int level)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(50 + (level * 10))
                .WithStats(8 + level, 6 + level, 4 + level, 4 + level)
                .WithArmor(level)
                .Build();
        }

        /// <summary>
        /// Creates a berserker enemy (high damage, aggressive)
        /// </summary>
        public static Enemy CreateBerserker(string name, int level)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(60 + (level * 12))
                .WithStats(12 + level, 4 + level, 8 + level, 2 + level)
                .WithPrimaryAttribute(PrimaryAttribute.Strength)
                .WithArchetype(EnemyArchetype.Berserker)
                .WithArmor(level / 2)
                .Build();
        }

        /// <summary>
        /// Creates an assassin enemy (high agility, fast attacks)
        /// </summary>
        public static Enemy CreateAssassin(string name, int level)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(40 + (level * 8))
                .WithStats(4 + level, 12 + level, 6 + level, 4 + level)
                .WithPrimaryAttribute(PrimaryAttribute.Agility)
                .WithArchetype(EnemyArchetype.Assassin)
                .WithArmor(level)
                .Build();
        }

        /// <summary>
        /// Creates a brute enemy (high health, heavy hitter)
        /// </summary>
        public static Enemy CreateBrute(string name, int level)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(80 + (level * 15))
                .WithStats(10 + level, 3 + level, 5 + level, 2 + level)
                .WithPrimaryAttribute(PrimaryAttribute.Strength)
                .WithArchetype(EnemyArchetype.Brute)
                .WithArmor(level * 2)
                .Build();
        }

        /// <summary>
        /// Creates a guardian enemy (high armor, protective)
        /// </summary>
        public static Enemy CreateGuardian(string name, int level)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(70 + (level * 12))
                .WithStats(6 + level, 4 + level, 8 + level, 6 + level)
                .WithPrimaryAttribute(PrimaryAttribute.Technique)
                .WithArchetype(EnemyArchetype.Guardian)
                .WithArmor(level * 3)
                .Build();
        }

        /// <summary>
        /// Creates a mage enemy (high intelligence, magical)
        /// </summary>
        public static Enemy CreateMage(string name, int level)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(45 + (level * 9))
                .WithStats(2 + level, 4 + level, 4 + level, 12 + level)
                .WithPrimaryAttribute(PrimaryAttribute.Intelligence)
                .WithArchetype(EnemyArchetype.Mage)
                .WithArmor(level)
                .Build();
        }

        /// <summary>
        /// Creates an undead enemy (immune to poison/bleed)
        /// </summary>
        public static Enemy CreateUndead(string name, int level)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(55 + (level * 11))
                .WithStats(8 + level, 5 + level, 5 + level, 3 + level)
                .WithPrimaryAttribute(PrimaryAttribute.Strength)
                .WithLivingStatus(false)
                .WithArchetype(EnemyArchetype.Brute)
                .WithArmor(level)
                .Build();
        }

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