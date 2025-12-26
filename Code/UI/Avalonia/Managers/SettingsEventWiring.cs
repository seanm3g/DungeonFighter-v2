using Avalonia.Controls;
using RPGGame.UI.Avalonia.Helpers;
using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles event wiring for SettingsPanel controls.
    /// Extracted from SettingsPanel to improve Single Responsibility Principle compliance.
    /// </summary>
    public class SettingsEventWiring
    {
        private readonly SettingsEventManager eventManager;

        public SettingsEventWiring(
            Func<string, Task> executeTest,
            Func<int, Task> runBattleTest,
            Func<Task> runWeaponTypeTests,
            Func<Task> runComprehensiveWeaponEnemyTests,
            System.Action onSave,
            System.Action onReset,
            System.Action onBack,
            System.Action onBattleStatisticsClick)
        {
            eventManager = new SettingsEventManager(
                executeTest,
                runBattleTest,
                runWeaponTypeTests,
                runComprehensiveWeaponEnemyTests,
                onSave,
                onReset,
                onBack,
                onBattleStatisticsClick);
        }

        /// <summary>
        /// Wires up all events for the settings panel controls
        /// </summary>
        public void WireUpAllEvents(
            UserControl settingsPanel,
            Slider narrativeBalanceSlider,
            TextBox narrativeBalanceTextBox,
            Slider combatSpeedSlider,
            TextBox combatSpeedTextBox,
            Slider enemyHealthMultiplierSlider,
            TextBox enemyHealthMultiplierTextBox,
            Slider enemyDamageMultiplierSlider,
            TextBox enemyDamageMultiplierTextBox,
            Slider playerHealthMultiplierSlider,
            TextBox playerHealthMultiplierTextBox,
            Slider playerDamageMultiplierSlider,
            TextBox playerDamageMultiplierTextBox,
            Slider brightnessMaskIntensitySlider,
            TextBox brightnessMaskIntensityTextBox,
            Slider brightnessMaskWaveLengthSlider,
            TextBox brightnessMaskWaveLengthTextBox,
            Slider undulationSpeedSlider,
            TextBox undulationSpeedTextBox,
            Slider undulationWaveLengthSlider,
            TextBox undulationWaveLengthTextBox,
            Button saveButton,
            Button resetButton,
            Button backButton,
            Button runAllTestsButton,
            Button colorSystemTestsButton,
            Button characterSystemTestsButton,
            Button combatSystemTestsButton,
            Button inventoryDungeonTestsButton,
            Button dataUITestsButton,
            Button actionSystemTestsButton,
            Button advancedIntegrationTestsButton,
            Button combatLogFilteringTestsButton,
            Button generateRandomItemsButton,
            Button itemGenerationAnalysisButton,
            Button tierDistributionTestButton,
            Button commonItemModificationTestButton,
            Button actionEditorTestsButton)
        {
            // Fix TextBox focus styling to prevent white-on-white text
            TextBoxStylingHelper.FixTextBoxFocusStyling(settingsPanel);
            
            // Wire up slider events
            eventManager.WireUpSliderEvents(
                narrativeBalanceSlider,
                narrativeBalanceTextBox,
                combatSpeedSlider,
                combatSpeedTextBox,
                enemyHealthMultiplierSlider,
                enemyHealthMultiplierTextBox,
                enemyDamageMultiplierSlider,
                enemyDamageMultiplierTextBox,
                playerHealthMultiplierSlider,
                playerHealthMultiplierTextBox,
                playerDamageMultiplierSlider,
                playerDamageMultiplierTextBox,
                brightnessMaskIntensitySlider,
                brightnessMaskIntensityTextBox,
                brightnessMaskWaveLengthSlider,
                brightnessMaskWaveLengthTextBox,
                undulationSpeedSlider,
                undulationSpeedTextBox,
                undulationWaveLengthSlider,
                undulationWaveLengthTextBox);
            
            // Wire up textbox events
            eventManager.WireUpTextBoxEvents(
                narrativeBalanceTextBox,
                narrativeBalanceSlider,
                combatSpeedTextBox,
                combatSpeedSlider,
                enemyHealthMultiplierTextBox,
                enemyHealthMultiplierSlider,
                enemyDamageMultiplierTextBox,
                enemyDamageMultiplierSlider,
                playerHealthMultiplierTextBox,
                playerHealthMultiplierSlider,
                playerDamageMultiplierTextBox,
                playerDamageMultiplierSlider,
                brightnessMaskIntensityTextBox,
                brightnessMaskIntensitySlider,
                brightnessMaskWaveLengthTextBox,
                brightnessMaskWaveLengthSlider,
                undulationSpeedTextBox,
                undulationSpeedSlider,
                undulationWaveLengthTextBox,
                undulationWaveLengthSlider);
            
            // Wire up button events
            eventManager.WireUpButtonEvents(
                saveButton,
                resetButton,
                backButton,
                runAllTestsButton,
                colorSystemTestsButton,
                characterSystemTestsButton,
                combatSystemTestsButton,
                inventoryDungeonTestsButton,
                dataUITestsButton,
                actionSystemTestsButton,
                advancedIntegrationTestsButton,
                combatLogFilteringTestsButton,
                generateRandomItemsButton,
                itemGenerationAnalysisButton,
                tierDistributionTestButton,
                commonItemModificationTestButton,
                actionEditorTestsButton,
                settingsPanel.FindControl<Button>("QuickTestButton"),
                settingsPanel.FindControl<Button>("StandardTestButton"),
                settingsPanel.FindControl<Button>("ComprehensiveTestButton"),
                settingsPanel.FindControl<Button>("WeaponTypeTestButton"),
                settingsPanel.FindControl<Button>("ComprehensiveWeaponEnemyTestButton"),
                settingsPanel.FindControl<Button>("BattleStatisticsButton"));
        }
    }
}

