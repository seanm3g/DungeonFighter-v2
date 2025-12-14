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
            if (customUIManager is not CanvasUICoordinator canvasUI)
                return;

            // Check if we're waiting for any key to return to test menu
            if (testCoordinator != null && testCoordinator.WaitingForTestMenuReturn)
            {
                // Clear the flag FIRST before showing menu
                testCoordinator.WaitingForTestMenuReturn = false;
                // Show the menu
                ShowTestingMenu();
                // Don't process this input as a menu selection - it was just to return to menu
                return;
            }

            // Find and execute the command
            if (commands.TryGetValue(input, out var command))
            {
                TestUICoordinator.ClearAndPrepareForTest(canvasUI);
                await command.ExecuteAsync();
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid choice. Please select 1-11 or 0 to return.");
            }
        }

    }
}

