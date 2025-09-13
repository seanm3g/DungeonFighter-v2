namespace RPGGame
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class Game
    {
        private Character player;
        private List<Item> inventory;
        private List<Dungeon> availableDungeons;
        private Random random;

        public Game()
        {
            random = new Random();
            var settings = GameSettings.Instance;
            
            // Apply player multipliers
            player = new Character("Hero", 1);
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                player.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            inventory = new List<Item>();
            availableDungeons = new List<Dungeon>();
            InitializeGame();
        }

        public Game(Character existingCharacter)
        {
            random = new Random();
            var settings = GameSettings.Instance;
            
            // Use existing character
            player = existingCharacter;
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                player.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            inventory = new List<Item>();
            availableDungeons = new List<Dungeon>();
            InitializeGameForExistingCharacter();
        }

        private void InitializeGame()
        {
            // Prompt player to choose a starter weapon
            Console.WriteLine();
            Console.WriteLine("Choose your starter weapon:");
            Console.WriteLine("1. Sword");
            Console.WriteLine("2. Mace");
            Console.WriteLine("3. Dagger");
            Console.WriteLine("4. Wand");
            Console.WriteLine();
            int weaponChoice = 1;
            while (true)
            {
                Console.Write("Choose an option: ");
                if (int.TryParse(Console.ReadLine(), out weaponChoice) && weaponChoice >= 1 && weaponChoice <= 4)
                    break;
                Console.WriteLine("Invalid choice.");
            }
            WeaponItem starterWeapon;
            switch (weaponChoice)
            {
                case 1:
                    starterWeapon = new WeaponItem("Starter Sword", 1, 5, 2.0, WeaponType.Sword);
                    break;
                case 2:
                    starterWeapon = new WeaponItem("Starter Mace", 1, 7, 3.0, WeaponType.Mace);
                    break;
                case 3:
                    starterWeapon = new WeaponItem("Starter Dagger", 1, 3, 1.0, WeaponType.Dagger);
                    break;
                case 4:
                    starterWeapon = new WeaponItem("Starter Wand", 1, 4, 1.5, WeaponType.Wand);
                    break;
                default:
                    starterWeapon = new WeaponItem("Starter Sword", 1, 5, 2.0, WeaponType.Sword);
                    break;
            }
            var leatherHelmet = new HeadItem("Starter Leather Helmet", 1, 2);
            var leatherArmor = new ChestItem("Starter Leather Armor", 1, 4);
            var leatherBoots = new FeetItem("Starter Leather Boots", 1, 1);
            player.EquipItem(starterWeapon, "weapon");
            player.EquipItem(leatherHelmet, "head");
            player.EquipItem(leatherArmor, "body");
            player.EquipItem(leatherBoots, "feet");

            // Themed dungeons
            availableDungeons.Clear();
            int playerLevel = player.Level;
            int[] dungeonLevels = new int[] { Math.Max(1, playerLevel - 1), playerLevel, playerLevel + 1 };
            string[] themes = new[] { "Forest", "Lava", "Crypt", "Cavern", "Swamp", "Desert", "Ice", "Ruins", "Castle", "Graveyard" };
            
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

        private void InitializeGameForExistingCharacter()
        {
            // For existing characters, just set up dungeons without weapon selection
            availableDungeons.Clear();
            int playerLevel = player.Level;
            int[] dungeonLevels = new int[] { Math.Max(1, playerLevel - 1), playerLevel, playerLevel + 1 };
            string[] themes = new[] { "Forest", "Lava", "Crypt", "Cavern", "Swamp", "Desert", "Ice", "Ruins", "Castle", "Graveyard" };
            
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

        public void Run()
        {
            Console.WriteLine("\nWelcome to the Dungeon Crawler!\n");
            Console.WriteLine($"Player: {player.Name} (Level {player.Level})");

            while (true)
            {
                // Ask if player wants to manage gear first
                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine();
                Console.WriteLine("1. Choose a Dungeon");
                Console.WriteLine("2. Inventory");
                Console.WriteLine("3. Exit Game and save\n");
                Console.Write("Enter your choice: ");

                if (int.TryParse(Console.ReadLine(), out int initialChoice))
                {
                    switch (initialChoice)
                    {
                        case 1:
                            // Go straight to dungeon selection
                            break;
                        case 2:
                            var inventoryManager = new Inventory(player, inventory);
                            bool continueToDungeon = inventoryManager.ShowGearMenu();
                            if (!continueToDungeon)
                            {
                                continue; // Return to main menu instead of going to dungeon
                            }
                            // Fall through to dungeon selection
                            break;
                        case 3:
                            Console.WriteLine("Saving game before exit...");
                            player.SaveCharacter();
                            Console.WriteLine("Game saved! Thanks for playing!");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                            continue; // Continue the loop instead of exiting
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a number (1, 2, or 3).");
                    continue; // Continue the loop instead of exiting
                }

                // Always regenerate dungeons based on current player level
                availableDungeons.Clear();
                int playerLevel = player.Level;
                int[] dungeonLevels = new int[] { Math.Max(1, playerLevel - 1), playerLevel, playerLevel + 1 };
                string[] themes = new[] { "Forest", "Lava", "Crypt", "Cavern", "Swamp", "Desert", "Ice", "Ruins", "Castle", "Graveyard" };
                
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

                // Dungeon Selection and Run
                Dungeon selectedDungeon = ChooseDungeon();
                selectedDungeon.Generate();

                Console.WriteLine($"\nEntering {selectedDungeon.Name}...");

                // Room Sequence
                foreach (Environment room in selectedDungeon.Rooms)
                {
                    Console.WriteLine($"\nEntering room: {room.Name}");
                    Console.WriteLine(room.Description);

                    while (room.HasLivingEnemies())
                    {
                        Enemy? currentEnemy = room.GetNextLivingEnemy();
                        if (currentEnemy == null) break;

                        Console.WriteLine($"\nEncountered {currentEnemy.Name}!");
                        Console.WriteLine(); // Blank line between "Encountered" and stats
                        Console.WriteLine($"Hero Stats - Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}, Armor: {player.GetTotalArmor()}, Attack: STR {player.GetEffectiveStrength()}, AGI {player.GetEffectiveAgility()}, TEC {player.GetEffectiveTechnique()}, INT {player.GetEffectiveIntelligence()}");
                        Console.WriteLine($"Enemy Stats - Health: {currentEnemy.CurrentHealth}/{currentEnemy.MaxHealth}, Armor: {currentEnemy.Armor}, Attack: STR {currentEnemy.Strength}, AGI {currentEnemy.Agility}, TEC {currentEnemy.Technique}, INT {currentEnemy.Intelligence}");
                        Console.WriteLine(); // Line break between stats and action

                        // Start battle narrative
                        Combat.StartBattleNarrative(player.Name, currentEnemy.Name, room.Name, player.CurrentHealth, currentEnemy.CurrentHealth);

                        // Combat Loop with intelligent delay system
                        var settings = GameSettings.Instance;
                        
                        while (player.IsAlive && currentEnemy.IsAlive)
                        {
                            // Player acts
                            if (player.IsAlive)
                            {
                                // Always recalculate comboActions and actionIdx after any combo reset
                                var comboActions = player.GetComboActions();
                                int actionIdx = 0; // Always start at 0 after a reset
                                if (comboActions.Count > 0)
                                    actionIdx = player.ComboStep % comboActions.Count;
                                // The action that will be attempted this turn
                                var attemptedAction = comboActions.Count > 0 ? comboActions[actionIdx] : null;

                                // Execute multiple attacks based on attack speed
                                string result = Combat.ExecuteMultiAttack(player, currentEnemy, room);
                                bool textDisplayed = !string.IsNullOrEmpty(result);
                                
                                // Show individual action messages
                                if (textDisplayed)
                                {
                                    Console.WriteLine(result);
                                }
                                
                                // Apply delay only if text was displayed
                                double lastActionLength = attemptedAction != null ? attemptedAction.Length : 1.0;
                                Combat.ApplyTextDisplayDelay(lastActionLength, textDisplayed);
                                
                                if (!currentEnemy.IsAlive)
                                {
                                    break;
                                }
                            }
                            
                            // Enemy acts
                            if (currentEnemy.IsAlive)
                            {
                                // Use the enemy's multi-attack system
                                var (result, success) = currentEnemy.AttemptMultiAction(player, room);
                                bool textDisplayed = !string.IsNullOrEmpty(result);
                                
                                // Show individual action messages
                                if (textDisplayed)
                                {
                                    Console.WriteLine(result);
                                }
                                
                                // Apply delay only if text was displayed
                                var enemyAction = currentEnemy.SelectAction();
                                double enemyActionLength = enemyAction != null ? enemyAction.Length : 1.0;
                                Combat.ApplyTextDisplayDelay(enemyActionLength, textDisplayed);
                                
                                if (!player.IsAlive)
                                {
                                    break;
                                }
                            }
                            
                            // Environment acts
                            if (room.IsHostile && room.ActionPool.Count > 0 && room.ShouldEnvironmentAct())
                            {
                                var envAction = room.ActionPool[0].action;
                                
                                // Create list of all characters in the room (player and current enemy)
                                var allTargets = new List<Entity> { player, currentEnemy };
                                
                                // Use area of effect action to target all characters
                                string result = Combat.ExecuteAreaOfEffectAction(room, allTargets, room);
                                bool textDisplayed = !string.IsNullOrEmpty(result);
                                
                                // Show individual action messages
                                if (textDisplayed)
                                {
                                    Console.WriteLine(result);
                                }
                                
                                // Apply delay only if text was displayed
                                Combat.ApplyTextDisplayDelay(envAction.Length, textDisplayed);
                                
                                if (!player.IsAlive)
                                {
                                    break;
                                }
                            }
                        }

                        // End the battle narrative and display it if narrative balance is high enough
                        Combat.EndBattleNarrative();
                        var narrativeSettings = GameSettings.Instance;
                        
                        if (currentEnemy.IsAlive)
                        {
                            Console.WriteLine("You have been defeated!");
                            // Delete save file when character dies
                            Character.DeleteSaveFile();
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"{currentEnemy.Name} has been defeated!");
                            player.AddXP(currentEnemy.XPReward);
                        }
                        
                        // Display narrative if balance is set to show poetic text
                        if (narrativeSettings.NarrativeBalance > 0.3)
                        {
                            Console.WriteLine("\nBattle narrative completed.");
                        }
                    }

                    Console.WriteLine("Room cleared!");
                    Console.WriteLine($"Remaining Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}");
                    
                    // Reset combo at end of each room
                    player.ResetCombo();
                }

                // Dungeon Completion
                AwardLootAndXP();
            }
        }

        private Dungeon ChooseDungeon()
        {
            Console.WriteLine("\nAvailable Dungeons:\n");
            for (int i = 0; i < availableDungeons.Count; i++)
            {
                var d = availableDungeons[i];
                Console.WriteLine($"{i + 1}. {d.Name} - Theme: {d.Theme}");
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

        private void AwardLootAndXP()
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
                reward = LootGenerator.GenerateLoot(player.Level, dungeonLevel);
                attempts++;
            }
            
            // If still no loot after max attempts, create a basic fallback item
            if (reward == null)
            {
                // Create a basic weapon as guaranteed fallback
                reward = new WeaponItem("Basic Sword", player.Level, 5 + player.Level, 1.0, WeaponType.Sword);
                reward.Rarity = "Common";
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

            // Award XP (scaled by dungeon level using tuning config)
            var tuning = TuningConfig.Instance;
            int xpReward = random.Next(tuning.Progression.EnemyXPBase, tuning.Progression.EnemyXPBase + 50) * player.Level;
            player.AddXP(xpReward);
            Console.WriteLine($"Gained {xpReward} XP!");

            if (player.Level > 1)
            {
                Console.WriteLine($"Level up! You are now level {player.Level}");
            }
        }
    }

    public class Dungeon
    {
        public string Name { get; private set; }
        public int MinLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public string Theme { get; private set; }
        public List<Environment> Rooms { get; private set; }
        private Random random;

        public Dungeon(string name, int minLevel, int maxLevel, string theme)
        {
            random = new Random();
            Name = name;
            MinLevel = minLevel;
            MaxLevel = maxLevel;
            Theme = theme;
            Rooms = new List<Environment>();
        }

        public void Generate()
        {
            int roomCount = Math.Max(1, (int)Math.Ceiling(MinLevel / 2.0) + 0); // 1 room at level 1, scales up
            Rooms.Clear();

            for (int i = 0; i < roomCount; i++)
            {
                // Determine room type and difficulty
                bool isHostile = random.NextDouble() < 0.8; // 80% chance of hostile room
                int roomLevel = random.Next(MinLevel, MaxLevel + 1);

                // Create room with appropriate theme
                string roomTheme = GetRoomTheme(i, roomCount);
                string? desc = GetRoomDescription(roomTheme, isHostile);
                string roomDesc = desc ?? "A mysterious room with an unknown purpose.";
                var room = new Environment(
                    name: $"{roomTheme} Room",
                    description: roomDesc,
                    isHostile: isHostile,
                    theme: Theme
                );

                // Generate enemies with scaled levels
                room.GenerateEnemies(roomLevel);
                Rooms.Add(room);
            }
        }

        private string GetRoomTheme(int roomIndex, int totalRooms)
        {
            if (roomIndex == 0) return "Entrance";
            if (roomIndex == totalRooms - 1) return "Boss";

            string[] themes = new[]
            {
                "Treasure", "Guard", "Trap", "Puzzle", "Rest",
                "Storage", "Library", "Armory", "Kitchen", "Dining"
            };

            return themes[random.Next(themes.Length)];
        }

        private string? GetRoomDescription(string theme, bool isHostile)
        {
            return theme switch
            {
                "Entrance" => "A large, imposing entrance to the dungeon. The air is thick with anticipation.",
                "Boss" => "A grand chamber, clearly the domain of a powerful being. The walls are adorned with trophies of past conquests.",
                "Treasure" => "A room filled with glittering gold and precious artifacts. But is it too good to be true?",
                "Guard" => "A well-fortified position, manned by vigilant guards.",
                "Trap" => "The floor is suspiciously clean, and the walls have strange markings.",
                "Puzzle" => "Ancient mechanisms and cryptic symbols cover the walls.",
                "Rest" => "A surprisingly peaceful area, with comfortable furnishings.",
                "Storage" => "Shelves and crates line the walls, filled with supplies.",
                "Library" => "Rows of ancient tomes and scrolls fill this room.",
                "Armory" => "Weapons and armor are displayed on racks and stands.",
                "Kitchen" => "A large cooking area, with pots and pans hanging from the ceiling.",
                "Dining" => "A long table set for a feast, though the food looks... questionable.",
                _ => null
            } + (isHostile ? " Danger lurks in the shadows." : " It seems safe... for now.");
        }
    }
} 