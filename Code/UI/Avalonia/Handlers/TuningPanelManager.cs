using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame.Editors;
using RPGGame.Utils;
using System;

namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Manages tuning panel show/hide operations for MainWindow
    /// </summary>
    public class TuningPanelManager
    {
        private TuningMenuPanel? tuningMenuPanel;
        private Border? tuningPanelOverlay;
        private GameCoordinator? game;
        
        public TuningPanelManager(TuningMenuPanel tuningMenuPanel, Border tuningPanelOverlay, GameCoordinator? game)
        {
            this.tuningMenuPanel = tuningMenuPanel;
            this.tuningPanelOverlay = tuningPanelOverlay;
            this.game = game;
        }
        
        public void SetGame(GameCoordinator? game)
        {
            this.game = game;
        }
        
        /// <summary>
        /// Shows the tuning menu panel with the specified variable editor
        /// </summary>
        public void ShowTuningMenuPanel(VariableEditor variableEditor, System.Action<string> updateStatus)
        {
            ScrollDebugLogger.Log($"TuningPanelManager.ShowTuningMenuPanel: variableEditor={variableEditor != null}");
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    ScrollDebugLogger.Log("TuningPanelManager.ShowTuningMenuPanel: Initializing panel");
                    if (variableEditor != null)
                    {
                        tuningMenuPanel?.Initialize(variableEditor);
                    }
                    ScrollDebugLogger.Log($"TuningPanelManager.ShowTuningMenuPanel: Setting overlay visible, current IsVisible={tuningPanelOverlay?.IsVisible}");
                    
                    // Make sure the overlay is visible
                    if (tuningPanelOverlay != null)
                    {
                        tuningPanelOverlay.IsVisible = true;
                        
                        // Bring to front
                        if (tuningPanelOverlay.Parent is Panel parentPanel)
                        {
                            var currentIndex = parentPanel.Children.IndexOf(tuningPanelOverlay);
                            if (currentIndex >= 0 && currentIndex < parentPanel.Children.Count - 1)
                            {
                                parentPanel.Children.Remove(tuningPanelOverlay);
                                parentPanel.Children.Add(tuningPanelOverlay);
                            }
                        }
                    }
                    
                    ScrollDebugLogger.Log($"TuningPanelManager.ShowTuningMenuPanel: Overlay IsVisible now={tuningPanelOverlay?.IsVisible}, Panel IsVisible={tuningMenuPanel?.IsVisible}");
                    
                    // Wire up events
                    if (tuningMenuPanel != null)
                    {
                        tuningMenuPanel.BackRequested += OnTuningPanelBackRequested;
                        tuningMenuPanel.SaveRequested += OnTuningPanelSaveRequested;
                    }
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"TuningPanelManager.ShowTuningMenuPanel: Error - {ex.Message}\n{ex.StackTrace}");
                    updateStatus($"Error showing tuning menu: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Hides the tuning menu panel
        /// </summary>
        public void HideTuningMenuPanel()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (tuningPanelOverlay != null)
                {
                    tuningPanelOverlay.IsVisible = false;
                }
                
                // Unwire events
                if (tuningMenuPanel != null)
                {
                    tuningMenuPanel.BackRequested -= OnTuningPanelBackRequested;
                    tuningMenuPanel.SaveRequested -= OnTuningPanelSaveRequested;
                }
            });
        }
        
        private async void OnTuningPanelBackRequested(object? sender, EventArgs e)
        {
            if (game != null)
            {
                HideTuningMenuPanel();
                await game.HandleInput("0");
            }
        }
        
        private async void OnTuningPanelSaveRequested(object? sender, EventArgs e)
        {
            if (game != null && tuningMenuPanel != null)
            {
                tuningMenuPanel.ShowStatusMessage("Saving changes...", isError: false);
                
                // Trigger save via game input
                await game.HandleInput("s");
                
                // Refresh the panel to show updated values
                tuningMenuPanel.RefreshVariableValues();
            }
        }
    }
}

