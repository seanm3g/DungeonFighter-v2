using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Resources;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Builders
{
    public partial class ActionFormSectionBuilders
    {

        private void AddChainPositionBonusRow(StackPanel chainRows, ActionData action, ChainPositionBonusEntry entry)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var valueBox = new TextBox
            {
                Text = entry.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                FontSize = 14,
                Background = new SolidColorBrush(Colors.White),
                Foreground = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            valueBox.LostFocus += (s, e) =>
            {
                if (double.TryParse(valueBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    entry.Value = v;
            };
            Grid.SetColumn(valueBox, 0);
            row.Children.Add(valueBox);

            var paramCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.ChainPositionBonusTargetOptions,
                SelectedItem = string.IsNullOrEmpty(entry.ModifiesParam) ? ActionFormOptions.ChainPositionBonusTargetOptions[0] : entry.ModifiesParam,
                FontSize = 13,
                Background = SettingsThemeBrushes.InputBackground, Foreground = SettingsThemeBrushes.TextPrimary, BorderBrush = SettingsThemeBrushes.InputBorder
            };
            paramCombo.SelectionChanged += (s, e) =>
            {
                if (paramCombo.SelectedItem is string sel)
                    entry.ModifiesParam = sel ?? "";
            };
            Grid.SetColumn(paramCombo, 1);
            row.Children.Add(paramCombo);

            var kindCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.ChainPositionValueKindOptions,
                SelectedItem = string.IsNullOrEmpty(entry.ValueKind) ? "#" : entry.ValueKind,
                FontSize = 13,
                Background = SettingsThemeBrushes.InputBackground, Foreground = SettingsThemeBrushes.TextPrimary, BorderBrush = SettingsThemeBrushes.InputBorder
            };
            kindCombo.SelectionChanged += (s, e) =>
            {
                if (kindCombo.SelectedItem is string sel)
                    entry.ValueKind = sel ?? "#";
            };
            Grid.SetColumn(kindCombo, 2);
            row.Children.Add(kindCombo);

            var basisCombo = new ComboBox
            {
                ItemsSource = ActionFormOptions.ChainPositionBasisOptions,
                SelectedItem = string.IsNullOrEmpty(entry.PositionBasis) ? ActionFormOptions.ChainPositionBasisOptions[0] : entry.PositionBasis,
                FontSize = 12,
                Background = SettingsThemeBrushes.InputBackground, Foreground = SettingsThemeBrushes.TextPrimary, BorderBrush = SettingsThemeBrushes.InputBorder
            };
            basisCombo.SelectionChanged += (s, e) =>
            {
                if (basisCombo.SelectedItem is string sel)
                    entry.PositionBasis = sel ?? "";
            };
            Grid.SetColumn(basisCombo, 3);
            row.Children.Add(basisCombo);

            var removeButton = new Button
            {
                Content = "Remove",
                Width = 76,
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(120, 60, 60)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            removeButton.Click += (s, e) =>
            {
                action.ChainPositionBonuses.Remove(entry);
                chainRows.Children.Remove(row);
            };
            Grid.SetColumn(removeButton, 4);
            row.Children.Add(removeButton);

            chainRows.Children.Add(row);
        }
    }
}
