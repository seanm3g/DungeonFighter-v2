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
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Check if we're waiting for any key to return to test menu
                if (waitingForTestMenuReturn)
                {
                    // Clear the flag FIRST before showing menu
                    waitingForTestMenuReturn = false;
                    // Show the menu
                    ShowTestingMenu();
                    // Don't process this input as a menu selection - it was just to return to menu
                    return;
                }
                
                var testRunner = new GameSystemTestRunner(canvasUI);
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
                    case "8":
                        // Item Generation Analysis Test
                        canvasUI.ClearDisplayBuffer();
                        await RunItemGenerationTest();
                        break;
                    case "9":
                        // Tier Distribution Verification Test
                        canvasUI.ClearDisplayBuffer();
                        await RunTierDistributionTest();
                        break;
                    case "10":
                        // Common Item Modification Test
                        canvasUI.ClearDisplayBuffer();
                        await RunCommonItemModificationTest();
                        break;
                    case "11":
                        // Color System Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunColorSystemTests(testRunner);
                        break;
                    case "0":
                        // Return to Settings
                        stateManager.TransitionToState(GameState.Settings);
                        ShowMainMenuEvent?.Invoke();
                        break;
                    default:
                        ShowMessageEvent?.Invoke("Invalid choice. Please select 1-11 or 0 to return.");
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
        /// Run comprehensive color system tests
        /// </summary>
        private async Task RunColorSystemTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () =>
                    {
                        // Run all UI system tests which includes comprehensive color tests:
                        // - Color Palette System
                        // - Color Pattern System
                        // - Color Application
                        // - Keyword Coloring
                        // - Damage & Healing Colors
                        // - Rarity Colors
                        // - Status Effect Colors
                        // - Colored Text Visual Tests
                        await testRunner.RunSystemTests("ui");
                    },
                    "RunColorSystemTests",
                    preTestAction: (ui) =>
                    {
                        ui.WriteLine("=== COLOR SYSTEM TESTS ===", UIMessageType.System);
                        ui.WriteLine("Testing all color system features:", UIMessageType.System);
                        ui.WriteLine("• Color Palette System", UIMessageType.System);
                        ui.WriteLine("• Color Pattern System", UIMessageType.System);
                        ui.WriteLine("• Color Application", UIMessageType.System);
                        ui.WriteLine("• Keyword Coloring", UIMessageType.System);
                        ui.WriteLine("• Damage & Healing Colors", UIMessageType.System);
                        ui.WriteLine("• Rarity Colors", UIMessageType.System);
                        ui.WriteLine("• Status Effect Colors", UIMessageType.System);
                        ui.WriteLine("• Colored Text Visual Tests", UIMessageType.System);
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in GenerateRandomItems: {ex.Message}");
                    canvasUI.WriteLine($"Error generating items: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Runs the item generation test
        /// </summary>
        private async Task RunItemGenerationTest()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                try
                {
                    canvasUI.WriteLine("=== ITEM GENERATION ANALYSIS TEST ===", UIMessageType.System);
                    canvasUI.WriteLine("This will generate 100 items at each level from 1-20 and analyze the results.", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("Starting test (this may take a moment)...", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    canvasUI.RenderDisplayBuffer();
                    
                    // Capture console output
                    var originalOut = Console.Out;
                    using (var stringWriter = new System.IO.StringWriter())
                    {
                        Console.SetOut(stringWriter);
                        
                        try
                        {
                            // Run the test - note: this will block on Console.ReadLine() calls
                            // We'll need to modify TestManager to skip user prompts in UI mode
                            await Task.Run(() =>
                            {
                                // Temporarily redirect Console.ReadKey to auto-continue
                                TestManager.RunItemGenerationTest();
                            });
                            
                            string output = stringWriter.ToString();
                            
                            // Display captured output
                            foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                            {
                                if (!string.IsNullOrWhiteSpace(line) && !line.Contains("Press any key"))
                                {
                                    canvasUI.WriteLine(line, UIMessageType.System);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                            if (ex.StackTrace != null)
                            {
                                canvasUI.WriteLine($"Stack trace: {ex.StackTrace}", UIMessageType.System);
                            }
                        }
                        finally
                        {
                            Console.SetOut(originalOut);
                        }
                    }
                    
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("Test completed! Results saved to 'item_generation_test_results.txt'", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunItemGenerationTest: {ex.Message}");
                    canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Runs the tier distribution test
        /// </summary>
        private async Task RunTierDistributionTest()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                try
                {
                    canvasUI.WriteLine("=== TIER DISTRIBUTION VERIFICATION TEST ===", UIMessageType.System);
                    canvasUI.WriteLine("Testing tier distribution across various player/dungeon level scenarios.", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("Starting test (this may take a moment)...", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    canvasUI.RenderDisplayBuffer();
                    
                    // Capture console output
                    var originalOut = Console.Out;
                    using (var stringWriter = new System.IO.StringWriter())
                    {
                        Console.SetOut(stringWriter);
                        
                        try
                        {
                            await Task.Run(() =>
                            {
                                TierDistributionTest.TestTierDistribution();
                            });
                            
                            string output = stringWriter.ToString();
                            
                            // Display captured output
                            foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    canvasUI.WriteLine(line, UIMessageType.System);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                            if (ex.StackTrace != null)
                            {
                                canvasUI.WriteLine($"Stack trace: {ex.StackTrace}", UIMessageType.System);
                            }
                        }
                        finally
                        {
                            Console.SetOut(originalOut);
                        }
                    }
                    
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("Test completed!", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunTierDistributionTest: {ex.Message}");
                    canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Runs the common item modification test
        /// </summary>
        private async Task RunCommonItemModificationTest()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                try
                {
                    canvasUI.WriteLine("=== COMMON ITEM MODIFICATION TEST ===", UIMessageType.System);
                    canvasUI.WriteLine("This will generate 1000 Common items and verify the 25% chance for modifications.", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("Starting test (this may take a moment)...", UIMessageType.System);
                    canvasUI.WriteBlankLine();
                    canvasUI.RenderDisplayBuffer();
                    
                    // Capture console output
                    var originalOut = Console.Out;
                    using (var stringWriter = new System.IO.StringWriter())
                    {
                        Console.SetOut(stringWriter);
                        
                        try
                        {
                            // Run the test - note: this will block on Console.ReadLine() calls
                            await Task.Run(() =>
                            {
                                TestManager.RunCommonItemModificationTest();
                            });
                            
                            string output = stringWriter.ToString();
                            
                            // Display captured output
                            foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                            {
                                if (!string.IsNullOrWhiteSpace(line) && !line.Contains("Press any key"))
                                {
                                    canvasUI.WriteLine(line, UIMessageType.System);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                            if (ex.StackTrace != null)
                            {
                                canvasUI.WriteLine($"Stack trace: {ex.StackTrace}", UIMessageType.System);
                            }
                        }
                        finally
                        {
                            Console.SetOut(originalOut);
                        }
                    }
                    
                    canvasUI.WriteBlankLine();
                    canvasUI.WriteLine("Test completed!", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestingSystemHandler] Error in RunCommonItemModificationTest: {ex.Message}");
                    canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                    canvasUI.RenderDisplayBuffer();
                    waitingForTestMenuReturn = true;
                }
            }
            
            await Task.CompletedTask;
        }

    }
}

