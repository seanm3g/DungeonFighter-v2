using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Helper class for fixing TextBox focus styling
    /// Extracted from SettingsPanel to reduce complexity
    /// </summary>
    public static class TextBoxStylingHelper
    {
        /// <summary>
        /// Ensures all TextBoxes maintain dark background and white text when focused
        /// This is a backup to the XAML styles to handle cases where the template overrides styles
        /// </summary>
        public static void FixTextBoxFocusStyling(UserControl parent)
        {
            var darkBackground = new SolidColorBrush(Color.FromRgb(42, 42, 42)); // #FF2A2A2A
            var whiteForeground = Brushes.White;
            var blueBorder = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // #FF0078D4
            var defaultBorder = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // #FF555555
            
            // Use Dispatcher to ensure this runs after the visual tree is loaded
            Dispatcher.UIThread.Post(() =>
            {
                // Find all TextBoxes using Avalonia's visual tree traversal
                var textBoxes = new List<TextBox>();
                FindTextBoxes(parent, textBoxes);
                
                // Apply focus handlers to all found TextBoxes
                foreach (var textBox in textBoxes)
                {
                    // Only fix TextBoxes that have dark backgrounds (settings TextBoxes)
                    var bg = textBox.Background as SolidColorBrush;
                    if (bg != null && (bg.Color.R == 42 && bg.Color.G == 42 && bg.Color.B == 42))
                    {
                        textBox.GotFocus += (s, e) =>
                        {
                            textBox.Background = darkBackground;
                            textBox.Foreground = whiteForeground;
                            textBox.BorderBrush = blueBorder;
                        };
                        
                        textBox.LostFocus += (s, e) =>
                        {
                            textBox.Background = darkBackground;
                            textBox.Foreground = whiteForeground;
                            textBox.BorderBrush = defaultBorder;
                        };
                    }
                }
            }, DispatcherPriority.Loaded);
        }
        
        private static void FindTextBoxes(Control control, List<TextBox> textBoxes)
        {
            if (control is TextBox tb)
            {
                textBoxes.Add(tb);
            }
            
            // Traverse visual children
            var visualChildren = control.GetVisualChildren();
            foreach (var child in visualChildren)
            {
                if (child is Control childControl)
                {
                    FindTextBoxes(childControl, textBoxes);
                }
            }
            
            // Also check logical children for ContentControls
            if (control is ContentControl cc && cc.Content is Control content)
            {
                FindTextBoxes(content, textBoxes);
            }
            
            // Check Panel children
            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Control childControl)
                    {
                        FindTextBoxes(childControl, textBoxes);
                    }
                }
            }
        }
    }
}

