namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Game.Testing.Commands;

    /// <summary>
    /// Handles testing system menu and test execution.
    /// Extracted from Game.cs to isolate testing concerns.
    /// Refactored to use Command pattern for test menu options.
    /// </summary>
    public class TestingSystemHandler
    {
        private IUIManager? customUIManager;
        private GameStateManager stateManager;
        private TestExecutionCoordinator? testCoordinator;
        private Dictionary<string, ITestCommand> commands;
        private string? currentSubMenu = null; // null = main menu, "System" = system tests, "Item" = item tests
        
        // Delegates
        public delegate void OnShowMainMenu();
        public delegate void OnShowMessage(string message);
        
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowMessage? ShowMessageEvent;

        public TestingSystemHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.testCoordinator = new TestExecutionCoordinator(customUIManager);
            this.commands = new Dictionary<string, ITestCommand>();
            InitializeCommands();
        }

        /// <summary>
        /// Initializes the command dictionary with all test menu commands.
        /// </summary>
        private void InitializeCommands()
        {
            if (customUIManager is not CanvasUICoordinator canvasUI)
                return;

            commands["1"] = new RunAllTestsCommand(canvasUI, testCoordinator, stateManager);
            commands["2"] = new RunSystemTestsCommand(canvasUI, testCoordinator, stateManager, "Character");
            commands["3"] = new RunCombatTestsCommand(canvasUI, testCoordinator, stateManager);
            commands["4"] = new RunInventoryDungeonTestsCommand(canvasUI, testCoordinator, stateManager);
            commands["5"] = new RunDataUITestsCommand(canvasUI, testCoordinator, stateManager);
            commands["6"] = new RunAdvancedTestsCommand(canvasUI, testCoordinator, stateManager);
            commands["7"] = new GenerateRandomItemsCommand(canvasUI, testCoordinator, stateManager);
            commands["8"] = new RunItemGenerationTestCommand(canvasUI, testCoordinator, stateManager);
            commands["9"] = new RunTierDistributionTestCommand(canvasUI, testCoordinator, stateManager);
            commands["10"] = new RunCommonItemModificationTestCommand(canvasUI, testCoordinator, stateManager);
            commands["11"] = new RunColorSystemTestsCommand(canvasUI, testCoordinator, stateManager);
            commands["0"] = new ReturnToSettingsCommand(canvasUI, testCoordinator, stateManager, () => ShowMainMenuEvent?.Invoke());
        }

        /// <summary>
        /// Show testing menu (main menu or sub-menu)
        /// </summary>
        public void ShowTestingMenu(string? subMenu = null)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                currentSubMenu = subMenu;
                canvasUI.RenderTestingMenu(subMenu);
            }
            stateManager.TransitionToState(GameState.Testing);
        }
        
        /// <summary>
        /// Reset to main testing menu (called when entering from settings)
        /// </summary>
        public void ResetToMainMenu()
        {
            currentSubMenu = null;
            ShowTestingMenu(null);
        }

        /// <summary>
        /// Handle testing menu input
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            if (customUIManager is not CanvasUICoordinator canvasUI)
                return;

            // Check if we're waiting for any key to return to test menu
            if (testCoordinator != null && testCoordinator.WaitingForTestMenuReturn)
            {
                // Clear the flag FIRST before showing menu
                testCoordinator.WaitingForTestMenuReturn = false;
                // Show the menu (return to current sub-menu or main menu)
                ShowTestingMenu(currentSubMenu);
                // Don't process this input as a menu selection - it was just to return to menu
                return;
            }

            // Handle sub-menu navigation
            if (currentSubMenu == null)
            {
                // Main menu
                switch (input)
                {
                    case "1":
                        // Run All Tests
                        if (commands.TryGetValue("1", out var allTestsCommand))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await allTestsCommand.ExecuteAsync();
                        }
                        break;
                    case "2":
                        // System Tests sub-menu
                        ShowTestingMenu("System");
                        break;
                    case "3":
                        // Item Tests sub-menu
                        ShowTestingMenu("Item");
                        break;
                    case "4":
                        // Color System Tests (direct execution)
                        if (commands.TryGetValue("11", out var colorCommand))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await colorCommand.ExecuteAsync();
                        }
                        break;
                    case "0":
                        // Back to Settings
                        currentSubMenu = null;
                        if (commands.TryGetValue("0", out var backCommand))
                        {
                            await backCommand.ExecuteAsync();
                        }
                        break;
                    default:
                        ShowMessageEvent?.Invoke("Invalid choice. Please select 1-4 or 0 to return.");
                        break;
                }
            }
            else if (currentSubMenu == "System")
            {
                // System Tests sub-menu
                switch (input)
                {
                    case "1":
                        // Character System Tests
                        if (commands.TryGetValue("2", out var cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "2":
                        // Combat System Tests
                        if (commands.TryGetValue("3", out cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "3":
                        // Inventory & Dungeon Tests
                        if (commands.TryGetValue("4", out cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "4":
                        // Data & UI System Tests
                        if (commands.TryGetValue("5", out cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "5":
                        // Advanced & Integration Tests
                        if (commands.TryGetValue("6", out cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "0":
                        // Back to main test menu
                        ShowTestingMenu(null);
                        break;
                    default:
                        ShowMessageEvent?.Invoke("Invalid choice. Please select 1-5 or 0 to return.");
                        break;
                }
            }
            else if (currentSubMenu == "Item")
            {
                // Item Tests sub-menu
                switch (input)
                {
                    case "1":
                        // Generate 10 Random Items
                        if (commands.TryGetValue("7", out var cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "2":
                        // Item Generation Analysis
                        if (commands.TryGetValue("8", out cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "3":
                        // Tier Distribution Verification
                        if (commands.TryGetValue("9", out cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "4":
                        // Common Item Modification Chance
                        if (commands.TryGetValue("10", out cmd))
                        {
                            TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                            await cmd.ExecuteAsync();
                        }
                        break;
                    case "0":
                        // Back to main test menu
                        ShowTestingMenu(null);
                        break;
                    default:
                        ShowMessageEvent?.Invoke("Invalid choice. Please select 1-4 or 0 to return.");
                        break;
                }
            }
        }

    }
}

