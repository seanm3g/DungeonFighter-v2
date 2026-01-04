using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia;
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
            // Run tests and write summary through console (while capture is active)
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunAllTests(displaySummary: false),
                "Running all tests...",
                "All tests complete",
                getSummaryAfter: () => ComprehensiveTestRunner.GetOverallSummary());
        }

        /// <summary>
        /// Appends the test summary directly to the TextBox after all tests complete
        /// </summary>
        private async Task AppendSummaryDirectly()
        {
            try
            {
                // Get the summary on a background thread
                var summary = await Task.Run(() =>
                {
                    try
                    {
                        var result = ComprehensiveTestRunner.GetOverallSummary();
                        // Add a clear separator before the summary to make it visible
                        if (!string.IsNullOrEmpty(result))
                        {
                            // Ensure summary starts with newlines for separation
                            if (!result.StartsWith("\n"))
                            {
                                result = "\n\n" + result;
                            }
                        }
                        return result;
                    }
                    catch (Exception ex)
                    {
                        // Return error message if summary generation fails
                        return $"\n\n{new string('=', 60)}\n⚠️ Error generating summary: {ex.Message}\n{ex.StackTrace}\n{new string('=', 60)}";
                    }
                });
                
                if (string.IsNullOrEmpty(summary))
                {
                    // If summary is empty, append a message indicating this
                    summary = $"\n\n{new string('=', 60)}\n⚠️ Summary could not be generated (no test results collected)\nThis may indicate tests did not record results properly.\n{new string('=', 60)}";
                }
                
                // Append directly to TextBox on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        if (outputTextBox != null)
                        {
                            var currentText = outputTextBox.Text ?? string.Empty;
                            
                            // Get current length for debugging
                            var lengthBefore = currentText.Length;
                            
                            // Ensure we append to the end
                            // Don't trim - just append the summary as-is
                            outputTextBox.Text = currentText + summary;
                            
                            // Auto-scroll to bottom to show the summary
                            outputTextBox.CaretIndex = outputTextBox.Text.Length;
                            
                            // Verify the summary was actually appended
                            var lengthAfter = outputTextBox.Text.Length;
                            if (lengthAfter <= lengthBefore)
                            {
                                // Summary wasn't appended - try again with force
                                System.Diagnostics.Debug.WriteLine($"Warning: Summary may not have been appended. Before: {lengthBefore}, After: {lengthAfter}");
                                outputTextBox.Text = currentText + "\n\n[SUMMARY APPENDED]" + summary;
                                outputTextBox.CaretIndex = outputTextBox.Text.Length;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't crash
                        System.Diagnostics.Debug.WriteLine($"Error appending summary to TextBox: {ex.Message}\n{ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                // Final fallback - try to append error message
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (outputTextBox != null)
                    {
                        var currentText = outputTextBox.Text ?? string.Empty;
                        var errorMsg = $"\n\n{new string('=', 60)}\n⚠️ Error appending summary: {ex.Message}\n{ex.StackTrace}\n{new string('=', 60)}";
                        outputTextBox.Text = currentText + errorMsg;
                        outputTextBox.CaretIndex = outputTextBox.Text.Length;
                    }
                });
            }
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
        /// Runs combo system tests (including action sequence tests)
        /// </summary>
        public async Task RunComboSystemTestsAsync()
        {
            await orchestrator.RunTestAsync(
                () => ComprehensiveTestRunner.RunComboSystemTests(),
                "Running combo system tests...",
                "Combo system tests complete");
        }

        /// <summary>
        /// Copies the output TextBox content to the clipboard
        /// </summary>
        public async Task CopyOutputAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    if (outputTextBox != null && !string.IsNullOrEmpty(outputTextBox.Text))
                    {
                        // Get the clipboard from the top-level window
                        var topLevel = TopLevel.GetTopLevel(outputTextBox);
                        if (topLevel?.Clipboard != null)
                        {
                            await topLevel.Clipboard.SetTextAsync(outputTextBox.Text);
                            // Update status to show copy was successful
                            if (statusTextBlock != null)
                            {
                                var originalText = statusTextBlock.Text;
                                statusTextBlock.Text = "Copied to clipboard!";
                                // Reset status after 2 seconds
                                _ = Task.Run(async () =>
                                {
                                    await Task.Delay(2000);
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        if (statusTextBlock != null)
                                        {
                                            statusTextBlock.Text = originalText;
                                        }
                                    });
                                });
                            }
                        }
                        else
                        {
                            if (statusTextBlock != null)
                            {
                                statusTextBlock.Text = "Clipboard not available";
                            }
                        }
                    }
                    else
                    {
                        if (statusTextBlock != null)
                        {
                            statusTextBlock.Text = "No text to copy";
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't crash
                    System.Diagnostics.Debug.WriteLine($"Error copying to clipboard: {ex.Message}");
                    if (statusTextBlock != null)
                    {
                        statusTextBlock.Text = $"Error: {ex.Message}";
                    }
                }
            });
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
