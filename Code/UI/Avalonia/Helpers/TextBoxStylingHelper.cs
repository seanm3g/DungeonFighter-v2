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

