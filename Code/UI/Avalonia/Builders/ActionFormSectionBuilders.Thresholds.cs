using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using RPGGame;
using RPGGame.Editors;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Builders
{
    public partial class ActionFormSectionBuilders
    {
        public void BuildThresholdsSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Thresholds");
            parent.Children.Add(section);

            action.NormalizeThresholds();
            if (action.Thresholds == null)
                action.Thresholds = new List<ThresholdEntry>();

            var thresholdsLabel = new TextBlock
            {
                Text = "Qualifier: Enemy/Hero/Environment, Type: attribute, Operator: <,>,=, etc., Value:",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 5)
            };
            stack.Children.Add(thresholdsLabel);

            var thresholdRows = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(thresholdRows);

            for (int i = 0; i < action.Thresholds.Count; i++)
                AddThresholdRow(thresholdRows, action, i);

            var addThresholdButton = new Button
            {
                Content = "Add threshold",
                Width = 140,
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(60, 90, 120)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            addThresholdButton.Click += (s, e) =>
            {
                action.Thresholds.Add(new ThresholdEntry { Qualifier = "", Type = "Health", Operator = "", ValueKind = "#", Value = 0.0 });
                AddThresholdRow(thresholdRows, action, action.Thresholds.Count - 1);
            };
            stack.Children.Add(addThresholdButton);
        }

        private void AddThresholdRow(StackPanel thresholdRows, ActionData action, int index)
        {
            if (index < 0 || index >= action.Thresholds.Count) return;
            var entry = action.Thresholds[index];
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var qualifierCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.ThresholdQualifierOptions,
                SelectedItem = string.IsNullOrEmpty(entry.Qualifier) ? "" : entry.Qualifier,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            qualifierCombo.SelectionChanged += (s, e) =>
            {
                if (index < action.Thresholds.Count && qualifierCombo.SelectedItem is string selected)
                    action.Thresholds[index].Qualifier = selected ?? "";
            };
            Grid.SetColumn(qualifierCombo, 0);
            row.Children.Add(qualifierCombo);

            var typeCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.ThresholdTypeDropdownOptions,
                SelectedItem = entry.Type,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            typeCombo.SelectionChanged += (s, e) =>
            {
                if (index < action.Thresholds.Count && typeCombo.SelectedItem is string selected)
                    action.Thresholds[index].Type = selected ?? "Health";
            };
            Grid.SetColumn(typeCombo, 1);
            row.Children.Add(typeCombo);

            var operatorCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.ThresholdOperatorOptions,
                SelectedItem = string.IsNullOrEmpty(entry.Operator) ? "" : entry.Operator,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            operatorCombo.SelectionChanged += (s, e) =>
            {
                if (index < action.Thresholds.Count && operatorCombo.SelectedItem is string selected)
                    action.Thresholds[index].Operator = selected ?? "";
            };
            Grid.SetColumn(operatorCombo, 2);
            row.Children.Add(operatorCombo);

            var valueBox = new TextBox
            {
                Text = entry.Value.ToString("F2"),
                FontSize = 14,
                Background = new SolidColorBrush(Colors.White),
                Foreground = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            valueBox.LostFocus += (s, e) =>
            {
                if (index < action.Thresholds.Count && double.TryParse(valueBox.Text, out double v))
                    action.Thresholds[index].Value = v;
            };
            Grid.SetColumn(valueBox, 3);
            row.Children.Add(valueBox);

            var valueKindCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.ThresholdValueKindOptions,
                SelectedItem = string.IsNullOrEmpty(entry.ValueKind) ? "#" : (Array.IndexOf(ActionFormOptions.ThresholdValueKindOptions, entry.ValueKind) >= 0 ? entry.ValueKind : "#"),
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            valueKindCombo.SelectionChanged += (s, e) =>
            {
                if (index < action.Thresholds.Count && valueKindCombo.SelectedItem is string selected)
                    action.Thresholds[index].ValueKind = selected ?? "#";
            };
            Grid.SetColumn(valueKindCombo, 4);
            row.Children.Add(valueKindCombo);

            var removeButton = new Button
            {
                Content = "Remove",
                Width = 80,
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(120, 60, 60)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            removeButton.Click += (s, e) =>
            {
                if (index < action.Thresholds.Count)
                {
                    action.Thresholds.RemoveAt(index);
                    thresholdRows.Children.Clear();
                    for (int i = 0; i < action.Thresholds.Count; i++)
                        AddThresholdRow(thresholdRows, action, i);
                }
            };
            Grid.SetColumn(removeButton, 5);
            row.Children.Add(removeButton);

            thresholdRows.Children.Add(row);
        }
    }
}
