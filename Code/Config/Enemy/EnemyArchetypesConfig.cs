using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Enemy archetypes configuration
    /// </summary>
    public class EnemyArchetypesConfig
    {
        public Dictionary<string, EnemyArchetypeConfig> Archetypes { get; set; } = new();
    }
}

