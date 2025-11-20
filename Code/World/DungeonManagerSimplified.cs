using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Simplified dungeon manager that delegates specific responsibilities to specialized classes
    /// Replaces the original DungeonManager with cleaner, more maintainable code
    /// </summary>
    public class DungeonManagerSimplified
    {
        private readonly Random random = new Random();
        private readonly DungeonRunner dungeonRunner;
        private readonly RewardManager rewardManager;
        private List<DungeonData>? allDungeons = null;

        public DungeonManagerSimplified()
        {
            this.dungeonRunner = new DungeonRunner();
            this.rewardManager = new RewardManager();
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
        /// Always ensures exactly 3 unique dungeons are selected
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="availableDungeons">List to populate with available dungeons</param>
        public void RegenerateDungeons(Character player, List<Dungeon> availableDungeons)
        {
            availableDungeons.Clear();
            int playerLevel = player.Level;
            
            // Load all dungeons from Dungeons.json
            var allDungeons = LoadAllDungeons();
            
            // Filter dungeons that are appropriate for the player's level
            var suitableDungeons = GetSuitableDungeons(allDungeons, playerLevel);
            
            // If no dungeons are suitable for current level, find the closest ones
            if (suitableDungeons.Count == 0)
            {
                suitableDungeons = GetClosestDungeons(allDungeons, playerLevel);
            }
            
            // Ensure we have at least 3 unique dungeons available
            // Group by name to ensure uniqueness, then shuffle and select exactly 3
            var uniqueDungeons = suitableDungeons
                .GroupBy(d => d.name)
                .Select(g => g.First())
                .OrderBy(x => random.Next())
                .ToList();
            
            // If we have fewer than 3 unique dungeons, pad with additional unique dungeons from the full list
            if (uniqueDungeons.Count < 3)
            {
                var additionalDungeons = allDungeons
                    .Where(d => !uniqueDungeons.Any(ud => ud.name == d.name))
                    .OrderBy(x => random.Next())
                    .Take(3 - uniqueDungeons.Count);
                uniqueDungeons.AddRange(additionalDungeons);
            }
            
            // Take exactly 3 unique dungeons
            var selectedDungeons = uniqueDungeons.Take(3).ToList();
            
            // Create Dungeon objects from the selected DungeonData
            CreateDungeonObjects(selectedDungeons, availableDungeons, playerLevel);
            
            // Sort dungeons by level (lowest to highest) - modify the list in place
            var sortedDungeons = availableDungeons.OrderBy(d => d.MinLevel).ToList();
            availableDungeons.Clear();
            availableDungeons.AddRange(sortedDungeons);
        }

        /// <summary>
        /// Gets dungeons suitable for the player's level
        /// </summary>
        private List<DungeonData> GetSuitableDungeons(List<DungeonData> allDungeons, int playerLevel)
        {
            return allDungeons.Where(d => 
                playerLevel >= d.minLevel && playerLevel <= d.maxLevel
            ).ToList();
        }

        /// <summary>
        /// Gets the closest dungeons when no suitable ones are found
        /// </summary>
        private List<DungeonData> GetClosestDungeons(List<DungeonData> allDungeons, int playerLevel)
        {
            return allDungeons
                .OrderBy(d => Math.Abs(d.minLevel - playerLevel))
                .Take(3)
                .ToList();
        }

        /// <summary>
        /// Creates Dungeon objects from DungeonData
        /// </summary>
        private void CreateDungeonObjects(List<DungeonData> dungeonDataList, List<Dungeon> availableDungeons, int playerLevel)
        {
            foreach (var dungeonData in dungeonDataList)
            {
                // Scale dungeon level based on player level, but keep it within the dungeon's min/max range
                // Calculate scaled level: use player level, but clamp to dungeon's min/max range
                int scaledLevel = Math.Max(dungeonData.minLevel, Math.Min(dungeonData.maxLevel, playerLevel));
                
                availableDungeons.Add(new Dungeon(
                    dungeonData.name, 
                    scaledLevel, 
                    scaledLevel, 
                    dungeonData.theme,
                    dungeonData.possibleEnemies
                ));
            }
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
            rewardManager.AwardLootAndXP(player, inventory, dungeonLevel);
        }
        
        /// <summary>
        /// Awards loot and XP for dungeon completion and returns the rewards
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons to determine dungeon level</param>
        /// <returns>Tuple containing XP gained and loot received</returns>
        public (int xpGained, Item? lootReceived) AwardLootAndXPWithReturns(Character player, List<Item> inventory, List<Dungeon> availableDungeons)
        {
            // Determine current dungeon level
            int dungeonLevel = GetCurrentDungeonLevel(player, availableDungeons);
            
            // Delegate to RewardManager
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

        /// <summary>
        /// Runs a complete dungeon with all its rooms (delegates to DungeonRunner)
        /// </summary>
        /// <param name="selectedDungeon">The dungeon to run</param>
        /// <param name="player">The player character</param>
        /// <param name="combatManager">Combat manager for handling battles</param>
        /// <returns>True if player survived the dungeon, false if player died</returns>
        public async Task<bool> RunDungeon(Dungeon selectedDungeon, Character player, CombatManager combatManager)
        {
            // Delegate to DungeonRunner
            return await dungeonRunner.RunDungeon(selectedDungeon, player, combatManager);
        }
    }
}
