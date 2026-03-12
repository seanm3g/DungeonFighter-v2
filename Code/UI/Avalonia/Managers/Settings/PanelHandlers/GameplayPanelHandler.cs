using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Handles wiring and loading for the Gameplay settings panel
    /// </summary>
    public class GameplayPanelHandler : ISettingsPanelHandler
    {
        private readonly SettingsManager? settingsManager;
        private readonly Action<string, bool>? showStatusMessage;
        private GameStateManager? stateManager;

        public string PanelType => "Gameplay";

        public GameplayPanelHandler(GameSettings settings, SettingsManager? settingsManager, Action<string, bool>? showStatusMessage = null)
        {
            this.settingsManager = settingsManager;
            this.showStatusMessage = showStatusMessage;
        }

        /// <summary>
        /// Sets the game state manager for clearing in-memory player data
        /// </summary>
        public void SetStateManager(GameStateManager? stateManager)
        {
            this.stateManager = stateManager;
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not GameplaySettingsPanel gameplayPanel || settingsManager == null) return;

            // Resolve controls (property first, then FindControl fallback so wiring works when named controls are null)
            var showIndividualCb = gameplayPanel.ShowIndividualActionMessagesCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowIndividualActionMessagesCheckBox");
            var fastCombatCb = gameplayPanel.FastCombatCheckBox ?? gameplayPanel.FindControl<CheckBox>("FastCombatCheckBox");
            var enableTextDelaysCb = gameplayPanel.EnableTextDisplayDelaysCheckBox ?? gameplayPanel.FindControl<CheckBox>("EnableTextDisplayDelaysCheckBox");
            var showDetailedStatsCb = gameplayPanel.ShowDetailedStatsCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowDetailedStatsCheckBox");
            var showHealthBarsCb = gameplayPanel.ShowHealthBarsCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowHealthBarsCheckBox");
            var showDamageNumbersCb = gameplayPanel.ShowDamageNumbersCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowDamageNumbersCheckBox");
            var showComboProgressCb = gameplayPanel.ShowComboProgressCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowComboProgressCheckBox");

            if (showIndividualCb != null)
                showIndividualCb.IsCheckedChanged += (s, e) => { if (showIndividualCb.IsChecked.HasValue) GameSettings.Instance.ShowIndividualActionMessages = showIndividualCb.IsChecked.Value; };
            if (fastCombatCb != null)
                fastCombatCb.IsCheckedChanged += (s, e) => { if (fastCombatCb.IsChecked.HasValue) GameSettings.Instance.FastCombat = fastCombatCb.IsChecked.Value; };
            if (enableTextDelaysCb != null)
                enableTextDelaysCb.IsCheckedChanged += (s, e) => { if (enableTextDelaysCb.IsChecked.HasValue) GameSettings.Instance.EnableTextDisplayDelays = enableTextDelaysCb.IsChecked.Value; };
            if (showDetailedStatsCb != null)
                showDetailedStatsCb.IsCheckedChanged += (s, e) => { if (showDetailedStatsCb.IsChecked.HasValue) GameSettings.Instance.ShowDetailedStats = showDetailedStatsCb.IsChecked.Value; };
            if (showHealthBarsCb != null)
                showHealthBarsCb.IsCheckedChanged += (s, e) => { if (showHealthBarsCb.IsChecked.HasValue) GameSettings.Instance.ShowHealthBars = showHealthBarsCb.IsChecked.Value; };
            if (showDamageNumbersCb != null)
                showDamageNumbersCb.IsCheckedChanged += (s, e) => { if (showDamageNumbersCb.IsChecked.HasValue) GameSettings.Instance.ShowDamageNumbers = showDamageNumbersCb.IsChecked.Value; };
            if (showComboProgressCb != null)
                showComboProgressCb.IsCheckedChanged += (s, e) => { if (showComboProgressCb.IsChecked.HasValue) GameSettings.Instance.ShowComboProgress = showComboProgressCb.IsChecked.Value; };

            // Wire up clear saved characters button
            var clearButton = gameplayPanel.ClearSavedCharactersButton ?? gameplayPanel.FindControl<Button>("ClearSavedCharactersButton");
            if (clearButton != null)
            {
                clearButton.Click += async (s, e) =>
                {
                    await HandleClearSavedCharactersAsync();
                };
            }

            // Apply current settings once when panel is wired. Do not subscribe to Loaded: Loaded can fire
            // again on layout/focus (e.g. when user clicks Save), and a deferred LoadSettings would overwrite
            // the user's checkbox changes with file values before we read them at save.
            LoadSettings(gameplayPanel);
        }

        private async Task HandleClearSavedCharactersAsync()
        {
            // Get the parent window for the dialog - find it from the current control tree
            Window? parentWindow = null;
            
            // Try to find the window from the application
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                parentWindow = desktop.MainWindow;
            }

            // Show confirmation dialog
            bool confirmed = await ConfirmationDialog.ShowAsync(
                parentWindow,
                "Clear All Saved Characters",
                "Are you sure you want to delete all saved characters? This action cannot be undone.");

            if (confirmed)
            {
                try
                {
                    int deletedCount = CharacterSaveManager.ClearAllSavedCharacters();
                    
                    // Also clear the in-memory player if one exists
                    // This ensures the main menu doesn't show a load game option for a cleared character
                    if (stateManager != null && stateManager.CurrentPlayer != null)
                    {
                        stateManager.SetCurrentPlayer(null);
                    }
                    
                    if (deletedCount > 0)
                    {
                        showStatusMessage?.Invoke($"Successfully deleted {deletedCount} saved character(s).", true);
                    }
                    else
                    {
                        showStatusMessage?.Invoke("No saved characters found to delete.", true);
                    }
                }
                catch (Exception ex)
                {
                    showStatusMessage?.Invoke($"Error clearing saved characters: {ex.Message}", false);
                }
            }
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not GameplaySettingsPanel gameplayPanel || settingsManager == null) return;
            var s = GameSettings.Instance;
            var showIndividual = gameplayPanel.ShowIndividualActionMessagesCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowIndividualActionMessagesCheckBox");
            var fastCombat = gameplayPanel.FastCombatCheckBox ?? gameplayPanel.FindControl<CheckBox>("FastCombatCheckBox");
            var enableTextDelays = gameplayPanel.EnableTextDisplayDelaysCheckBox ?? gameplayPanel.FindControl<CheckBox>("EnableTextDisplayDelaysCheckBox");
            var showDetailedStats = gameplayPanel.ShowDetailedStatsCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowDetailedStatsCheckBox");
            var showHealthBars = gameplayPanel.ShowHealthBarsCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowHealthBarsCheckBox");
            var showDamageNumbers = gameplayPanel.ShowDamageNumbersCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowDamageNumbersCheckBox");
            var showComboProgress = gameplayPanel.ShowComboProgressCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowComboProgressCheckBox");
            if (showIndividual != null) showIndividual.IsChecked = s.ShowIndividualActionMessages;
            if (fastCombat != null) fastCombat.IsChecked = s.FastCombat;
            if (enableTextDelays != null) enableTextDelays.IsChecked = s.EnableTextDisplayDelays;
            if (showDetailedStats != null) showDetailedStats.IsChecked = s.ShowDetailedStats;
            if (showHealthBars != null) showHealthBars.IsChecked = s.ShowHealthBars;
            if (showDamageNumbers != null) showDamageNumbers.IsChecked = s.ShowDamageNumbers;
            if (showComboProgress != null) showComboProgress.IsChecked = s.ShowComboProgress;
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not GameplaySettingsPanel gameplayPanel || settingsManager == null) return;
            var showIndividual = gameplayPanel.ShowIndividualActionMessagesCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowIndividualActionMessagesCheckBox");
            var enableTextDelays = gameplayPanel.EnableTextDisplayDelaysCheckBox ?? gameplayPanel.FindControl<CheckBox>("EnableTextDisplayDelaysCheckBox");
            var fastCombat = gameplayPanel.FastCombatCheckBox ?? gameplayPanel.FindControl<CheckBox>("FastCombatCheckBox");
            var showDetailedStats = gameplayPanel.ShowDetailedStatsCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowDetailedStatsCheckBox");
            var showHealthBars = gameplayPanel.ShowHealthBarsCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowHealthBarsCheckBox");
            var showDamageNumbers = gameplayPanel.ShowDamageNumbersCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowDamageNumbersCheckBox");
            var showComboProgress = gameplayPanel.ShowComboProgressCheckBox ?? gameplayPanel.FindControl<CheckBox>("ShowComboProgressCheckBox");
            if (showIndividual != null)
                settingsManager.SaveGameplaySettings(showIndividual, enableTextDelays!, fastCombat!, showDetailedStats!, showHealthBars!, showDamageNumbers!, showComboProgress!, null);
        }
    }
}

