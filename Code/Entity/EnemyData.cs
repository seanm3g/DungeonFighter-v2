using System;
using System.Collections.Generic;

namespace RPGGame
{
    public enum PrimaryAttribute
    {
        Strength,
        Agility,
        Technique,
        Intelligence
    }

    /// <summary>
    /// Defines different enemy attack archetypes based on DPS distribution
    /// </summary>
    public enum EnemyArchetype
    {
        Knight,
        Assassin,
        Berserker,
        Acrobat,
        Brute,
        Warlord,
        Sage,
        Duelist,
        Trickster
    }

    /// <summary>
    /// Configuration for enemy attack patterns
    /// </summary>
    public class EnemyAttackProfile
    {
        public EnemyArchetype Archetype { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        
        // Speed multiplier (affects attack time - lower = faster attacks)
        public double SpeedMultiplier { get; set; } = 1.0;
        
        // Damage multiplier (affects damage per hit)
        public double DamageMultiplier { get; set; } = 1.0;
        
        // Health multiplier (affects total health)
        public double HealthMultiplier { get; set; } = 1.0;
        
        // Armor multiplier (affects damage reduction)
        public double ArmorMultiplier { get; set; } = 1.0;
        
        // Action pool configuration
        public List<string> PreferredActions { get; set; } = new List<string>();
        public List<string> AvoidActions { get; set; } = new List<string>();
    }
}
