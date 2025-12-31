using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Settings;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Handles wiring and loading for the Gameplay settings panel
    /// </summary>
    public class GameplayPanelHandler : ISettingsPanelHandler
    {
        private readonly GameSettings settings;
        private readonly SettingsManager? settingsManager;

        public string PanelType => "Gameplay";

        public GameplayPanelHandler(GameSettings settings, SettingsManager? settingsManager)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.settingsManager = settingsManager;
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

            // Load current settings after panel is fully loaded
            gameplayPanel.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LoadSettings(gameplayPanel);
                }, DispatcherPriority.Loaded);
            };
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

