using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Helper class for retrieving archetype configurations from TuningConfig
    /// </summary>
    public static class ArchetypeConfigHelper
    {
        /// <summary>
        /// Gets the archetype configuration for a given enemy archetype
        /// </summary>
        /// <param name="archetype">The enemy archetype</param>
        /// <param name="config">The enemy balance configuration</param>
        /// <returns>Archetype configuration with ratios and modifiers</returns>
        public static ArchetypeConfig GetArchetypeConfig(EnemyArchetype archetype, EnemyBalanceConfig config)
        {
            if (config.ArchetypeConfigs == null)
            {
                // Return default configuration if archetype configs are not available
                return new ArchetypeConfig
                {
                    AttributePoolRatio = 0.6,
                    SUSTAINPoolRatio = 0.4,
                    StrengthRatio = 0.4,
                    AgilityRatio = 0.3,
                    TechniqueRatio = 0.2,
                    IntelligenceRatio = 0.1,
                    SUSTAINHealthRatio = 0.5,
                    SUSTAINArmorRatio = 0.5,
                    Level1Modifiers = new Level1Modifiers()
                };
            }

            return archetype switch
            {
                EnemyArchetype.Berserker => config.ArchetypeConfigs.GetValueOrDefault("Berserker") ?? GetDefaultConfig(),
                EnemyArchetype.Assassin => config.ArchetypeConfigs.GetValueOrDefault("Assassin") ?? GetDefaultConfig(),
                EnemyArchetype.Brute => config.ArchetypeConfigs.GetValueOrDefault("Brute") ?? GetDefaultConfig(),
                EnemyArchetype.Guardian => config.ArchetypeConfigs.GetValueOrDefault("Guardian") ?? GetDefaultConfig(),
                EnemyArchetype.Mage => config.ArchetypeConfigs.GetValueOrDefault("Mage") ?? GetDefaultConfig(),
                _ => GetDefaultConfig()
            };
        }

        /// <summary>
        /// Gets a default archetype configuration
        /// </summary>
        private static ArchetypeConfig GetDefaultConfig()
        {
            return new ArchetypeConfig
            {
                AttributePoolRatio = 0.6,
                SUSTAINPoolRatio = 0.4,
                StrengthRatio = 0.4,
                AgilityRatio = 0.3,
                TechniqueRatio = 0.2,
                IntelligenceRatio = 0.1,
                SUSTAINHealthRatio = 0.5,
                SUSTAINArmorRatio = 0.5,
                Level1Modifiers = new Level1Modifiers()
            };
        }
    }
}

