using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using RPGGame;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages ListBox item colors and styles (selected, hover states)
    /// </summary>
    public class ListBoxColorManager
    {
        private readonly SettingsPanel? settingsPanel;
        private readonly GameSettings settings;

        public ListBoxColorManager(SettingsPanel? settingsPanel, GameSettings settings)
        {
            this.settingsPanel = settingsPanel;
            this.settings = settings;
        }

        /// <summary>
        /// Applies ListBox colors and styles
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                var categoryListBox = settingsPanel.FindControl<ListBox>("CategoryListBox");
                if (categoryListBox != null)
                {
                    // Update ListBox background to use settings background
                    categoryListBox.Background = new SolidColorBrush(SettingsColorManager.ParseColor(settings.SettingsBackgroundColor));
                    
                    // Update styles programmatically for hover and selected states
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
        /// Updates ListBox styles dynamically to apply appearance settings
        /// </summary>
        private void UpdateListBoxStyles(ListBox listBox)
        {
            if (settingsPanel == null || listBox == null) return;

            try
            {
                var selectedColor = SettingsColorManager.ParseColor(settings.ListBoxSelectedColor);
                var selectedBackgroundColor = SettingsColorManager.ParseColor(settings.ListBoxSelectedBackgroundColor);
                var hoverBackgroundColor = SettingsColorManager.ParseColor(settings.ListBoxHoverBackgroundColor);
                // Use white for unselected items (works well on dark background)
                var unselectedColor = Colors.White;

                // Update styles in the UserControl
                var styles = settingsPanel.Styles;
                
                // Remove existing ListBoxItem styles if they exist
                var existingStyles = styles.Where(s => 
                    s is Style style && style.Selector?.ToString().Contains("ListBoxItem") == true).ToList();
                foreach (var style in existingStyles)
                {
                    styles.Remove(style);
                }

                // Add updated styles
                // Default ListBoxItem style
                var defaultStyle = new Style(x => x.OfType<ListBoxItem>());
                defaultStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, new SolidColorBrush(unselectedColor)));
                defaultStyle.Setters.Add(new Setter(ListBoxItem.FontSizeProperty, 16.0));
                defaultStyle.Setters.Add(new Setter(ListBoxItem.FontWeightProperty, FontWeight.SemiBold));
                defaultStyle.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(15, 12)));
                defaultStyle.Setters.Add(new Setter(ListBoxItem.MarginProperty, new Thickness(0, 2)));
                styles.Add(defaultStyle);

                // Selected ListBoxItem style
                var selectedStyle = new Style(x => x.OfType<ListBoxItem>().Class(":selected"));
                selectedStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, new SolidColorBrush(selectedColor)));
                selectedStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(selectedBackgroundColor)));
                styles.Add(selectedStyle);

                // Hover ListBoxItem style
                var hoverStyle = new Style(x => x.OfType<ListBoxItem>().Class(":pointerover"));
                hoverStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, new SolidColorBrush(unselectedColor)));
                hoverStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(hoverBackgroundColor)));
                styles.Add(hoverStyle);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ListBox styles: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates all ListBoxItem instances directly with appearance settings
        /// </summary>
        private void UpdateListBoxItems(ListBox listBox)
        {
            if (listBox == null) return;

            try
            {
                var selectedColor = SettingsColorManager.ParseColor(settings.ListBoxSelectedColor);
                var selectedBackgroundColor = SettingsColorManager.ParseColor(settings.ListBoxSelectedBackgroundColor);
                // Use white for unselected items (works well on dark background)
                // Could be made configurable in the future
                var unselectedColor = Colors.White;

                Dispatcher.UIThread.Post(() =>
                {
                    var items = listBox.GetLogicalDescendants().OfType<ListBoxItem>().ToList();
                    foreach (var item in items)
                    {
                        if (item == listBox.SelectedItem)
                        {
                            item.Foreground = new SolidColorBrush(selectedColor);
                            item.Background = new SolidColorBrush(selectedBackgroundColor);
                        }
                        else
                        {
                            item.Foreground = new SolidColorBrush(unselectedColor);
                            item.Background = new SolidColorBrush(Colors.Transparent);
                        }
                    }
                }, DispatcherPriority.Loaded);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ListBox items: {ex.Message}");
            }
        }

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
