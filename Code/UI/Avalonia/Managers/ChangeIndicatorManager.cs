using Avalonia.Controls;
using Avalonia.Media;
using RPGGame.Editors;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages change indicators for variable editing
    /// </summary>
    public class ChangeIndicatorManager
    {
        private Dictionary<string, object> originalValues;
        
        public ChangeIndicatorManager()
        {
            originalValues = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Sets the original value for a variable
        /// </summary>
        public void SetOriginalValue(string variableName, object value)
        {
            originalValues[variableName] = value;
        }
        
        /// <summary>
        /// Updates the change indicator based on current text input
        /// </summary>
        public void UpdateChangeIndicator(
            EditableVariable variable,
            TextBox textBox,
            TextBlock indicator,
            Func<string, Type, (bool IsValid, object? Value)> validateValue)
        {
            if (!originalValues.TryGetValue(variable.Name, out var originalValue))
            {
                indicator.IsVisible = false;
                return;
            }
            
            var currentText = textBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(currentText))
            {
                indicator.IsVisible = false;
                return;
            }
            
            try
            {
                var valueType = variable.GetValueType();
                var (isValid, newValue) = validateValue(currentText, valueType);
                
                if (isValid && newValue != null)
                {
                    // Compare with original
                    if (!newValue.Equals(originalValue))
                    {
                        // Calculate change percentage for numeric types
                        if (valueType == typeof(int) || valueType == typeof(double))
                        {
                            try
                            {
                                double orig = Convert.ToDouble(originalValue);
                                double newVal = Convert.ToDouble(newValue);
                                double changePercent = orig != 0 ? ((newVal - orig) / orig) * 100 : 0;
                                
                                indicator.Text = changePercent >= 0 
                                    ? $"+{changePercent:F1}%" 
                                    : $"{changePercent:F1}%";
                                indicator.Foreground = changePercent >= 0 
                                    ? new SolidColorBrush(Color.FromRgb(100, 255, 100))
                                    : new SolidColorBrush(Color.FromRgb(255, 150, 100));
                            }
                            catch
                            {
                                indicator.Text = "Changed";
                                indicator.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                            }
                        }
                        else
                        {
                            indicator.Text = "Changed";
                            indicator.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                        }
                        indicator.IsVisible = true;
                    }
                    else
                    {
                        indicator.IsVisible = false;
                    }
                }
                else
                {
                    indicator.Text = "Invalid";
                    indicator.Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100));
                    indicator.IsVisible = true;
                }
            }
            catch
            {
                indicator.IsVisible = false;
            }
        }
        
        /// <summary>
        /// Updates the original value for a variable (after successful save)
        /// </summary>
        public void UpdateOriginalValue(string variableName, object newValue)
        {
            originalValues[variableName] = newValue;
        }
        
        /// <summary>
        /// Clears all original values
        /// </summary>
        public void Clear()
        {
            originalValues.Clear();
        }
    }
}

