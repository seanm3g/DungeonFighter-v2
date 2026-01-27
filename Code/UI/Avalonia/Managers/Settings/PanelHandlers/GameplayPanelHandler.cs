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
        private readonly GameSettings settings;
        private readonly SettingsManager? settingsManager;
        private readonly Action<string, bool>? showStatusMessage;
        private GameStateManager? stateManager;

        public string PanelType => "Gameplay";

        public GameplayPanelHandler(GameSettings settings, SettingsManager? settingsManager, Action<string, bool>? showStatusMessage = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
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

            // Wire up checkboxes
            if (gameplayPanel.ShowIndividualActionMessagesCheckBox != null)
            {
                gameplayPanel.ShowIndividualActionMessagesCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (gameplayPanel.ShowIndividualActionMessagesCheckBox.IsChecked.HasValue)
                        settings.ShowIndividualActionMessages = gameplayPanel.ShowIndividualActionMessagesCheckBox.IsChecked.Value;
                };
            }

            if (gameplayPanel.FastCombatCheckBox != null)
            {
                gameplayPanel.FastCombatCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (gameplayPanel.FastCombatCheckBox.IsChecked.HasValue)
                        settings.FastCombat = gameplayPanel.FastCombatCheckBox.IsChecked.Value;
                };
            }

            if (gameplayPanel.EnableTextDisplayDelaysCheckBox != null)
            {
                gameplayPanel.EnableTextDisplayDelaysCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (gameplayPanel.EnableTextDisplayDelaysCheckBox.IsChecked.HasValue)
                        settings.EnableTextDisplayDelays = gameplayPanel.EnableTextDisplayDelaysCheckBox.IsChecked.Value;
                };
            }

            if (gameplayPanel.ShowDetailedStatsCheckBox != null)
            {
                gameplayPanel.ShowDetailedStatsCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (gameplayPanel.ShowDetailedStatsCheckBox.IsChecked.HasValue)
                        settings.ShowDetailedStats = gameplayPanel.ShowDetailedStatsCheckBox.IsChecked.Value;
                };
            }

            // Wire up visual indicator checkboxes
            if (gameplayPanel.ShowHealthBarsCheckBox != null)
            {
                gameplayPanel.ShowHealthBarsCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (gameplayPanel.ShowHealthBarsCheckBox.IsChecked.HasValue)
                        settings.ShowHealthBars = gameplayPanel.ShowHealthBarsCheckBox.IsChecked.Value;
                };
            }

            if (gameplayPanel.ShowDamageNumbersCheckBox != null)
            {
                gameplayPanel.ShowDamageNumbersCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (gameplayPanel.ShowDamageNumbersCheckBox.IsChecked.HasValue)
                        settings.ShowDamageNumbers = gameplayPanel.ShowDamageNumbersCheckBox.IsChecked.Value;
                };
            }

            if (gameplayPanel.ShowComboProgressCheckBox != null)
            {
                gameplayPanel.ShowComboProgressCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (gameplayPanel.ShowComboProgressCheckBox.IsChecked.HasValue)
                        settings.ShowComboProgress = gameplayPanel.ShowComboProgressCheckBox.IsChecked.Value;
                };
            }

            // Wire up clear saved characters button
            if (gameplayPanel.ClearSavedCharactersButton != null)
            {
                gameplayPanel.ClearSavedCharactersButton.Click += async (s, e) =>
                {
                    await HandleClearSavedCharactersAsync();
                };
            }

            // Load current settings after panel is fully loaded
            gameplayPanel.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LoadSettings(gameplayPanel);
                }, DispatcherPriority.Loaded);
            };
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

            // Load settings into panel controls with null checks
            // Use FindControl as fallback to ensure controls are found

            if (gameplayPanel.ShowIndividualActionMessagesCheckBox != null)
            {
                gameplayPanel.ShowIndividualActionMessagesCheckBox.IsChecked = settings.ShowIndividualActionMessages;
            }

            if (gameplayPanel.FastCombatCheckBox != null)
            {
                gameplayPanel.FastCombatCheckBox.IsChecked = settings.FastCombat;
            }

            if (gameplayPanel.EnableTextDisplayDelaysCheckBox != null)
            {
                gameplayPanel.EnableTextDisplayDelaysCheckBox.IsChecked = settings.EnableTextDisplayDelays;
            }

            if (gameplayPanel.ShowDetailedStatsCheckBox != null)
            {
                gameplayPanel.ShowDetailedStatsCheckBox.IsChecked = settings.ShowDetailedStats;
            }

            if (gameplayPanel.ShowHealthBarsCheckBox != null)
            {
                gameplayPanel.ShowHealthBarsCheckBox.IsChecked = settings.ShowHealthBars;
            }

            if (gameplayPanel.ShowDamageNumbersCheckBox != null)
            {
                gameplayPanel.ShowDamageNumbersCheckBox.IsChecked = settings.ShowDamageNumbers;
            }

            if (gameplayPanel.ShowComboProgressCheckBox != null)
            {
                gameplayPanel.ShowComboProgressCheckBox.IsChecked = settings.ShowComboProgress;
            }
        }
    }
}

