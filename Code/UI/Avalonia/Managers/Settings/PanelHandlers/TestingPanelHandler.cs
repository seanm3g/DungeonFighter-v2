using System;
using Avalonia.Controls;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Handles wiring for the Testing settings panel
    /// </summary>
    public class TestingPanelHandler : ISettingsPanelHandler
    {
        private readonly CanvasUICoordinator? canvasUI;
        private TextBoxTestRunner? textBoxTestRunner;

        public string PanelType => "Testing";

        public TestingPanelHandler(CanvasUICoordinator? canvasUI)
        {
            this.canvasUI = canvasUI;
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not TestingSettingsPanel testingPanel || canvasUI == null) return;

            // Create TextBox-based test runner for the settings panel
            var outputTextBox = testingPanel.FindControl<TextBox>("TestOutputTextBox");
            var statusTextBlock = testingPanel.FindControl<TextBlock>("TestOutputProgressText");
            textBoxTestRunner = new TextBoxTestRunner(
                outputTextBox,
                statusTextBlock,
                null
            );

            WireUpTestButtons(testingPanel);
        }

        public void LoadSettings(UserControl panel)
        {
            // Testing panel doesn't need to load settings
        }

        private void WireUpTestButtons(TestingSettingsPanel panel)
        {
            if (panel == null || textBoxTestRunner == null) return;

            // Use FindControl to access buttons (consistent with other controls)
            var testActionBlocksButton = panel.FindControl<Button>("TestActionBlocksButton");
            var testDiceRollButton = panel.FindControl<Button>("TestDiceRollButton");
            var testDungeonEnemyButton = panel.FindControl<Button>("TestDungeonEnemyButton");
            var testActionExecutionButton = panel.FindControl<Button>("TestActionExecutionButton");
            var testCharacterSystemButton = panel.FindControl<Button>("TestCharacterSystemButton");
            var testEquipmentSystemButton = panel.FindControl<Button>("TestEquipmentSystemButton");
            var testDungeonRewardsButton = panel.FindControl<Button>("TestDungeonRewardsButton");
            var testLevelUpSystemButton = panel.FindControl<Button>("TestLevelUpSystemButton");
            var testXPSystemButton = panel.FindControl<Button>("TestXPSystemButton");
            var testMultiSourceXPButton = panel.FindControl<Button>("TestMultiSourceXPButton");
            var testSaveLoadSystemButton = panel.FindControl<Button>("TestSaveLoadSystemButton");
            var testGameStateManagementButton = panel.FindControl<Button>("TestGameStateManagementButton");
            var testErrorHandlingButton = panel.FindControl<Button>("TestErrorHandlingButton");
            var testMultiHitButton = panel.FindControl<Button>("TestMultiHitButton");
            var testStatusEffectsButton = panel.FindControl<Button>("TestStatusEffectsButton");
            var testActionImportButton = panel.FindControl<Button>("TestActionImportButton");
            var testActionMechanicsButton = panel.FindControl<Button>("TestActionMechanicsButton");
            var testCombatButton = panel.FindControl<Button>("TestCombatButton");
            var testDungeonButton = panel.FindControl<Button>("TestDungeonButton");
            var testGameplayFlowButton = panel.FindControl<Button>("TestGameplayFlowButton");
            var testAllButton = panel.FindControl<Button>("TestAllButton");
            var testClearOutputButton = panel.FindControl<Button>("TestClearOutputButton");

            // Wire up all test buttons with null checks
            if (testActionBlocksButton != null)
            {
                testActionBlocksButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunActionBlockTestsAsync();
                };
            }

            if (testDiceRollButton != null)
            {
                testDiceRollButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunDiceRollMechanicsTestsAsync();
                };
            }

            if (testDungeonEnemyButton != null)
            {
                testDungeonEnemyButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunDungeonEnemyGenerationTestsAsync();
                };
            }

            if (testActionExecutionButton != null)
            {
                testActionExecutionButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunActionExecutionTestsAsync();
                };
            }

            if (testCharacterSystemButton != null)
            {
                testCharacterSystemButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunCharacterSystemTestsAsync();
                };
            }

            if (testEquipmentSystemButton != null)
            {
                testEquipmentSystemButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunEquipmentSystemTestsAsync();
                };
            }

            if (testDungeonRewardsButton != null)
            {
                testDungeonRewardsButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunDungeonRewardsTestsAsync();
                };
            }

            if (testLevelUpSystemButton != null)
            {
                testLevelUpSystemButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunLevelUpSystemTestsAsync();
                };
            }

            if (testXPSystemButton != null)
            {
                testXPSystemButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunXPSystemTestsAsync();
                };
            }

            if (testMultiSourceXPButton != null)
            {
                testMultiSourceXPButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunMultiSourceXPRewardTestsAsync();
                };
            }

            if (testSaveLoadSystemButton != null)
            {
                testSaveLoadSystemButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunSaveLoadSystemTestsAsync();
                };
            }

            if (testGameStateManagementButton != null)
            {
                testGameStateManagementButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunGameStateManagementTestsAsync();
                };
            }

            if (testErrorHandlingButton != null)
            {
                testErrorHandlingButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunErrorHandlingTestsAsync();
                };
            }

            if (testMultiHitButton != null)
            {
                testMultiHitButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunMultiHitTestsAsync();
                };
            }

            if (testStatusEffectsButton != null)
            {
                testStatusEffectsButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunStatusEffectsTestsAsync();
                };
            }

            if (testActionImportButton != null)
            {
                testActionImportButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunSpreadsheetImportTestsAsync();
                };
            }

            if (testActionMechanicsButton != null)
            {
                testActionMechanicsButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunActionMechanicsTestsAsync();
                };
            }

            if (testCombatButton != null)
            {
                testCombatButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunCombatTestsAsync();
                };
            }

            if (testDungeonButton != null)
            {
                testDungeonButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunDungeonTestsAsync();
                };
            }

            if (testGameplayFlowButton != null)
            {
                testGameplayFlowButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunGameplayFlowTestsAsync();
                };
            }

            if (testAllButton != null)
            {
                testAllButton.Click += async (s, e) =>
                {
                    await textBoxTestRunner.RunAllTestsAsync();
                };
            }

            if (testClearOutputButton != null)
            {
                testClearOutputButton.Click += (s, e) =>
                {
                    textBoxTestRunner.ClearOutput();
                };
            }

            var resyncActionsButton = panel.FindControl<Button>("ResyncActionsButton");
            if (resyncActionsButton != null)
            {
                resyncActionsButton.Click += async (s, e) =>
                {
                    await ResyncActionsFromGoogleSheets(panel);
                };
            }
        }

        private async System.Threading.Tasks.Task ResyncActionsFromGoogleSheets(TestingSettingsPanel panel)
        {
            if (textBoxTestRunner == null) return;

            try
            {
                textBoxTestRunner.AppendOutput("=== Resyncing Actions from Google Sheets ===\n");
                textBoxTestRunner.AppendOutput("Fetching latest actions from Google Sheets...\n");
                
                // Update status text if available
                var statusTextBlock = panel.FindControl<TextBlock>("TestOutputProgressText");
                if (statusTextBlock != null)
                {
                    statusTextBlock.Text = "Resyncing actions...";
                }

                // Call the update service
                await RPGGame.Data.ActionUpdateService.UpdateFromGoogleSheetsAsync();

                textBoxTestRunner.AppendOutput("✓ Successfully updated Actions.json from Google Sheets\n");
                textBoxTestRunner.AppendOutput("Reloading actions...\n");

                // Reload actions
                RPGGame.ActionLoader.LoadActions();
                var actionCount = RPGGame.ActionLoader.GetAllActions().Count;
                
                textBoxTestRunner.AppendOutput($"✓ Actions reloaded successfully ({actionCount} actions loaded)\n");
                textBoxTestRunner.AppendOutput("=== Resync Complete ===\n\n");

                if (statusTextBlock != null)
                {
                    statusTextBlock.Text = $"Resync complete - {actionCount} actions loaded";
                }
            }
            catch (Exception ex)
            {
                textBoxTestRunner.AppendOutput($"✗ Error resyncing actions: {ex.Message}\n");
                textBoxTestRunner.AppendOutput($"{ex.StackTrace}\n\n");
                
                var statusTextBlock = panel.FindControl<TextBlock>("TestOutputProgressText");
                if (statusTextBlock != null)
                {
                    statusTextBlock.Text = $"Error: {ex.Message}";
                }
            }
        }
    }
}

