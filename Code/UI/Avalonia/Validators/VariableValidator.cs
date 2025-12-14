using Avalonia.Controls;
using Avalonia.Media;
using RPGGame.Editors;
using System;

namespace RPGGame.UI.Avalonia.Validators
{
    /// <summary>
    /// Validates and updates variable values from text input
    /// </summary>
    public static class VariableValidator
    {
        /// <summary>
        /// Validates text input and converts it to the appropriate type
        /// </summary>
        public static (bool IsValid, object? Value) ValidateValue(string text, Type valueType)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, null);
            }
            
            var trimmed = text.Trim();
            
            if (valueType == typeof(int))
            {
                if (int.TryParse(trimmed, out int intValue))
                {
                    return (true, intValue);
                }
            }
            else if (valueType == typeof(double))
            {
                if (double.TryParse(trimmed, out double doubleValue))
                {
                    return (true, doubleValue);
                }
            }
            else if (valueType == typeof(bool))
            {
                string lower = trimmed.ToLower();
                if (bool.TryParse(lower, out bool boolValue))
                {
                    return (true, boolValue);
                }
                else if (lower == "1" || lower == "true" || lower == "t")
                {
                    return (true, true);
                }
                else if (lower == "0" || lower == "false" || lower == "f")
                {
                    return (true, false);
                }
            }
            else if (valueType == typeof(string))
            {
                return (true, trimmed);
            }
            
            return (false, null);
        }
        
        /// <summary>
        /// Validates and updates a variable from a text box
        /// </summary>
        public static (bool Success, string? ErrorMessage) ValidateAndUpdate(
            EditableVariable variable,
            TextBox textBox,
            Action<string, bool> showStatusMessage)
        {
            try
            {
                var valueType = variable.GetValueType();
                var (isValid, newValue) = ValidateValue(textBox.Text ?? "", valueType);
                
                if (isValid && newValue != null)
                {
                    variable.SetValue(newValue);
                    textBox.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    showStatusMessage($"✓ Updated {variable.Name} to {newValue}", false);
                    return (true, null);
                }
                else
                {
                    textBox.Background = new SolidColorBrush(Color.FromRgb(80, 40, 40));
                    string errorMsg = $"Invalid value for {variable.Name}. Expected {valueType.Name}.";
                    showStatusMessage(errorMsg, true);
                    // Reset to current value
                    textBox.Text = variable.GetValue()?.ToString() ?? "";
                    return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                textBox.Background = new SolidColorBrush(Color.FromRgb(80, 40, 40));
                string errorMsg = $"Error updating {variable.Name}: {ex.Message}";
                showStatusMessage(errorMsg, true);
                // Reset to current value
                textBox.Text = variable.GetValue()?.ToString() ?? "";
                return (false, errorMsg);
            }
        }
    }
}

