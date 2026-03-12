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
        private readonly Action<string, bool>? showStatusMessage;
        
        public DifficultySettingsManager(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }
        
        /// <summary>
        /// Loads difficulty settings into UI controls (uses GameSettings.Instance at use time).
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
            enemyHealthMultiplierSlider.Value = GameSettings.Instance.EnemyHealthMultiplier;
            enemyHealthMultiplierTextBox.Text = GameSettings.Instance.EnemyHealthMultiplier.ToString("F2");
            enemyDamageMultiplierSlider.Value = GameSettings.Instance.EnemyDamageMultiplier;
            enemyDamageMultiplierTextBox.Text = GameSettings.Instance.EnemyDamageMultiplier.ToString("F2");
            playerHealthMultiplierSlider.Value = GameSettings.Instance.PlayerHealthMultiplier;
            playerHealthMultiplierTextBox.Text = GameSettings.Instance.PlayerHealthMultiplier.ToString("F2");
            playerDamageMultiplierSlider.Value = GameSettings.Instance.PlayerDamageMultiplier;
            playerDamageMultiplierTextBox.Text = GameSettings.Instance.PlayerDamageMultiplier.ToString("F2");
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
            GameSettings.Instance.EnemyHealthMultiplier = enemyHealthMultiplierSlider.Value;
            GameSettings.Instance.EnemyDamageMultiplier = enemyDamageMultiplierSlider.Value;
            GameSettings.Instance.PlayerHealthMultiplier = playerHealthMultiplierSlider.Value;
            GameSettings.Instance.PlayerDamageMultiplier = playerDamageMultiplierSlider.Value;
        }
        
        /// <summary>
        /// Restores difficulty settings from a backup
        /// </summary>
        public void RestoreSettings(GameSettings backup)
        {
            GameSettings.Instance.EnemyHealthMultiplier = backup.EnemyHealthMultiplier;
            GameSettings.Instance.EnemyDamageMultiplier = backup.EnemyDamageMultiplier;
            GameSettings.Instance.PlayerHealthMultiplier = backup.PlayerHealthMultiplier;
            GameSettings.Instance.PlayerDamageMultiplier = backup.PlayerDamageMultiplier;
        }
    }
}
