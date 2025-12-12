using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Simplified dungeon manager using environmental effect registry
    /// Replaces the original DungeonManager with cleaner, more maintainable code
    /// </summary>
    public class DungeonManagerWithRegistry
    {
        private readonly Random random = new Random();
        private readonly EnvironmentalEffectRegistry environmentalRegistry;
        private List<DungeonData>? allDungeons = null;

        public DungeonManagerWithRegistry()
        {
            this.environmentalRegistry = new EnvironmentalEffectRegistry();
        }

        /// <summary>
        /// Loads all dungeons from Dungeons.json file
        /// </summary>
        /// <returns>List of all available dungeons</returns>
        private List<DungeonData> LoadAllDungeons()
        {
            if (allDungeons != null) return allDungeons;

            try
            {
                // Use FileManager to get the correct path (handles multiple possible locations)
                string jsonPath = FileManager.GetGameDataFilePath("Dungeons.json");
                string jsonContent = File.ReadAllText(jsonPath);
                allDungeons = JsonSerializer.Deserialize<List<DungeonData>>(jsonContent) ?? new List<DungeonData>();
                return allDungeons;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dungeons: {ex.Message}");
                // Return fallback dungeons if loading fails
                allDungeons = CreateFallbackDungeons();
                return allDungeons;
            }
        }

        /// <summary>
        /// Creates fallback dungeons when loading fails
        /// </summary>
        private List<DungeonData> CreateFallbackDungeons()
        {
            return new List<DungeonData>
            {
                new DungeonData { name = "Ancient Forest", theme = "Forest", minLevel = 1, maxLevel = 100, possibleEnemies = new List<string> { "Goblin", "Wolf", "Bear" } },
                new DungeonData { name = "Lava Caves", theme = "Lava", minLevel = 2, maxLevel = 100, possibleEnemies = new List<string> { "Fire Elemental", "Lava Golem" } },
                new DungeonData { name = "Haunted Crypt", theme = "Crypt", minLevel = 3, maxLevel = 100, possibleEnemies = new List<string> { "Skeleton", "Zombie", "Wraith" } }
            };
        }

        /// <summary>
        /// Regenerates available dungeons based on player level using actual dungeons from Dungeons.json
        /// Randomly selects 3 unique dungeons from all 25 available dungeons
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="availableDungeons">List to populate with available dungeons</param>
        public void RegenerateDungeons(Character player, List<Dungeon> availableDungeons)
        {
            availableDungeons.Clear();
            int playerLevel = player.Level;
            
            // Load all dungeons from Dungeons.json
            var allDungeons = LoadAllDungeons();
            
            // Randomly select 3 unique dungeons from the full list
            var selectedDungeons = allDungeons
                .OrderBy(x => random.Next())
                .Take(3)
                .ToList();
            
            // Create Dungeon objects with appropriate level scaling
            // First dungeon: player level - 1 (easier)
            // Second dungeon: player level (current difficulty)
            // Third dungeon: player level + 1 (harder)
            for (int i = 0; i < selectedDungeons.Count; i++)
            {
                var dungeonData = selectedDungeons[i];
                int dungeonLevel = Math.Max(1, playerLevel + (i - 1)); // -1, 0, +1
                
                availableDungeons.Add(new Dungeon(
                    dungeonData.name,
                    dungeonLevel,
                    dungeonLevel,
                    dungeonData.theme,
                    dungeonData.possibleEnemies,
                    dungeonData.colorOverride
                ));
            }
            
            // Sort dungeons by level (lowest to highest)
            var sortedDungeons = availableDungeons.OrderBy(d => d.MinLevel).ToList();
            availableDungeons.Clear();
            availableDungeons.AddRange(sortedDungeons);
        }


        /// <summary>
        /// Allows player to choose a dungeon from available options
        /// </summary>
        /// <param name="availableDungeons">List of available dungeons</param>
        /// <returns>The selected dungeon</returns>
        public Dungeon ChooseDungeon(List<Dungeon> availableDungeons)
        {
            UIManager.WriteMenuLine("\nAvailable Dungeons:");
            UIManager.WriteMenuLine("--------------------------");
            for (int i = 0; i < availableDungeons.Count; i++)
            {
                var d = availableDungeons[i];
                UIManager.WriteMenuLine($"{i + 1}. {d.Name} (lvl {d.MinLevel})");
            }

            int choice = -1;
            while (choice < 1 || choice > availableDungeons.Count)
            {
                Console.Write($"\nChoose a dungeon (1-{availableDungeons.Count}): ");
                string? input = Console.ReadLine();
                if (!int.TryParse(input, out choice) || choice < 1 || choice > availableDungeons.Count)
                {
                    UIManager.WriteLine("Invalid choice. Please enter a valid dungeon number.");
                }
            }
            return availableDungeons[choice - 1];
        }

        /// <summary>
        /// Awards loot and XP for dungeon completion (delegates to RewardManager)
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons to determine dungeon level</param>
        public void AwardLootAndXP(Character player, List<Item> inventory, List<Dungeon> availableDungeons)
        {
            // Determine current dungeon level
            int dungeonLevel = GetCurrentDungeonLevel(player, availableDungeons);
            
            // Delegate to RewardManager
            var rewardManager = new RewardManager();
            rewardManager.AwardLootAndXP(player, inventory, dungeonLevel);
        }
        
        /// <summary>
        /// Awards loot and XP for dungeon completion and returns the rewards
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons to determine dungeon level</param>
        /// <returns>Tuple containing XP gained, loot received, and level-up information</returns>
        public (int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos) AwardLootAndXPWithReturns(Character player, List<Item> inventory, List<Dungeon> availableDungeons)
        {
            // Determine current dungeon level
            int dungeonLevel = GetCurrentDungeonLevel(player, availableDungeons);
            
            // Delegate to RewardManager
            var rewardManager = new RewardManager();
            return rewardManager.AwardLootAndXPWithReturns(player, inventory, dungeonLevel);
        }

        /// <summary>
        /// Gets the current dungeon level
        /// </summary>
        private int GetCurrentDungeonLevel(Character player, List<Dungeon> availableDungeons)
        {
            int dungeonLevel = player.Level;
            if (availableDungeons.Count > 0)
            {
                var lastDungeon = availableDungeons.Find(d => d.Rooms.Count > 0);
                if (lastDungeon != null)
                    dungeonLevel = lastDungeon.MinLevel;
            }
            return dungeonLevel;
        }

        // NOTE: RunDungeon method removed - use DungeonRunnerManager.RunDungeon() instead
        // This method was legacy code that used the old DungeonRunner class with chunked text methods

        /// <summary>
        /// Applies environmental debuffs to entities using the effect registry
        /// </summary>
        /// <param name="source">The source of the debuff (usually environment)</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action causing the debuff</param>
        /// <param name="debuffType">Type of debuff to apply</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if debuff was applied</returns>
        public bool ApplyEnvironmentalDebuff(Actor source, Actor target, Action action, string debuffType, List<string> results)
        {
            // Use the environmental effect registry to apply effects
            return environmentalRegistry.ApplyEnvironmentalEffect(debuffType, target, action, results);
        }
    }
}


