namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

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
                        // Item System Tests
                        await RunSystemTests(testRunner, "Item");
                        break;
                    case "5":
                        // Balance Tests
                        await RunSystemTests(testRunner, "Balance");
                        break;
                    case "0":
                        // Return to Settings
                        stateManager.TransitionToState(GameState.Settings);
                        ShowMainMenuEvent?.Invoke();
                        break;
                    default:
                        ShowMessageEvent?.Invoke("Invalid choice. Please select 1-6 or 0 to return.");
                        break;
                }
            }
        }

        /// <summary>
        /// Run all available tests
        /// </summary>
        private async Task RunAllTests(GameSystemTestRunner testRunner)
        {
            await testRunner.RunAllTests();
            waitingForTestMenuReturn = true;
        }

        /// <summary>
        /// Run system-specific tests
        /// </summary>
        private async Task RunSystemTests(GameSystemTestRunner testRunner, string systemName)
        {
            await testRunner.RunSystemTests(systemName);
            waitingForTestMenuReturn = true;
        }

    }
}

