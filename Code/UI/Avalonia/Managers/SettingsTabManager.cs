using Avalonia.Controls;
using Avalonia.Input;
using RPGGame.Utils;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages tab navigation and persistence for the settings panel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size and improve separation of concerns
    /// </summary>
    public class SettingsTabManager
    {
        private readonly TabControl? tabControl;
        private readonly UserControl? parentControl;

        public SettingsTabManager(TabControl? tabControl, UserControl? parentControl)
        {
            this.tabControl = tabControl;
            this.parentControl = parentControl;
        }

        /// <summary>
        /// Restores the last viewed tab from user preferences
        /// </summary>
        public void RestoreLastTab()
        {
            try
            {
                if (tabControl != null)
                {
                    // Try to load last tab index from a simple storage mechanism
                    // For now, default to first tab (Gameplay)
                    int lastTabIndex = 0;
                    
                    // In the future, this could load from a user preferences file
                    // For now, we'll just ensure the tab control is properly initialized
                    if (tabControl.Items != null && tabControl.Items.Count > 0)
                    {
                        tabControl.SelectedIndex = Math.Clamp(lastTabIndex, 0, tabControl.Items.Count - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsTabManager: Error restoring last tab: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves the current tab index for persistence
        /// </summary>
        public void SaveCurrentTab()
        {
            try
            {
                if (tabControl != null && tabControl.SelectedIndex >= 0)
                {
                    // In the future, this could save to a user preferences file
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsTabManager: Error saving current tab: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets up keyboard navigation for the settings panel
        /// </summary>
        public void SetupKeyboardNavigation()
        {
            try
            {
                if (parentControl == null) return;
                
                // Handle Tab key navigation
                parentControl.KeyDown += (s, e) =>
                {
                    // Tab key navigation is handled automatically by Avalonia
                    // Arrow keys for tab navigation (when TabControl has focus)
                    if (tabControl != null && tabControl.IsFocused)
                    {
                        if (e.Key == Key.Left)
                        {
                            if (tabControl.SelectedIndex > 0)
                            {
                                tabControl.SelectedIndex--;
                                e.Handled = true;
                            }
                        }
                        else if (e.Key == Key.Right)
                        {
                            if (tabControl.SelectedIndex < tabControl.Items.Count - 1)
                            {
                                tabControl.SelectedIndex++;
                                e.Handled = true;
                            }
                        }
                    }
                };
                
                // Save tab when it changes
                if (tabControl != null)
                {
                    tabControl.SelectionChanged += (s, e) =>
                    {
                        SaveCurrentTab();
                    };
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsTabManager: Error setting up keyboard navigation: {ex.Message}");
            }
        }
    }
}

