using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Config;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.Avalonia.Resources;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages ListBox item colors and styles (selected, hover states). Uses GameSettings.Instance at apply time.
    /// </summary>
    public class ListBoxColorManager
    {
        private readonly SettingsPanel? settingsPanel;

        public ListBoxColorManager(SettingsPanel? settingsPanel)
        {
            this.settingsPanel = settingsPanel;
        }

        /// <summary>
        /// Applies ListBox colors and styles
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                var s = GameSettings.Instance;
                var categoryListBox = settingsPanel.FindControl<ListBox>("CategoryListBox");
                if (categoryListBox != null)
                {
                    // Update ListBox background to use settings background
                    categoryListBox.Background = new SolidColorBrush(SettingsColorManager.ParseColor(s.SettingsBackgroundColor));
                    
                    // Update styles programmatically for hover and selected states only
                    UpdateListBoxStyles(categoryListBox);
                    
                    // Update all ListBoxItem instances directly
                    UpdateListBoxItems(categoryListBox);
                    
                    // Ensure selection change updates colors
                    categoryListBox.SelectionChanged -= CategoryListBox_SelectionChanged;
                    categoryListBox.SelectionChanged += CategoryListBox_SelectionChanged;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying ListBox colors: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates CategoryListBox selected/hover styles from appearance settings.
        /// Unselected item chrome stays on SettingsTheme.axaml (#CategoryListBox) defaults.
        /// </summary>
        private void UpdateListBoxStyles(ListBox listBox)
        {
            if (settingsPanel == null || listBox == null) return;

            try
            {
                var s = GameSettings.Instance;
                var selectedColor = SettingsColorManager.ParseColor(s.ListBoxSelectedColor);
                var selectedBackgroundColor = SettingsColorManager.ParseColor(s.ListBoxSelectedBackgroundColor);
                var hoverBackgroundColor = SettingsColorManager.ParseColor(s.ListBoxHoverBackgroundColor);

                var styles = settingsPanel.Styles;
                
                // Remove prior CategoryListBox override styles only
                var existingStyles = styles.Where(st =>
                    st is Style style && style.Selector?.ToString()?.Contains("CategoryListBox") == true).ToList();
                foreach (var style in existingStyles)
                {
                    styles.Remove(style);
                }

                var selectedStyle = new Style(x => x.Name("CategoryListBox").OfType<ListBoxItem>().Class(":selected"));
                selectedStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, new SolidColorBrush(selectedColor)));
                selectedStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(selectedBackgroundColor)));
                styles.Add(selectedStyle);

                var selectedPresenterStyle = new Style(x => x.Name("CategoryListBox").OfType<ListBoxItem>().Class(":selected").Descendant().OfType<ContentPresenter>());
                selectedPresenterStyle.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, new SolidColorBrush(selectedColor)));
                styles.Add(selectedPresenterStyle);

                var hoverStyle = new Style(x => x.Name("CategoryListBox").OfType<ListBoxItem>().Class(":pointerover"));
                hoverStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(hoverBackgroundColor)));
                styles.Add(hoverStyle);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ListBox styles: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates ListBoxItem instances for selection; unselected panel items keep theme sidebar background.
        /// </summary>
        private void UpdateListBoxItems(ListBox listBox)
        {
            if (listBox == null) return;

            try
            {
                var s = GameSettings.Instance;
                var selectedColor = SettingsColorManager.ParseColor(s.ListBoxSelectedColor);
                var selectedBackgroundColor = SettingsColorManager.ParseColor(s.ListBoxSelectedBackgroundColor);
                var unselectedColor = Colors.White;
                var sidebarItemBackground = SettingsThemeBrushes.SidebarItem;

                Dispatcher.UIThread.Post(() =>
                {
                    var items = listBox.GetLogicalDescendants().OfType<ListBoxItem>().ToList();
                    foreach (var item in items)
                    {
                        if (IsGroupHeader(item))
                        {
                            item.ClearValue(ListBoxItem.BackgroundProperty);
                            item.ClearValue(ListBoxItem.ForegroundProperty);
                            continue;
                        }

                        if (item == listBox.SelectedItem)
                        {
                            item.Foreground = new SolidColorBrush(selectedColor);
                            item.Background = new SolidColorBrush(selectedBackgroundColor);
                        }
                        else
                        {
                            item.Foreground = new SolidColorBrush(unselectedColor);
                            item.Background = sidebarItemBackground;
                        }
                    }
                }, DispatcherPriority.Loaded);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ListBox items: {ex.Message}");
            }
        }

        private static bool IsGroupHeader(ListBoxItem item) =>
            item.Classes.Contains("settings-sidebar-group-header")
            || item.Tag is string tag && string.Equals(tag, SettingsSidebarGroups.HeaderTag, StringComparison.Ordinal);

        /// <summary>
        /// Handles ListBox selection changes to update item colors
        /// </summary>
        private void CategoryListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                UpdateListBoxItems(listBox);
            }
        }
    }
}
