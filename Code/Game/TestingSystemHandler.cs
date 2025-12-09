namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Avalonia.Media;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Handles testing system menu and test execution.
    /// Extracted from Game.cs to isolate testing concerns.
    /// </summary>
    public class TestingSystemHandler
    {
        private IUIManager? customUIManager;
        private GameStateManager stateManager;
        private bool waitingForTestMenuReturn = false;
        
        // Delegates
        public delegate void OnShowMainMenu();
        public delegate void OnShowMessage(string message);
        
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowMessage? ShowMessageEvent;

        public TestingSystemHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Show testing menu
        /// </summary>
        public void ShowTestingMenu()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderTestingMenu();
            }
            stateManager.TransitionToState(GameState.Testing);
        }

        /// <summary>
        /// Handle testing menu input
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            DebugLogger.Log("TestingSystemHandler", $"HandleMenuInput called with input: '{input}', state={stateManager.CurrentState}");
            ScrollDebugLogger.Log($"TestingSystemHandler.HandleMenuInput: input='{input}', state={stateManager.CurrentState}, waitingForTestMenuReturn={waitingForTestMenuReturn}");
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Check if we're waiting for any key to return to test menu
                if (waitingForTestMenuReturn)
                {
                    DebugLogger.Log("TestingSystemHandler", "Waiting for return - showing testing menu");
                    ScrollDebugLogger.Log("TestingSystemHandler: Waiting for return - showing testing menu");
                    // Clear the flag FIRST before showing menu
                    waitingForTestMenuReturn = false;
                    // Show the menu
                    ShowTestingMenu();
                    // Don't process this input as a menu selection - it was just to return to menu
                    return;
                }
                
                var testRunner = new GameSystemTestRunner(canvasUI);
                
                DebugLogger.Log("TestingSystemHandler", $"Processing input '{input}' in switch statement");
                ScrollDebugLogger.Log($"TestingSystemHandler: Processing input '{input}' in switch statement");
                
                switch (input)
                {
                    case "1":
                        // Run All Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunAllTests(testRunner);
                        break;
                    case "2":
                        // Character System Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunSystemTests(testRunner, "Character");
                        break;
                    case "3":
                        // Combat System Tests (includes Combat UI Fixes)
                        canvasUI.ClearDisplayBuffer();
                        await RunCombatTestsWithUI(testRunner);
                        break;
                    case "4":
                        // Inventory & Dungeon Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunInventoryAndDungeonTests(testRunner);
                        break;
                    case "5":
                        // Data & UI System Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunDataAndUITests(testRunner);
                        break;
                    case "6":
                        // Advanced & Integration Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunAdvancedAndIntegrationTests(testRunner);
                        break;
                    case "7":
                        // Generate 10 Random Items
                        canvasUI.ClearDisplayBuffer();
                        await GenerateRandomItems();
                        break;
                    case "0":
                        // Return to Settings
                        DebugLogger.Log("TestingSystemHandler", "Returning to Settings");
                        ScrollDebugLogger.Log("TestingSystemHandler: Returning to Settings");
                        stateManager.TransitionToState(GameState.Settings);
                        ShowMainMenuEvent?.Invoke();
                        break;
                    default:
                        DebugLogger.Log("TestingSystemHandler", $"Invalid input: '{input}'");
                        ScrollDebugLogger.Log($"TestingSystemHandler: Invalid input '{input}'");
                        ShowMessageEvent?.Invoke("Invalid choice. Please select 1-7 or 0 to return.");
                        break;
                }
            }
        }

        /// <summary>
        /// Run all available tests
        /// </summary>
        private async Task RunAllTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                Console.WriteLine("[TestingSystemHandler] Starting RunAllTests");
                DebugLogger.Log("TestingSystemHandler", "Starting RunAllTests");
                try
                {
                    await testRunner.RunAllTests();
                    Console.WriteLine("[TestingSystemHandler] RunAllTests completed");
                    DebugLogger.Log("TestingSystemHandler", "RunAllTests completed");
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== Tests Complete ===", UIMessageType.System);
                    canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
                    // Render the display buffer to show test results
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunAllTests: {ex.Message}");
                    DebugLogger.Log("TestingSystemHandler", $"Error in RunAllTests: {ex.Message}");
                    canvasUI.WriteLine($"Error running tests: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
        }

        /// <summary>
        /// Run system-specific tests
        /// </summary>
        private async Task RunSystemTests(GameSystemTestRunner testRunner, string systemName)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                DebugLogger.Log("TestingSystemHandler", $"Starting RunSystemTests for '{systemName}'");
                try
                {
                    await testRunner.RunSystemTests(systemName);
                    DebugLogger.Log("TestingSystemHandler", $"RunSystemTests for '{systemName}' completed");
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== Tests Complete ===", UIMessageType.System);
                    canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
                    // Render the display buffer to show test results
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunSystemTests for '{systemName}': {ex.Message}");
                    DebugLogger.Log("TestingSystemHandler", $"Error in RunSystemTests for '{systemName}': {ex.Message}");
                    canvasUI.WriteLine($"Error running tests: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
        }

        /// <summary>
        /// Run combat tests including UI fixes
        /// </summary>
        private async Task RunCombatTestsWithUI(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                Console.WriteLine("[TestingSystemHandler] Starting RunCombatTestsWithUI");
                DebugLogger.Log("TestingSystemHandler", "Starting RunCombatTestsWithUI");
                try
                {
                    // Run standard combat tests
                    await testRunner.RunSystemTests("Combat");
                    // Also run combat UI fixes as part of combat system
                    await testRunner.RunSystemTests("CombatUI");
                    Console.WriteLine("[TestingSystemHandler] RunCombatTestsWithUI completed");
                    DebugLogger.Log("TestingSystemHandler", "RunCombatTestsWithUI completed");
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== Tests Complete ===", UIMessageType.System);
                    canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
                    // Render the display buffer to show test results
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunCombatTestsWithUI: {ex.Message}");
                    DebugLogger.Log("TestingSystemHandler", $"Error in RunCombatTestsWithUI: {ex.Message}");
                    canvasUI.WriteLine($"Error running tests: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
        }

        /// <summary>
        /// Run inventory and dungeon tests together
        /// </summary>
        private async Task RunInventoryAndDungeonTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                DebugLogger.Log("TestingSystemHandler", "Starting RunInventoryAndDungeonTests");
                try
                {
                    canvasUI.WriteLine("=== INVENTORY & DUNGEON TESTS ===", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    await testRunner.RunSystemTests("Inventory");
                    canvasUI.WriteBlankLine();
                    await testRunner.RunSystemTests("Dungeon");
                    DebugLogger.Log("TestingSystemHandler", "RunInventoryAndDungeonTests completed");
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== Tests Complete ===", UIMessageType.System);
                    canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunInventoryAndDungeonTests: {ex.Message}");
                    DebugLogger.Log("TestingSystemHandler", $"Error in RunInventoryAndDungeonTests: {ex.Message}");
                    canvasUI.WriteLine($"Error running tests: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
        }

        /// <summary>
        /// Run data and UI system tests together
        /// </summary>
        private async Task RunDataAndUITests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                DebugLogger.Log("TestingSystemHandler", "Starting RunDataAndUITests");
                try
                {
                    canvasUI.WriteLine("=== DATA & UI SYSTEM TESTS ===", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    await testRunner.RunSystemTests("Data");
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== UI SYSTEM TESTS ===", UIMessageType.System);
                    canvasUI.WriteLine("Starting UI system tests...");
                    canvasUI.WriteBlankLine();
                    await testRunner.RunSystemTests("ui");
                    DebugLogger.Log("TestingSystemHandler", "RunDataAndUITests completed");
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== Tests Complete ===", UIMessageType.System);
                    canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunDataAndUITests: {ex.Message}");
                    DebugLogger.Log("TestingSystemHandler", $"Error in RunDataAndUITests: {ex.Message}");
                    canvasUI.WriteLine($"Error running tests: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
        }

        /// <summary>
        /// Run advanced mechanics and integration tests together
        /// </summary>
        private async Task RunAdvancedAndIntegrationTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                DebugLogger.Log("TestingSystemHandler", "Starting RunAdvancedAndIntegrationTests");
                try
                {
                    canvasUI.WriteLine("=== ADVANCED & INTEGRATION TESTS ===", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    await testRunner.RunSystemTests("AdvancedMechanics");
                    canvasUI.WriteBlankLine();
                    await testRunner.RunSystemTests("Integration");
                    DebugLogger.Log("TestingSystemHandler", "RunAdvancedAndIntegrationTests completed");
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== Tests Complete ===", UIMessageType.System);
                    canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunAdvancedAndIntegrationTests: {ex.Message}");
                    DebugLogger.Log("TestingSystemHandler", $"Error in RunAdvancedAndIntegrationTests: {ex.Message}");
                    canvasUI.WriteLine($"Error running tests: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
        }

        /// <summary>
        /// Generates and displays 10 random items on the screen
        /// </summary>
        private async Task GenerateRandomItems()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                DebugLogger.Log("TestingSystemHandler", "Starting GenerateRandomItems");
                try
                {
                    canvasUI.WriteLine("=== GENERATING 10 RANDOM ITEMS ===", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    
                    // Get player level for item generation (use level 10 as default if no player)
                    int playerLevel = stateManager.CurrentPlayer?.Level ?? 10;
                    int dungeonLevel = 1;
                    
                    // Generate 10 random items
                    var items = new List<Item>();
                    for (int i = 0; i < 10; i++)
                    {
                        var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel, stateManager.CurrentPlayer, guaranteedLoot: true);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                    
                    if (items.Count == 0)
                    {
                        canvasUI.WriteLine("Error: No items were generated.", UIMessageType.System);
                        canvasUI.RenderDisplayBuffer();
                        waitingForTestMenuReturn = true;
                        return;
                    }
                    
                    canvasUI.WriteLine($"Generated {items.Count} random items:", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    
                    // Display each item with proper formatting
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        
                        // Display item number and name with proper colored text
                        string displayType = ItemDisplayFormatter.GetDisplayType(item);
                        var coloredNameSegments = ItemDisplayFormatter.GetColoredFullItemNameNew(item);
                        
                        // Build the colored text line: "1. (Head) [colored item name]"
                        var itemLineBuilder = new ColoredTextBuilder();
                        itemLineBuilder.Add($"{i + 1}. ({displayType}) ", Colors.White);
                        itemLineBuilder.AddRange(coloredNameSegments);
                        canvasUI.WriteLineColoredSegments(itemLineBuilder.Build(), UIMessageType.System);
                        
                        // Display item stats
                        string itemStats = ItemDisplayFormatter.GetItemStatsDisplay(item, stateManager.CurrentPlayer ?? new Character());
                        if (!string.IsNullOrEmpty(itemStats))
                        {
                            canvasUI.WriteLine($"   {itemStats}", UIMessageType.System);
                        }
                        
                        // Display bonuses if any
                        if (item.StatBonuses.Count > 0 || item.ActionBonuses.Count > 0 || item.Modifications.Count > 0)
                        {
                            // Format bonuses using the formatter (it handles indentation internally)
                            ItemDisplayFormatter.FormatItemBonusesWithColor(item, (line) => 
                            {
                                canvasUI.WriteLine(line, UIMessageType.System);
                            });
                        }
                        
                        canvasUI.WriteBlankLine();
                    }
                    
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("=== Item Generation Complete ===", UIMessageType.System);
                    canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
                    
                    // Render the display buffer to show items
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                    
                    DebugLogger.Log("TestingSystemHandler", "GenerateRandomItems completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in GenerateRandomItems: {ex.Message}");
                    DebugLogger.Log("TestingSystemHandler", $"Error in GenerateRandomItems: {ex.Message}");
                    canvasUI.WriteLine($"Error generating items: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
            
            await Task.CompletedTask;
        }

    }
}

