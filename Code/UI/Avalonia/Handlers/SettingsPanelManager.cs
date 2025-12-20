using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Utils;
using System;

namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Manages settings panel show/hide operations for MainWindow
    /// </summary>
    public class SettingsPanelManager
    {
        private SettingsPanel? settingsPanel;
        private Border? settingsPanelOverlay;
        private GameCoordinator? game;
        
        public SettingsPanelManager(SettingsPanel settingsPanel, Border settingsPanelOverlay, GameCoordinator? game)
        {
            this.settingsPanel = settingsPanel;
            this.settingsPanelOverlay = settingsPanelOverlay;
            this.game = game;
        }
        
        public void SetGame(GameCoordinator? game)
        {
            this.game = game;
        }
        
        /// <summary>
        /// Shows the settings panel
        /// </summary>
        public void ShowSettingsPanel(System.Action<string> updateStatus)
        {
            ScrollDebugLogger.Log("SettingsPanelManager.ShowSettingsPanel: Showing settings panel");
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Make sure the overlay is visible
                    if (settingsPanelOverlay != null)
                    {
                        settingsPanelOverlay.IsVisible = true;
                        
                        // Bring to front
                        if (settingsPanelOverlay.Parent is Panel parentPanel)
                        {
                            var currentIndex = parentPanel.Children.IndexOf(settingsPanelOverlay);
                            if (currentIndex >= 0 && currentIndex < parentPanel.Children.Count - 1)
                            {
                                parentPanel.Children.Remove(settingsPanelOverlay);
                                parentPanel.Children.Add(settingsPanelOverlay);
                            }
                        }
                    }
                    
                    // Wire up callbacks
                    if (settingsPanel != null)
                    {
                        settingsPanel.SetBackCallback(OnSettingsPanelBackRequested);
                        settingsPanel.SetStatusCallback(updateStatus);
                    }
                    
                    ScrollDebugLogger.Log($"SettingsPanelManager.ShowSettingsPanel: Overlay IsVisible={settingsPanelOverlay?.IsVisible}");
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"SettingsPanelManager.ShowSettingsPanel: Error - {ex.Message}\n{ex.StackTrace}");
                    updateStatus($"Error showing settings panel: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Hides the settings panel
        /// </summary>
        public void HideSettingsPanel()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (settingsPanelOverlay != null)
                {
                    settingsPanelOverlay.IsVisible = false;
                }
            });
        }
        
        private async void OnSettingsPanelBackRequested()
        {
            if (game != null)
            {
                HideSettingsPanel();
                await game.HandleEscapeKey();
            }
        }
    }
}

