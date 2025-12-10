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
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () => await testRunner.RunAllTests(),
                    "RunAllTests",
                    logToConsole: true);
                waitingForTestMenuReturn = true;
            }
        }

        /// <summary>
        /// Run system-specific tests
        /// </summary>
        private async Task RunSystemTests(GameSystemTestRunner testRunner, string systemName)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () => await testRunner.RunSystemTests(systemName),
                    $"RunSystemTests for '{systemName}'");
                waitingForTestMenuReturn = true;
            }
        }

        /// <summary>
        /// Run combat tests including UI fixes
        /// </summary>
        private async Task RunCombatTestsWithUI(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () =>
                    {
                        // Run standard combat tests
                        await testRunner.RunSystemTests("Combat");
                        // Also run combat UI fixes as part of combat system
                        await testRunner.RunSystemTests("CombatUI");
                    },
                    "RunCombatTestsWithUI",
                    logToConsole: true);
                waitingForTestMenuReturn = true;
            }
        }

        /// <summary>
        /// Run inventory and dungeon tests together
        /// </summary>
        private async Task RunInventoryAndDungeonTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () =>
                    {
                        await testRunner.RunSystemTests("Inventory");
                        canvasUI.WriteBlankLine();
                        await testRunner.RunSystemTests("Dungeon");
                    },
                    "RunInventoryAndDungeonTests",
                    preTestAction: (ui) =>
                    {
                        ui.WriteLine("=== INVENTORY & DUNGEON TESTS ===", UIMessageType.System);
                        ui.WriteBlankLine();
                    });
                waitingForTestMenuReturn = true;
            }
        }

        /// <summary>
        /// Run data and UI system tests together
        /// </summary>
        private async Task RunDataAndUITests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () =>
                    {
                        await testRunner.RunSystemTests("Data");
                        canvasUI.WriteBlankLine();
                        canvasUI.WriteLine("=== UI SYSTEM TESTS ===", UIMessageType.System);
                        canvasUI.WriteLine("Starting UI system tests...");
                        canvasUI.WriteBlankLine();
                        await testRunner.RunSystemTests("ui");
                    },
                    "RunDataAndUITests",
                    preTestAction: (ui) =>
                    {
                        ui.WriteLine("=== DATA & UI SYSTEM TESTS ===", UIMessageType.System);
                        ui.WriteBlankLine();
                    });
                waitingForTestMenuReturn = true;
            }
        }

        /// <summary>
        /// Run advanced mechanics and integration tests together
        /// </summary>
        private async Task RunAdvancedAndIntegrationTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () =>
                    {
                        await testRunner.RunSystemTests("AdvancedMechanics");
                        canvasUI.WriteBlankLine();
                        await testRunner.RunSystemTests("Integration");
                    },
                    "RunAdvancedAndIntegrationTests",
                    preTestAction: (ui) =>
                    {
                        ui.WriteLine("=== ADVANCED & INTEGRATION TESTS ===", UIMessageType.System);
                        ui.WriteBlankLine();
                    });
                waitingForTestMenuReturn = true;
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
                    
                    // Use helper to display items
                    RandomItemDisplayHelper.DisplayItems(canvasUI, items, stateManager.CurrentPlayer);
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

