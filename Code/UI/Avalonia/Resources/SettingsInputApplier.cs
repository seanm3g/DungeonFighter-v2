using Avalonia.Controls;
using Avalonia.Media;

namespace RPGGame.UI.Avalonia.Resources
{
    /// <summary>Applies uniform settings-theme chrome to controls built in code.</summary>
    public static class SettingsInputApplier
    {
        public static void ApplyTextBlock(TextBlock textBlock, bool muted = false)
        {
            textBlock.Foreground = muted ? SettingsThemeBrushes.TextMuted : SettingsThemeBrushes.TextPrimary;
        }

        public static void ApplyCheckBox(CheckBox checkBox)
        {
            checkBox.Foreground = SettingsThemeBrushes.TextPrimary;
        }

        public static void ApplyTextBox(TextBox textBox, bool isError = false)
        {
            textBox.Background = isError ? SettingsThemeBrushes.InputErrorBackground : SettingsThemeBrushes.InputBackground;
            textBox.Foreground = SettingsThemeBrushes.TextPrimary;
            textBox.CaretBrush = SettingsThemeBrushes.TextPrimary;
            textBox.BorderBrush = isError ? SettingsThemeBrushes.InputErrorBorder : SettingsThemeBrushes.InputBorder;
            textBox.SelectionBrush = SettingsThemeBrushes.InputFocusBorder;
            textBox.SelectionForegroundBrush = Brushes.White;
        }

        public static void ApplyComboBox(ComboBox comboBox)
        {
            comboBox.Background = SettingsThemeBrushes.InputBackground;
            comboBox.Foreground = SettingsThemeBrushes.TextPrimary;
            comboBox.BorderBrush = SettingsThemeBrushes.InputBorder;
        }

        public static void ApplyNumericUpDown(NumericUpDown numericUpDown)
        {
            numericUpDown.Background = SettingsThemeBrushes.InputBackground;
            numericUpDown.Foreground = SettingsThemeBrushes.TextPrimary;
            numericUpDown.BorderBrush = SettingsThemeBrushes.InputBorder;
        }
    }
}
