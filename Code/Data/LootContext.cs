using System;

namespace RPGGame
{
    /// <summary>
    /// Lightweight context object for loot generation
    /// Carries information about the player, dungeon, and enemy that influences loot generation
    /// Used by modification and action selectors to apply contextual biases
    /// </summary>
    public class LootContext
    {
        /// <summary>
        /// The player's class (if available)
        /// Used to influence action selection
        /// </summary>
        public string? PlayerClass { get; set; }

        /// <summary>
        /// The dungeon theme (Forest, Lava, Crypt, etc.)
        /// Used to bias modifications toward thematic ones
        /// </summary>
        public string? DungeonTheme { get; set; }

        /// <summary>
        /// The enemy archetype that dropped the loot (Berserker, Guardian, Assassin, etc.)
        /// Used to bias modifications toward archetype-specific ones
        /// </summary>
        public string? EnemyArchetype { get; set; }

        /// <summary>
        /// The weapon type of the generated item (if applicable)
        /// Used to select appropriate actions (Wand -> Wizard table, Sword -> Warrior table, etc.)
        /// </summary>
        public string? WeaponType { get; set; }

        /// <summary>
        /// Factory method to create a context from a character and optional dungeon info
        /// </summary>
        public static LootContext Create(
            Character? player,
            string? dungeonTheme = null,
            string? enemyArchetype = null)
        {
            var context = new LootContext
            {
                PlayerClass = player != null ? player.GetCurrentClass() : null,
                DungeonTheme = dungeonTheme,
                EnemyArchetype = enemyArchetype
            };
            return context;
        }
    }
}
