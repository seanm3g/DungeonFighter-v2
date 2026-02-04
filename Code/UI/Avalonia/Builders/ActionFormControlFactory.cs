using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.Editors;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Creates form section containers and common field controls (text, dropdown, checkbox) for the action form.
    /// </summary>
    public class ActionFormControlFactory
    {
        private readonly Dictionary<string, Control> _controlRegistry;

        public ActionFormControlFactory(Dictionary<string, Control> controlRegistry)
        {
            _controlRegistry = controlRegistry;
        }

        public (Border section, StackPanel contentStack) CreateFormSection(string title)
        {
            var header = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var contentStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            var main = new StackPanel();
            main.Children.Add(header);
            main.Children.Add(contentStack);
            var section = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 150)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                Child = main
            };
            return (section, contentStack);
        }

        public void AddFormField(StackPanel parent, string label, string value, Action<string> setter, string[]? options = null, bool isMultiline = false, string? watermark = null, string? description = null)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            Control inputControl;
            if (options != null && options.Length > 0)
            {
                var comboBox = new ComboBox
                {
                    ItemsSource = options,
                    SelectedItem = value,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                comboBox.SelectionChanged += (s, e) =>
                {
                    if (comboBox.SelectedItem is string selected) setter(selected);
                };
                inputControl = comboBox;
            }
            else if (isMultiline)
            {
                var textBox = new TextBox
                {
                    Text = value,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    MinHeight = 80,
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.Black),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                textBox.LostFocus += (s, e) => setter(textBox.Text ?? "");
                inputControl = textBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = value,
                    FontSize = 14,
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.Black),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                if (!string.IsNullOrEmpty(watermark))
                    textBox.Watermark = watermark;
                textBox.LostFocus += (s, e) => setter(textBox.Text ?? "");
                inputControl = textBox;
            }

            if (!string.IsNullOrEmpty(description))
            {
                var inputStack = new StackPanel { Spacing = 2 };
                inputStack.Children.Add(inputControl);
                inputStack.Children.Add(new TextBlock
                {
                    Text = description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                });
                Grid.SetColumn(inputStack, 1);
                grid.Children.Add(inputStack);
            }
            else
            {
                Grid.SetColumn(inputControl, 1);
                grid.Children.Add(inputControl);
            }
            parent.Children.Add(grid);
            _controlRegistry[label] = inputControl;
        }

        /// <param name="controlName">Optional. When set, the CheckBox gets this Name so the displayed panel can be read at save time (e.g. DefaultStartingCheckBox).</param>
        public void AddBooleanField(StackPanel parent, string label, bool value, Action<bool> setter, string? controlName = null)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var checkBox = new CheckBox
            {
                IsChecked = value,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (!string.IsNullOrEmpty(controlName))
                checkBox.Name = controlName;
            checkBox.IsCheckedChanged += (s, e) =>
            {
                if (checkBox.IsChecked.HasValue)
                    setter(checkBox.IsChecked.Value);
            };

            Grid.SetColumn(checkBox, 1);
            grid.Children.Add(checkBox);
            parent.Children.Add(grid);
            _controlRegistry[label] = checkBox;
        }
    }
}
