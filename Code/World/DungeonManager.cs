using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    public class DungeonConfig
    {
        public List<string> dungeonThemes { get; set; } = new();
        public List<string> roomThemes { get; set; } = new();
        public DungeonGenerationConfig dungeonGeneration { get; set; } = new();
    }

    public class DungeonData
    {
        public string name { get; set; } = "";
        public string theme { get; set; } = "";
        public int minLevel { get; set; }
        public int maxLevel { get; set; }
        public List<string> possibleEnemies { get; set; } = new();
    }


    /// <summary>
    /// Manages dungeon-related operations including selection, generation, and completion rewards
    /// </summary>
    public class DungeonManager
    {
        private Random random = new Random();
        private List<DungeonData>? allDungeons = null;

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
                allDungeons = new List<DungeonData>
                {
                    new DungeonData { name = "Ancient Forest", theme = "Forest", minLevel = 1, maxLevel = 100, possibleEnemies = new List<string> { "Goblin", "Wolf", "Bear" } },
                    new DungeonData { name = "Lava Caves", theme = "Lava", minLevel = 2, maxLevel = 100, possibleEnemies = new List<string> { "Fire Elemental", "Lava Golem" } },
                    new DungeonData { name = "Haunted Crypt", theme = "Crypt", minLevel = 3, maxLevel = 100, possibleEnemies = new List<string> { "Skeleton", "Zombie", "Wraith" } }
                };
                return allDungeons;
            }
        }

        /// <summary>
        /// Regenerates available dungeons based on player level using actual dungeons from Dungeons.json
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
            var suitableDungeons = allDungeons.Where(d => 
                playerLevel >= d.minLevel && playerLevel <= d.maxLevel
            ).ToList();
            
            // If no dungeons are suitable for current level, find the closest ones
            if (suitableDungeons.Count == 0)
            {
                // Find dungeons with minimum level requirements closest to player level
                suitableDungeons = allDungeons
                    .OrderBy(d => Math.Abs(d.minLevel - playerLevel))
                    .Take(3)
                    .ToList();
            }
            
            // Shuffle and select up to 3 dungeons
            var shuffledDungeons = suitableDungeons.OrderBy(x => random.Next()).Take(3).ToList();
            
            // Create Dungeon objects from the selected DungeonData
            foreach (var dungeonData in shuffledDungeons)
            {
                // Use the actual dungeon level (minLevel) for the dungeon instance
                int dungeonLevel = dungeonData.minLevel;
                availableDungeons.Add(new Dungeon(
                    dungeonData.name, 
                    dungeonLevel, 
                    dungeonLevel, 
                    dungeonData.theme,
                    dungeonData.possibleEnemies
                ));
            }
            
            // Sort dungeons by level (lowest to highest)
            availableDungeons = availableDungeons.OrderBy(d => d.MinLevel).ToList();
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
        /// Awards loot and XP for dungeon completion
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons to determine dungeon level</param>
        public void AwardLootAndXP(Character player, List<Item> inventory, List<Dungeon> availableDungeons)
        {
            UIManager.WriteSystemLine("\nDungeon completed!");
            
            // Heal character back to max health between dungeons
            int effectiveMaxHealth = player.GetEffectiveMaxHealth();
            int healthRestored = effectiveMaxHealth - player.CurrentHealth;
            if (healthRestored > 0)
            {
                player.Heal(healthRestored);
                UIManager.WriteSystemLine($"You have been fully healed! (+{healthRestored} health)");
            }
            
            // Award XP (scaled by dungeon level using tuning config)
            var tuning = GameConfiguration.Instance;
            int xpReward = random.Next(tuning.Progression.EnemyXPBase, tuning.Progression.EnemyXPBase + 50) * player.Level;
            player.AddXP(xpReward);
            UIManager.WriteSystemLine($"Gained {xpReward} XP!");
            UIManager.WriteBlankLine(); // Blank line between XP and loot

            if (player.Level > 1)
            {
                UIManager.WriteLine($"Level up! You are now level {player.Level}");
                UIManager.WriteBlankLine(); // Add line break after level up message
            }
            
            // Determine current dungeon level
            int dungeonLevel = player.Level;
            if (availableDungeons.Count > 0)
            {
                var lastDungeon = availableDungeons.Find(d => d.Rooms.Count > 0);
                if (lastDungeon != null)
                    dungeonLevel = lastDungeon.MinLevel;
            }
            
            // Award guaranteed loot for dungeon completion
            Item? reward = LootGenerator.GenerateLoot(player.Level, dungeonLevel, player, guaranteedLoot: true);
            
            // If still no loot, notify about the issue
            if (reward == null)
            {
                UIManager.WriteLine("⚠️  WARNING: Guaranteed loot generation failed!");
                UIManager.WriteLine("   This indicates a serious issue with the loot generation system.");
                UIManager.WriteLine("   Please report this issue with the following details:");
                UIManager.WriteLine($"   - Player Level: {player.Level}");
                UIManager.WriteLine($"   - Dungeon Level: {dungeonLevel}");
                UIManager.WriteLine($"   - Guaranteed Loot Requested: Yes");
                UIManager.WriteSystemLine("");
                
                // Create a diagnostic fallback weapon to prevent game breaking
                reward = Program.CreateFallbackWeapon(player.Level);
                if (reward == null)
                {
                    // Ultimate fallback if weapon data loading fails
                    reward = new WeaponItem("Basic Sword", player.Level, 5 + player.Level, 1.0, WeaponType.Sword);
                    reward.Rarity = "Common";
                    UIManager.WriteSystemLine("   Created emergency fallback weapon to prevent game breaking.");
                }
                else
                {
                    UIManager.WriteSystemLine($"   Created fallback weapon: {reward.Name} (from weapon database)");
                }
                UIManager.WriteSystemLine("");
            }

            if (reward != null)
            {
                // Add to both inventories
                player.AddToInventory(reward);
                inventory.Add(reward);
                
                // Display "You found:" with half beat delay
                UIManager.WriteSystemLine("You found:");
                // Display item name with 2x beat delay (using title delay which is longer)
                UIManager.WriteTitleLine(FormatItemNameWithRarityInParentheses(reward));
                // Add blank line after item display
                UIManager.WriteSystemLine("");
            }
            else
            {
                // This should never happen with the fallback, but just in case
                UIManager.WriteSystemLine("You found no loot this time.");
            }
        }

        /// <summary>
        /// Runs a complete dungeon with all its rooms
        /// </summary>
        /// <param name="selectedDungeon">The dungeon to run</param>
        /// <param name="player">The player character</param>
        /// <param name="combatManager">Combat manager for handling battles</param>
        /// <returns>True if player survived the dungeon, false if player died</returns>
        public bool RunDungeon(Dungeon selectedDungeon, Character player, CombatManager combatManager)
        {
            UIManager.WriteDungeonLine($"\nEntering {selectedDungeon.Name}...\n");

            // Room Sequence
            foreach (Environment room in selectedDungeon.Rooms)
            {
                UIManager.WriteRoomLine($"Entering room: {room.Name}");
                UIManager.ApplyDelay(UIMessageType.Encounter); // Add configurable delay after encounter message

                UIManager.WriteRoomLine("");
                UIManager.WriteRoomLine(room.Description);
                UIManager.ApplyDelay(UIMessageType.Encounter);
                // Clear all temporary effects when entering a new room
                player.ClearAllTempEffects();

                while (room.HasLivingEnemies())
                {
                    Enemy? currentEnemy = room.GetNextLivingEnemy();
                    if (currentEnemy == null) break;

                    // Display enemy encounter with weapon information
                    string enemyWeaponInfo = "";
                    if (currentEnemy.Weapon != null)
                    {
                        enemyWeaponInfo = $" with {currentEnemy.Weapon.Name}";
                    }
                    UIManager.WriteEnemyLine($"\nEncountered [{currentEnemy.Name}]{enemyWeaponInfo}!");
                    UIManager.ApplyDelay(UIMessageType.Encounter); // Add configurable delay after encounter message
                    UIManager.WriteBlankLine(); // Blank line between "Encountered" and stats
                    UIManager.WriteSystemLine($"Hero Stats - Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}, Armor: {player.GetTotalArmor()}, Attack: STR {player.GetEffectiveStrength()}, AGI {player.GetEffectiveAgility()}, TEC {player.GetEffectiveTechnique()}, INT {player.GetEffectiveIntelligence()}, Attack Time: {player.GetTotalAttackSpeed():F2}s");
                    UIManager.WriteSystemLine($"Enemy Stats - Health: {currentEnemy.CurrentHealth}/{currentEnemy.MaxHealth}, Armor: {currentEnemy.Armor}, Attack: STR {currentEnemy.Strength}, AGI {currentEnemy.Agility}, TEC {currentEnemy.Technique}, INT {currentEnemy.Intelligence}, Attack Time: {currentEnemy.GetTotalAttackSpeed():F2}s");
                    // Turn separator line removed for cleaner combat logs
                    UIManager.ApplyDelay(UIMessageType.Encounter); // Add configurable delay after encounter message

                    // Show action speed info
                    var speedSystem = combatManager.GetCurrentActionSpeedSystem();
                    if (speedSystem != null)
                    {
                        UIManager.WriteMenuLine($"Turn Order: {speedSystem.GetTurnOrderInfo()}");
                    }
                    UIManager.WriteBlankLine(); // Line break between stats and action

                    // Clear all temporary effects before each fight
                    player.ClearAllTempEffects();
                    
                    // Reset Divine reroll charges for new combat
                    player.ResetRerollCharges();
                    
                    // Run combat using CombatManager
                    bool playerSurvived = combatManager.RunCombat(player, currentEnemy, room);
                    
                    if (!playerSurvived)
                    {
                        // Use TextDisplayIntegration for consistent entity tracking
                        TextDisplayIntegration.DisplayCombatAction("\nYou have been defeated!", new List<string>(), new List<string>(), "System");
                        // Delete save file when character dies
                        Character.DeleteSaveFile();
                        return false; // Player died
                    }
                    else
                    {
                        // Use TextDisplayIntegration for consistent entity tracking
                        string defeatMessage = $"[{currentEnemy.Name}] has been defeated!";
                        TextDisplayIntegration.DisplayCombatAction(defeatMessage, new List<string>(), new List<string>(), "System");
                        player.AddXP(currentEnemy.XPReward);
                    }
                    
                    // Display narrative if balance is set to show poetic text
                    var narrativeSettings = GameSettings.Instance;
                    if (narrativeSettings.NarrativeBalance > 0.3)
                    {
                        // Battle narrative completed message removed
                    }
                }
                
                UIManager.WriteRoomClearedLine($"\nRemaining Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}");
                UIManager.WriteRoomClearedLine("\nRoom cleared!");
                UIManager.WriteRoomClearedLine("====================================");
                
                // Reset combo at end of each room
                player.ResetCombo();
            }

            return true; // Player survived the dungeon
        }

        /// <summary>
        /// Formats an item name to display rarity in parentheses instead of as a prefix
        /// </summary>
        /// <param name="item">The item to format</param>
        /// <returns>Formatted item name with rarity in parentheses</returns>
        private static string FormatItemNameWithRarityInParentheses(Item item)
        {
            string name = item.Name;
            
            // Check for rarity prefixes and move them to parentheses
            string[] rarities = { "Legendary", "Epic", "Rare", "Uncommon", "Common" };
            
            foreach (string rarity in rarities)
            {
                if (name.StartsWith(rarity + " "))
                {
                    // Remove the rarity prefix and add it in parentheses
                    string nameWithoutRarity = name.Substring(rarity.Length + 1);
                    return $"({rarity}) {nameWithoutRarity}";
                }
            }
            
            // If no rarity prefix found, return the name as-is
            return name;
        }
    }
}
