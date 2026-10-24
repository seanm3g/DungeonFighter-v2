using System;
using System.Collections.Generic;
using RPGGame.World.DungeonMap;

namespace RPGGame
{
    /// <summary>
    /// Manages automatic exploration phases before and after combat
    /// Uses dice-based mechanics (1d20) similar to combat system
    /// </summary>
    public class ExplorationManager
    {
        private readonly Random random;
        private readonly EnvironmentDiscoveryGenerator discoveryGenerator;

        public ExplorationManager()
        {
            random = RandomUtility.Ambient;
            discoveryGenerator = new EnvironmentDiscoveryGenerator();
        }

        /// <summary>
        /// Automatically explores room before combat (dice-based).
        /// Legacy callers use Random-weighted approach tables.
        /// </summary>
        public ExplorationResult ExploreRoom(Environment room, Character player) =>
            ExploreRoom(room, player, TraversalStrategy.Random);

        /// <summary>
        /// Strategy-weighted approach: Smart skews first swing / intel; Random skews surprise / traps.
        /// </summary>
        public ExplorationResult ExploreRoom(Environment room, Character player, TraversalStrategy strategy)
        {
            int roll = Dice.Roll(20);
            var outcome = ResolveOutcomeForStrategy(roll, strategy);
            return BuildResult(room, roll, outcome, strategy);
        }

        /// <summary>
        /// Maps a natural d20 into an outcome using strategy-weighted bands.
        /// Smart: 1-2 FindNothing, 3-8 Discover, 9-10 Hazard, 11-20 SpotEnemyEarly
        /// Random: 1-8 FindNothing (surprise), 9-12 Discover, 13-18 Hazard, 19-20 SpotEnemyEarly
        /// </summary>
        public static ExplorationOutcome ResolveOutcomeForStrategy(int roll, TraversalStrategy strategy)
        {
            int r = Math.Clamp(roll, 1, 20);
            if (strategy == TraversalStrategy.Smart)
            {
                if (r <= 2) return ExplorationOutcome.FindNothing;
                if (r <= 8) return ExplorationOutcome.DiscoverEnvironment;
                if (r <= 10) return ExplorationOutcome.EnvironmentalHazard;
                return ExplorationOutcome.SpotEnemyEarly;
            }

            if (r <= 8) return ExplorationOutcome.FindNothing;
            if (r <= 12) return ExplorationOutcome.DiscoverEnvironment;
            if (r <= 18) return ExplorationOutcome.EnvironmentalHazard;
            return ExplorationOutcome.SpotEnemyEarly;
        }

        private ExplorationResult BuildResult(Environment room, int roll, ExplorationOutcome outcome, TraversalStrategy strategy)
        {
            switch (outcome)
            {
                case ExplorationOutcome.FindNothing:
                {
                    // Smart approach: no surprise; Random: surprise when enemies present
                    bool isSurprised = strategy == TraversalStrategy.Random && room.HasLivingEnemies();
                    var nothingMessages = new[]
                    {
                        "You carefully examine the room, but find nothing unusual.",
                        "The room appears empty and quiet.",
                        "You scan the area but detect nothing out of the ordinary.",
                        "Everything seems calm and undisturbed.",
                        "You take a moment to observe, but notice nothing significant."
                    };
                    return new ExplorationResult
                    {
                        Outcome = ExplorationOutcome.FindNothing,
                        Roll = roll,
                        Message = nothingMessages[random.Next(nothingMessages.Length)],
                        IsSurprised = isSurprised
                    };
                }
                case ExplorationOutcome.DiscoverEnvironment:
                {
                    string discovery = discoveryGenerator.GenerateDiscovery(room);
                    return new ExplorationResult
                    {
                        Outcome = ExplorationOutcome.DiscoverEnvironment,
                        Roll = roll,
                        Message = discovery,
                        EnvironmentInfo = discovery
                    };
                }
                case ExplorationOutcome.EnvironmentalHazard:
                {
                    var hazard = GenerateEnvironmentalHazard(room);
                    return new ExplorationResult
                    {
                        Outcome = ExplorationOutcome.EnvironmentalHazard,
                        Roll = roll,
                        Message = hazard.Message,
                        Hazard = hazard
                    };
                }
                default:
                {
                    var spotMessages = new[]
                    {
                        "A shadow moves in the corner! You spot the enemy before they notice you!",
                        "You catch a glimpse of movement ahead - the enemy hasn't seen you yet!",
                        "Your keen eyes spot the enemy first, giving you the advantage!",
                        "You notice the enemy before they're aware of your presence!",
                        "The enemy is distracted - you've spotted them first!"
                    };
                    return new ExplorationResult
                    {
                        Outcome = ExplorationOutcome.SpotEnemyEarly,
                        Roll = roll,
                        Message = spotMessages[random.Next(spotMessages.Length)],
                        PlayerGetsFirstAttack = true
                    };
                }
            }
        }

        /// <summary>
        /// Automatically searches room after combat (dice-based).
        /// On a successful search roll, yields food or a dungeon potion (not equipment).
        /// </summary>
        public SearchResult SearchRoom(Environment room, Character player, int dungeonLevel)
        {
            int roll = Dice.Roll(20);

            if (roll <= 18)
            {
                var nothingMessages = new[]
                {
                    "You search thoroughly but find nothing of value.",
                    "Your search reveals only dust and debris.",
                    "After a careful examination, you find nothing useful.",
                    "The area has been picked clean - nothing remains.",
                    "You search every corner but come up empty-handed.",
                    "Nothing of interest catches your eye.",
                    "The search yields no results."
                };
                return new SearchResult
                {
                    FoundLoot = false,
                    LootItem = null,
                    Roll = roll,
                    Message = nothingMessages[random.Next(nothingMessages.Length)]
                };
            }

            Item? loot = RoomSearchConsumableGenerator.Generate(random, player, dungeonLevel);
            var foundMessages = new[]
            {
                $"You search the area and find: {loot?.Name ?? "nothing"}",
                $"Hidden among the remains, you discover: {loot?.Name ?? "nothing"}",
                $"Your search uncovers: {loot?.Name ?? "nothing"}",
                $"After rummaging through the area, you find: {loot?.Name ?? "nothing"}",
                $"You spot something valuable: {loot?.Name ?? "nothing"}"
            };
            string message = loot != null
                ? foundMessages[random.Next(foundMessages.Length)]
                : "You search thoroughly but find nothing of value.";

            return new SearchResult
            {
                FoundLoot = loot != null,
                LootItem = loot,
                Roll = roll,
                Message = message
            };
        }

        private EnvironmentalHazard GenerateEnvironmentalHazard(Environment room)
        {
            int hazardRoll = Dice.Roll(20);
            bool skipToSearch = hazardRoll > 10;
            string message;
            int damage = 0;

            if (skipToSearch)
            {
                var skipMessages = new[]
                {
                    "The ceiling suddenly collapses, blocking the path forward!",
                    "A massive rockslide thunders down, cutting off your route!",
                    "A deep chasm opens in the floor, making the path impassable!",
                    "Ancient magic flares to life, sealing the passage ahead!",
                    "The floor gives way with a terrible crash, creating an impassable gap!",
                    "A wall of stone slams down, blocking your progress!",
                    "The corridor ahead collapses, leaving no way forward!"
                };
                message = skipMessages[random.Next(skipMessages.Length)];
            }
            else
            {
                damage = Dice.Roll(1, 5);
                var damageMessages = new[]
                {
                    $"A hidden trap springs from the floor! You take {damage} damage!",
                    $"The ground gives way beneath you! You take {damage} damage!",
                    $"Poisonous gas suddenly fills the area! You take {damage} damage!",
                    $"A falling rock crashes down on you! You take {damage} damage!",
                    $"An ancient trap activates with a snap! You take {damage} damage!",
                    $"A spike trap triggers underfoot! You take {damage} damage!",
                    $"Acid sprays from the walls! You take {damage} damage!"
                };
                message = damageMessages[random.Next(damageMessages.Length)];
            }

            return new EnvironmentalHazard
            {
                Message = message,
                SkipToCombat = !skipToSearch,
                SkipToSearch = skipToSearch,
                Damage = damage
            };
        }
    }
}
