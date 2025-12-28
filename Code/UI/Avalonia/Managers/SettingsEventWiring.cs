using Avalonia.Controls;
using Avalonia.Interactivity;
using RPGGame.UI.Avalonia.Helpers;
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
        private readonly ActionDelegate? onBattleStatisticsClick;

        public SettingsEventWiring(
            Func<int, Task>? runBattleTest,
            Func<Task>? runWeaponTypeTests,
            Func<Task>? runComprehensiveWeaponEnemyTests,
            ActionDelegate? onSave,
            ActionDelegate? onReset,
            ActionDelegate? onBack,
            ActionDelegate? onBattleStatisticsClick)
        {
            this.runBattleTest = runBattleTest;
            this.runWeaponTypeTests = runWeaponTypeTests;
            this.runComprehensiveWeaponEnemyTests = runComprehensiveWeaponEnemyTests;
            this.onSave = onSave;
            this.onReset = onReset;
            this.onBack = onBack;
            this.onBattleStatisticsClick = onBattleStatisticsClick;
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
                TextBoxStylingHelper.FixTextBoxFocusStyling(settingsPanel);
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
                settingsPanel.FindControl<Button>("ComprehensiveWeaponEnemyTestButton"),
                settingsPanel.FindControl<Button>("BattleStatisticsButton"));
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

            narrativeBalanceSlider.ValueChanged += (s, e) => 
            {
                if (narrativeBalanceTextBox != null)
                    narrativeBalanceTextBox.Text = narrativeBalanceSlider.Value.ToString("F2");
            };
            
            combatSpeedSlider.ValueChanged += (s, e) => 
            {
                if (combatSpeedTextBox != null)
                    combatSpeedTextBox.Text = combatSpeedSlider.Value.ToString("F2");
            };
            
            enemyHealthMultiplierSlider.ValueChanged += (s, e) => 
            {
                if (enemyHealthMultiplierTextBox != null)
                    enemyHealthMultiplierTextBox.Text = enemyHealthMultiplierSlider.Value.ToString("F2");
            };
            
            enemyDamageMultiplierSlider.ValueChanged += (s, e) => 
            {
                if (enemyDamageMultiplierTextBox != null)
                    enemyDamageMultiplierTextBox.Text = enemyDamageMultiplierSlider.Value.ToString("F2");
            };
            
            playerHealthMultiplierSlider.ValueChanged += (s, e) => 
            {
                if (playerHealthMultiplierTextBox != null)
                    playerHealthMultiplierTextBox.Text = playerHealthMultiplierSlider.Value.ToString("F2");
            };
            
            playerDamageMultiplierSlider.ValueChanged += (s, e) => 
            {
                if (playerDamageMultiplierTextBox != null)
                    playerDamageMultiplierTextBox.Text = playerDamageMultiplierSlider.Value.ToString("F2");
            };
            
            // Animation sliders
            if (brightnessMaskIntensitySlider != null && brightnessMaskIntensityTextBox != null)
            {
                brightnessMaskIntensitySlider.ValueChanged += (s, e) => 
                {
                    brightnessMaskIntensityTextBox.Text = brightnessMaskIntensitySlider.Value.ToString("F1");
                };
            }
            
            if (brightnessMaskWaveLengthSlider != null && brightnessMaskWaveLengthTextBox != null)
            {
                brightnessMaskWaveLengthSlider.ValueChanged += (s, e) => 
                {
                    brightnessMaskWaveLengthTextBox.Text = brightnessMaskWaveLengthSlider.Value.ToString("F1");
                };
            }
            
            if (undulationSpeedSlider != null && undulationSpeedTextBox != null)
            {
                undulationSpeedSlider.ValueChanged += (s, e) => 
                {
                    undulationSpeedTextBox.Text = undulationSpeedSlider.Value.ToString("F3");
                };
            }
            
            if (undulationWaveLengthSlider != null && undulationWaveLengthTextBox != null)
            {
                undulationWaveLengthSlider.ValueChanged += (s, e) => 
                {
                    undulationWaveLengthTextBox.Text = undulationWaveLengthSlider.Value.ToString("F1");
                };
            }
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

            narrativeBalanceTextBox.LostFocus += (s, e) => 
            {
                if (narrativeBalanceTextBox != null && narrativeBalanceSlider != null &&
                    double.TryParse(narrativeBalanceTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.0, 1.0);
                    narrativeBalanceSlider.Value = value;
                    narrativeBalanceTextBox.Text = value.ToString("F2");
                }
            };
            
            combatSpeedTextBox.LostFocus += (s, e) => 
            {
                if (combatSpeedTextBox != null && combatSpeedSlider != null &&
                    double.TryParse(combatSpeedTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 2.0);
                    combatSpeedSlider.Value = value;
                    combatSpeedTextBox.Text = value.ToString("F2");
                }
            };
            
            enemyHealthMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (enemyHealthMultiplierTextBox != null && enemyHealthMultiplierSlider != null &&
                    double.TryParse(enemyHealthMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    enemyHealthMultiplierSlider.Value = value;
                    enemyHealthMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            enemyDamageMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (enemyDamageMultiplierTextBox != null && enemyDamageMultiplierSlider != null &&
                    double.TryParse(enemyDamageMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    enemyDamageMultiplierSlider.Value = value;
                    enemyDamageMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            playerHealthMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (playerHealthMultiplierTextBox != null && playerHealthMultiplierSlider != null &&
                    double.TryParse(playerHealthMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    playerHealthMultiplierSlider.Value = value;
                    playerHealthMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            playerDamageMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (playerDamageMultiplierTextBox != null && playerDamageMultiplierSlider != null &&
                    double.TryParse(playerDamageMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    playerDamageMultiplierSlider.Value = value;
                    playerDamageMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            // Animation textboxes
            if (brightnessMaskIntensityTextBox != null && brightnessMaskIntensitySlider != null)
            {
                brightnessMaskIntensityTextBox.LostFocus += (s, e) => 
                {
                    if (float.TryParse(brightnessMaskIntensityTextBox.Text, out float value))
                    {
                        value = Math.Clamp(value, 0f, 50f);
                        brightnessMaskIntensitySlider.Value = value;
                        brightnessMaskIntensityTextBox.Text = value.ToString("F1");
                    }
                };
            }
            
            if (brightnessMaskWaveLengthTextBox != null && brightnessMaskWaveLengthSlider != null)
            {
                brightnessMaskWaveLengthTextBox.LostFocus += (s, e) => 
                {
                    if (float.TryParse(brightnessMaskWaveLengthTextBox.Text, out float value))
                    {
                        value = Math.Clamp(value, 1f, 20f);
                        brightnessMaskWaveLengthSlider.Value = value;
                        brightnessMaskWaveLengthTextBox.Text = value.ToString("F1");
                    }
                };
            }
            
            if (undulationSpeedTextBox != null && undulationSpeedSlider != null)
            {
                undulationSpeedTextBox.LostFocus += (s, e) => 
                {
                    if (double.TryParse(undulationSpeedTextBox.Text, out double value))
                    {
                        value = Math.Clamp(value, 0.0, 0.2);
                        undulationSpeedSlider.Value = value;
                        undulationSpeedTextBox.Text = value.ToString("F3");
                    }
                };
            }
            
            if (undulationWaveLengthTextBox != null && undulationWaveLengthSlider != null)
            {
                undulationWaveLengthTextBox.LostFocus += (s, e) => 
                {
                    if (float.TryParse(undulationWaveLengthTextBox.Text, out float value))
                    {
                        value = Math.Clamp(value, 1f, 20f);
                        undulationWaveLengthSlider.Value = value;
                        undulationWaveLengthTextBox.Text = value.ToString("F1");
                    }
                };
            }
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
            Button? comprehensiveWeaponEnemyTestButton,
            Button? battleStatisticsButton)
        {
            // Validate required buttons
            if (saveButton == null || resetButton == null || backButton == null)
            {
                RPGGame.Utils.ScrollDebugLogger.Log("SettingsEventWiring: Required buttons are missing");
                return;
            }

            saveButton.Click += (s, e) => onSave?.Invoke();
            resetButton.Click += (s, e) => onReset?.Invoke();
            backButton.Click += (s, e) => onBack?.Invoke();
            
            if (quickTestButton != null && runBattleTest != null)
                quickTestButton.Click += async (s, e) => await runBattleTest(100);
            if (standardTestButton != null && runBattleTest != null)
                standardTestButton.Click += async (s, e) => await runBattleTest(500);
            if (comprehensiveTestButton != null && runBattleTest != null)
                comprehensiveTestButton.Click += async (s, e) => await runBattleTest(1000);
            if (weaponTypeTestButton != null && runWeaponTypeTests != null)
                weaponTypeTestButton.Click += async (s, e) => await runWeaponTypeTests();
            if (comprehensiveWeaponEnemyTestButton != null && runComprehensiveWeaponEnemyTests != null)
                comprehensiveWeaponEnemyTestButton.Click += async (s, e) => await runComprehensiveWeaponEnemyTests();
            if (battleStatisticsButton != null)
                battleStatisticsButton.Click += (s, e) => onBattleStatisticsClick?.Invoke();
        }
    }
}

