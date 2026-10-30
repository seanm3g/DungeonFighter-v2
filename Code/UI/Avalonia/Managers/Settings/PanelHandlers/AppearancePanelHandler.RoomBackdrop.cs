using Avalonia.Controls;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Effects;
using RPGGame.UI.Avalonia.Settings;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Room Backdrop tab: edits <see cref="RoomBackdropSettings.Current"/> live (the canvas repaints on every change);
    /// Save persists it to UIConfiguration.json → roomBackdrop.
    /// </summary>
    public partial class AppearancePanelHandler
    {
        private bool loadingRoomBackdrop;

        private sealed record RoomBackdropSlider(string Name, double Max, Func<RoomBackdropConfig, double> Get, Action<RoomBackdropConfig, double> Set);

        private static readonly RoomBackdropSlider[] RoomBackdropSliders =
        {
            new("Brightness", RoomBackdropConfig.MaxBrightness, c => c.Brightness, (c, v) => c.Brightness = v),
            new("Contrast", RoomBackdropConfig.MaxContrast, c => c.Contrast, (c, v) => c.Contrast = v),
            new("Saturation", RoomBackdropConfig.MaxSaturation, c => c.Saturation, (c, v) => c.Saturation = v),
            new("Blur", RoomBackdropConfig.MaxBlur, c => c.Blur, (c, v) => c.Blur = v),
            new("Opacity", 1, c => c.Opacity, (c, v) => c.Opacity = v),
            new("AsciiImageUnderlay", 1, c => c.AsciiImageUnderlay, (c, v) => c.AsciiImageUnderlay = v),
            new("AsciiBackground", 1, c => c.AsciiBackground, (c, v) => c.AsciiBackground = v),
            new("AsciiGlyphGain", RoomBackdropConfig.MaxAsciiGlyphGain, c => c.AsciiGlyphGain, (c, v) => c.AsciiGlyphGain = v),
            new("AsciiVariety", 1, c => c.AsciiVariety, (c, v) => c.AsciiVariety = v),
            new("AsciiDensity", RoomBackdropConfig.MaxAsciiDensity, c => c.AsciiDensity, (c, v) => c.AsciiDensity = Math.Max(1, Math.Round(v))),
            new("ItemAsciiDensity", RoomBackdropConfig.MaxItemAsciiDensity, c => c.ItemAsciiDensity, (c, v) => c.ItemAsciiDensity = Math.Max(1, Math.Round(v))),
            new("ItemAsciiGlyphGain", RoomBackdropConfig.MaxAsciiGlyphGain, c => c.ItemAsciiGlyphGain, (c, v) => c.ItemAsciiGlyphGain = v),
            new("ItemAsciiBackground", 1, c => c.ItemAsciiBackground, (c, v) => c.ItemAsciiBackground = v),
            new("ItemAsciiCutoff", RoomBackdropConfig.MaxItemAsciiCutoff, c => c.ItemAsciiCutoff, (c, v) => c.ItemAsciiCutoff = v),
        };

        private void WireUpRoomBackdropControls(AppearanceSettingsPanel appearancePanel)
        {
            foreach (var spec in RoomBackdropSliders)
            {
                var slider = appearancePanel.FindControl<Slider>($"RoomBackdrop{spec.Name}Slider");
                var textBox = appearancePanel.FindControl<TextBox>($"RoomBackdrop{spec.Name}TextBox");
                if (slider == null || textBox == null)
                    continue;

                slider.ValueChanged += (_, e) =>
                {
                    textBox.Text = e.NewValue.ToString("F2");
                    if (loadingRoomBackdrop)
                        return;
                    spec.Set(RoomBackdropSettings.Current, e.NewValue);
                    RefreshRoomBackdrop();
                };
                textBox.LostFocus += (_, _) =>
                {
                    if (double.TryParse(textBox.Text, out double v))
                        slider.Value = Math.Clamp(v, 0, spec.Max);
                    textBox.Text = slider.Value.ToString("F2");
                };
            }

            var enabled = appearancePanel.FindControl<CheckBox>("RoomBackdropEnabledCheckBox");
            if (enabled != null)
            {
                enabled.IsCheckedChanged += (_, _) =>
                {
                    if (loadingRoomBackdrop)
                        return;
                    RoomBackdropSettings.Current.Enabled = enabled.IsChecked == true;
                    RefreshRoomBackdrop();
                };
            }

            var itemAscii = appearancePanel.FindControl<CheckBox>("RoomBackdropItemAsciiCheckBox");
            if (itemAscii != null)
            {
                itemAscii.IsCheckedChanged += (_, _) =>
                {
                    if (loadingRoomBackdrop)
                        return;
                    RoomBackdropSettings.Current.ItemAscii = itemAscii.IsChecked == true;
                    RefreshRoomBackdrop();
                };
            }

            var blendMode = appearancePanel.FindControl<ComboBox>("RoomBackdropBlendModeComboBox");
            if (blendMode != null)
            {
                blendMode.ItemsSource = RoomBackdropConfig.BlendModeNames;
                blendMode.SelectionChanged += (_, _) =>
                {
                    if (loadingRoomBackdrop || blendMode.SelectedItem is not string mode)
                        return;
                    RoomBackdropSettings.Current.BlendMode = mode;
                    RefreshRoomBackdrop();
                };
            }

            var style = appearancePanel.FindControl<ComboBox>("RoomBackdropStyleComboBox");
            if (style != null)
            {
                style.ItemsSource = RoomBackdropConfig.StyleNames;
                style.SelectionChanged += (_, _) =>
                {
                    if (loadingRoomBackdrop || style.SelectedItem is not string name)
                        return;
                    RoomBackdropSettings.Current.Style = name;
                    RefreshRoomBackdrop();
                };
            }

            var baseColor = appearancePanel.FindControl<TextBox>("RoomBackdropBaseColorTextBox");
            var baseColorPreview = appearancePanel.FindControl<Border>("RoomBackdropBaseColorPreview");
            if (baseColor != null)
            {
                baseColor.TextChanged += (_, _) =>
                {
                    if (loadingRoomBackdrop || !SettingsColorManager.IsValidHexColor(baseColor.Text ?? ""))
                        return;
                    RoomBackdropSettings.Current.BaseColor = baseColor.Text!;
                    if (baseColorPreview != null)
                        baseColorPreview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(baseColor.Text!));
                    RefreshRoomBackdrop();
                };
                baseColor.LostFocus += (_, _) =>
                {
                    if (!SettingsColorManager.IsValidHexColor(baseColor.Text ?? ""))
                        baseColor.Text = RoomBackdropSettings.Current.BaseColor;
                };
            }

            var reset = appearancePanel.FindControl<Button>("RoomBackdropResetButton");
            if (reset != null)
            {
                reset.Click += (_, _) =>
                {
                    RoomBackdropSettings.Current = new RoomBackdropConfig();
                    ShowRoomBackdropControls(appearancePanel);
                    RefreshRoomBackdrop();
                };
            }

            ShowRoomBackdropControls(appearancePanel);
        }

        private void LoadRoomBackdropControls(AppearanceSettingsPanel appearancePanel)
        {
            RoomBackdropSettings.Current = UIConfiguration.LoadFromFile().RoomBackdrop ?? new RoomBackdropConfig();
            ShowRoomBackdropControls(appearancePanel);
            RefreshRoomBackdrop();
        }

        /// <summary>Copies <see cref="RoomBackdropSettings.Current"/> into the controls without writing it back.</summary>
        private void ShowRoomBackdropControls(AppearanceSettingsPanel appearancePanel)
        {
            var cfg = RoomBackdropSettings.Current;
            loadingRoomBackdrop = true;
            try
            {
                foreach (var spec in RoomBackdropSliders)
                {
                    double value = Math.Clamp(spec.Get(cfg), 0, spec.Max);
                    var slider = appearancePanel.FindControl<Slider>($"RoomBackdrop{spec.Name}Slider");
                    var textBox = appearancePanel.FindControl<TextBox>($"RoomBackdrop{spec.Name}TextBox");
                    if (slider != null)
                        slider.Value = value;
                    if (textBox != null)
                        textBox.Text = value.ToString("F2");
                }

                var enabled = appearancePanel.FindControl<CheckBox>("RoomBackdropEnabledCheckBox");
                if (enabled != null)
                    enabled.IsChecked = cfg.Enabled;

                var itemAscii = appearancePanel.FindControl<CheckBox>("RoomBackdropItemAsciiCheckBox");
                if (itemAscii != null)
                    itemAscii.IsChecked = cfg.ItemAscii;

                var blendMode = appearancePanel.FindControl<ComboBox>("RoomBackdropBlendModeComboBox");
                if (blendMode != null)
                {
                    int index = -1;
                    for (int i = 0; i < RoomBackdropConfig.BlendModeNames.Count; i++)
                    {
                        if (string.Equals(RoomBackdropConfig.BlendModeNames[i], cfg.BlendMode, StringComparison.OrdinalIgnoreCase))
                            index = i;
                    }
                    blendMode.SelectedIndex = Math.Max(0, index);
                }

                var style = appearancePanel.FindControl<ComboBox>("RoomBackdropStyleComboBox");
                if (style != null)
                    style.SelectedIndex = cfg.IsAscii ? 1 : 0;

                var baseColor = appearancePanel.FindControl<TextBox>("RoomBackdropBaseColorTextBox");
                if (baseColor != null)
                    baseColor.Text = cfg.BaseColor;
                var baseColorPreview = appearancePanel.FindControl<Border>("RoomBackdropBaseColorPreview");
                if (baseColorPreview != null && SettingsColorManager.IsValidHexColor(cfg.BaseColor ?? ""))
                    baseColorPreview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(cfg.BaseColor!));
            }
            finally
            {
                loadingRoomBackdrop = false;
            }
        }

        private static void SaveRoomBackdropToFile()
        {
            try
            {
                var uiConfig = UIConfiguration.LoadFromFile();
                uiConfig.RoomBackdrop = RoomBackdropSettings.Current.Clone();

                string? foundPath = JsonLoader.FindGameDataFile("UIConfiguration.json");
                if (foundPath != null)
                {
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };
                    string json = System.Text.Json.JsonSerializer.Serialize(uiConfig, jsonOptions);
                    System.IO.File.WriteAllText(foundPath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving room backdrop: {ex.Message}");
            }
        }

        private static void RefreshRoomBackdrop()
        {
            try
            {
                if (UIManager.GetCustomUIManager() is CanvasUICoordinator coordinator)
                    coordinator.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshRoomBackdrop: {ex.Message}");
            }
        }
    }
}
