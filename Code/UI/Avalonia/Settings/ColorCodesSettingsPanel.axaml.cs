using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Settings.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Colors = Avalonia.Media.Colors;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ColorCodesSettingsPanel : UserControl
    {
        public ColorCodesSettingsPanel()
        {
            InitializeComponent();
            LoadColorCodes();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadColorCodes()
        {
            var colorCodesControl = this.FindControl<ItemsControl>("ColorCodesItemsControl");
            if (colorCodesControl == null) return;

            var config = ColorConfigurationLoader.LoadColorConfiguration();
            var colorCodes = config.ColorCodes ?? new List<ColorCodeData>();

            var items = new List<Control>();
            foreach (var colorCode in colorCodes.OrderBy(c => c.Code))
            {
                var grid = new Grid
                {
                    Margin = new Thickness(2),
                    ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto")
                };

                var codeLabel = new TextBlock
                {
                    Text = $"&{colorCode.Code}",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Colors.Black),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0),
                    Width = 35
                };
                Grid.SetColumn(codeLabel, 0);
                grid.Children.Add(codeLabel);

                var nameLabel = new TextBlock
                {
                    Text = colorCode.Name ?? "",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Black),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Grid.SetColumn(nameLabel, 1);
                grid.Children.Add(nameLabel);

                var preview = new Border
                {
                    Width = 40,
                    Height = 20,
                    Background = new SolidColorBrush(AppearanceSettingsHelper.ParseColor(colorCode.Hex ?? "#FFFFFF")),
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Grid.SetColumn(preview, 2);
                grid.Children.Add(preview);

                var hexTextBox = new TextBox
                {
                    Text = colorCode.Hex ?? "#FFFFFF",
                    Width = 80,
                    Height = 24,
                    Background = new SolidColorBrush(AppearanceSettingsHelper.ParseColor("#FF2A2A2A")),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4, 2),
                    FontSize = 11
                };
                hexTextBox.TextChanged += (s, e) =>
                {
                    if (s is TextBox tb && !string.IsNullOrEmpty(tb.Text))
                    {
                        try
                        {
                            preview.Background = new SolidColorBrush(AppearanceSettingsHelper.ParseColor(tb.Text));
                        }
                        catch { }
                    }
                };
                Grid.SetColumn(hexTextBox, 3);
                grid.Children.Add(hexTextBox);

                items.Add(grid);
            }

            colorCodesControl.ItemsSource = items;
        }
    }
}
