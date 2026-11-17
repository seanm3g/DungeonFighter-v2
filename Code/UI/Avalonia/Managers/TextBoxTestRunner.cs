using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame.Utils.Testing;

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
        /// Runs dice roll mechanics tests
        /// </summary>
        public async Task RunDiceRollMechanicsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunDiceRollMechanicsTests"),
                "Running dice roll mechanics tests...",
                "Dice roll mechanics tests complete");
        }

        /// <summary>
        /// Runs save/load system tests
        /// </summary>
        public async Task RunSaveLoadSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Unit.SaveLoadSystemTests", "RunAllTests"),
                "Running save/load system tests...",
                "Save/load system tests complete");
        }

        /// <summary>
        /// Runs gameplay flow integration tests
        /// </summary>
        public async Task RunGameplayFlowTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Integration.GameplayFlowTests", "RunAllTests"),
                "Running gameplay flow tests...",
                "Gameplay flow tests complete");
        }

        /// <summary>
        /// Runs multi-hit tests
        /// </summary>
        public async Task RunMultiHitTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunMultiHitTests"),
                "Running multi-hit tests...",
                "Multi-hit tests complete");
        }

        /// <summary>
        /// Runs status effects tests
        /// </summary>
        public async Task RunStatusEffectsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunStatusEffectsTests"),
                "Running status effects tests...",
                "Status effects tests complete");
        }

        /// <summary>
        /// Runs action mechanics tests (all mechanics)
        /// </summary>
        public async Task RunActionMechanicsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunActionMechanicsTests"),
                "Running action mechanics tests (all)...",
                "Action mechanics tests complete");
        }

        /// <summary>
        /// Runs combat mechanics tests (damage, hit, speed, thresholds).
        /// </summary>
        public async Task RunCombatMechanicsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunCombatMechanicsTests"),
                "Running combat mechanics tests...",
                "Combat mechanics tests complete");
        }

        /// <summary>
        /// Runs progression tests (level-up, XP, multi-source XP).
        /// </summary>
        public async Task RunProgressionTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunProgressionTests"),
                "Running progression tests...",
                "Progression tests complete");
        }

        /// <summary>
        /// Runs dungeon and rewards tests (enemy generation, loot, XP).
        /// </summary>
        public async Task RunDungeonAndRewardsTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunDungeonAndRewardsTests"),
                "Running dungeon & rewards tests...",
                "Dungeon & rewards tests complete");
        }

        /// <summary>
        /// Runs combo system tests.
        /// </summary>
        public async Task RunComboSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunComboSystemTests"),
                "Running combo tests...",
                "Combo tests complete");
        }

        /// <summary>
        /// Runs only game mechanics and reliability tests (no loaders or UI tests).
        /// </summary>
        public async Task RunMechanicsAndReliabilityTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => TestAssembly.Run("Runners.ComprehensiveTestRunner", "RunMechanicsAndReliabilityTests"),
                "Running all mechanics & reliability tests...",
                "Mechanics & reliability tests complete");
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

        /// <summary>
        /// Appends text to the output TextBox
        /// </summary>
        public void AppendOutput(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (outputTextBox != null)
                {
                    outputTextBox.Text += text;
                    outputTextBox.CaretIndex = outputTextBox.Text.Length;
                }
            });
        }
    }
}
