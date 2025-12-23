using System;
using System.Collections.Generic;

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
            random = new Random();
            discoveryGenerator = new EnvironmentDiscoveryGenerator();
        }

        /// <summary>
        /// Automatically explores room before combat (dice-based)
        /// Returns exploration outcome based on 1d20 roll
        /// </summary>
        public ExplorationResult ExploreRoom(Environment room, Character player, bool isLastRoom = false)
        {
            int roll = Dice.Roll(20);
            
            // Thresholds:
            // 1-5 (25%): Find Nothing → may trigger surprise if enemies present
            // 6-12 (35%): Discover Environment Info
            // 13-15 (15%): Environmental Hazard (trap/collapse)
            // 16-20 (25%): Spot Enemy Early → player gets first attack
            
            if (roll <= 5)
            {
                // Find Nothing - check for surprise
                bool isSurprised = room.HasLivingEnemies();
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
            else if (roll <= 12)
            {
                // Discover Environment Info
                string discovery = discoveryGenerator.GenerateDiscovery(room);
                return new ExplorationResult
                {
                    Outcome = ExplorationOutcome.DiscoverEnvironment,
                    Roll = roll,
                    Message = discovery,
                    EnvironmentInfo = discovery
                };
            }
            else if (roll <= 15)
            {
                // Environmental Hazard
                var hazard = GenerateEnvironmentalHazard(room);
                return new ExplorationResult
                {
                    Outcome = ExplorationOutcome.EnvironmentalHazard,
                    Roll = roll,
                    Message = hazard.Message,
                    Hazard = hazard
                };
            }
            else
            {
                // Spot Enemy Early - player gets first attack
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

        /// <summary>
        /// Automatically searches room after combat (dice-based)
        /// 90% chance of nothing, 10% chance of loot
        /// Last room has 100% loot drop
        /// </summary>
        public SearchResult SearchRoom(Environment room, Character player, int dungeonLevel, bool isLastRoom = false)
        {
            // Last room: guaranteed loot
            if (isLastRoom)
            {
                Item? loot = LootGenerator.GenerateLoot(player.Level, dungeonLevel, player, guaranteedLoot: true);
                var foundMessages = new[]
                {
                    $"You thoroughly search the area and discover: {loot?.Name ?? "nothing"}",
                    $"After a careful search, you find: {loot?.Name ?? "nothing"}",
                    $"Your search reveals: {loot?.Name ?? "nothing"}",
                    $"Hidden among the debris, you find: {loot?.Name ?? "nothing"}",
                    $"A thorough examination uncovers: {loot?.Name ?? "nothing"}"
                };
                string message = loot != null 
                    ? foundMessages[random.Next(foundMessages.Length)]
                    : "You search thoroughly but find nothing of value.";
                
                return new SearchResult
                {
                    FoundLoot = loot != null,
                    LootItem = loot,
                    Roll = 20, // Roll 20 = guaranteed success
                    Message = message
                };
            }
            
            // Regular rooms: 90% nothing, 10% loot (roll 19-20)
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
            else
            {
                Item? loot = LootGenerator.GenerateLoot(player.Level, dungeonLevel, player, guaranteedLoot: false);
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
        }

        /// <summary>
        /// Generates environmental hazard (trap, collapse, etc.)
        /// </summary>
        private EnvironmentalHazard GenerateEnvironmentalHazard(Environment room)
        {
            int hazardRoll = Dice.Roll(20);
            
            // 50% chance: Skip to combat (hazard damages but combat proceeds)
            // 50% chance: Skip to search (hazard blocks path, no combat)
            bool skipToSearch = hazardRoll > 10;
            
            string message;
            int damage = 0;
            
            if (skipToSearch)
            {
                // Skip to search - block path
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
                // Skip to combat - apply damage
                damage = Dice.Roll(1, 5); // 1-5 damage
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

