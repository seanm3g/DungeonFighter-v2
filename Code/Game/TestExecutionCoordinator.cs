using System;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Coordinates test execution with UI, consolidating common test execution patterns
    /// </summary>
    public class TestExecutionCoordinator
    {
        private IUIManager? customUIManager;
        private bool waitingForTestMenuReturn = false;
        
        public TestExecutionCoordinator(IUIManager? customUIManager)
        {
            this.customUIManager = customUIManager;
        }
        
        /// <summary>
        /// Gets or sets whether we're waiting for a key press to return to test menu
        /// </summary>
        public bool WaitingForTestMenuReturn
        {
            get => waitingForTestMenuReturn;
            set => waitingForTestMenuReturn = value;
        }
        
        /// <summary>
        /// Executes a single test action with UI coordination
        /// </summary>
        public async Task<bool> ExecuteTest(Func<Task> testAction, string testName, bool logToConsole = false, Action<CanvasUICoordinator>? preTestAction = null)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                bool result = await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    testAction,
                    testName,
                    logToConsole,
                    preTestAction);
                waitingForTestMenuReturn = true;
                return result;
            }
            return false;
        }
        
        /// <summary>
        /// Executes multiple test actions in sequence
        /// </summary>
        public async Task<bool> ExecuteMultipleTests(string testName, params (Func<Task> action, string name)[] tests)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                bool allPassed = true;
                await TestExecutionHelper.ExecuteTestWithUI(
                    canvasUI,
                    async () =>
                    {
                        foreach (var (action, name) in tests)
                        {
                            await action();
                            if (tests.Length > 1)
                            {
                                canvasUI.WriteBlankLine();
                            }
                        }
                    },
                    testName,
                    logToConsole: true);
                waitingForTestMenuReturn = true;
                return allPassed;
            }
            return false;
        }
        
        /// <summary>
        /// Executes a system test by name
        /// </summary>
        public async Task<bool> ExecuteSystemTest(GameSystemTestRunner testRunner, string systemName)
        {
            return await ExecuteTest(
                async () => await testRunner.RunSystemTests(systemName),
                $"RunSystemTests for '{systemName}'");
        }
        
        /// <summary>
        /// Executes all tests
        /// </summary>
        public async Task<bool> ExecuteAllTests(GameSystemTestRunner testRunner)
        {
            return await ExecuteTest(
                async () => await testRunner.RunAllTests(),
                "RunAllTests",
                logToConsole: true);
        }
        
        /// <summary>
        /// Executes combat tests including UI fixes
        /// </summary>
        public async Task<bool> ExecuteCombatTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                return await ExecuteTest(
                    async () =>
                    {
                        await testRunner.RunSystemTests("Combat");
                        await testRunner.RunSystemTests("CombatUI");
                    },
                    "RunCombatTestsWithUI",
                    logToConsole: true);
            }
            return false;
        }
        
        /// <summary>
        /// Executes inventory and dungeon tests together
        /// </summary>
        public async Task<bool> ExecuteInventoryAndDungeonTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                return await ExecuteTest(
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
            }
            return false;
        }
        
        /// <summary>
        /// Executes data and UI system tests together
        /// </summary>
        public async Task<bool> ExecuteDataAndUITests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                return await ExecuteTest(
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
            }
            return false;
        }
        
        /// <summary>
        /// Executes color system tests
        /// </summary>
        public async Task<bool> ExecuteColorSystemTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                return await ExecuteTest(
                    async () => await testRunner.RunSystemTests("ui"),
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
            }
            return false;
        }
        
        /// <summary>
        /// Executes advanced mechanics and integration tests together
        /// </summary>
        public async Task<bool> ExecuteAdvancedAndIntegrationTests(GameSystemTestRunner testRunner)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                return await ExecuteTest(
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
            }
            return false;
        }
    }
}

