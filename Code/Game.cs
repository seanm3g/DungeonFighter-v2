namespace RPGGame
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Text.Json;

    public class StartingGearData
    {
        public List<StartingWeapon> weapons { get; set; } = new();
        public List<StartingArmor> armor { get; set; } = new();
    }

    public class StartingWeapon
    {
        public string name { get; set; } = "";
        public int damage { get; set; }
        public double weight { get; set; }
        public double attackSpeed { get; set; } = 0.05;
    }

    public class StartingArmor
    {
        public string slot { get; set; } = "";
        public string name { get; set; } = "";
        public int armor { get; set; }
        public double weight { get; set; }
    }

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
            
            // Create a new character (for "New Game")
            player = new Character(null, 1); // null will trigger random name generation
            
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                player.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            inventory = new List<Item>();
            availableDungeons = new List<Dungeon>();
            
            // Start the game ticker
            GameTicker.Instance.Start();
            
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
            
            // Start the game ticker
            GameTicker.Instance.Start();
            
            InitializeGameForExistingCharacter();
        }

        private StartingGearData LoadStartingGear()
        {
            try
            {
                string jsonPath = Path.Combine("..", "GameData", "StartingGear.json");
                string jsonContent = File.ReadAllText(jsonPath);
                var startingGear = JsonSerializer.Deserialize<StartingGearData>(jsonContent) ?? new StartingGearData();
                
                // Apply weapon scaling from tuning config
                var weaponScaling = TuningConfig.Instance.WeaponScaling;
                if (weaponScaling != null)
                {
                    foreach (var weapon in startingGear.weapons)
                    {
                        // Apply global damage multiplier
                        weapon.damage = (int)(weapon.damage * weaponScaling.GlobalDamageMultiplier);
                        
                        // Apply weapon-specific starting damage from tuning config
                        weapon.damage = weapon.name.ToLower() switch
                        {
                            var name when name.Contains("mace") => weaponScaling.StartingWeaponDamage.Mace,
                            var name when name.Contains("sword") => weaponScaling.StartingWeaponDamage.Sword,
                            var name when name.Contains("dagger") => weaponScaling.StartingWeaponDamage.Dagger,
                            var name when name.Contains("wand") => weaponScaling.StartingWeaponDamage.Wand,
                            _ => weapon.damage
                        };
                    }
                }
                
                return startingGear;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading starting gear: {ex.Message}");
                return new StartingGearData();
            }
        }

        private void InitializeGame()
        {
            // Load starting gear from JSON
            var startingGear = LoadStartingGear();
            
            // Prompt player to choose a starter weapon
            Console.WriteLine();
            Console.WriteLine("Choose your starter weapon:");
            Console.WriteLine();
            for (int i = 0; i < startingGear.weapons.Count; i++)
            {
                var weapon = startingGear.weapons[i];
                Console.WriteLine($"{i + 1}. {weapon.name}");
            }
            Console.WriteLine();
            
            int weaponChoice = 1;
            while (true)
            {
                Console.Write("Choose an option: ");
                if (int.TryParse(Console.ReadLine(), out weaponChoice) && weaponChoice >= 1 && weaponChoice <= startingGear.weapons.Count)
                    break;
                Console.WriteLine("Invalid choice.");
            }
            
            // Create weapon from JSON data
            var selectedWeaponData = startingGear.weapons[weaponChoice - 1];
            WeaponType weaponType = selectedWeaponData.name.ToLower() switch
            {
                var name when name.Contains("sword") => WeaponType.Sword,
                var name when name.Contains("mace") => WeaponType.Mace,
                var name when name.Contains("dagger") => WeaponType.Dagger,
                var name when name.Contains("wand") => WeaponType.Wand,
                _ => WeaponType.Sword
            };
            
            WeaponItem starterWeapon = new WeaponItem(selectedWeaponData.name, 1, selectedWeaponData.damage, selectedWeaponData.attackSpeed, weaponType);
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: About to equip starter weapon: {starterWeapon.Name} (Type: {starterWeapon.WeaponType})");
            player.EquipItem(starterWeapon, "weapon");
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: After equipping weapon, player has {player.ActionPool.Count} actions in pool");
            
            // Initialize combo sequence with weapon actions now that weapon is equipped
            player.InitializeDefaultCombo();
            if (TuningConfig.IsDebugEnabled)
                Console.WriteLine($"DEBUG: After InitializeDefaultCombo, player has {player.ComboSequence.Count} actions in combo sequence");
            
            // Equip starting armor from JSON
            foreach (var armorData in startingGear.armor)
            {
                Item armorItem = armorData.slot.ToLower() switch
                {
                    "head" => new HeadItem(armorData.name, 1, armorData.armor),
                    "chest" => new ChestItem(armorData.name, 1, armorData.armor),
                    "feet" => new FeetItem(armorData.name, 1, armorData.armor),
                    _ => new HeadItem(armorData.name, 1, armorData.armor)
                };
                
                // Starting gear should NEVER have actions - leave GearAction as null
                
                string slot = armorData.slot.ToLower() switch
                {
                    "head" => "head",
                    "chest" => "body", 
                    "feet" => "feet",
                    _ => "head"
                };
                
                player.EquipItem(armorItem, slot);
            }

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
            Console.WriteLine("Welcome to the Dungeon Fighter!\n");
            Console.WriteLine($"Player: {player.GetFullNameWithQualifier()} (Level {player.Level})");

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

                // Declare action speed system variable for use throughout dungeon run
                ActionSpeedSystem? actionSpeedSystem = null;
                
                // Dungeon Selection and Run
                Dungeon selectedDungeon = ChooseDungeon();
                selectedDungeon.Generate();

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
                        var speedSystem = Combat.GetCurrentActionSpeedSystem();
                        if (speedSystem != null)
                        {
                            Console.WriteLine($"Turn Order: {speedSystem.GetTurnOrderInfo()}");
                        }
                        Console.WriteLine(); // Line break between stats and action

                        // Clear all temporary effects when starting a new battle (in case there are multiple enemies in the room)
                        player.ClearAllTempEffects();
                        
                        // Reset Divine reroll charges for new combat
                        player.ResetRerollCharges();
                        
                        // Start battle narrative and initialize action speed system
                        Combat.StartBattleNarrative(player.Name, currentEnemy.Name, room.Name, player.CurrentHealth, currentEnemy.CurrentHealth);
                        Combat.InitializeCombatEntities(player, currentEnemy, room);
                        
                        // Reset game time AFTER initializing combat entities to avoid timing conflicts
                        GameTicker.Instance.Reset();
                        
                        // Reset environment action count for new fight
                        room.ResetForNewFight();

                        // Combat Loop with action speed system
                        var settings = GameSettings.Instance;
                        
                        while (player.IsAlive && currentEnemy.IsAlive)
                        {
                            // Get the next entity that should act based on action speed
                            Entity? nextEntity = Combat.GetNextEntityToAct();
                            
                            if (nextEntity == null)
                            {
                                // No entities ready, advance time to the next entity's action time
                                var currentSpeedSystem = Combat.GetCurrentActionSpeedSystem();
                                if (currentSpeedSystem != null)
                                {
                                    // Find the next entity that will be ready and advance time to that point
                                    var nextReadyTime = currentSpeedSystem.GetNextReadyTime();
                                    if (nextReadyTime > 0)
                                    {
                                        double timeToAdvance = nextReadyTime - currentSpeedSystem.GetCurrentTime();
                                        if (timeToAdvance > 0)
                                        {
                                            currentSpeedSystem.AdvanceTime(timeToAdvance);
                                        }
                                        else
                                        {
                                            // Fallback: advance time slightly
                                            currentSpeedSystem.AdvanceTime(0.1);
                                        }
                                    }
                                    else
                                    {
                                        // No entities ready and no next ready time - this shouldn't happen
                                        Console.WriteLine("ERROR: No entities ready and no next ready time. Breaking combat loop.");
                                        break;
                                    }
                                }
                                else
                                {
                                    // ActionSpeedSystem is null - this shouldn't happen
                                    break;
                                }
                                continue;
                            }

                            // Player acts
                            if (nextEntity == player && player.IsAlive)
                            {
                                // Check if player is stunned
                                if (player.StunTurnsRemaining > 0)
                                {
                                    Combat.WriteCombatLog($"[{player.Name}] is stunned and cannot act! ({player.StunTurnsRemaining} turns remaining)");
                                    // Update temp effects (including reducing stun and weaken turns) even when stunned
                                    player.UpdateTempEffects(1.0); // 1.0 represents one turn
                                    // Advance the player's turn in the action speed system based on their action speed
                                    var currentSpeedSystem = Combat.GetCurrentActionSpeedSystem();
                                    if (currentSpeedSystem != null)
                                    {
                                        // Use the player's actual action speed for turn duration
                                        double playerActionSpeed = player.GetTotalAttackSpeed();
                                        currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
                                    }
                                }
                                else
                                {
                                    // Always recalculate comboActions and actionIdx after any combo reset
                                    var comboActions = player.GetComboActions();
                                    int actionIdx = 0; // Always start at 0 after a reset
                                    if (comboActions.Count > 0)
                                        actionIdx = player.ComboStep % comboActions.Count;
                                    // The action that will be attempted this turn
                                    var attemptedAction = comboActions.Count > 0 ? comboActions[actionIdx] : null;

                                    // Execute single action (not multi-attack) with speed tracking
                                    if (attemptedAction != null)
                                    {
                                        string result = Combat.ExecuteActionWithSpeed(player, currentEnemy, attemptedAction, room);
                                        bool textDisplayed = !string.IsNullOrEmpty(result);
                                        
                                        // Show individual action messages with consistent delay
                                        if (textDisplayed)
                                        {
                                            Combat.WriteCombatLog(result);
                                        }
                                    }
                                    else
                                    {
                                        // Player has no actions available - advance their turn to prevent infinite loop
                                        Combat.WriteCombatLog($"[{player.Name}] has no actions available and cannot act!");
                                        var currentSpeedSystem = Combat.GetCurrentActionSpeedSystem();
                                        if (currentSpeedSystem != null)
                                        {
                                            double playerActionSpeed = player.GetTotalAttackSpeed();
                                            currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
                                        }
                                    }
                                }
                                
                                // Process health regeneration for player after they act
                                int playerHealthRegen = player.GetEquipmentHealthRegenBonus();
                                if (playerHealthRegen > 0 && player.CurrentHealth < player.GetEffectiveMaxHealth())
                                {
                                    int oldHealth = player.CurrentHealth;
                                    // Use negative damage to heal (TakeDamage with negative value heals)
                                    player.TakeDamage(-playerHealthRegen);
                                    // Cap at max health
                                    if (player.CurrentHealth > player.GetEffectiveMaxHealth())
                                    {
                                        player.TakeDamage(player.CurrentHealth - player.GetEffectiveMaxHealth());
                                    }
                                    int actualRegen = player.CurrentHealth - oldHealth;
                                    if (actualRegen > 0)
                                    {
                                        Combat.WriteCombatLog($"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})");
                                    }
                                }
                                
                                if (!currentEnemy.IsAlive)
                                {
                                    break;
                                }
                            }
                            // Enemy acts
                            else if (nextEntity == currentEnemy && currentEnemy.IsAlive)
                            {
                                // Check if enemy is stunned
                                if (currentEnemy.StunTurnsRemaining > 0)
                                {
                                    Combat.WriteCombatLog($"[{currentEnemy.Name}] is stunned and cannot act! ({currentEnemy.StunTurnsRemaining} turns remaining)");
                                    // Update temp effects (including reducing stun and weaken turns) even when stunned
                                    currentEnemy.UpdateTempEffects(1.0); // 1.0 represents one turn
                                    // Advance the enemy's turn in the action speed system based on their action speed
                                    var currentSpeedSystem = Combat.GetCurrentActionSpeedSystem();
                                    if (currentSpeedSystem != null)
                                    {
                                        // Use the enemy's actual action speed for turn duration
                                        double enemyActionSpeed = currentEnemy.GetTotalAttackSpeed();
                                        currentSpeedSystem.AdvanceEntityTurn(currentEnemy, enemyActionSpeed);
                                    }
                                }
                                else
                                {
                                    var enemyAction = currentEnemy.SelectAction();
                                    if (enemyAction != null)
                                    {
                                        string result = Combat.ExecuteActionWithSpeed(currentEnemy, player, enemyAction, room);
                                        bool textDisplayed = !string.IsNullOrEmpty(result);
                                        
                                        // Show individual action messages with consistent delay
                                        if (textDisplayed)
                                        {
                                            Combat.WriteCombatLog(result);
                                        }
                                    }
                                }
                                
                                if (!player.IsAlive)
                                {
                                    break;
                                }
                                
                                // Process poison/bleed damage after enemy's turn
                                double currentTime = GameTicker.Instance.GetCurrentGameTime();
                                
                                // Process poison for player
                                int playerPoisonDamage = player.ProcessPoison(currentTime);
                                if (playerPoisonDamage > 0)
                                {
                                    string damageType = player.GetDamageTypeText();
                                    Combat.WriteCombatLog($"[{player.Name}] takes {playerPoisonDamage} {damageType} damage ({damageType}: {player.PoisonStacks} stacks remain)");
                                }
                                
                                // Process poison for enemy (only if living)
                                if (currentEnemy.IsLiving)
                                {
                                    int enemyPoisonDamage = currentEnemy.ProcessPoison(currentTime);
                                    if (enemyPoisonDamage > 0)
                                    {
                                        string damageType = currentEnemy.GetDamageTypeText();
                                        Combat.WriteCombatLog($"[{currentEnemy.Name}] takes {enemyPoisonDamage} {damageType} damage ({damageType}: {currentEnemy.PoisonStacks} stacks remain)");
                                    }
                                }
                                
                                // Process burn damage for player
                                int playerBurnDamage = player.ProcessBurn(currentTime);
                                if (playerBurnDamage > 0)
                                {
                                    Combat.WriteCombatLog($"[{player.Name}] takes {playerBurnDamage} burn damage (burn: {player.BurnStacks} stacks remain)");
                                }
                                
                                // Process burn damage for enemy (only if living)
                                if (currentEnemy.IsLiving)
                                {
                                    int enemyBurnDamage = currentEnemy.ProcessBurn(currentTime);
                                    if (enemyBurnDamage > 0)
                                    {
                                        Combat.WriteCombatLog($"[{currentEnemy.Name}] takes {enemyBurnDamage} burn damage (burn: {currentEnemy.BurnStacks} stacks remain)");
                                    }
                                }
                            }
                            // Environment acts (now handled through action speed system)
                            else if (nextEntity == room && room.IsHostile && room.ActionPool.Count > 0)
                            {
                                if (room.ShouldEnvironmentAct())
                                {
                                    var envAction = room.SelectAction();
                                    if (envAction != null)
                                    {
                                        // Create list of all characters in the room (player and current enemy)
                                        var allTargets = new List<Entity> { player, currentEnemy };
                                        
                                        // Use area of effect action to target all characters
                                        string result = Combat.ExecuteAreaOfEffectAction(room, allTargets, room, envAction);
                                        bool textDisplayed = !string.IsNullOrEmpty(result);
                                        
                                        // Show individual action messages
                                        if (textDisplayed)
                                        {
                                            Combat.WriteCombatLog(result);
                                        }
                                        
                                        // Update environment's action timing in the action speed system
                                        actionSpeedSystem = Combat.GetCurrentActionSpeedSystem();
                                        if (actionSpeedSystem != null)
                                        {
                                            actionSpeedSystem.ExecuteAction(room, envAction);
                                        }
                                        
                                        if (!player.IsAlive)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    // Environment doesn't want to act, but we still need to advance its turn
                                    // to prevent it from getting stuck in the action speed system
                                    actionSpeedSystem = Combat.GetCurrentActionSpeedSystem();
                                    if (actionSpeedSystem != null)
                                    {
                                        // Advance the environment's turn by a small amount to prevent infinite loops
                                        actionSpeedSystem.AdvanceEntityTurn(room, 1.0);
                                    }
                                }
                            }
                        }
                        
                        // End the battle narrative and display it if narrative balance is high enough
                        Combat.EndBattleNarrative();
                        var narrativeSettings = GameSettings.Instance;
                        
                        if (currentEnemy.IsAlive)
                        {
                            Combat.WriteCombatLog("\nYou have been defeated!");
                            // Delete save file when character dies
                            Character.DeleteSaveFile();
                            return;
                        }
                        else
                        {
                            Combat.WriteCombatLog($"\n{currentEnemy.Name} has been defeated!");
                            player.AddXP(currentEnemy.XPReward);
                        }
                        
                        // Display narrative if balance is set to show poetic text
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

        private WeaponItem? CreateFallbackWeapon(int playerLevel)
        {
            try
            {
                // Try to load weapon data and create a tier 1 weapon as fallback
                string? filePath = FindGameDataFile("Weapons.json");
                if (filePath == null) 
                {
                    Console.WriteLine("   ERROR: Weapons.json file not found in any expected location");
                    return null;
                }
                
                string json = File.ReadAllText(filePath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var weaponData = System.Text.Json.JsonSerializer.Deserialize<List<WeaponData>>(json, options);
                if (weaponData == null) 
                {
                    Console.WriteLine("   ERROR: Failed to deserialize weapon data from Weapons.json");
                    return null;
                }
                
                // Find a tier 1 weapon
                var tier1Weapons = weaponData.Where(w => w.Tier == 1).ToList();
                if (!tier1Weapons.Any()) 
                {
                    Console.WriteLine($"   ERROR: No Tier 1 weapons found in Weapons.json (total weapons: {weaponData.Count})");
                    return null;
                }
                
                // Pick a random tier 1 weapon
                var random = new Random();
                var selectedWeapon = tier1Weapons[random.Next(tier1Weapons.Count)];
                
                var weaponType = Enum.Parse<WeaponType>(selectedWeapon.Type);
                var weapon = new WeaponItem(selectedWeapon.Name, selectedWeapon.Tier, 
                    selectedWeapon.BaseDamage, selectedWeapon.AttackSpeed, weaponType);
                weapon.Rarity = "Common";
                
                return weapon;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ERROR: Exception in CreateFallbackWeapon: {ex.Message}");
                return null;
            }
        }
        
        private string? FindGameDataFile(string fileName)
        {
            // Try current directory first
            if (File.Exists(fileName)) return fileName;
            
            // Try GameData subdirectory
            string gameDataPath = Path.Combine("GameData", fileName);
            if (File.Exists(gameDataPath)) return gameDataPath;
            
            // Try parent directory + GameData
            string parentGameDataPath = Path.Combine("..", "GameData", fileName);
            if (File.Exists(parentGameDataPath)) return parentGameDataPath;
            
            return null;
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
            
            // Award XP (scaled by dungeon level using tuning config)
            var tuning = TuningConfig.Instance;
            int xpReward = random.Next(tuning.Progression.EnemyXPBase, tuning.Progression.EnemyXPBase + 50) * player.Level;
            player.AddXP(xpReward);
            Console.WriteLine($"Gained {xpReward} XP!");

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
                reward = CreateFallbackWeapon(player.Level);
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
            
            // Add spacing before returning to main menu
            Console.WriteLine();
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
            int roomCount = Math.Max(2, (int)Math.Ceiling(MinLevel / 2.0) + 0); // 2 rooms minimum, scales up
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
                    theme: Theme,
                    roomType: roomTheme
                );

                // Generate enemies with scaled levels
                room.GenerateEnemies(roomLevel);
                Rooms.Add(room);
            }
        }

        private string GetRoomTheme(int roomIndex, int totalRooms)
        {
            if (roomIndex == totalRooms - 1) return "Boss";

            string[] themes = new[]
            {
                "Treasure", "Guard", "Trap", "Puzzle", "Rest",
                "Storage", "Library", "Armory", "Kitchen", "Dining",
                "Chamber", "Hall", "Vault", "Sanctum", "Grotto",
                "Catacomb", "Shrine", "Laboratory", "Observatory", "Throne"
            };

            return themes[random.Next(themes.Length)];
        }

        private string? GetRoomDescription(string roomTheme, bool isHostile)
        {
            // Get theme-specific description from FlavorText
            string baseDescription = FlavorText.GenerateLocationDescription(Theme);
            
            // Add room-specific context based on both dungeon theme and room type
            string roomContext = GetThemeAwareRoomContext(roomTheme);
            
            // Add hostility context
            string hostilityContext = isHostile ? " Danger lurks in the shadows." : " It seems safe... for now.";
            
            return baseDescription + roomContext + hostilityContext;
        }
        
        private string GetThemeAwareRoomContext(string roomTheme)
        {
            // Combine dungeon theme with room type for contextual descriptions
            string dungeonTheme = Theme.ToLower();
            string roomType = roomTheme.ToLower();
            
            return (dungeonTheme, roomType) switch
            {
                // Forest-themed rooms
                ("forest", "boss") => " This ancient grove is clearly the domain of a powerful forest guardian.",
                ("forest", "treasure") => " Glittering treasures are hidden among the roots and branches.",
                ("forest", "library") => " Ancient druidic knowledge is preserved in living tree bark and moss-covered stones.",
                ("forest", "armory") => " Weapons made from living wood and enchanted thorns line the walls.",
                ("forest", "kitchen") => " The scent of wild herbs and forest fruits fills this natural cooking area.",
                ("forest", "chamber") => " This natural chamber is formed by ancient tree roots and living wood.",
                ("forest", "sanctum") => " The air here pulses with the life force of the ancient forest.",
                ("forest", "shrine") => " Sacred grove markers and nature offerings mark this as a place of druidic worship.",
                
                // Ice-themed rooms
                ("ice", "boss") => " This frozen throne room is clearly the domain of an ice lord.",
                ("ice", "treasure") => " Frozen treasures glisten within walls of solid ice.",
                ("ice", "library") => " Ancient knowledge is preserved in ice crystals and frozen scrolls.",
                ("ice", "armory") => " Weapons forged from ice and frost line the frozen walls.",
                ("ice", "kitchen") => " The cold air carries the scent of preserved meats and frozen provisions.",
                ("ice", "chamber") => " This chamber is carved entirely from solid ice and permafrost.",
                ("ice", "sanctum") => " The air here crackles with the power of eternal winter.",
                ("ice", "shrine") => " Ice crystals and frozen offerings mark this as a place of winter worship.",
                
                // Lava-themed rooms
                ("lava", "boss") => " This molten chamber is clearly the domain of a fire lord.",
                ("lava", "treasure") => " Treasures forged in fire glow with inner heat within the molten walls.",
                ("lava", "library") => " Ancient knowledge is preserved in fire-resistant stone tablets.",
                ("lava", "armory") => " Weapons forged in volcanic fires line the superheated walls.",
                ("lava", "kitchen") => " The intense heat carries the scent of charred meat and molten metal.",
                ("lava", "chamber") => " This chamber is carved from volcanic rock and flows with molten lava.",
                ("lava", "sanctum") => " The air here burns with the power of the earth's molten heart.",
                ("lava", "shrine") => " Molten offerings and fire crystals mark this as a place of volcanic worship.",
                
                // Crypt-themed rooms
                ("crypt", "boss") => " This ancient tomb is clearly the domain of a powerful undead lord.",
                ("crypt", "treasure") => " Funerary treasures and grave goods are piled in shadowy corners.",
                ("crypt", "library") => " Ancient necromantic knowledge is preserved in bone scrolls and death runes.",
                ("crypt", "armory") => " Weapons of the dead and cursed blades line the tomb walls.",
                ("crypt", "kitchen") => " The air carries the scent of embalming spices and decay.",
                ("crypt", "chamber") => " This burial chamber is lined with ancient sarcophagi and death masks.",
                ("crypt", "sanctum") => " The air here pulses with the dark energy of undeath.",
                ("crypt", "shrine") => " Death symbols and funerary offerings mark this as a place of necromantic worship.",
                
                // Desert-themed rooms
                ("desert", "boss") => " This sand-swept chamber is clearly the domain of a desert king.",
                ("desert", "treasure") => " Ancient treasures are buried beneath shifting sands.",
                ("desert", "library") => " Desert knowledge is preserved in sand-etched tablets and wind-worn stones.",
                ("desert", "armory") => " Weapons forged in desert heat and sand-blasted steel line the walls.",
                ("desert", "kitchen") => " The dry air carries the scent of preserved foods and desert spices.",
                ("desert", "chamber") => " This chamber is carved from sandstone and filled with shifting dunes.",
                ("desert", "sanctum") => " The air here shimmers with the power of the endless sands.",
                ("desert", "shrine") => " Sand symbols and desert offerings mark this as a place of sun worship.",
                
                // Castle-themed rooms
                ("castle", "boss") => " This grand throne room is clearly the domain of a powerful lord.",
                ("castle", "treasure") => " Royal treasures and noble artifacts fill the castle vaults.",
                ("castle", "library") => " Noble knowledge is preserved in leather-bound tomes and royal scrolls.",
                ("castle", "armory") => " Noble weapons and armor of the realm line the castle walls.",
                ("castle", "kitchen") => " The air carries the scent of royal feasts and noble cuisine.",
                ("castle", "chamber") => " This chamber is built with castle stone and noble architecture.",
                ("castle", "sanctum") => " The air here resonates with the power of royal authority.",
                ("castle", "shrine") => " Royal symbols and noble offerings mark this as a place of court worship.",
                
                // Generic room contexts (fallback)
                (_, "boss") => " This grand chamber is clearly the domain of a powerful being.",
                (_, "treasure") => " Glittering treasures catch your eye, but danger may lurk nearby.",
                (_, "guard") => " This area appears to be well-fortified and guarded.",
                (_, "trap") => " Something about this room seems suspicious and dangerous.",
                (_, "puzzle") => " Ancient mechanisms and cryptic symbols cover the walls here.",
                (_, "rest") => " This area seems surprisingly peaceful and safe.",
                (_, "storage") => " Shelves and supplies line the walls of this storage area.",
                (_, "library") => " Rows of ancient tomes and scrolls fill this scholarly space.",
                (_, "armory") => " Weapons and armor are displayed on racks throughout this room.",
                (_, "kitchen") => " The smell of cooking and the sound of clanging pots fills this area.",
                (_, "dining") => " A long table is set for a feast, though the food looks questionable.",
                (_, "chamber") => " This chamber has an air of mystery and ancient power.",
                (_, "hall") => " A long corridor stretches before you, lined with ornate decorations.",
                (_, "vault") => " Heavy doors and reinforced walls suggest this place holds something valuable.",
                (_, "sanctum") => " The air here feels charged with mystical energy.",
                (_, "grotto") => " Natural rock formations create an otherworldly atmosphere.",
                (_, "catacomb") => " Ancient burial chambers and stone sarcophagi line the walls.",
                (_, "shrine") => " Sacred symbols and offerings mark this as a place of worship.",
                (_, "laboratory") => " Strange equipment and bubbling potions fill this alchemical workspace.",
                (_, "observatory") => " Star charts and celestial instruments suggest this room studies the heavens.",
                (_, "throne") => " An ornate throne dominates this regal chamber.",
                _ => ""
            };
        }
    }
} 