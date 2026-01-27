using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame.Tests.Runners;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages test execution and displays output in a TextBox with batched updates to prevent flicker
    /// </summary>
    public class TextBoxTestRunner
    {
        private readonly TextBox? outputTextBox;
        private readonly TextBlock? statusTextBlock;
        private readonly ProgressBar? progressBar;
        private readonly TestExecutionOrchestrator orchestrator;

        public TextBoxTestRunner(TextBox? outputTextBox, TextBlock? statusTextBlock = null, ProgressBar? progressBar = null)
        {
            this.outputTextBox = outputTextBox;
            this.statusTextBlock = statusTextBlock;
            this.progressBar = progressBar;
            this.orchestrator = new TestExecutionOrchestrator(outputTextBox, statusTextBlock, progressBar);
        }

        /// <summary>
        /// Runs all tests and displays results in the TextBox
        /// </summary>
        public async Task RunAllTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunAllTests(),
                "Running all tests...",
                "All tests complete");
        }

        /// <summary>
        /// Runs action system tests
        /// </summary>
        public async Task RunActionSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunActionSystemTests(),
                "Running action system tests...",
                "Action system tests complete");
        }

        /// <summary>
        /// Runs combat integration tests
        /// </summary>
        public async Task RunCombatTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunCombatTests(),
                "Running combat tests...",
                "Combat tests complete");
        }

        /// <summary>
        /// Runs dungeon-related tests
        /// </summary>
        public async Task RunDungeonTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunDungeonTests(),
                "Running dungeon tests...",
                "Dungeon tests complete");
        }

        /// <summary>
        /// Runs room-related tests (using integration tests as proxy)
        /// </summary>
        public async Task RunRoomTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunIntegrationTests(),
                "Running room tests...",
                "Room tests complete");
        }

        /// <summary>
        /// Runs narrative-related tests (using display system tests as proxy)
        /// </summary>
        public async Task RunNarrativeTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunDisplaySystemTests(),
                "Running narrative tests...",
                "Narrative tests complete");
        }

        /// <summary>
        /// Runs action block tests
        /// </summary>
        public async Task RunActionBlockTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunActionBlockTests(),
                "Running action block tests...",
                "Action block tests complete");
        }

        /// <summary>
        /// Runs dice roll mechanics tests
        /// </summary>
        public async Task RunDiceRollMechanicsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunDiceRollMechanicsTests(),
                "Running dice roll mechanics tests...",
                "Dice roll mechanics tests complete");
        }

        /// <summary>
        /// Runs dungeon and enemy generation tests
        /// </summary>
        public async Task RunDungeonEnemyGenerationTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunDungeonEnemyGenerationTests(),
                "Running dungeon/enemy generation tests...",
                "Dungeon/enemy generation tests complete");
        }

        /// <summary>
        /// Runs action execution tests
        /// </summary>
        public async Task RunActionExecutionTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunActionExecutionTests(),
                "Running action execution tests...",
                "Action execution tests complete");
        }

        /// <summary>
        /// Runs character system tests (including healing with equipment bonuses)
        /// </summary>
        public async Task RunCharacterSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunCharacterSystemTests(),
                "Running character system tests...",
                "Character system tests complete");
        }

        /// <summary>
        /// Runs equipment system tests
        /// </summary>
        public async Task RunEquipmentSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.EquipmentSystemTests.RunAllTests(),
                "Running equipment system tests...",
                "Equipment system tests complete");
        }

        /// <summary>
        /// Runs dungeon rewards tests
        /// </summary>
        public async Task RunDungeonRewardsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.DungeonRewardsTests.RunAllTests(),
                "Running dungeon rewards tests...",
                "Dungeon rewards tests complete");
        }

        /// <summary>
        /// Runs level up system tests
        /// </summary>
        public async Task RunLevelUpSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.LevelUpSystemTests.RunAllTests(),
                "Running level up system tests...",
                "Level up system tests complete");
        }

        /// <summary>
        /// Runs XP system tests
        /// </summary>
        public async Task RunXPSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.XPSystemTests.RunAllTests(),
                "Running XP system tests...",
                "XP system tests complete");
        }

        /// <summary>
        /// Runs multi-source XP reward system tests
        /// </summary>
        public async Task RunMultiSourceXPRewardTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.MultiSourceXPRewardTests.RunAllTests(),
                "Running multi-source XP reward system tests...",
                "Multi-source XP reward system tests complete");
        }

        /// <summary>
        /// Runs save/load system tests
        /// </summary>
        public async Task RunSaveLoadSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.SaveLoadSystemTests.RunAllTests(),
                "Running save/load system tests...",
                "Save/load system tests complete");
        }

        /// <summary>
        /// Runs game state management tests
        /// </summary>
        public async Task RunGameStateManagementTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.GameStateManagementTests.RunAllTests(),
                "Running game state management tests...",
                "Game state management tests complete");
        }

        /// <summary>
        /// Runs error handling tests
        /// </summary>
        public async Task RunErrorHandlingTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Unit.ErrorHandlingTests.RunAllTests(),
                "Running error handling tests...",
                "Error handling tests complete");
        }

        /// <summary>
        /// Runs gameplay flow integration tests
        /// </summary>
        public async Task RunGameplayFlowTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => Tests.Integration.GameplayFlowTests.RunAllTests(),
                "Running gameplay flow tests...",
                "Gameplay flow tests complete");
        }

        /// <summary>
        /// Runs multi-hit tests
        /// </summary>
        public async Task RunMultiHitTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunMultiHitTests(),
                "Running multi-hit tests...",
                "Multi-hit tests complete");
        }

        /// <summary>
        /// Runs status effects tests
        /// </summary>
        public async Task RunStatusEffectsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunStatusEffectsTests(),
                "Running status effects tests...",
                "Status effects tests complete");
        }

        /// <summary>
        /// Clears the output TextBox
        /// </summary>
        public void ClearOutput()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (outputTextBox != null)
                {
                    outputTextBox.Text = string.Empty;
                }
                if (statusTextBlock != null)
                {
                    statusTextBlock.Text = "Ready";
                }
                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }
            });
        }
    }
}
