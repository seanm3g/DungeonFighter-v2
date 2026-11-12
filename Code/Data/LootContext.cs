using System;
using System.Collections.Generic;

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
        /// The hero's class tags (barbarian / warrior / rogue / wizard) for gear action rolls: the top class-point path,
        /// plus the second when it has points (a hybrid hero). Falls back to the equipped weapon's class. Empty = unknown.
        /// </summary>
        public List<string> HeroClassTags { get; set; } = new();

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
        /// Class-material bias from the skill-tree market rules (Blood Market, Muster Hall, …): class tag → chance that
        /// a non-weapon drop takes that class's material for its rarity.
        /// </summary>
        public Dictionary<string, double> ClassMaterialChance { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        private static readonly (string Effect, string ClassTag)[] MarketRules =
        {
            ("blood_market", ClassTags.Barbarian),
            ("muster_hall", ClassTags.Warrior),
            ("black_market", ClassTags.Rogue),
            ("reagent_market", ClassTags.Wizard)
        };

        /// <summary>Base chance per unit of market power ("+50% class-tag item generation").</summary>
        public const double MarketChancePerPower = 0.5;

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
            if (player != null)
            {
                context.HeroClassTags = ResolveHeroClassTags(player);
                foreach (var (effect, classTag) in MarketRules)
                {
                    double power = SkillRanks.GetPower(player, effect);
                    if (power > 0)
                        context.ClassMaterialChance[classTag] = Math.Min(1.0, MarketChancePerPower * power);
                }
            }
            return context;
        }

        /// <summary>Top class-point path (+ second when it has points); else the equipped weapon's class; else empty.</summary>
        public static List<string> ResolveHeroClassTags(Character player)
        {
            var tags = new List<string>();
            var progression = player.Progression;
            if (progression != null)
            {
                foreach (var path in new[] { progression.GetPrimaryClassWeaponType(), progression.GetSecondaryClassWeaponType() })
                    if (path is WeaponType wt && ClassTags.ClassOf(wt) is string tag && !tags.Contains(tag))
                        tags.Add(tag);
            }
            if (tags.Count == 0 && player.Weapon is WeaponItem weapon && ClassTags.ClassOf(weapon.WeaponType) is string weaponTag)
                tags.Add(weaponTag);
            return tags;
        }
    }
}
