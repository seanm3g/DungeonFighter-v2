using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using RPGGame.Editors;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Builds UI controls for variable editing in the tuning menu panel
    /// </summary>
    public class VariableControlBuilder
    {
        /// <summary>
        /// Creates a control for editing a variable
        /// </summary>
        public static (Control Container, TextBox TextBox, TextBlock Indicator) CreateVariableControl(
            EditableVariable variable,
            System.Action<EditableVariable, TextBox, TextBlock> onTextChanged,
            System.Action<EditableVariable, TextBox> onLostFocus,
            System.Action<EditableVariable, TextBox, KeyEventArgs> onKeyDown)
        {
            var container = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(4, 2),
                Margin = new Thickness(0, 0, 0, 1)
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Variable name
            var nameText = new TextBlock
            {
                Text = variable.Name,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 2)
            };
            Grid.SetColumn(nameText, 0);
            Grid.SetRow(nameText, 0);
            Grid.SetColumnSpan(nameText, 2);
            grid.Children.Add(nameText);
            
            // Description
            var descText = new TextBlock
            {
                Text = variable.Description,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 4),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(descText, 0);
            Grid.SetRow(descText, 1);
            Grid.SetColumnSpan(descText, 2);
            grid.Children.Add(descText);
            
            // Value label
            var valueLabel = new TextBlock
            {
                Text = "Value:",
                FontSize = 15,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            Grid.SetColumn(valueLabel, 0);
            Grid.SetRow(valueLabel, 2);
            grid.Children.Add(valueLabel);
            
            // Value text box
            var valueTextBox = new TextBox
            {
                Text = variable.GetValue()?.ToString() ?? "",
                FontSize = 14,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 26,
                Watermark = "Enter value..."
            };
            
            // Set appropriate input type
            var valueType = variable.GetValueType();
            if (valueType == typeof(int))
            {
                valueTextBox.Watermark = "Enter integer...";
            }
            else if (valueType == typeof(double))
            {
                valueTextBox.Watermark = "Enter number...";
            }
            else if (valueType == typeof(bool))
            {
                valueTextBox.Watermark = "Enter true/false...";
            }
            
            // Real-time change indicator
            var changeIndicator = new TextBlock
            {
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0),
                IsVisible = false
            };
            
            // Wire up events
            valueTextBox.TextChanged += (s, e) => onTextChanged(variable, valueTextBox, changeIndicator);
            valueTextBox.LostFocus += (s, e) => onLostFocus(variable, valueTextBox);
            valueTextBox.KeyDown += (s, e) => onKeyDown(variable, valueTextBox, e);
            
            Grid.SetColumn(valueTextBox, 1);
            Grid.SetRow(valueTextBox, 2);
            grid.Children.Add(valueTextBox);
            
            Grid.SetColumn(changeIndicator, 2);
            Grid.SetRow(changeIndicator, 2);
            grid.Children.Add(changeIndicator);
            
            container.Child = grid;
            return (container, valueTextBox, changeIndicator);
        }
    }
}

