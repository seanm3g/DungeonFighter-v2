using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using RPGGame;
using RPGGame.Editors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Builders
{
    public partial class ActionFormSectionBuilders
    {
        public void BuildAccumulationsSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Accumulations");
            parent.Children.Add(section);

            stack.Children.Add(new TextBlock
            {
                Text = "Per [accumulation]: modify [param] by [value]. E.g. +5 Damage for each Hits landed.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 200)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            action.NormalizeAccumulations();
            if (action.Accumulations == null)
                action.Accumulations = new List<AccumulationEntry>();

            var accumulationRows = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(accumulationRows);

            for (int i = 0; i < action.Accumulations.Count; i++)
                AddAccumulationRow(accumulationRows, action, i);

            var addAccumulationButton = new Button
            {
                Content = "Add accumulation",
                Width = 160,
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(60, 90, 120)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            addAccumulationButton.Click += (s, e) =>
            {
                action.Accumulations.Add(new AccumulationEntry { Type = "HitsLanded", ModifiesParam = "Damage", ValueKind = "#", Value = 0 });
                AddAccumulationRow(accumulationRows, action, action.Accumulations.Count - 1);
            };
            stack.Children.Add(addAccumulationButton);
        }

        private void AddAccumulationRow(StackPanel accumulationRows, ActionData action, int index)
        {
            if (index < 0 || index >= action.Accumulations.Count) return;
            var entry = action.Accumulations[index];
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var typeCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.AccumulationTypeOptions.Select(x => x.Label).ToList(),
                SelectedItem = ActionFormOptions.AccumulationTypeOptions.FirstOrDefault(x => string.Equals(x.Type, entry.Type, StringComparison.OrdinalIgnoreCase)).Label ?? entry.Type,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            typeCombo.SelectionChanged += (s, e) =>
            {
                if (index < action.Accumulations.Count && typeCombo.SelectedItem is string label)
                {
                    var pair = ActionFormOptions.AccumulationTypeOptions.FirstOrDefault(x => x.Label == label);
                    if (pair.Type != null)
                        action.Accumulations[index].Type = pair.Type;
                }
            };
            Grid.SetColumn(typeCombo, 0);
            row.Children.Add(typeCombo);

            var modifiesCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.AccumulationModifiesOptions,
                SelectedItem = string.IsNullOrEmpty(entry.ModifiesParam) || Array.IndexOf(ActionFormOptions.AccumulationModifiesOptions, entry.ModifiesParam) < 0 ? "Damage" : entry.ModifiesParam,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            modifiesCombo.SelectionChanged += (s, e) =>
            {
                if (index < action.Accumulations.Count && modifiesCombo.SelectedItem is string selected)
                    action.Accumulations[index].ModifiesParam = selected ?? "Damage";
            };
            Grid.SetColumn(modifiesCombo, 1);
            row.Children.Add(modifiesCombo);

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
                if (index < action.Accumulations.Count && double.TryParse(valueBox.Text, out double v))
                    action.Accumulations[index].Value = v;
            };
            Grid.SetColumn(valueBox, 2);
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
                if (index < action.Accumulations.Count && valueKindCombo.SelectedItem is string selected)
                    action.Accumulations[index].ValueKind = selected ?? "#";
            };
            Grid.SetColumn(valueKindCombo, 3);
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
                if (index < action.Accumulations.Count)
                {
                    action.Accumulations.RemoveAt(index);
                    accumulationRows.Children.Remove(row);
                    accumulationRows.Children.Clear();
                    for (int i = 0; i < action.Accumulations.Count; i++)
                        AddAccumulationRow(accumulationRows, action, i);
                }
            };
            Grid.SetColumn(removeButton, 4);
            row.Children.Add(removeButton);

            accumulationRows.Children.Add(row);
        }
    }
}
