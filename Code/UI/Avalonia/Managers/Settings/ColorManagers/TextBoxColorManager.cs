using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages TextBox colors and focus states (text, background, border, focus)
    /// </summary>
    public class TextBoxColorManager
    {
        private readonly SettingsPanel? settingsPanel;
        private readonly GameSettings settings;

        public TextBoxColorManager(SettingsPanel? settingsPanel, GameSettings settings)
        {
            this.settingsPanel = settingsPanel;
            this.settings = settings;
        }

        /// <summary>
        /// Applies TextBox colors and sets up focus event handlers
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            // Use Dispatcher to ensure this runs after the visual tree is loaded
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var textColor = SettingsColorManager.ParseColor(settings.TextBoxTextColor);
                    var darkBackground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBackgroundColor));
                    var hoverBackground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxHoverBackgroundColor));
                    var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxFocusBorderColor));
                    var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBorderColor));

                    // Find all TextBoxes in settings panels
                    var textBoxes = settingsPanel.GetLogicalDescendants().OfType<TextBox>()
                        .Where(tb => tb.Name != "PanelBackgroundTextBox" &&
                                    tb.Name != "PanelBorderTextBox" &&
                                    tb.Name != "PanelTextTextBox" &&
                                    tb.Name != "SettingsBackgroundTextBox" &&
                                    tb.Name != "SettingsTitleTextBox" &&
                                    tb.Name != "ListBoxSelectedTextBox" &&
                                    tb.Name != "ListBoxSelectedBackgroundTextBox" &&
                                    tb.Name != "ListBoxHoverBackgroundTextBox" &&
                                    tb.Name != "ButtonPrimaryTextBox" &&
                                    tb.Name != "ButtonSecondaryTextBox" &&
                                    tb.Name != "ButtonBackTextBox" &&
                                    tb.Name != "TextBoxTextColorTextBox" &&
                                    tb.Name != "TextBoxBackgroundTextBox" &&
                                    tb.Name != "TextBoxHoverBackgroundTextBox" &&
                                    tb.Name != "TextBoxBorderTextBox" &&
                                    tb.Name != "TextBoxFocusBorderTextBox")
                        .ToList();

                    foreach (var textBox in textBoxes)
                    {
                        // Check if this is a settings TextBox (has dark background or is in a settings panel)
                        var bg = textBox.Background as SolidColorBrush;
                        var expectedBgColor = SettingsColorManager.ParseColor(settings.TextBoxBackgroundColor);
                        bool isSettingsTextBox = bg != null && (bg.Color.R == expectedBgColor.R && bg.Color.G == expectedBgColor.G && bg.Color.B == expectedBgColor.B);
                        
                        // Also check if background is white (which means it needs fixing)
                        bool needsFixing = bg != null && bg.Color.R == 255 && bg.Color.G == 255 && bg.Color.B == 255;
                        
                        // Also apply to any TextBox in the settings panel (to catch newly created ones)
                        bool isInSettingsPanel = textBox.GetLogicalAncestors().OfType<SettingsPanel>().Any();
                        
                        if (isSettingsTextBox || needsFixing || isInSettingsPanel)
                        {
                            // Set background to dark gray immediately
                            textBox.Background = darkBackground;
                            // Set text color from setting
                            textBox.Foreground = new SolidColorBrush(textColor);
                            // Set selection colors for visible text selection
                            textBox.SelectionBrush = blueBorder;
                            textBox.SelectionForegroundBrush = new SolidColorBrush(Colors.White);
                            
                            // Remove existing event handlers to avoid duplicates
                            textBox.GotFocus -= TextBox_GotFocus;
                            textBox.LostFocus -= TextBox_LostFocus;
                            textBox.PointerEntered -= TextBox_PointerEntered;
                            textBox.PointerExited -= TextBox_PointerExited;
                            
                            // Add event handlers to maintain dark background
                            textBox.GotFocus += TextBox_GotFocus;
                            textBox.LostFocus += TextBox_LostFocus;
                            textBox.PointerEntered += TextBox_PointerEntered;
                            textBox.PointerExited += TextBox_PointerExited;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating TextBox text colors: {ex.Message}");
                }
            }, DispatcherPriority.Loaded);
        }
        
        private void TextBox_GotFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var darkBackground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBackgroundColor));
                var textColor = SettingsColorManager.ParseColor(settings.TextBoxTextColor);
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxFocusBorderColor));
                
                textBox.Background = darkBackground;
                textBox.Foreground = new SolidColorBrush(textColor);
                textBox.BorderBrush = blueBorder;
                textBox.SelectionBrush = blueBorder;
                textBox.SelectionForegroundBrush = new SolidColorBrush(Colors.White);
            }
        }
        
        private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var darkBackground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBackgroundColor));
                var textColor = SettingsColorManager.ParseColor(settings.TextBoxTextColor);
                var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBorderColor));
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxFocusBorderColor));
                
                textBox.Background = darkBackground;
                textBox.Foreground = new SolidColorBrush(textColor);
                textBox.BorderBrush = defaultBorder;
                textBox.SelectionBrush = blueBorder;
                textBox.SelectionForegroundBrush = new SolidColorBrush(Colors.White);
            }
        }
        
        private void TextBox_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // On hover, use slightly lighter background but still dark
                var hoverBackground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxHoverBackgroundColor));
                var textColor = SettingsColorManager.ParseColor(settings.TextBoxTextColor);
                var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBorderColor));
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxFocusBorderColor));
                
                // Only change if not focused (if focused, keep the focus styling)
                if (!textBox.IsFocused)
                {
                    textBox.Background = hoverBackground;
                    textBox.Foreground = new SolidColorBrush(textColor);
                    textBox.BorderBrush = defaultBorder;
                }
                else
                {
                    // If focused, maintain focus styling
                    var darkBackground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBackgroundColor));
                    textBox.Background = darkBackground;
                    textBox.Foreground = new SolidColorBrush(textColor);
                    textBox.BorderBrush = blueBorder;
                }
                textBox.SelectionBrush = blueBorder;
                textBox.SelectionForegroundBrush = new SolidColorBrush(Colors.White);
            }
        }
        
        private void TextBox_PointerExited(object? sender, PointerEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var darkBackground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBackgroundColor));
                var textColor = SettingsColorManager.ParseColor(settings.TextBoxTextColor);
                var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxBorderColor));
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(settings.TextBoxFocusBorderColor));
                
                // If focused, keep focus styling, otherwise use default
                if (textBox.IsFocused)
                {
                    textBox.Background = darkBackground;
                    textBox.Foreground = new SolidColorBrush(textColor);
                    textBox.BorderBrush = blueBorder;
                }
                else
                {
                    textBox.Background = darkBackground;
                    textBox.Foreground = new SolidColorBrush(textColor);
                    textBox.BorderBrush = defaultBorder;
                }
                textBox.SelectionBrush = blueBorder;
                textBox.SelectionForegroundBrush = new SolidColorBrush(Colors.White);
            }
        }
    }
}
