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
        public void BuildAdvancedSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Advanced Mechanics");
            parent.Children.Add(section);

            action.NormalizeStatBonuses();
            if (action.StatBonuses == null)
                action.StatBonuses = new List<StatBonusEntry>();

            var statBonusLabel = new TextBlock
            {
                Text = "Stat Bonuses (duration from Cadence):",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 5)
            };
            stack.Children.Add(statBonusLabel);

            var statBonusRows = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(statBonusRows);

            foreach (var entry in action.StatBonuses)
                AddStatBonusRow(statBonusRows, action, entry);

            var addStatBonusButton = new Button
            {
                Content = "Add stat bonus",
                Width = 140,
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(60, 90, 120)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            addStatBonusButton.Click += (s, e) =>
            {
                action.StatBonuses.Add(new StatBonusEntry());
                AddStatBonusRow(statBonusRows, action, action.StatBonuses[action.StatBonuses.Count - 1]);
            };
            stack.Children.Add(addStatBonusButton);
        }

        private void AddStatBonusRow(StackPanel statBonusRows, ActionData action, StatBonusEntry entry)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var valueBox = new TextBox
            {
                Text = entry.Value.ToString(),
                FontSize = 14,
                Background = new SolidColorBrush(Colors.White),
                Foreground = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            valueBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(valueBox.Text, out int v))
                    entry.Value = v;
            };
            Grid.SetColumn(valueBox, 0);
            row.Children.Add(valueBox);

            var typeCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.StatBonusTypeDropdownOptions,
                SelectedItem = entry.Type,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            typeCombo.SelectionChanged += (s, e) =>
            {
                if (typeCombo.SelectedItem is string selected)
                    entry.Type = selected;
            };
            Grid.SetColumn(typeCombo, 1);
            row.Children.Add(typeCombo);

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
                action.StatBonuses.Remove(entry);
                statBonusRows.Children.Remove(row);
            };
            Grid.SetColumn(removeButton, 2);
            row.Children.Add(removeButton);

            statBonusRows.Children.Add(row);
        }
    }
}
