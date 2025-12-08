namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

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
                        // Inventory System Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunSystemTests(testRunner, "Inventory");
                        break;
                    case "5":
                        // Dungeon System Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunSystemTests(testRunner, "Dungeon");
                        break;
                    case "6":
                        // Data System Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunSystemTests(testRunner, "Data");
                        break;
                    case "7":
                        // UI System Tests
                        canvasUI.ClearDisplayBuffer();
                        canvasUI.WriteLine("=== UI SYSTEM TESTS ===");
                        canvasUI.WriteLine("Starting UI system tests...");
                        canvasUI.WriteBlankLine();
                        await RunSystemTests(testRunner, "ui");
                        break;
                    case "8":
                        // Advanced Mechanics Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunSystemTests(testRunner, "AdvancedMechanics");
                        break;
                    case "9":
                        // Integration Tests
                        canvasUI.ClearDisplayBuffer();
                        await RunSystemTests(testRunner, "Integration");
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
                        ShowMessageEvent?.Invoke("Invalid choice. Please select 1-9 or 0 to return.");
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

    }
}

