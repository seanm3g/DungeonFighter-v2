namespace RPGGame
{
    using System.Text.Json;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    public enum GameState
    {
        MainMenu,
        WeaponSelection,
        CharacterCreation,
        GameLoop,
        Inventory,
        CharacterInfo,
        Settings,
        Testing,
        DungeonSelection,
        Dungeon,
        Combat,
        DungeonCompletion
    }


    public class Game
    {
        private GameMenuManager menuManager;
        private IUIManager? customUIManager;
        private GameInitializer gameInitializer;
        
        // Game state management
        private GameState currentState = GameState.MainMenu;
        private Character? currentPlayer;
        private List<Item> currentInventory = new();
        private List<Dungeon> availableDungeons = new();
        
        // Game loop state
        private GameLoopManager? gameLoopManager;
        private DungeonManagerWithRegistry? dungeonManager;
        private CombatManager? combatManager;
        private Dungeon? currentDungeon = null;
        private Environment? currentRoom = null;
        private List<string> dungeonLog = new();
        private List<string> dungeonHeaderInfo = new();  // Stores dungeon info for each encounter
        private List<string> currentRoomInfo = new();    // Stores current room info for each encounter

        public Game()
        {
            // Start the game ticker
            GameTicker.Instance.Start();
            menuManager = new GameMenuManager();
            gameInitializer = new GameInitializer();
            
            // Initialize game loop managers
            gameLoopManager = new GameLoopManager();
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();
        }

        public Game(IUIManager uiManager)
        {
            // Start the game ticker
            GameTicker.Instance.Start();
            customUIManager = uiManager;
            
            // Set the custom UI manager for the static UIManager class
            // This ensures combat text goes to the GUI instead of console
            UIManager.SetCustomUIManager(uiManager);
            
            menuManager = new GameMenuManager();
            gameInitializer = new GameInitializer();
            
            // Initialize game loop managers
            gameLoopManager = new GameLoopManager();
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();
        }

        public Game(Character existingCharacter)
        {
            var settings = GameSettings.Instance;
            
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                existingCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            // Start the game ticker
            GameTicker.Instance.Start();
            menuManager = new GameMenuManager();
            gameInitializer = new GameInitializer();
            
            // Initialize game loop managers
            gameLoopManager = new GameLoopManager();
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();
            
            // Initialize existing game
            var inventory = new List<Item>();
            var availableDungeons = new List<Dungeon>();
            gameInitializer.InitializeExistingGame(existingCharacter, availableDungeons);
        }


        public static Dictionary<string, List<string>> GetThemeSpecificRooms()
        {
            return new Dictionary<string, List<string>>
            {
                ["Forest"] = new List<string> { "Grove", "Thicket", "Canopy", "Meadow", "Wilderness", "Tree Hollow", "Sacred Grove" },
                ["Lava"] = new List<string> { "Magma Chamber", "Fire Pit", "Molten Pool", "Volcanic Vent", "Ash Field", "Ember Cave", "Inferno Hall" },
                ["Crypt"] = new List<string> { "Burial Chamber", "Tomb", "Mausoleum", "Necropolis", "Death Shrine", "Sarcophagus Room", "Undead Vault" },
                ["Cavern"] = new List<string> { "Crystal Cave", "Underground Lake", "Stalactite Hall", "Deep Tunnel", "Mining Shaft", "Cave System", "Underground River" },
                ["Swamp"] = new List<string> { "Bog", "Marsh", "Quagmire", "Fen", "Wetland", "Mud Pit", "Marshland" },
                ["Desert"] = new List<string> { "Sand Dune", "Oasis", "Mirage", "Sandstorm", "Dune Valley", "Desert Spring", "Sand Temple" },
                ["Ice"] = new List<string> { "Glacier", "Ice Cave", "Frozen Lake", "Blizzard", "Frost Field", "Ice Palace", "Frozen Tundra" },
                ["Ruins"] = new List<string> { "Crumbling Hall", "Broken Tower", "Fallen Temple", "Ancient Ruins", "Decayed Chamber", "Lost City", "Forgotten Shrine" },
                ["Castle"] = new List<string> { "Throne Room", "Great Hall", "Dungeon", "Tower", "Courtyard", "Royal Chamber", "Castle Keep" },
                ["Graveyard"] = new List<string> { "Cemetery", "Mausoleum", "Tomb", "Grave Site", "Burial Ground", "Death Garden", "Necropolis" },
                ["Crystal"] = new List<string> { "Crystal Chamber", "Prism Hall", "Geode Cave", "Crystal Garden", "Shard Room", "Crystal Palace", "Gem Vault" },
                ["Temple"] = new List<string> { "Sanctuary", "Altar Room", "Prayer Hall", "Sacred Chamber", "Divine Shrine", "Holy Sanctum", "Temple Vault" },
                ["Generic"] = new List<string> { "Common Room", "Storage", "Hallway", "Chamber", "Vault", "Guard Room", "Meeting Hall" },
                ["Shadow"] = new List<string> { "Shadow Realm", "Dark Chamber", "Void Space", "Shadow Garden", "Umbra Hall", "Dark Sanctum", "Shadow Vault" },
                ["Steampunk"] = new List<string> { "Steam Chamber", "Gear Room", "Clockwork Hall", "Mechanical Vault", "Steam Engine", "Cog Chamber", "Industrial Hall" },
                ["Astral"] = new List<string> { "Star Chamber", "Cosmic Hall", "Nebula Room", "Galaxy Vault", "Celestial Sanctum", "Astral Observatory", "Space Temple" },
                ["Underground"] = new List<string> { "Deep Tunnel", "Subterranean Hall", "Underground City", "Cave System", "Mining District", "Underground Lake", "Deep Chamber" },
                ["Storm"] = new List<string> { "Thunder Hall", "Lightning Chamber", "Storm Eye", "Tempest Room", "Hurricane Vault", "Wind Tunnel", "Storm Sanctum" },
                ["Nature"] = new List<string> { "Garden", "Grove", "Meadow", "Wilderness", "Natural Spring", "Flower Field", "Nature Shrine" },
                ["Arcane"] = new List<string> { "Study", "Library", "Laboratory", "Spell Chamber", "Magic Vault", "Arcane Sanctum", "Wizard Tower" },
                ["Volcano"] = new List<string> { "Magma Chamber", "Volcanic Vent", "Ash Field", "Lava Pool", "Volcanic Crater", "Fire Chamber", "Volcano Summit" },
                ["Ocean"] = new List<string> { "Underwater Chamber", "Coral Reef", "Deep Sea Vault", "Ocean Floor", "Sea Cave", "Abyssal Hall", "Ocean Depths" },
                ["Mountain"] = new List<string> { "Peak", "Summit", "Mountain Pass", "High Altitude", "Rocky Outcrop", "Mountain Cave", "Alpine Chamber" },
                ["Temporal"] = new List<string> { "Time Chamber", "Chronos Hall", "Temporal Rift", "Time Vault", "Echo Chamber", "Paradox Room", "Timeline Sanctum" },
                ["Dream"] = new List<string> { "Dreamscape", "Nightmare Realm", "Lucid Chamber", "Subconscious Hall", "Fantasy Vault", "Dream Sanctum", "Sleep Chamber" },
                ["Void"] = new List<string> { "Void Chamber", "Emptiness Hall", "Null Space", "Void Vault", "Nothingness Room", "Absence Chamber", "Void Sanctum" },
                ["Dimensional"] = new List<string> { "Dimension Rift", "Reality Chamber", "Multiverse Hall", "Space-Time Vault", "Quantum Room", "Dimensional Sanctum", "Rift Chamber" },
                ["Divine"] = new List<string> { "Heavenly Chamber", "Divine Hall", "Sacred Vault", "Celestial Sanctum", "Holy Chamber", "Divine Temple", "Eternal Hall" }
            };
        }

        public static DungeonGenerationConfig GetDungeonGenerationConfig()
        {
            return new DungeonGenerationConfig
            {
                minRooms = 2,
                roomCountScaling = 0.5,
                hostileRoomChance = 0.8,
                bossRoomName = "Boss"
            };
        }


        public async void ShowMainMenu()
        {
            // Try to load saved character if we don't have one yet
            if (currentPlayer == null)
            {
                // Show loading message
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowLoadingAnimation("Loading saved game...");
                }
                
                // Load character on background thread to avoid blocking UI
                var savedCharacter = await Task.Run(() => Character.LoadCharacter());
                if (savedCharacter != null)
                {
                    currentPlayer = savedCharacter;
                    
                    // Initialize game data on background thread
                    await Task.Run(() => gameInitializer.InitializeExistingGame(currentPlayer, availableDungeons));
                    
                    // Set character in UI manager for persistent display (on UI thread)
                    if (customUIManager is CanvasUICoordinator canvasUI2)
                    {
                        canvasUI2.SetCharacter(currentPlayer);
                    }
                    
                    // Apply health multiplier if configured
                    var settings = GameSettings.Instance;
                    if (settings.PlayerHealthMultiplier != 1.0)
                    {
                        currentPlayer.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                    }
                    
                    // Load inventory
                    if (currentPlayer.Inventory != null)
                    {
                        currentInventory = currentPlayer.Inventory;
                    }
                }
            }
            
            if (customUIManager != null)
            {
                // Use custom UI manager (e.g., CanvasUICoordinator)
                ShowMainMenuWithCustomUI();
            }
            else
            {
                // Use default console UI
                menuManager.ShowMainMenu();
            }
        }

        public void SetUIManager(IUIManager uiManager)
        {
            customUIManager = uiManager;
            
            // Set the custom UI manager for the static UIManager class
            // This ensures combat text goes to the GUI instead of console
            UIManager.SetCustomUIManager(uiManager);
        }

        private void ShowMainMenuWithCustomUI()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Check if we have a saved game
                bool hasSavedGame = currentPlayer != null;
                string? characterName = currentPlayer?.Name;
                int characterLevel = currentPlayer?.Level ?? 0;
                
                canvasUI.RenderMainMenu(hasSavedGame, characterName, characterLevel);
            }
            currentState = GameState.MainMenu;
        }

        public async Task HandleInput(string input)
        {
            // Handle input for custom UI
            if (customUIManager != null)
            {
                // Process input based on current game state
                switch (currentState)
                {
                    case GameState.MainMenu:
                        HandleMainMenuInput(input);
                        break;
                    case GameState.WeaponSelection:
                        await HandleWeaponSelectionInput(input);
                        break;
                    case GameState.Inventory:
                        HandleInventoryInput(input);
                        break;
                    case GameState.CharacterInfo:
                        HandleCharacterInfoInput(input);
                        break;
                    case GameState.Settings:
                        HandleSettingsInput(input);
                        break;
                    case GameState.Testing:
                        HandleTestingInput(input);
                        break;
                    case GameState.GameLoop:
                        await HandleGameLoopInput(input);
                        break;
                    case GameState.CharacterCreation:
                        await HandleCharacterCreationInput(input);
                        break;
                    case GameState.DungeonSelection:
                        await HandleDungeonSelectionInput(input);
                        break;
                    case GameState.DungeonCompletion:
                        await HandleDungeonCompletionInput(input);
                        break;
                    case GameState.Dungeon:
                    case GameState.Combat:
                        // During dungeon/combat, input is handled automatically
                        // Ignore keyboard input to prevent interference
                        break;
                    default:
                        // Default to main menu for unknown states
                        currentState = GameState.MainMenu;
                        ShowMainMenuWithCustomUI();
                        break;
                }
            }
        }

        private void HandleMainMenuInput(string input)
        {
            switch (input)
            {
                case "1":
                    // New Game
                    StartNewGame();
                    break;
                case "2":
                    // Load Game
                    LoadGame();
                    break;
                case "3":
                    // Settings
                    currentState = GameState.Settings;
                    ShowSettings();
                    break;
                case "0":
                    // Quit
                    ExitGame();
                    break;
                default:
                    ShowMessage("Invalid choice. Please select 1, 2, 3, or 0.");
                    break;
            }
        }

        private async void LoadGame()
        {
            if (currentPlayer != null)
            {
                // Character already loaded, go to game loop
                currentState = GameState.GameLoop;
                ShowGameLoopMenu();
            }
            else
            {
                // Show loading message
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowLoadingAnimation("Loading saved game...");
                }
                
                // Try to load saved character on background thread
                var savedCharacter = await Task.Run(() => Character.LoadCharacter());
                if (savedCharacter != null)
                {
                    currentPlayer = savedCharacter;
                    
                    // Initialize game data on background thread
                    await Task.Run(() => gameInitializer.InitializeExistingGame(currentPlayer, availableDungeons));
                    
                    // Set character in UI manager (on UI thread)
                    if (customUIManager is CanvasUICoordinator canvasUI2)
                    {
                        canvasUI2.SetCharacter(currentPlayer);
                    }
                    
                    // Apply health multiplier if configured
                    var settings = GameSettings.Instance;
                    if (settings.PlayerHealthMultiplier != 1.0)
                    {
                        currentPlayer.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                    }
                    
                    // Load inventory
                    if (currentPlayer.Inventory != null)
                    {
                        currentInventory = currentPlayer.Inventory;
                    }
                    
                    ShowMessage($"Welcome back, {currentPlayer.Name}!");
                    
                    // Go to game loop
                    currentState = GameState.GameLoop;
                    ShowGameLoopMenu();
                }
                else
                {
                    ShowMessage("No saved game found. Please start a new game.");
                    ShowMainMenuWithCustomUI();
                }
            }
        }

        public Task HandleEscapeKey()
        {
            // Handle escape key based on current game state
            switch (currentState)
            {
                case GameState.Inventory:
                case GameState.CharacterInfo:
                case GameState.Settings:
                    // Return to main menu
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    break;
                case GameState.GameLoop:
                case GameState.Dungeon:
                case GameState.Combat:
                    // Return to main menu from game
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    break;
                default:
                    // Default to main menu
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    break;
            }
            
            return Task.CompletedTask;
        }

        // Game action methods
        private void StartNewGame()
        {
            try
            {
                // Check if we have a saved character
                var savedCharacter = Character.LoadCharacter();
                if (savedCharacter != null)
                {
                    // Load existing character
                    currentPlayer = savedCharacter;
                    gameInitializer.InitializeExistingGame(currentPlayer, availableDungeons);
                    
                    // Set character in UI manager for persistent display
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.SetCharacter(currentPlayer);
                    }
                    
                    // Apply health multiplier if configured
                    var settings = GameSettings.Instance;
                    if (settings.PlayerHealthMultiplier != 1.0)
                    {
                        currentPlayer.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                    }
                    
                    // Go directly to game loop for existing character
                    currentState = GameState.GameLoop;
                    ShowGameLoop();
                }
                else
                {
                    // Create new character (without equipment yet)
                    currentPlayer = new Character(null, 1); // null triggers random name generation
                    
                    // Apply health multiplier if configured
                    var settings = GameSettings.Instance;
                    if (settings.PlayerHealthMultiplier != 1.0)
                    {
                        currentPlayer.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                    }
                    
                    // Go to weapon selection first
                    currentState = GameState.WeaponSelection;
                    ShowWeaponSelection();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error starting game: {ex.Message}");
                currentState = GameState.MainMenu;
                ShowMainMenuWithCustomUI();
            }
        }

        private void ShowInventory()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && currentPlayer != null)
            {
                canvasUI.SetCharacter(currentPlayer);
                canvasUI.RenderInventory(currentPlayer, currentInventory);
            }
        }

        private void ShowGameLoopMenu()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && currentPlayer != null)
            {
                canvasUI.SetCharacter(currentPlayer);
                canvasUI.RenderGameMenu(currentPlayer, currentInventory);
            }
        }

        private void ShowCharacterInfo()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && currentPlayer != null)
            {
                // For now, show inventory which includes character stats
                canvasUI.RenderInventory(currentPlayer, currentInventory);
            }
        }

        private void ShowSettings()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderSettings();
            }
        }

        private Task SaveGame()
        {
            if (currentPlayer != null)
            {
                try
                {
                    currentPlayer.SaveCharacter();
                    ShowMessage($"Game saved successfully!");
                }
                catch (Exception ex)
                {
                    ShowMessage($"Error saving game: {ex.Message}");
                }
            }
            
            return Task.CompletedTask;
        }

        private void ExitGame()
        {
            ShowMessage("Thanks for playing Dungeon Fighter!");
            // Close the application
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.Close();
            }
            System.Environment.Exit(0);
        }

        private void ShowCharacterCreation()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && currentPlayer != null)
            {
                canvasUI.RenderCharacterCreation(currentPlayer);
            }
        }

        private void ShowGameLoop()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && currentPlayer != null)
            {
                // Show the main game menu (like the original console version)
                canvasUI.RenderGameMenu(currentPlayer, currentInventory);
            }
        }

        private void ShowMessage(string message)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ShowMessage(message);
            }
        }

        private void ShowTestingMenu()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderTestingMenu();
            }
        }

        private async void HandleTestingInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Check if we're waiting for any key to return to test menu
                if (waitingForTestMenuReturn)
                {
                    // Clear the display buffer and show the test menu
                    canvasUI.ClearDisplayBuffer();
                    waitingForTestMenuReturn = false;
                    ShowTestingMenu();
                    return;
                }
                
                var testRunner = new GameSystemTestRunner(canvasUI);
                
                switch (input)
                {
                    case "1":
                        // Run All Tests
                        await RunAllTests(testRunner);
                        break;
                    case "2":
                        // Character System Tests
                        await RunSystemTests(testRunner, "Character");
                        break;
                    case "3":
                        // Combat System Tests
                        await RunSystemTests(testRunner, "Combat");
                        break;
                    case "4":
                        // Inventory System Tests
                        await RunSystemTests(testRunner, "Inventory");
                        break;
                    case "5":
                        // Dungeon System Tests
                        await RunSystemTests(testRunner, "Dungeon");
                        break;
                    case "6":
                        // Data System Tests
                        await RunSystemTests(testRunner, "Data");
                        break;
                    case "7":
                        // UI System Tests
                        await RunSystemTests(testRunner, "UI");
                        break;
                    case "8":
                        // Combat UI Fixes
                        await RunCombatUIFixes(testRunner);
                        break;
                    case "9":
                        // Integration Tests
                        await RunIntegrationTests(testRunner);
                        break;
                    case "0":
                        // Back to Settings
                        currentState = GameState.Settings;
                        ShowSettings();
                        break;
                    default:
                        canvasUI.WriteLine("Invalid choice. Please select 1-9 or 0.");
                        break;
                }
            }
        }

        private async Task RunAllTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ClearDisplayBuffer();
                var results = await testRunner.RunAllTests();
                
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine("=== TEST COMPLETE ===");
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine(testRunner.GetTestSummary());
                canvasUI.WriteBlankLine();
                
                // Position "Press any key" message at bottom right corner
                canvasUI.WriteLineColored("Press any key to return to test menu...", 150, 55);
                
                // Set flag to indicate we're waiting for any key to return to menu
                waitingForTestMenuReturn = true;
            }
        }

        private async Task RunSystemTests(GameSystemTestRunner testRunner, string systemName)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ClearDisplayBuffer();
                var results = await testRunner.RunSystemTests(systemName);
                
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine("=== TEST COMPLETE ===");
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine(testRunner.GetTestSummary());
                canvasUI.WriteBlankLine();
                
                // Position "Press any key" message at bottom right corner
                canvasUI.WriteLineColored("Press any key to return to test menu...", 150, 55);
                
                // Set flag to indicate we're waiting for any key to return to menu
                waitingForTestMenuReturn = true;
            }
        }

        private async Task RunCombatUIFixes(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ClearDisplayBuffer();
                canvasUI.WriteLine("=== COMBAT UI FIXES TESTS ===");
                canvasUI.WriteBlankLine();
                
                var result1 = await testRunner.RunSpecificTest("Combat Panel Containment");
                var result2 = await testRunner.RunSpecificTest("Combat Freezing Prevention");
                var result3 = await testRunner.RunSpecificTest("Combat Log Cleanup");
                
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine("=== TEST COMPLETE ===");
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine(testRunner.GetTestSummary());
                canvasUI.WriteBlankLine();
                
                // Position "Press any key" message at bottom right corner
                canvasUI.WriteLineColored("Press any key to return to test menu...", 150, 55);
                
                // Set flag to indicate we're waiting for any key to return to menu
                waitingForTestMenuReturn = true;
            }
        }

        private async Task RunIntegrationTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ClearDisplayBuffer();
                canvasUI.WriteLine("=== INTEGRATION TESTS ===");
                canvasUI.WriteBlankLine();
                
                var result1 = await testRunner.RunSpecificTest("Game Flow Integration");
                var result2 = await testRunner.RunSpecificTest("Performance Integration");
                
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine("=== TEST COMPLETE ===");
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine(testRunner.GetTestSummary());
                canvasUI.WriteBlankLine();
                
                // Position "Press any key" message at bottom right corner
                canvasUI.WriteLineColored("Press any key to return to test menu...", 150, 55);
                
                // Set flag to indicate we're waiting for any key to return to menu
                waitingForTestMenuReturn = true;
            }
        }

        // Placeholder methods for other input handlers
        private void HandleInventoryInput(string input)
        {
            if (currentPlayer == null) return;
            
            // Handle multi-step actions (item selection, slot selection)
            if (waitingForItemSelection && int.TryParse(input, out int itemIndex))
            {
                waitingForItemSelection = false;
                
                if (itemIndex == 0)
                {
                    ShowMessage("Cancelled.");
                    ShowInventory();
                    return;
                }
                
                itemIndex--; // Convert 1-based to 0-based
                
                if (itemSelectionAction == "equip")
                {
                    EquipItem(itemIndex);
                }
                else if (itemSelectionAction == "discard")
                {
                    DiscardItem(itemIndex);
                }
                
                itemSelectionAction = "";
                return;
            }
            
            if (waitingForSlotSelection && int.TryParse(input, out int slotChoice))
            {
                waitingForSlotSelection = false;
                
                if (slotChoice == 0)
                {
                    ShowMessage("Cancelled.");
                    ShowInventory();
                    return;
                }
                
                UnequipItem(slotChoice);
                return;
            }
            
            // Normal menu actions
            switch (input)
            {
                case "1":
                    // Equip Item
                    PromptEquipItem();
                    break;
                case "2":
                    // Unequip Item
                    PromptUnequipItem();
                    break;
                case "3":
                    // Discard Item
                    PromptDiscardItem();
                    break;
                case "4":
                    // Manage Combo Actions
                    ShowComboManagement();
                    break;
                case "5":
                    // Continue to Dungeon
                    currentState = GameState.GameLoop;
                    ShowGameLoopMenu();
                    break;
                case "6":
                    // Return to Main Menu
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    break;
                case "0":
                    // Exit Game
                    ExitGame();
                    break;
                default:
                    ShowMessage("Invalid choice. Press 1-6, 0, or ESC to go back.");
                    break;
            }
        }

        private void HandleCharacterInfoInput(string input)
        {
            // Character info is read-only, just go back to main menu
            currentState = GameState.MainMenu;
            ShowMainMenuWithCustomUI();
        }

        // Inventory action methods
        private void PromptEquipItem()
        {
            if (currentPlayer == null) return;
            
            if (currentInventory.Count == 0)
            {
                ShowMessage("No items in inventory to equip.");
                ShowInventory();
                return;
            }
            
            // Render item selection screen for equipping
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderItemSelectionPrompt(currentPlayer, currentInventory, "Select Item to Equip", "equip");
            }
            
            // State will be handled by input processing
            waitingForItemSelection = true;
            itemSelectionAction = "equip";
        }

        private void PromptUnequipItem()
        {
            if (currentPlayer == null) return;
            
            // Render slot selection screen for unequipping
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderSlotSelectionPrompt(currentPlayer);
            }
            
            waitingForSlotSelection = true;
        }

        private void PromptDiscardItem()
        {
            if (currentPlayer == null) return;
            
            if (currentInventory.Count == 0)
            {
                ShowMessage("No items in inventory to discard.");
                ShowInventory();
                return;
            }
            
            // Render item selection screen for discarding
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderItemSelectionPrompt(currentPlayer, currentInventory, "Select Item to Discard", "discard");
            }
            
            waitingForItemSelection = true;
            itemSelectionAction = "discard";
        }

        private void EquipItem(int itemIndex)
        {
            if (currentPlayer == null || itemIndex < 0 || itemIndex >= currentInventory.Count) return;
            
            var item = currentInventory[itemIndex];
            string slot = item.Type switch
            {
                ItemType.Weapon => "weapon",
                ItemType.Head => "head",
                ItemType.Chest => "body",
                ItemType.Feet => "feet",
                _ => ""
            };
            
            // Get the previously equipped item (if any)
            var previousItem = currentPlayer.EquipItem(item, slot);
            
            // Remove the new item from inventory
            currentInventory.RemoveAt(itemIndex);
            
            // Destroy the previous item (do not add back to inventory)
            if (previousItem != null)
            {
                ShowMessage($"Unequipped and destroyed {previousItem.Name}. Equipped {item.Name}.");
            }
            else
            {
                ShowMessage($"Equipped {item.Name}.");
            }
            
            // Refresh inventory display
            ShowInventory();
        }

        private void UnequipItem(int slotChoice)
        {
            if (currentPlayer == null) return;
            
            string slot = slotChoice switch
            {
                1 => "weapon",
                2 => "head",
                3 => "body",
                4 => "feet",
                _ => ""
            };
            
            if (string.IsNullOrEmpty(slot))
            {
                ShowMessage("Invalid slot choice.");
                ShowInventory();
                return;
            }
            
            var unequippedItem = currentPlayer.UnequipItem(slot);
            if (unequippedItem != null)
            {
                currentInventory.Add(unequippedItem);
                ShowMessage($"Unequipped {unequippedItem.Name}.");
            }
            else
            {
                ShowMessage($"No item was equipped in the {slot} slot.");
            }
            
            ShowInventory();
        }

        private void DiscardItem(int itemIndex)
        {
            if (currentPlayer == null || itemIndex < 0 || itemIndex >= currentInventory.Count) return;
            
            var item = currentInventory[itemIndex];
            currentInventory.RemoveAt(itemIndex);
            ShowMessage($"Discarded {item.Name}.");
            
            ShowInventory();
        }

        private void ShowComboManagement()
        {
            if (currentPlayer == null) return;
            
            ShowMessage("Combo Management - Coming soon!\nPress any key to return to inventory.");
            // TODO: Implement full combo management UI
            ShowInventory();
        }

        // State tracking for multi-step actions
        private bool waitingForItemSelection = false;
        private bool waitingForSlotSelection = false;
        private string itemSelectionAction = "";


        private void HandleSettingsInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "1":
                        // Run Tests
                        currentState = GameState.Testing;
                        ShowTestingMenu();
                        break;
                    case "0":
                        // Back to Main Menu
                        canvasUI.ResetDeleteConfirmation();
                        currentState = GameState.MainMenu;
                        ShowMainMenuWithCustomUI();
                        break;
                    default:
                        // Any other input cancels the delete confirmation
                        canvasUI.ResetDeleteConfirmation();
                        ShowSettings();
                        break;
                }
            }
            else
            {
                // Fallback for non-UI mode
                currentState = GameState.MainMenu;
                ShowMainMenuWithCustomUI();
            }
        }
        
        private void HandleDeleteCharacter(CanvasUICoordinator canvasUI)
        {
            // Check if we have a saved character
            if (!CharacterSaveManager.SaveFileExists())
            {
                ShowMessage("No saved character found.");
                canvasUI.ResetDeleteConfirmation();
                ShowSettings();
                return;
            }
            
            // Two-step confirmation process
            if (!deleteConfirmationPending)
            {
                // First click: Set confirmation pending
                deleteConfirmationPending = true;
                canvasUI.SetDeleteConfirmationPending(true);
                ShowSettings(); // Re-render with confirmation prompt
            }
            else
            {
                // Second click: Actually delete the character
                try
                {
                    CharacterSaveManager.DeleteSaveFile();
                    ShowMessage("Character deleted successfully.");
                    deleteConfirmationPending = false;
                    canvasUI.ResetDeleteConfirmation();
                    
                    // If we just deleted the current player, clear them
                    if (currentPlayer != null)
                    {
                        currentPlayer = null;
                    }
                    
                    // Return to main menu
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                }
                catch (Exception ex)
                {
                    ShowMessage($"Error deleting character: {ex.Message}");
                    deleteConfirmationPending = false;
                    canvasUI.ResetDeleteConfirmation();
                    ShowSettings();
                }
            }
        }
        
        // Track delete confirmation state
        private bool deleteConfirmationPending = false;
        private bool waitingForTestMenuReturn = false;

        private async Task HandleGameLoopInput(string input)
        {
            if (currentPlayer == null) return;
            
            switch (input)
            {
                case "1":
                    // Go to Dungeon Selection
                    await StartDungeonSelection();
                    break;
                case "2":
                    // Show Inventory Menu
                    currentState = GameState.Inventory;
                    ShowInventory();
                    break;
                case "0":
                    // Save and Exit
                    await SaveGame();
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    break;
                default:
                    ShowMessage("Invalid choice. Please select 1, 2, or 0.");
                    break;
            }
        }
        
        private async Task HandleDungeonCompletionInput(string input)
        {
            if (currentPlayer == null) return;
            
            // Clear dungeon state
            currentDungeon = null;
            currentRoom = null;
            
            switch (input)
            {
                case "1":
                    // Go to Dungeon Selection
                    await StartDungeonSelection();
                    break;
                case "2":
                    // Show Inventory Menu
                    currentState = GameState.Inventory;
                    ShowInventory();
                    break;
                case "0":
                    // Save and Exit
                    await SaveGame();
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    break;
                default:
                    ShowMessage("Invalid choice. Please select 1, 2, or 0.");
                    break;
            }
        }

        private Task StartDungeonSelection()
        {
            if (currentPlayer == null || dungeonManager == null) return Task.CompletedTask;
            
            // Regenerate dungeons based on current player level
            dungeonManager.RegenerateDungeons(currentPlayer, availableDungeons);
            
            // Set state to dungeon selection
            currentState = GameState.DungeonSelection;
            
            // Show dungeon selection screen
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderDungeonSelection(currentPlayer, availableDungeons);
            }
            
            return Task.CompletedTask;
        }

        private async Task HandleDungeonSelectionInput(string input)
        {
            if (currentPlayer == null) return;
            
            if (int.TryParse(input, out int choice))
            {
                if (choice >= 1 && choice <= availableDungeons.Count)
                {
                    // Stop dungeon selection animation
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.StopDungeonSelectionAnimation();
                    }
                    
                    // Select the dungeon (choice is 1-based, convert to 0-based)
                    await SelectDungeon(choice - 1);
                }
                else if (choice == 0)
                {
                    // Stop dungeon selection animation
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.StopDungeonSelectionAnimation();
                    }
                    
                    // Return to game menu
                    currentState = GameState.GameLoop;
                    ShowGameLoop();
                }
                else
                {
                    ShowMessage("Invalid choice. Please select a valid dungeon or 0 to return.");
                }
            }
            else
            {
                ShowMessage("Invalid input. Please enter a number.");
            }
        }

        private async Task SelectDungeon(int dungeonIndex)
        {
            if (currentPlayer == null || dungeonManager == null || combatManager == null) return;
            
            if (dungeonIndex < 0 || dungeonIndex >= availableDungeons.Count)
            {
                ShowMessage("Invalid dungeon selection.");
                return;
            }
            
            currentDungeon = availableDungeons[dungeonIndex];
            currentDungeon.Generate();
            
            // Start the dungeon run
            await RunDungeon();
        }

        private async Task RunDungeon()
        {
            if (currentPlayer == null || currentDungeon == null || combatManager == null) return;
            
            // Set game state to Dungeon to prevent dungeon selection input from being processed
            currentState = GameState.Dungeon;
            
            // Store dungeon header info separately so we can reuse it for each encounter
            dungeonHeaderInfo.Clear();
            
            // Create colored dungeon header using new system
            var headerText = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringDungeonHeader);
            var coloredHeader = new ColoredTextBuilder()
                .Add(headerText, ColorPalette.Warning)
                .Build();
            dungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(coloredHeader));
            
            // Get theme color for dungeon name to match the entrance screen
            char themeColorCode = DungeonThemeColors.GetThemeColorCode(currentDungeon.Theme);
            var dungeonNameColor = GetColorFromThemeCode(themeColorCode);
            
            var dungeonInfo = new ColoredTextBuilder()
                .Add("Dungeon: ", ColorPalette.Warning)
                .Add(currentDungeon.Name, dungeonNameColor)
                .Build();
            dungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(dungeonInfo));
            
            var levelInfo = new ColoredTextBuilder()
                .Add("Level Range: ", ColorPalette.Warning)
                .Add($"{currentDungeon.MinLevel} - {currentDungeon.MaxLevel}", ColorPalette.Info)
                .Build();
            dungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(levelInfo));
            
            var roomInfo = new ColoredTextBuilder()
                .Add("Total Rooms: ", ColorPalette.Warning)
                .Add(currentDungeon.Rooms.Count.ToString(), ColorPalette.Info)
                .Build();
            dungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(roomInfo));
            dungeonHeaderInfo.Add("");
            
            // Run through all rooms in the dungeon
            foreach (Environment room in currentDungeon.Rooms)
            {
                if (!await ProcessRoom(room))
                {
                    // Player died
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    return;
                }
            }
            
            // Dungeon completed successfully
            CompleteDungeon();
        }

        private async Task<bool> ProcessRoom(Environment room)
        {
            if (currentPlayer == null || combatManager == null) return false;
            
            currentRoom = room;
            
            // Store room info separately so we can reuse it for each enemy encounter in this room
            currentRoomInfo.Clear();
            
            // Create colored room header using new system
            var roomHeaderText = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringRoomHeader);
            var coloredRoomHeader = new ColoredTextBuilder()
                .Add(roomHeaderText, ColorPalette.Warning)
                .Build();
            currentRoomInfo.Add(ColoredTextRenderer.RenderAsPlainText(coloredRoomHeader));
            
            var roomNameInfo = new ColoredTextBuilder()
                .Add("Room: ", ColorPalette.White)
                .Add(room.Name, ColorPalette.White)
                .Build();
            currentRoomInfo.Add(ColoredTextRenderer.RenderAsPlainText(roomNameInfo));
            
            var roomDescription = new ColoredTextBuilder()
                .Add(room.Description, ColorPalette.White)
                .Build();
            currentRoomInfo.Add(ColoredTextRenderer.RenderAsPlainText(roomDescription));
            currentRoomInfo.Add("");
            
            // Clear temporary effects when entering a new room
            currentPlayer.ClearAllTempEffects();
            
            // Process all enemies in the room
            bool roomWasHostile = room.IsHostile;
            while (room.HasLivingEnemies())
            {
                Enemy? currentEnemy = room.GetNextLivingEnemy();
                if (currentEnemy == null) break;
                
                if (!await ProcessEnemyEncounter(currentEnemy))
                {
                    return false; // Player died
                }
            }
            
            // Add room completion message to combat log only if room was hostile (don't clear screen)
            if (roomWasHostile && customUIManager is CanvasUICoordinator canvasUI2)
            {
                canvasUI2.AddRoomClearedMessage();
                await Task.Delay(2000);  // Matches dungeon completion delay
            }
            return true; // Player survived the room
        }

        private async Task<bool> ProcessEnemyEncounter(Enemy enemy)
        {
            if (currentPlayer == null || combatManager == null) return false;
            
            // Build fresh context for this specific encounter (dungeon + room + enemy)
            dungeonLog.Clear();
            dungeonLog.AddRange(dungeonHeaderInfo);
            dungeonLog.AddRange(currentRoomInfo);
            
            // Add current enemy encounter info with color markup
            string enemyWeaponInfo = enemy.Weapon != null 
                ? string.Format(AsciiArtAssets.UIText.WeaponSuffix, enemy.Weapon.Name)
                : "";
            dungeonLog.Add("&Y" + string.Format(AsciiArtAssets.UIText.EncounteredFormat, enemy.Name, enemyWeaponInfo));
            dungeonLog.Add("&C" + AsciiArtAssets.UIText.FormatEnemyStats(enemy.CurrentHealth, enemy.MaxHealth, enemy.Armor));
            dungeonLog.Add("&C" + AsciiArtAssets.UIText.FormatEnemyAttack(enemy.Strength, enemy.Agility, enemy.Technique, enemy.Intelligence));
            dungeonLog.Add("");
            
            // Show accumulated dungeon log with enemy info briefly before combat
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderEnemyEncounter(enemy, currentPlayer, dungeonLog, currentDungeon?.Name, currentRoom?.Name);
            }
            
            await Task.Delay(1500);  // Brief pause to see enemy info
            
            // Prepare for combat - set dungeon context so it persists during combat
            if (customUIManager is CanvasUICoordinator canvasUI2)
            {
                canvasUI2.SetDungeonContext(dungeonLog);
                canvasUI2.SetCurrentEnemy(enemy);  // Track enemy so health bar stays visible
                canvasUI2.SetDungeonName(currentDungeon?.Name);  // Set dungeon name for right panel
                canvasUI2.SetRoomName(currentRoom?.Name);  // Set room name for right panel
                canvasUI2.ResetForNewBattle();
                canvasUI2.SetCharacter(currentPlayer);
                
                // Render the combat screen to show the combat interface
                canvasUI2.RenderCombat(currentPlayer, enemy, dungeonLog);
            }
            
            // Run the actual combat using the original combat system
            // Run synchronously to allow turn-by-turn display in the GUI
            bool playerSurvived = combatManager.RunCombat(currentPlayer, enemy, currentRoom!);
            
            // Get the battle narrative for combat log
            var battleNarrative = combatManager.GetCurrentBattleNarrative();
            
            // Add combat result message to the combat log (don't clear screen)
            if (customUIManager is CanvasUICoordinator canvasUI3)
            {
                if (enemy.CurrentHealth <= 0)
                {
                    // Victory - add message to combat log
                    canvasUI3.AddVictoryMessage(enemy, battleNarrative);
                }
                else if (!playerSurvived)
                {
                    // Defeat - add message to combat log
                    canvasUI3.AddDefeatMessage();
                }
                
                // Keep the message visible for a moment
                await Task.Delay(2000);
                
                // Clear enemy tracking after combat (removes health bar but keeps combat log)
                canvasUI3.ClearCurrentEnemy();
            }
            
            // Check if player survived
            return currentPlayer.CurrentHealth > 0;
        }

        private void CompleteDungeon()
        {
            if (currentPlayer == null || dungeonManager == null) return;
            
            // Award loot and XP and get the rewards
            var (xpGained, lootReceived) = dungeonManager.AwardLootAndXPWithReturns(currentPlayer, currentInventory, availableDungeons);
            
            // Show consolidated dungeon completion screen with statistics and menu choices
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Clear the display buffer to remove combat log before showing completion screen
                canvasUI.ClearDisplayBuffer();
                canvasUI.RenderDungeonCompletion(currentDungeon!, currentPlayer, xpGained, lootReceived);
            }
            
            // Set state to handle input from the completion screen
            currentState = GameState.DungeonCompletion;
        }
        
        /// <summary>
        /// Helper method to convert theme color codes to ColorPalette values
        /// </summary>
        private ColorPalette GetColorFromThemeCode(char themeColorCode)
        {
            return themeColorCode switch
            {
                'R' => ColorPalette.Damage,
                'r' => ColorPalette.DarkRed,
                'G' => ColorPalette.Success,
                'g' => ColorPalette.DarkGreen,
                'B' => ColorPalette.Info,
                'b' => ColorPalette.DarkBlue,
                'Y' => ColorPalette.Warning,
                'C' => ColorPalette.Cyan,
                'c' => ColorPalette.DarkCyan,
                'M' => ColorPalette.Magenta,
                'm' => ColorPalette.DarkMagenta,
                'W' => ColorPalette.White,
                'w' => ColorPalette.Brown,
                'O' => ColorPalette.Orange,
                'o' => ColorPalette.Orange,
                'K' => ColorPalette.Black,
                'k' => ColorPalette.DarkGray,
                _ => ColorPalette.White
            };
        }


        private Task HandleCharacterCreationInput(string input)
        {
            switch (input)
            {
                case "1":
                    // Start Adventure
                    currentState = GameState.GameLoop;
                    ShowGameLoop();
                    break;
                case "2":
                    // Back to Main Menu
                    currentState = GameState.MainMenu;
                    ShowMainMenuWithCustomUI();
                    break;
                default:
                    ShowMessage("Invalid choice. Press 1-2 or ESC to go back.");
                    break;
            }
            
            return Task.CompletedTask;
        }

        private void ShowWeaponSelection()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && gameInitializer != null)
            {
                var startingGear = gameInitializer.LoadStartingGear();
                canvasUI.RenderWeaponSelection(startingGear.weapons);
            }
        }

        private Task HandleWeaponSelectionInput(string input)
        {
            if (currentPlayer == null) return Task.CompletedTask;
            
            if (int.TryParse(input, out int weaponChoice))
            {
                var startingGear = gameInitializer.LoadStartingGear();
                if (weaponChoice >= 1 && weaponChoice <= startingGear.weapons.Count)
                {
                    // Initialize the character with the selected weapon
                    gameInitializer.InitializeNewGame(currentPlayer, availableDungeons, weaponChoice);
                    
                    // Set character in UI manager for persistent display
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.SetCharacter(currentPlayer);
                    }
                    
                    // Show character creation summary
                    currentState = GameState.CharacterCreation;
                    ShowCharacterCreation();
                    return Task.CompletedTask;
                }
            }
            
            // Invalid input
            ShowMessage("Invalid choice. Please select a weapon by number.");
            return Task.CompletedTask;
        }

    }
} 