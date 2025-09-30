using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public class DungeonConfig
    {
        public List<string> dungeonThemes { get; set; } = new();
        public List<string> roomThemes { get; set; } = new();
        public DungeonGenerationConfig dungeonGeneration { get; set; } = new();
    }

    public class DungeonGenerationConfig
    {
        public int minRooms { get; set; } = 2;
        public double roomCountScaling { get; set; } = 0.5;
        public double hostileRoomChance { get; set; } = 0.8;
        public string bossRoomName { get; set; } = "Boss";
        public string DefaultTheme { get; set; } = "Forest";
        public string DefaultRoomType { get; set; } = "Chamber";
        public int DefaultDungeonLevels { get; set; } = 5;
        public int DefaultRoomCount { get; set; } = 3;
        public List<string> EquipmentSlots { get; set; } = new() { "Head", "Chest", "Feet", "Weapon" };
        public List<string> StatusEffectTypes { get; set; } = new() { "bleed", "poison", "burn", "slow", "weaken", "stun" };
        public string DefaultCharacterName { get; set; } = "Unknown";
        public string Description { get; set; } = "Dungeon generation defaults and configuration";
    }

    /// <summary>
    /// Manages dungeon-related operations including selection, generation, and completion rewards
    /// </summary>
    public class DungeonManager
    {
        private Random random = new Random();

        /// <summary>
        /// Regenerates available dungeons based on player level
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="availableDungeons">List to populate with available dungeons</param>
        public void RegenerateDungeons(Character player, List<Dungeon> availableDungeons)
        {
            // Always regenerate dungeons based on current player level
            availableDungeons.Clear();
            int playerLevel = player.Level;
            int[] dungeonLevels = new int[] { Math.Max(1, playerLevel - 1), playerLevel, playerLevel + 1 };
            
            // Load dungeon themes from config
            var dungeonConfig = Game.LoadDungeonConfig();
            var themes = dungeonConfig.dungeonThemes.ToArray();
            
            // Shuffle themes to ensure no repeats
            var shuffledThemes = themes.OrderBy(x => random.Next()).ToArray();
            
            // Create unique dungeon combinations with proper theme selection
            var usedThemes = new HashSet<string>();
            int dungeonCount = 0;
            int themeIndex = 0;
            
            // Sort dungeon levels to ensure proper ordering
            Array.Sort(dungeonLevels);
            
            while (dungeonCount < 3 && themeIndex < shuffledThemes.Length)
            {
                string currentTheme = shuffledThemes[themeIndex];
                if (!usedThemes.Contains(currentTheme))
                {
                    usedThemes.Add(currentTheme);
                    int level = dungeonLevels[dungeonCount % dungeonLevels.Length];
                    string themedName = $"{currentTheme} Dungeon (Level {level})";
                    availableDungeons.Add(new Dungeon(themedName, level, level, currentTheme));
                    dungeonCount++;
                }
                themeIndex++;
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
            UIManager.WriteMenuLine("");
            for (int i = 0; i < availableDungeons.Count; i++)
            {
                var d = availableDungeons[i];
                UIManager.WriteMenuLine($"{i + 1}. {d.Name}");
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
            UIManager.WriteLine("\nDungeon completed!");
            
            // Heal character back to max health between dungeons
            int effectiveMaxHealth = player.GetEffectiveMaxHealth();
            int healthRestored = effectiveMaxHealth - player.CurrentHealth;
            if (healthRestored > 0)
            {
                player.Heal(healthRestored);
                UIManager.WriteLine($"You have been fully healed! (+{healthRestored} health)");
            }
            
            // Award XP (scaled by dungeon level using tuning config)
            var tuning = GameConfiguration.Instance;
            int xpReward = random.Next(tuning.Progression.EnemyXPBase, tuning.Progression.EnemyXPBase + 50) * player.Level;
            player.AddXP(xpReward);
            UIManager.WriteLine($"Gained {xpReward} XP!");
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
                UIManager.WriteSystemLine($"You found: {reward.Name}");
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
            UIManager.WriteDungeonLine($"\nEntering {selectedDungeon.Name}...");

            // Room Sequence
            foreach (Environment room in selectedDungeon.Rooms)
            {
                UIManager.WriteRoomLine($"\nEntering room: {room.Name}");
                UIManager.WriteRoomLine("");
                UIManager.WriteRoomLine(room.Description);
                
                // Clear all temporary effects when entering a new room
                player.ClearAllTempEffects();

                while (room.HasLivingEnemies())
                {
                    Enemy? currentEnemy = room.GetNextLivingEnemy();
                    if (currentEnemy == null) break;

                    UIManager.WriteEnemyLine($"\nEncountered [{currentEnemy.Name}]!");
                    UIManager.WriteBlankLine(); // Blank line between "Encountered" and stats
                    UIManager.WriteMenuLine($"Hero Stats - Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}, Armor: {player.GetTotalArmor()}, Attack: STR {player.GetEffectiveStrength()}, AGI {player.GetEffectiveAgility()}, TEC {player.GetEffectiveTechnique()}, INT {player.GetEffectiveIntelligence()}, Attack Time: {player.GetTotalAttackSpeed():F2}s");
                    UIManager.WriteMenuLine($"Enemy Stats - Health: {currentEnemy.CurrentHealth}/{currentEnemy.MaxHealth}, Armor: {currentEnemy.Armor}, Attack: STR {currentEnemy.Strength}, AGI {currentEnemy.Agility}, TEC {currentEnemy.Technique}, INT {currentEnemy.Intelligence}, Attack Time: {currentEnemy.GetTotalAttackSpeed():F2}s");
                    
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
                        UIManager.WriteCombatLine("\nYou have been defeated!");
                        // Delete save file when character dies
                        Character.DeleteSaveFile();
                        return false; // Player died
                    }
                    else
                    {
                        UIManager.WriteCombatLine($"\n[{currentEnemy.Name}] has been defeated!");
                        player.AddXP(currentEnemy.XPReward);
                    }
                    
                    // Display narrative if balance is set to show poetic text
                    var narrativeSettings = GameSettings.Instance;
                    if (narrativeSettings.NarrativeBalance > 0.3)
                    {
                        // Battle narrative completed message removed
                    }
                }

                UIManager.WriteBlankLine(); // Add blank line before room cleared message
                UIManager.WriteRoomClearedLine($"Remaining Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}");
                UIManager.WriteRoomClearedLine("Room cleared!");
                
                // Reset combo at end of each room
                player.ResetCombo();
            }

            return true; // Player survived the dungeon
        }
    }
}
