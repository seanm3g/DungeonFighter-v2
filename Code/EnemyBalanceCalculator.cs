using System;

namespace RPGGame
{
    /// <summary>
    /// Calculates enemy stats using a layered DPS/SUSTAIN point allocation system
    /// </summary>
    public static class EnemyBalanceCalculator
    {
        /// <summary>
        /// Calculates balanced enemy stats based on level and archetype
        /// </summary>
        /// <param name="level">Enemy level</param>
        /// <param name="archetype">Enemy archetype (affects DPS/SUSTAIN allocation)</param>
        /// <param name="baseStats">Base stats from JSON data (used for archetype determination)</param>
        /// <returns>Calculated enemy stats</returns>
        public static CalculatedEnemyStats CalculateStats(int level, EnemyArchetype archetype, EnemyBaseStats baseStats)
        {
            var config = GameConfiguration.Instance.EnemyBalance;
            
            // Calculate total points available for this level
            int totalPoints = config.BaseTotalPointsAtLevel1 + ((level - 1) * config.TotalPointsPerLevel);
            
            // Determine DPS/SUSTAIN allocation based on archetype
            var allocation = DetermineAllocation(archetype, config);
            
            // Calculate DPS and SUSTAIN points
            int dpsPoints = (int)Math.Round(totalPoints * allocation.DPSPercentage);
            int sustainPoints = totalPoints - dpsPoints; // Ensure total points are used exactly
            
            // Calculate individual stats
            var stats = new CalculatedEnemyStats();
            
            // DPS Components: Damage and Attack Speed
            int damagePoints = (int)Math.Round(dpsPoints * config.DPSComponents.AttackWeight);
            int attackSpeedPoints = dpsPoints - damagePoints;
            
            stats.Strength = Math.Max(1, (int)Math.Round(damagePoints * config.StatConversionRates.DamagePerPoint));
            stats.AttackSpeed = Math.Max(0.5, attackSpeedPoints * config.StatConversionRates.AttackSpeedPerPoint);
            
            // SUSTAIN Components: Health and Armor
            int healthPoints = (int)Math.Round(sustainPoints * config.SUSTAINComponents.HealthWeight);
            int armorPoints = sustainPoints - healthPoints;
            
            stats.Health = Math.Max(10, (int)Math.Round(healthPoints * config.StatConversionRates.HealthPerPoint));
            stats.Armor = Math.Max(0, (int)Math.Round(armorPoints * config.StatConversionRates.ArmorPerPoint));
            
            // Set other stats based on archetype and primary attribute
            SetSecondaryStats(stats, archetype, baseStats);
            
            return stats;
        }
        
        /// <summary>
        /// Determines DPS/SUSTAIN allocation based on enemy archetype
        /// </summary>
        private static (double DPSPercentage, double SUSTAINPercentage) DetermineAllocation(EnemyArchetype archetype, EnemyBalanceConfig config)
        {
            return archetype switch
            {
                EnemyArchetype.Berserker => (config.DPSAllocation.MaxPercentage, config.SUSTAINAllocation.MinPercentage),
                EnemyArchetype.Assassin => (config.DPSAllocation.MaxPercentage, config.SUSTAINAllocation.MinPercentage),
                EnemyArchetype.Juggernaut => (config.DPSAllocation.MinPercentage, config.SUSTAINAllocation.MaxPercentage),
                EnemyArchetype.Guardian => (config.DPSAllocation.MinPercentage, config.SUSTAINAllocation.MaxPercentage),
                EnemyArchetype.Warrior => (config.DPSAllocation.DefaultPercentage, config.SUSTAINAllocation.DefaultPercentage),
                EnemyArchetype.Brute => (config.DPSAllocation.DefaultPercentage, config.SUSTAINAllocation.DefaultPercentage),
                _ => (config.DPSAllocation.DefaultPercentage, config.SUSTAINAllocation.DefaultPercentage)
            };
        }
        
        /// <summary>
        /// Sets secondary stats (Agility, Technique, Intelligence) based on archetype and base stats
        /// </summary>
        private static void SetSecondaryStats(CalculatedEnemyStats stats, EnemyArchetype archetype, EnemyBaseStats baseStats)
        {
            // Use base stats as a foundation but scale them appropriately
            stats.Agility = Math.Max(1, baseStats.Agility);
            stats.Technique = Math.Max(1, baseStats.Technique);
            stats.Intelligence = Math.Max(1, baseStats.Intelligence);
            
            // Apply archetype-specific bonuses
            switch (archetype)
            {
                case EnemyArchetype.Assassin:
                    stats.Agility = (int)Math.Round(stats.Agility * 1.3);
                    stats.AttackSpeed *= 1.2; // Assassins are faster
                    break;
                case EnemyArchetype.Berserker:
                    stats.Strength = (int)Math.Round(stats.Strength * 1.2);
                    stats.AttackSpeed *= 1.1; // Berserkers are slightly faster
                    break;
                case EnemyArchetype.Juggernaut:
                    stats.Health = (int)Math.Round(stats.Health * 1.3);
                    stats.Armor = (int)Math.Round(stats.Armor * 1.2);
                    break;
                case EnemyArchetype.Guardian:
                    stats.Health = (int)Math.Round(stats.Health * 1.5);
                    stats.Armor = (int)Math.Round(stats.Armor * 1.4);
                    stats.Strength = (int)Math.Round(stats.Strength * 0.8); // Reduce damage
                    break;
                case EnemyArchetype.Warrior:
                    // Balanced approach - no major bonuses
                    break;
                case EnemyArchetype.Brute:
                    stats.Strength = (int)Math.Round(stats.Strength * 1.1);
                    stats.Health = (int)Math.Round(stats.Health * 1.1);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Represents calculated enemy stats from the balance system
    /// </summary>
    public class CalculatedEnemyStats
    {
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public double AttackSpeed { get; set; }
    }
    
    /// <summary>
    /// Represents base enemy stats from JSON data
    /// </summary>
    public class EnemyBaseStats
    {
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
    }
}
