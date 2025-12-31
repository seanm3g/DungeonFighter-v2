using Avalonia.Controls;
using RPGGame.Config;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages difficulty-related settings (health and damage multipliers).
    /// Extracted from SettingsManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class DifficultySettingsManager
    {
        private readonly GameSettings settings;
        private readonly Action<string, bool>? showStatusMessage;
        
        public DifficultySettingsManager(GameSettings settings, Action<string, bool>? showStatusMessage = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.showStatusMessage = showStatusMessage;
        }
        
        /// <summary>
        /// Loads difficulty settings into UI controls
        /// </summary>
        public void LoadSettings(
            Slider enemyHealthMultiplierSlider,
            TextBox enemyHealthMultiplierTextBox,
            Slider enemyDamageMultiplierSlider,
            TextBox enemyDamageMultiplierTextBox,
            Slider playerHealthMultiplierSlider,
            TextBox playerHealthMultiplierTextBox,
            Slider playerDamageMultiplierSlider,
            TextBox playerDamageMultiplierTextBox)
        {
            // Difficulty Settings
            enemyHealthMultiplierSlider.Value = settings.EnemyHealthMultiplier;
            enemyHealthMultiplierTextBox.Text = settings.EnemyHealthMultiplier.ToString("F2");
            enemyDamageMultiplierSlider.Value = settings.EnemyDamageMultiplier;
            enemyDamageMultiplierTextBox.Text = settings.EnemyDamageMultiplier.ToString("F2");
            playerHealthMultiplierSlider.Value = settings.PlayerHealthMultiplier;
            playerHealthMultiplierTextBox.Text = settings.PlayerHealthMultiplier.ToString("F2");
            playerDamageMultiplierSlider.Value = settings.PlayerDamageMultiplier;
            playerDamageMultiplierTextBox.Text = settings.PlayerDamageMultiplier.ToString("F2");
        }
        
        /// <summary>
        /// Saves difficulty settings from UI controls
        /// </summary>
        public void SaveSettings(
            Slider enemyHealthMultiplierSlider,
            Slider enemyDamageMultiplierSlider,
            Slider playerHealthMultiplierSlider,
            Slider playerDamageMultiplierSlider)
        {
            // Difficulty Settings
            settings.EnemyHealthMultiplier = enemyHealthMultiplierSlider.Value;
            settings.EnemyDamageMultiplier = enemyDamageMultiplierSlider.Value;
            settings.PlayerHealthMultiplier = playerHealthMultiplierSlider.Value;
            settings.PlayerDamageMultiplier = playerDamageMultiplierSlider.Value;
        }
        
        /// <summary>
        /// Restores difficulty settings from a backup
        /// </summary>
        public void RestoreSettings(GameSettings backup)
        {
            settings.EnemyHealthMultiplier = backup.EnemyHealthMultiplier;
            settings.EnemyDamageMultiplier = backup.EnemyDamageMultiplier;
            settings.PlayerHealthMultiplier = backup.PlayerHealthMultiplier;
            settings.PlayerDamageMultiplier = backup.PlayerDamageMultiplier;
        }
    }
}
