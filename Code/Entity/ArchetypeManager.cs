using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages enemy archetype logic and attack profiles
    /// Extracts archetype management from the main Enemy class
    /// </summary>
    public static class ArchetypeManager
    {
        /// <summary>
        /// Suggests an archetype for an enemy based on their stats
        /// </summary>
        public static EnemyArchetype SuggestArchetypeForEnemy(string name, int strength, int agility, int technique, int intelligence)
        {
            // Simple archetype suggestion based on primary stat
            int maxStat = Math.Max(Math.Max(strength, agility), Math.Max(technique, intelligence));
            
            if (maxStat == strength)
                return EnemyArchetype.Brute;
            else if (maxStat == agility)
                return EnemyArchetype.Assassin;
            else if (maxStat == technique)
                return EnemyArchetype.Berserker;
            else
                return EnemyArchetype.Guardian;
        }

        /// <summary>
        /// Gets the attack profile for a given archetype
        /// </summary>
        public static EnemyAttackProfile GetArchetypeProfile(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Berserker => new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Berserker,
                    Name = "Berserker",
                    SpeedMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    HealthMultiplier = 1.0,
                    ArmorMultiplier = 1.0
                },
                EnemyArchetype.Assassin => new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Assassin,
                    Name = "Assassin",
                    SpeedMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    HealthMultiplier = 1.0,
                    ArmorMultiplier = 1.0
                },
                EnemyArchetype.Brute => new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Brute,
                    Name = "Brute",
                    SpeedMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    HealthMultiplier = 1.0,
                    ArmorMultiplier = 1.0
                },
                EnemyArchetype.Guardian => new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Guardian,
                    Name = "Guardian",
                    SpeedMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    HealthMultiplier = 1.0,
                    ArmorMultiplier = 1.0
                },
                EnemyArchetype.Mage => new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Mage,
                    Name = "Mage",
                    SpeedMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    HealthMultiplier = 1.0,
                    ArmorMultiplier = 1.0
                },
                _ => new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Berserker,
                    Name = "Warrior",
                    SpeedMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    HealthMultiplier = 1.0,
                    ArmorMultiplier = 1.0
                }
            };
        }

        /// <summary>
        /// Gets the archetype-modified damage multiplier for an enemy
        /// </summary>
        public static double GetArchetypeDamageMultiplier(EnemyAttackProfile attackProfile)
        {
            return attackProfile.DamageMultiplier;
        }
    }
}
