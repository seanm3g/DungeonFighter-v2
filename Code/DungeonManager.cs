using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
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
            Console.WriteLine("\nAvailable Dungeons:\n");
            for (int i = 0; i < availableDungeons.Count; i++)
            {
                var d = availableDungeons[i];
                Console.WriteLine($"{i + 1}. {d.Name}");
            }

            int choice = -1;
            while (choice < 1 || choice > availableDungeons.Count)
            {
                Console.Write($"\nChoose a dungeon (1-{availableDungeons.Count}): ");
                string? input = Console.ReadLine();
                if (!int.TryParse(input, out choice) || choice < 1 || choice > availableDungeons.Count)
                {
                    Console.WriteLine("Invalid choice. Please enter a valid dungeon number.");
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
            Console.WriteLine("\nDungeon completed!");
            
            // Heal character back to max health between dungeons
            int effectiveMaxHealth = player.GetEffectiveMaxHealth();
            int healthRestored = effectiveMaxHealth - player.CurrentHealth;
            if (healthRestored > 0)
            {
                player.Heal(healthRestored);
                Console.WriteLine($"You have been fully healed! (+{healthRestored} health)");
            }
            
            // Award XP (scaled by dungeon level using tuning config)
            var tuning = TuningConfig.Instance;
            int xpReward = random.Next(tuning.Progression.EnemyXPBase, tuning.Progression.EnemyXPBase + 50) * player.Level;
            player.AddXP(xpReward);
            Console.WriteLine($"Gained {xpReward} XP!");
            Console.WriteLine(); // Blank line between XP and loot

            if (player.Level > 1)
            {
                Console.WriteLine($"Level up! You are now level {player.Level}");
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
            Item? reward = null;
            int attempts = 0;
            const int maxAttempts = 10; // Prevent infinite loop
            
            // Keep trying until we get loot (guaranteed reward for dungeon completion)
            while (reward == null && attempts < maxAttempts)
            {
                reward = LootGenerator.GenerateLoot(player.Level, dungeonLevel, player);
                attempts++;
            }
            
            // If still no loot after max attempts, notify about the issue
            if (reward == null)
            {
                Console.WriteLine("⚠️  WARNING: Loot generation failed after multiple attempts!");
                Console.WriteLine("   This indicates an issue with the loot generation system.");
                Console.WriteLine("   Please report this issue with the following details:");
                Console.WriteLine($"   - Player Level: {player.Level}");
                Console.WriteLine($"   - Dungeon Level: {dungeonLevel}");
                Console.WriteLine($"   - Attempts Made: {maxAttempts}");
                Console.WriteLine();
                
                // Create a diagnostic fallback weapon to prevent game breaking
                reward = Program.CreateFallbackWeapon(player.Level);
                if (reward == null)
                {
                    // Ultimate fallback if weapon data loading fails
                    reward = new WeaponItem("Basic Sword", player.Level, 5 + player.Level, 1.0, WeaponType.Sword);
                    reward.Rarity = "Common";
                    Console.WriteLine("   Created emergency fallback weapon to prevent game breaking.");
                }
                else
                {
                    Console.WriteLine($"   Created fallback weapon: {reward.Name} (from weapon database)");
                }
                Console.WriteLine();
            }

            if (reward != null)
            {
                // Add to both inventories
                player.AddToInventory(reward);
                inventory.Add(reward);
                Console.WriteLine($"You found: {reward.Name}");
            }
            else
            {
                // This should never happen with the fallback, but just in case
                Console.WriteLine("You found no loot this time.");
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
            Console.WriteLine($"\nEntering {selectedDungeon.Name}...");
            Thread.Sleep(TuningConfig.Instance.UI.DungeonEntryDelay);

            // Room Sequence
            foreach (Environment room in selectedDungeon.Rooms)
            {
                Console.WriteLine($"\nEntering room: {room.Name}");
                Console.WriteLine(room.Description);
                
                // Clear all temporary effects when entering a new room
                player.ClearAllTempEffects();
                
                Thread.Sleep(TuningConfig.Instance.UI.RoomEntryDelay);

                while (room.HasLivingEnemies())
                {
                    Enemy? currentEnemy = room.GetNextLivingEnemy();
                    if (currentEnemy == null) break;

                    Console.WriteLine($"\nEncountered {currentEnemy.Name}!");
                    Thread.Sleep(TuningConfig.Instance.UI.EnemyEncounterDelay);
                    Console.WriteLine(); // Blank line between "Encountered" and stats
                    Console.WriteLine($"Hero Stats - Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}, Armor: {player.GetTotalArmor()}, Attack: STR {player.GetEffectiveStrength()}, AGI {player.GetEffectiveAgility()}, TEC {player.GetEffectiveTechnique()}, INT {player.GetEffectiveIntelligence()}, Attack Time: {player.GetTotalAttackSpeed():F2}s");
                    Console.WriteLine($"Enemy Stats - Health: {currentEnemy.CurrentHealth}/{currentEnemy.MaxHealth}, Armor: {currentEnemy.Armor}, Attack: STR {currentEnemy.Strength}, AGI {currentEnemy.Agility}, TEC {currentEnemy.Technique}, INT {currentEnemy.Intelligence}, Attack Time: {currentEnemy.GetTotalAttackSpeed():F2}s");
                    
                    // Show action speed info
                    var speedSystem = combatManager.GetCurrentActionSpeedSystem();
                    if (speedSystem != null)
                    {
                        Console.WriteLine($"Turn Order: {speedSystem.GetTurnOrderInfo()}");
                    }
                    Console.WriteLine(); // Line break between stats and action

                    // Clear all temporary effects before each fight
                    player.ClearAllTempEffects();
                    
                    // Reset Divine reroll charges for new combat
                    player.ResetRerollCharges();
                    
                    // Run combat using CombatManager
                    bool playerSurvived = combatManager.RunCombat(player, currentEnemy, room);
                    
                    if (!playerSurvived)
                    {
                        CombatLogger.Log("\nYou have been defeated!");
                        // Delete save file when character dies
                        Character.DeleteSaveFile();
                        return false; // Player died
                    }
                    else
                    {
                        CombatLogger.Log($"\n{currentEnemy.Name} has been defeated!");
                        player.AddXP(currentEnemy.XPReward);
                    }
                    
                    // Display narrative if balance is set to show poetic text
                    var narrativeSettings = GameSettings.Instance;
                    if (narrativeSettings.NarrativeBalance > 0.3)
                    {
                        Console.WriteLine("\nBattle narrative completed.");
                    }
                }

                Console.WriteLine(); // Add blank line before room cleared message
                Console.WriteLine($"Remaining Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}");
                Console.WriteLine("Room cleared!");
                Thread.Sleep(TuningConfig.Instance.UI.RoomClearedDelay);
                
                // Reset combo at end of each room
                player.ResetCombo();
            }

            return true; // Player survived the dungeon
        }
    }
}
