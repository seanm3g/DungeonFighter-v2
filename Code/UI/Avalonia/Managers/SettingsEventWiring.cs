using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Managers.Settings;
using System;
using System.Threading.Tasks;
using ActionDelegate = System.Action;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Unified event wiring system for SettingsPanel controls.
    /// Consolidates SettingsEventWiring and SettingsEventManager for better maintainability.
    /// </summary>
    public class SettingsEventWiring
    {
        private readonly Func<int, Task>? runBattleTest;
        private readonly Func<Task>? runWeaponTypeTests;
        private readonly Func<Task>? runComprehensiveWeaponEnemyTests;
        private readonly ActionDelegate? onSave;
        private readonly ActionDelegate? onReset;
        private readonly ActionDelegate? onBack;

        public SettingsEventWiring(
            Func<int, Task>? runBattleTest,
            Func<Task>? runWeaponTypeTests,
            Func<Task>? runComprehensiveWeaponEnemyTests,
            ActionDelegate? onSave,
            ActionDelegate? onReset,
            ActionDelegate? onBack)
        {
            this.runBattleTest = runBattleTest;
            this.runWeaponTypeTests = runWeaponTypeTests;
            this.runComprehensiveWeaponEnemyTests = runComprehensiveWeaponEnemyTests;
            this.onSave = onSave;
            this.onReset = onReset;
            this.onBack = onBack;
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
            Button backButton)
        {
            if (settingsPanel == null)
            {
                RPGGame.Utils.ScrollDebugLogger.Log("SettingsEventWiring: settingsPanel is null, cannot wire events");
                return;
            }

            // Fix TextBox focus styling to prevent white-on-white text
            try
            {
                var settings = GameSettings.Instance;
                var textColor = SettingsColorManager.ParseColor(settings.TextBoxTextColor);
                TextBoxStylingHelper.FixTextBoxFocusStyling(settingsPanel, textColor);
            }
            catch (Exception ex)
            {
                RPGGame.Utils.ScrollDebugLogger.Log($"SettingsEventWiring: Error fixing TextBox styling: {ex.Message}");
            }
            
            // Wire up slider events
            WireUpSliderEvents(
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
            WireUpTextBoxEvents(
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
            WireUpButtonEvents(
                saveButton,
                resetButton,
                backButton,
                settingsPanel.FindControl<Button>("QuickTestButton"),
                settingsPanel.FindControl<Button>("StandardTestButton"),
                settingsPanel.FindControl<Button>("ComprehensiveTestButton"),
                settingsPanel.FindControl<Button>("WeaponTypeTestButton"),
                settingsPanel.FindControl<Button>("ComprehensiveWeaponEnemyTestButton"));
        }

        /// <summary>
        /// Wires up slider value changed events to update corresponding textboxes
        /// </summary>
        private void WireUpSliderEvents(
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
            Slider? brightnessMaskIntensitySlider = null,
            TextBox? brightnessMaskIntensityTextBox = null,
            Slider? brightnessMaskWaveLengthSlider = null,
            TextBox? brightnessMaskWaveLengthTextBox = null,
            Slider? undulationSpeedSlider = null,
            TextBox? undulationSpeedTextBox = null,
            Slider? undulationWaveLengthSlider = null,
            TextBox? undulationWaveLengthTextBox = null)
        {
            // Validate required controls
            if (narrativeBalanceSlider == null || narrativeBalanceTextBox == null ||
                combatSpeedSlider == null || combatSpeedTextBox == null ||
                enemyHealthMultiplierSlider == null || enemyHealthMultiplierTextBox == null ||
                enemyDamageMultiplierSlider == null || enemyDamageMultiplierTextBox == null ||
                playerHealthMultiplierSlider == null || playerHealthMultiplierTextBox == null ||
                playerDamageMultiplierSlider == null || playerDamageMultiplierTextBox == null)
            {
                RPGGame.Utils.ScrollDebugLogger.Log("SettingsEventWiring: Required slider controls are missing");
                return;
            }

            // Bind sliders to textboxes using helper
            SliderTextBoxBinder.BindSliderToTextBox(narrativeBalanceSlider, narrativeBalanceTextBox, "F2");
            SliderTextBoxBinder.BindSliderToTextBox(combatSpeedSlider, combatSpeedTextBox, "F2");
            SliderTextBoxBinder.BindSliderToTextBox(enemyHealthMultiplierSlider, enemyHealthMultiplierTextBox, "F2");
            SliderTextBoxBinder.BindSliderToTextBox(enemyDamageMultiplierSlider, enemyDamageMultiplierTextBox, "F2");
            SliderTextBoxBinder.BindSliderToTextBox(playerHealthMultiplierSlider, playerHealthMultiplierTextBox, "F2");
            SliderTextBoxBinder.BindSliderToTextBox(playerDamageMultiplierSlider, playerDamageMultiplierTextBox, "F2");
            
            // Animation sliders
            if (brightnessMaskIntensitySlider != null && brightnessMaskIntensityTextBox != null)
                SliderTextBoxBinder.BindSliderToTextBox(brightnessMaskIntensitySlider, brightnessMaskIntensityTextBox, "F1");
            
            if (brightnessMaskWaveLengthSlider != null && brightnessMaskWaveLengthTextBox != null)
                SliderTextBoxBinder.BindSliderToTextBox(brightnessMaskWaveLengthSlider, brightnessMaskWaveLengthTextBox, "F1");
            
            if (undulationSpeedSlider != null && undulationSpeedTextBox != null)
                SliderTextBoxBinder.BindSliderToTextBox(undulationSpeedSlider, undulationSpeedTextBox, "F3");
            
            if (undulationWaveLengthSlider != null && undulationWaveLengthTextBox != null)
                SliderTextBoxBinder.BindSliderToTextBox(undulationWaveLengthSlider, undulationWaveLengthTextBox, "F1");
        }

        /// <summary>
        /// Wires up textbox lost focus events to update corresponding sliders
        /// </summary>
        private void WireUpTextBoxEvents(
            TextBox narrativeBalanceTextBox,
            Slider narrativeBalanceSlider,
            TextBox combatSpeedTextBox,
            Slider combatSpeedSlider,
            TextBox enemyHealthMultiplierTextBox,
            Slider enemyHealthMultiplierSlider,
            TextBox enemyDamageMultiplierTextBox,
            Slider enemyDamageMultiplierSlider,
            TextBox playerHealthMultiplierTextBox,
            Slider playerHealthMultiplierSlider,
            TextBox playerDamageMultiplierTextBox,
            Slider playerDamageMultiplierSlider,
            TextBox? brightnessMaskIntensityTextBox = null,
            Slider? brightnessMaskIntensitySlider = null,
            TextBox? brightnessMaskWaveLengthTextBox = null,
            Slider? brightnessMaskWaveLengthSlider = null,
            TextBox? undulationSpeedTextBox = null,
            Slider? undulationSpeedSlider = null,
            TextBox? undulationWaveLengthTextBox = null,
            Slider? undulationWaveLengthSlider = null)
        {
            // Validate required controls
            if (narrativeBalanceTextBox == null || narrativeBalanceSlider == null ||
                combatSpeedTextBox == null || combatSpeedSlider == null ||
                enemyHealthMultiplierTextBox == null || enemyHealthMultiplierSlider == null ||
                enemyDamageMultiplierTextBox == null || enemyDamageMultiplierSlider == null ||
                playerHealthMultiplierTextBox == null || playerHealthMultiplierSlider == null ||
                playerDamageMultiplierTextBox == null || playerDamageMultiplierSlider == null)
            {
                RPGGame.Utils.ScrollDebugLogger.Log("SettingsEventWiring: Required textbox controls are missing");
                return;
            }

            // Bind textboxes to sliders using helper with validation
            SliderTextBoxBinder.BindTextBoxToSlider(narrativeBalanceTextBox, narrativeBalanceSlider, 0.0, 1.0, "F2");
            SliderTextBoxBinder.BindTextBoxToSlider(combatSpeedTextBox, combatSpeedSlider, 0.5, 2.0, "F2");
            SliderTextBoxBinder.BindTextBoxToSlider(enemyHealthMultiplierTextBox, enemyHealthMultiplierSlider, 0.5, 3.0, "F2");
            SliderTextBoxBinder.BindTextBoxToSlider(enemyDamageMultiplierTextBox, enemyDamageMultiplierSlider, 0.5, 3.0, "F2");
            SliderTextBoxBinder.BindTextBoxToSlider(playerHealthMultiplierTextBox, playerHealthMultiplierSlider, 0.5, 3.0, "F2");
            SliderTextBoxBinder.BindTextBoxToSlider(playerDamageMultiplierTextBox, playerDamageMultiplierSlider, 0.5, 3.0, "F2");
            
            // Animation textboxes
            if (brightnessMaskIntensityTextBox != null && brightnessMaskIntensitySlider != null)
                SliderTextBoxBinder.BindTextBoxToSliderFloat(brightnessMaskIntensityTextBox, brightnessMaskIntensitySlider, 0f, 50f, "F1");
            
            if (brightnessMaskWaveLengthTextBox != null && brightnessMaskWaveLengthSlider != null)
                SliderTextBoxBinder.BindTextBoxToSliderFloat(brightnessMaskWaveLengthTextBox, brightnessMaskWaveLengthSlider, 1f, 20f, "F1");
            
            if (undulationSpeedTextBox != null && undulationSpeedSlider != null)
                SliderTextBoxBinder.BindTextBoxToSlider(undulationSpeedTextBox, undulationSpeedSlider, 0.0, 0.2, "F3");
            
            if (undulationWaveLengthTextBox != null && undulationWaveLengthSlider != null)
                SliderTextBoxBinder.BindTextBoxToSliderFloat(undulationWaveLengthTextBox, undulationWaveLengthSlider, 1f, 20f, "F1");
        }

        /// <summary>
        /// Wires up button click events
        /// </summary>
        private void WireUpButtonEvents(
            Button saveButton,
            Button resetButton,
            Button backButton,
            Button? quickTestButton,
            Button? standardTestButton,
            Button? comprehensiveTestButton,
            Button? weaponTypeTestButton,
            Button? comprehensiveWeaponEnemyTestButton)
        {
            // Validate required buttons
            if (saveButton == null || resetButton == null || backButton == null)
            {
                RPGGame.Utils.ScrollDebugLogger.Log("SettingsEventWiring: Required buttons are missing");
                return;
            }

            // Wire up button events using helper
            ButtonEventHandler.WireAction(saveButton, onSave);
            ButtonEventHandler.WireAction(resetButton, onReset);
            ButtonEventHandler.WireAction(backButton, onBack);
            
            ButtonEventHandler.WireAsyncAction(quickTestButton, runBattleTest != null ? () => runBattleTest(100) : null);
            ButtonEventHandler.WireAsyncAction(standardTestButton, runBattleTest != null ? () => runBattleTest(500) : null);
            ButtonEventHandler.WireAsyncAction(comprehensiveTestButton, runBattleTest != null ? () => runBattleTest(1000) : null);
            ButtonEventHandler.WireAsyncAction(weaponTypeTestButton, runWeaponTypeTests);
            ButtonEventHandler.WireAsyncAction(comprehensiveWeaponEnemyTestButton, runComprehensiveWeaponEnemyTests);
        }
    }
}

