using Avalonia.Controls;
using RPGGame;
using RPGGame.Config;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Managers.Settings;
using System;
using ActionDelegate = System.Action;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages loading and saving of game settings
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// Refactored to use specialized managers for different setting categories
    /// </summary>
    public class SettingsManager
    {
        private readonly Action<string, bool>? showStatusMessage;
        private readonly Action<string>? updateStatus;
        private readonly TextDelaySettingsManager textDelayManager;
        private readonly Managers.Settings.AnimationSettingsManager animationSettingsManager;
        private readonly GameplaySettingsManager gameplaySettingsManager;
        private readonly DifficultySettingsManager difficultySettingsManager;
        private readonly SettingsLoader loader;
        private SettingsSaver saver;
        private GameVariablesTabManager? gameVariablesTabManager;

        public SettingsManager(GameSettings settings, Action<string, bool>? showStatusMessage, Action<string>? updateStatus)
        {
            ArgumentNullException.ThrowIfNull(settings);
            this.showStatusMessage = showStatusMessage;
            this.updateStatus = updateStatus;
            this.textDelayManager = new TextDelaySettingsManager(showStatusMessage);
            this.animationSettingsManager = new Managers.Settings.AnimationSettingsManager(showStatusMessage);
            this.gameplaySettingsManager = new GameplaySettingsManager(showStatusMessage);
            this.difficultySettingsManager = new DifficultySettingsManager(showStatusMessage);
            this.loader = new SettingsLoader(this);
            this.saver = new SettingsSaver(this, null); // Will be set via SetGameVariablesTabManager
        }

        /// <summary>
        /// Sets the game variables tab manager (called after construction when available)
        /// </summary>
        public void SetGameVariablesTabManager(GameVariablesTabManager? gameVariablesTabManager)
        {
            this.gameVariablesTabManager = gameVariablesTabManager;
            // Update saver with game variables tab manager
            this.saver = new SettingsSaver(this, gameVariablesTabManager);
        }

        /// <summary>
        /// Saves settings from UI controls using DTO. Uses GameSettings.Instance at use time.
        /// </summary>
        public void SaveSettings(MainSettingsControls controls, ActionDelegate? saveGameVariables = null)
        {
            try
            {
                if (controls == null) return;
                if (controls.NarrativeBalanceSlider == null || controls.CombatSpeedSlider == null ||
                    controls.EnemyHealthMultiplierSlider == null || controls.EnemyDamageMultiplierSlider == null ||
                    controls.PlayerHealthMultiplierSlider == null || controls.PlayerDamageMultiplierSlider == null)
                {
                    showStatusMessage?.Invoke("Error: Some settings controls are missing", false);
                    return;
                }

                var s = GameSettings.Instance;
                var originalSettings = new GameSettings
                {
                    NarrativeBalance = s.NarrativeBalance,
                    EnableNarrativeEvents = s.EnableNarrativeEvents,
                    EnableInformationalSummaries = s.EnableInformationalSummaries,
                    CombatSpeed = s.CombatSpeed,
                    ShowIndividualActionMessages = s.ShowIndividualActionMessages,
                    EnableComboSystem = s.EnableComboSystem,
                    EnableTextDisplayDelays = s.EnableTextDisplayDelays,
                    FastCombat = s.FastCombat,
                    EnableAutoSave = s.EnableAutoSave,
                    AutoSaveInterval = s.AutoSaveInterval,
                    ShowDetailedStats = s.ShowDetailedStats,
                    EnableSoundEffects = s.EnableSoundEffects,
                    EnemyHealthMultiplier = s.EnemyHealthMultiplier,
                    EnemyDamageMultiplier = s.EnemyDamageMultiplier,
                    PlayerHealthMultiplier = s.PlayerHealthMultiplier,
                    PlayerDamageMultiplier = s.PlayerDamageMultiplier,
                    ShowHealthBars = s.ShowHealthBars,
                    ShowDamageNumbers = s.ShowDamageNumbers,
                    ShowComboProgress = s.ShowComboProgress
                };

                gameplaySettingsManager.SaveSettings(
                    controls.NarrativeBalanceSlider,
                    controls.EnableNarrativeEventsCheckBox!,
                    controls.EnableInformationalSummariesCheckBox!,
                    controls.CombatSpeedSlider,
                    controls.ShowIndividualActionMessagesCheckBox!,
                    controls.EnableComboSystemCheckBox,
                    controls.EnableTextDisplayDelaysCheckBox!,
                    controls.FastCombatCheckBox!,
                    controls.EnableAutoSaveCheckBox,
                    controls.AutoSaveIntervalTextBox,
                    controls.ShowDetailedStatsCheckBox!,
                    controls.EnableSoundEffectsCheckBox);

                difficultySettingsManager.SaveSettings(
                    controls.EnemyHealthMultiplierSlider!,
                    controls.EnemyDamageMultiplierSlider!,
                    controls.PlayerHealthMultiplierSlider!,
                    controls.PlayerDamageMultiplierSlider!);

                s.ShowHealthBars = controls.ShowHealthBarsCheckBox?.IsChecked ?? true;
                s.ShowDamageNumbers = controls.ShowDamageNumbersCheckBox?.IsChecked ?? true;
                s.ShowComboProgress = controls.ShowComboProgressCheckBox?.IsChecked ?? true;

                s.ValidateAndFix();
                if (!s.SaveSettings())
                {
                    showStatusMessage?.Invoke("Error: Failed to save settings to file.", false);
                    updateStatus?.Invoke("Error: Failed to save settings to file.");
                    return;
                }

                try
                {
                    saveGameVariables?.Invoke();
                }
                catch (Exception ex)
                {
                    RestoreSettings(originalSettings);
                    showStatusMessage?.Invoke($"Error saving game variables: {ex.Message}. Settings rolled back.", false);
                    updateStatus?.Invoke($"Error saving game variables: {ex.Message}");
                    return;
                }

                showStatusMessage?.Invoke("Settings saved successfully!", true);
                updateStatus?.Invoke("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving settings: {ex.Message}");
                RPGGame.Utils.ScrollDebugLogger.Log($"SettingsManager.SaveSettings error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Saves gameplay settings without sliders (for simplified gameplay panel).
        /// </summary>
        /// <returns>True if save to file succeeded, false otherwise.</returns>
        public bool SaveGameplaySettings(
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox showHealthBarsCheckBox,
            CheckBox showDamageNumbersCheckBox,
            CheckBox showComboProgressCheckBox,
            ActionDelegate? saveGameVariables = null)
        {
            try
            {
                var s = GameSettings.Instance;
                // Store original values for rollback
                var originalSettings = new GameSettings
                {
                    ShowIndividualActionMessages = s.ShowIndividualActionMessages,
                    EnableTextDisplayDelays = s.EnableTextDisplayDelays,
                    FastCombat = s.FastCombat,
                    ShowDetailedStats = s.ShowDetailedStats,
                    ShowHealthBars = s.ShowHealthBars,
                    ShowDamageNumbers = s.ShowDamageNumbers,
                    ShowComboProgress = s.ShowComboProgress
                };

                // Save gameplay settings
                gameplaySettingsManager.SaveGameplaySettings(
                    showIndividualActionMessagesCheckBox,
                    enableTextDisplayDelaysCheckBox,
                    fastCombatCheckBox,
                    showDetailedStatsCheckBox);
                
                // UI Settings (in-memory only; orchestrator persists GameSettings once at end of save)
                s.ShowHealthBars = showHealthBarsCheckBox.IsChecked ?? true;
                s.ShowDamageNumbers = showDamageNumbersCheckBox.IsChecked ?? true;
                s.ShowComboProgress = showComboProgressCheckBox.IsChecked ?? true;
                
                s.ValidateAndFix();
                
                // Save Game Variables if any were modified (orchestrator already called this; null when used from handler)
                try
                {
                    saveGameVariables?.Invoke();
                }
                catch (Exception ex)
                {
                    RestoreSettings(originalSettings);
                    showStatusMessage?.Invoke($"Error saving game variables: {ex.Message}. Settings rolled back.", false);
                    updateStatus?.Invoke($"Error saving game variables: {ex.Message}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving settings: {ex.Message}");
                RPGGame.Utils.ScrollDebugLogger.Log($"SettingsManager.SaveGameplaySettings error: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Restores settings from a backup
        /// </summary>
        private void RestoreSettings(GameSettings backup)
        {
            // Restore gameplay settings
            gameplaySettingsManager.RestoreSettings(backup);
            
            // Restore difficulty settings
            difficultySettingsManager.RestoreSettings(backup);
            
            // Restore UI settings
            var s = GameSettings.Instance;
            s.ShowHealthBars = backup.ShowHealthBars;
            s.ShowDamageNumbers = backup.ShowDamageNumbers;
            s.ShowComboProgress = backup.ShowComboProgress;
        }

        public void ResetToDefaults()
        {
            GameSettings.Instance.ResetToDefaults();
        }

        /// <summary>
        /// Loads animation settings from UIConfiguration into UI controls
        /// Delegates to AnimationSettingsManager
        /// </summary>
        public void LoadAnimationSettings(
            CheckBox brightnessMaskEnabledCheckBox,
            Slider brightnessMaskIntensitySlider,
            TextBox brightnessMaskIntensityTextBox,
            Slider brightnessMaskWaveLengthSlider,
            TextBox brightnessMaskWaveLengthTextBox,
            TextBox brightnessMaskUpdateIntervalTextBox,
            Slider undulationSpeedSlider,
            TextBox undulationSpeedTextBox,
            Slider undulationWaveLengthSlider,
            TextBox undulationWaveLengthTextBox,
            TextBox undulationIntervalTextBox)
        {
            animationSettingsManager.LoadAnimationSettings(
                brightnessMaskEnabledCheckBox,
                brightnessMaskIntensitySlider,
                brightnessMaskIntensityTextBox,
                brightnessMaskWaveLengthSlider,
                brightnessMaskWaveLengthTextBox,
                brightnessMaskUpdateIntervalTextBox,
                undulationSpeedSlider,
                undulationSpeedTextBox,
                undulationWaveLengthSlider,
                undulationWaveLengthTextBox,
                undulationIntervalTextBox);
        }

        /// <summary>
        /// Saves animation settings from UI controls to UIConfiguration
        /// Delegates to AnimationSettingsManager
        /// </summary>
        public void SaveAnimationSettings(
            CheckBox brightnessMaskEnabledCheckBox,
            Slider brightnessMaskIntensitySlider,
            Slider brightnessMaskWaveLengthSlider,
            TextBox brightnessMaskUpdateIntervalTextBox,
            Slider undulationSpeedSlider,
            Slider undulationWaveLengthSlider,
            TextBox undulationIntervalTextBox)
        {
            animationSettingsManager.SaveAnimationSettings(
                brightnessMaskEnabledCheckBox,
                brightnessMaskIntensitySlider,
                brightnessMaskWaveLengthSlider,
                brightnessMaskUpdateIntervalTextBox,
                undulationSpeedSlider,
                undulationWaveLengthSlider,
                undulationIntervalTextBox);
        }
        
        /// <summary>
        /// Gets the animation settings manager for event wiring
        /// </summary>
        public Managers.Settings.AnimationSettingsManager GetAnimationSettingsManager()
        {
            return animationSettingsManager;
        }

        #region DTO-based Methods (merged from SettingsPersistenceManager)

        /// <summary>
        /// Loads current settings into the UI controls using DTO.
        /// </summary>
        public void LoadSettings(MainSettingsControls controls)
        {
            if (controls == null) return;
            if (controls.NarrativeBalanceSlider == null || controls.CombatSpeedSlider == null ||
                controls.EnemyHealthMultiplierSlider == null || controls.PlayerHealthMultiplierSlider == null ||
                controls.PlayerDamageMultiplierSlider == null)
                return;

            gameplaySettingsManager.LoadSettings(
                controls.NarrativeBalanceSlider,
                controls.NarrativeBalanceTextBox!,
                controls.EnableNarrativeEventsCheckBox!,
                controls.EnableInformationalSummariesCheckBox!,
                controls.CombatSpeedSlider,
                controls.CombatSpeedTextBox!,
                controls.ShowIndividualActionMessagesCheckBox!,
                controls.EnableComboSystemCheckBox,
                controls.EnableTextDisplayDelaysCheckBox!,
                controls.FastCombatCheckBox!,
                controls.EnableAutoSaveCheckBox,
                controls.AutoSaveIntervalTextBox,
                controls.ShowDetailedStatsCheckBox!,
                controls.EnableSoundEffectsCheckBox);

            difficultySettingsManager.LoadSettings(
                controls.EnemyHealthMultiplierSlider!,
                controls.EnemyHealthMultiplierTextBox!,
                controls.EnemyDamageMultiplierSlider!,
                controls.EnemyDamageMultiplierTextBox!,
                controls.PlayerHealthMultiplierSlider!,
                controls.PlayerHealthMultiplierTextBox!,
                controls.PlayerDamageMultiplierSlider!,
                controls.PlayerDamageMultiplierTextBox!);

            var s = GameSettings.Instance;
            if (controls.ShowHealthBarsCheckBox != null) controls.ShowHealthBarsCheckBox.IsChecked = s.ShowHealthBars;
            if (controls.ShowDamageNumbersCheckBox != null) controls.ShowDamageNumbersCheckBox.IsChecked = s.ShowDamageNumbers;
            if (controls.ShowComboProgressCheckBox != null) controls.ShowComboProgressCheckBox.IsChecked = s.ShowComboProgress;
        }

        /// <summary>
        /// Loads text delay settings into UI controls using DTO.
        /// </summary>
        public void LoadTextDelaySettings(TextDelaySettingsControls controls, Action<Slider, TextBox>? wireUpActionDelaySlider = null, Action<Slider, TextBox>? wireUpMessageDelaySlider = null)
        {
            textDelayManager.LoadTextDelaySettings(controls);
        }

        /// <summary>
        /// Loads animation settings into UI controls using DTO (backward compatibility)
        /// </summary>
        public void LoadAnimationSettings(AnimationSettingsControls controls)
        {
            loader.LoadAnimationSettings(controls);
        }

        /// <summary>
        /// Saves current UI values to settings using DTO (backward compatibility). Uses null for saveGameVariables.
        /// </summary>
        public void SaveSettings(MainSettingsControls controls)
        {
            SaveSettings(controls, null);
        }

        /// <summary>
        /// Saves text delay settings from UI controls using DTO.
        /// </summary>
        public void SaveTextDelaySettings(TextDelaySettingsControls controls)
        {
            textDelayManager.SaveTextDelaySettings(controls);
        }

        /// <summary>
        /// Saves animation settings from UI controls using DTO (backward compatibility)
        /// </summary>
        public void SaveAnimationSettings(AnimationSettingsControls controls)
        {
            saver.SaveAnimationSettings(controls);
        }

        #endregion
    }
}

