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
                string jsonPath = Path.Combine("..", "GameData", "Dungeons.json");
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
        /// Shows exactly 3 dungeons: one at player level -1, one at player level, and one at player level +1
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
            
            // Create exactly 3 dungeons: one for each level (below, current, above)
            CreateDungeonObjects(suitableDungeons, availableDungeons, playerLevel);
            
            // Sort dungeons by level (lowest to highest) - modify the list in place
            var sortedDungeons = availableDungeons.OrderBy(d => d.MinLevel).ToList();
            availableDungeons.Clear();
            availableDungeons.AddRange(sortedDungeons);
        }

        /// <summary>
        /// Gets dungeons suitable for the player's level
        /// Shows dungeons at player level -1, player level, and player level +1
        /// </summary>
        private List<DungeonData> GetSuitableDungeons(List<DungeonData> allDungeons, int playerLevel)
        {
            // Calculate the three levels we want to show: player level -1, player level, player level +1
            int levelBelow = Math.Max(1, playerLevel - 1); // Ensure we don't go below level 1
            int currentLevel = playerLevel;
            int levelAbove = playerLevel + 1;
            
            return allDungeons.Where(d => 
                (levelBelow >= d.minLevel && levelBelow <= d.maxLevel) ||
                (currentLevel >= d.minLevel && currentLevel <= d.maxLevel) ||
                (levelAbove >= d.minLevel && levelAbove <= d.maxLevel)
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
        /// Creates dungeons at player level -1, player level, and player level +1
        /// </summary>
        private void CreateDungeonObjects(List<DungeonData> dungeonDataList, List<Dungeon> availableDungeons, int playerLevel)
        {
            // Calculate the three levels we want to show: player level -1, player level, player level +1
            int levelBelow = Math.Max(1, playerLevel - 1); // Ensure we don't go below level 1
            int currentLevel = playerLevel;
            int levelAbove = playerLevel + 1;
            
            // Group dungeons by which level they support
            var dungeonsForLevelBelow = dungeonDataList.Where(d => levelBelow >= d.minLevel && levelBelow <= d.maxLevel).ToList();
            var dungeonsForCurrentLevel = dungeonDataList.Where(d => currentLevel >= d.minLevel && currentLevel <= d.maxLevel).ToList();
            var dungeonsForLevelAbove = dungeonDataList.Where(d => levelAbove >= d.minLevel && levelAbove <= d.maxLevel).ToList();
            
            // Create one dungeon for each level (below, current, above)
            CreateDungeonForLevel(dungeonsForLevelBelow, levelBelow, availableDungeons);
            CreateDungeonForLevel(dungeonsForCurrentLevel, currentLevel, availableDungeons);
            CreateDungeonForLevel(dungeonsForLevelAbove, levelAbove, availableDungeons);
        }
        
        /// <summary>
        /// Creates a dungeon for a specific level from available dungeon data
        /// </summary>
        private void CreateDungeonForLevel(List<DungeonData> dungeonDataList, int targetLevel, List<Dungeon> availableDungeons)
        {
            if (dungeonDataList.Count == 0) return;
            
            // Select a random dungeon from the available ones for this level
            var selectedDungeon = dungeonDataList[random.Next(dungeonDataList.Count)];
            
            availableDungeons.Add(new Dungeon(
                selectedDungeon.name, 
                targetLevel, 
                targetLevel, 
                selectedDungeon.theme,
                selectedDungeon.possibleEnemies
            ));
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
        public bool RunDungeon(Dungeon selectedDungeon, Character player, CombatManager combatManager)
        {
            // Delegate to DungeonRunner
            var dungeonRunner = new DungeonRunner();
            return dungeonRunner.RunDungeon(selectedDungeon, player, combatManager);
        }

        /// <summary>
        /// Applies environmental debuffs to entities using the effect registry
        /// </summary>
        /// <param name="source">The source of the debuff (usually environment)</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action causing the debuff</param>
        /// <param name="debuffType">Type of debuff to apply</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if debuff was applied</returns>
        public bool ApplyEnvironmentalDebuff(Entity source, Entity target, Action action, string debuffType, List<string> results)
        {
            // Use the environmental effect registry to apply effects
            return environmentalRegistry.ApplyEnvironmentalEffect(debuffType, target, action, results);
        }
    }
}
