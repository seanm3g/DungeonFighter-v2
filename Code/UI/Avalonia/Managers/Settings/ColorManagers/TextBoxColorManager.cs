using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Config;
using RPGGame.UI.Avalonia.Settings;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages TextBox colors and focus states (text, background, border, focus). Uses GameSettings.Instance at apply time.
    /// </summary>
    public class TextBoxColorManager
    {
        private readonly SettingsPanel? settingsPanel;

        public TextBoxColorManager(SettingsPanel? settingsPanel)
        {
            this.settingsPanel = settingsPanel;
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
                    var s = GameSettings.Instance;
                    var backgroundColor = SettingsColorManager.ParseColor(s.TextBoxBackgroundColor);
                    var textColor = SettingsColorManager.EnsureContrastingTextColor(
                        SettingsColorManager.ParseColor(s.TextBoxTextColor),
                        backgroundColor);
                    var darkBackground = new SolidColorBrush(backgroundColor);
                    var hoverBackground = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxHoverBackgroundColor));
                    var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxFocusBorderColor));
                    var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxBorderColor));

                    // Find all TextBoxes in settings panels
                    var textBoxes = settingsPanel.GetLogicalDescendants().OfType<TextBox>()
                        .Where(tb => !IsTextAnimationPanelTextBox(tb) &&
                                    tb.Name != "PanelBackgroundTextBox" &&
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
                        textBox.Background = darkBackground;
                        textBox.Foreground = new SolidColorBrush(textColor);
                        textBox.BorderBrush = defaultBorder;
                        textBox.SelectionBrush = blueBorder;
                        textBox.SelectionForegroundBrush = new SolidColorBrush(Colors.White);
                        textBox.InvalidateVisual();

                        textBox.GotFocus -= TextBox_GotFocus;
                        textBox.LostFocus -= TextBox_LostFocus;
                        textBox.PointerEntered -= TextBox_PointerEntered;
                        textBox.PointerExited -= TextBox_PointerExited;

                        textBox.GotFocus += TextBox_GotFocus;
                        textBox.LostFocus += TextBox_LostFocus;
                        textBox.PointerEntered += TextBox_PointerEntered;
                        textBox.PointerExited += TextBox_PointerExited;
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating TextBox text colors: {ex.Message}");
                }
            }, DispatcherPriority.Loaded);
        }
        
        private static bool IsTextAnimationPanelTextBox(TextBox textBox)
        {
            for (var node = textBox as ILogical; node != null; node = node.LogicalParent)
            {
                if (node is TextAnimationPresetsSettingsPanel)
                    return true;
            }

            return false;
        }

        private static Color ResolveTextBoxColors(out Color backgroundColor)
        {
            var s = GameSettings.Instance;
            backgroundColor = SettingsColorManager.ParseColor(s.TextBoxBackgroundColor);
            return SettingsColorManager.EnsureContrastingTextColor(
                SettingsColorManager.ParseColor(s.TextBoxTextColor),
                backgroundColor);
        }

        private void TextBox_GotFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var s = GameSettings.Instance;
                var textColor = ResolveTextBoxColors(out var backgroundColor);
                var darkBackground = new SolidColorBrush(backgroundColor);
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxFocusBorderColor));
                
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
                var s = GameSettings.Instance;
                var textColor = ResolveTextBoxColors(out var backgroundColor);
                var darkBackground = new SolidColorBrush(backgroundColor);
                var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxBorderColor));
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxFocusBorderColor));
                
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
                var s = GameSettings.Instance;
                var textColor = ResolveTextBoxColors(out _);
                var hoverBackground = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxHoverBackgroundColor));
                var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxBorderColor));
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxFocusBorderColor));
                
                // Only change if not focused (if focused, keep the focus styling)
                if (!textBox.IsFocused)
                {
                    textBox.Background = hoverBackground;
                    textBox.Foreground = new SolidColorBrush(textColor);
                    textBox.BorderBrush = defaultBorder;
                }
                else
                {
                    var darkBackground = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxBackgroundColor));
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
                var s = GameSettings.Instance;
                var textColor = ResolveTextBoxColors(out var backgroundColor);
                var darkBackground = new SolidColorBrush(backgroundColor);
                var defaultBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxBorderColor));
                var blueBorder = new SolidColorBrush(SettingsColorManager.ParseColor(s.TextBoxFocusBorderColor));
                
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
