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
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Variable name
            var nameText = new TextBlock
            {
                Text = variable.Name,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetColumn(nameText, 0);
            Grid.SetRow(nameText, 0);
            Grid.SetColumnSpan(nameText, 2);
            grid.Children.Add(nameText);
            
            // Description
            var descText = new TextBlock
            {
                Text = variable.Description,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 8),
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
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(valueLabel, 0);
            Grid.SetRow(valueLabel, 2);
            grid.Children.Add(valueLabel);
            
            // Value text box
            var valueTextBox = new TextBox
            {
                Text = variable.GetValue()?.ToString() ?? "",
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
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
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
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

