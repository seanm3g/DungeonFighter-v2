using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using ActionDelegate = System.Action;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages event wiring for SettingsPanel controls
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// </summary>
    public class SettingsEventManager
    {
        private readonly Func<string, Task> executeTest;
        private readonly Func<int, Task> runBattleTest;
        private readonly Func<Task> runWeaponTypeTests;
        private readonly Func<Task> runComprehensiveWeaponEnemyTests;
        private readonly ActionDelegate onSave;
        private readonly ActionDelegate onReset;
        private readonly ActionDelegate onBack;
        private readonly ActionDelegate onBattleStatisticsClick;

        public SettingsEventManager(
            Func<string, Task> executeTest,
            Func<int, Task> runBattleTest,
            Func<Task> runWeaponTypeTests,
            Func<Task> runComprehensiveWeaponEnemyTests,
            ActionDelegate onSave,
            ActionDelegate onReset,
            ActionDelegate onBack,
            ActionDelegate onBattleStatisticsClick)
        {
            this.executeTest = executeTest;
            this.runBattleTest = runBattleTest;
            this.runWeaponTypeTests = runWeaponTypeTests;
            this.runComprehensiveWeaponEnemyTests = runComprehensiveWeaponEnemyTests;
            this.onSave = onSave;
            this.onReset = onReset;
            this.onBack = onBack;
            this.onBattleStatisticsClick = onBattleStatisticsClick;
        }

        public void WireUpSliderEvents(
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
            narrativeBalanceSlider.ValueChanged += (s, e) => 
            {
                narrativeBalanceTextBox.Text = narrativeBalanceSlider.Value.ToString("F2");
            };
            
            combatSpeedSlider.ValueChanged += (s, e) => 
            {
                combatSpeedTextBox.Text = combatSpeedSlider.Value.ToString("F2");
            };
            
            enemyHealthMultiplierSlider.ValueChanged += (s, e) => 
            {
                enemyHealthMultiplierTextBox.Text = enemyHealthMultiplierSlider.Value.ToString("F2");
            };
            
            enemyDamageMultiplierSlider.ValueChanged += (s, e) => 
            {
                enemyDamageMultiplierTextBox.Text = enemyDamageMultiplierSlider.Value.ToString("F2");
            };
            
            playerHealthMultiplierSlider.ValueChanged += (s, e) => 
            {
                playerHealthMultiplierTextBox.Text = playerHealthMultiplierSlider.Value.ToString("F2");
            };
            
            playerDamageMultiplierSlider.ValueChanged += (s, e) => 
            {
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

        public void WireUpTextBoxEvents(
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
            narrativeBalanceTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(narrativeBalanceTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.0, 1.0);
                    narrativeBalanceSlider.Value = value;
                    narrativeBalanceTextBox.Text = value.ToString("F2");
                }
            };
            
            combatSpeedTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(combatSpeedTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 2.0);
                    combatSpeedSlider.Value = value;
                    combatSpeedTextBox.Text = value.ToString("F2");
                }
            };
            
            enemyHealthMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(enemyHealthMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    enemyHealthMultiplierSlider.Value = value;
                    enemyHealthMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            enemyDamageMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(enemyDamageMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    enemyDamageMultiplierSlider.Value = value;
                    enemyDamageMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            playerHealthMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(playerHealthMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    playerHealthMultiplierSlider.Value = value;
                    playerHealthMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            playerDamageMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(playerDamageMultiplierTextBox.Text, out double value))
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

        public void WireUpButtonEvents(
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
            Button actionEditorTestsButton,
            Button? quickTestButton,
            Button? standardTestButton,
            Button? comprehensiveTestButton,
            Button? weaponTypeTestButton,
            Button? comprehensiveWeaponEnemyTestButton,
            Button? battleStatisticsButton)
        {
            saveButton.Click += (s, e) => onSave();
            resetButton.Click += (s, e) => onReset();
            backButton.Click += (s, e) => onBack();
            
            runAllTestsButton.Click += async (s, e) => await executeTest("1");
            colorSystemTestsButton.Click += async (s, e) => await executeTest("11");
            characterSystemTestsButton.Click += async (s, e) => await executeTest("2");
            combatSystemTestsButton.Click += async (s, e) => await executeTest("3");
            inventoryDungeonTestsButton.Click += async (s, e) => await executeTest("4");
            dataUITestsButton.Click += async (s, e) => await executeTest("5");
            actionSystemTestsButton.Click += async (s, e) => await executeTest("12");
            advancedIntegrationTestsButton.Click += async (s, e) => await executeTest("6");
            combatLogFilteringTestsButton.Click += async (s, e) => await executeTest("14");
            generateRandomItemsButton.Click += async (s, e) => await executeTest("7");
            itemGenerationAnalysisButton.Click += async (s, e) => await executeTest("8");
            tierDistributionTestButton.Click += async (s, e) => await executeTest("9");
            commonItemModificationTestButton.Click += async (s, e) => await executeTest("10");
            actionEditorTestsButton.Click += async (s, e) => await executeTest("13");
            
            if (quickTestButton != null) quickTestButton.Click += async (s, e) => await runBattleTest(100);
            if (standardTestButton != null) standardTestButton.Click += async (s, e) => await runBattleTest(500);
            if (comprehensiveTestButton != null) comprehensiveTestButton.Click += async (s, e) => await runBattleTest(1000);
            if (weaponTypeTestButton != null) weaponTypeTestButton.Click += async (s, e) => await runWeaponTypeTests();
            if (comprehensiveWeaponEnemyTestButton != null) comprehensiveWeaponEnemyTestButton.Click += async (s, e) => await runComprehensiveWeaponEnemyTests();
            if (battleStatisticsButton != null) battleStatisticsButton.Click += (s, e) => onBattleStatisticsClick();
        }
    }
}

