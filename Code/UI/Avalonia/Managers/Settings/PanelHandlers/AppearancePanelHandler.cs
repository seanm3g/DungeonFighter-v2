using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Config;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Handles wiring and loading for the Appearance settings panel
    /// </summary>
    public class AppearancePanelHandler : ISettingsPanelHandler
    {
        private readonly SettingsColorManager? colorManager;

        public string PanelType => "Appearance";

        public AppearancePanelHandler(GameSettings settings, SettingsColorManager? colorManager)
        {
            this.colorManager = colorManager;
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not AppearanceSettingsPanel appearancePanel || colorManager == null) return;

            // Get all controls using FindControl for reliability
            var panelBackgroundTextBox = appearancePanel.FindControl<TextBox>("PanelBackgroundTextBox");
            var panelBackgroundPreview = appearancePanel.FindControl<Border>("PanelBackgroundPreview");
            var panelBorderTextBox = appearancePanel.FindControl<TextBox>("PanelBorderTextBox");
            var panelBorderPreview = appearancePanel.FindControl<Border>("PanelBorderPreview");
            var panelTextTextBox = appearancePanel.FindControl<TextBox>("PanelTextTextBox");
            var panelTextPreview = appearancePanel.FindControl<Border>("PanelTextPreview");
            var settingsBackgroundTextBox = appearancePanel.FindControl<TextBox>("SettingsBackgroundTextBox");
            var settingsBackgroundPreview = appearancePanel.FindControl<Border>("SettingsBackgroundPreview");
            var settingsTitleTextBox = appearancePanel.FindControl<TextBox>("SettingsTitleTextBox");
            var settingsTitlePreview = appearancePanel.FindControl<Border>("SettingsTitlePreview");
            var listBoxSelectedTextBox = appearancePanel.FindControl<TextBox>("ListBoxSelectedTextBox");
            var listBoxSelectedPreview = appearancePanel.FindControl<Border>("ListBoxSelectedPreview");
            var listBoxSelectedBackgroundTextBox = appearancePanel.FindControl<TextBox>("ListBoxSelectedBackgroundTextBox");
            var listBoxSelectedBackgroundPreview = appearancePanel.FindControl<Border>("ListBoxSelectedBackgroundPreview");
            var listBoxHoverBackgroundTextBox = appearancePanel.FindControl<TextBox>("ListBoxHoverBackgroundTextBox");
            var listBoxHoverBackgroundPreview = appearancePanel.FindControl<Border>("ListBoxHoverBackgroundPreview");
            var buttonPrimaryTextBox = appearancePanel.FindControl<TextBox>("ButtonPrimaryTextBox");
            var buttonPrimaryPreview = appearancePanel.FindControl<Border>("ButtonPrimaryPreview");
            var buttonSecondaryTextBox = appearancePanel.FindControl<TextBox>("ButtonSecondaryTextBox");
            var buttonSecondaryPreview = appearancePanel.FindControl<Border>("ButtonSecondaryPreview");
            var buttonBackTextBox = appearancePanel.FindControl<TextBox>("ButtonBackTextBox");
            var buttonBackPreview = appearancePanel.FindControl<Border>("ButtonBackPreview");
            var textBoxTextColorTextBox = appearancePanel.FindControl<TextBox>("TextBoxTextColorTextBox");
            var textBoxTextColorPreview = appearancePanel.FindControl<Border>("TextBoxTextColorPreview");
            var textBoxBackgroundTextBox = appearancePanel.FindControl<TextBox>("TextBoxBackgroundTextBox");
            var textBoxBackgroundPreview = appearancePanel.FindControl<Border>("TextBoxBackgroundPreview");
            var textBoxHoverBackgroundTextBox = appearancePanel.FindControl<TextBox>("TextBoxHoverBackgroundTextBox");
            var textBoxHoverBackgroundPreview = appearancePanel.FindControl<Border>("TextBoxHoverBackgroundPreview");
            var textBoxBorderTextBox = appearancePanel.FindControl<TextBox>("TextBoxBorderTextBox");
            var textBoxBorderPreview = appearancePanel.FindControl<Border>("TextBoxBorderPreview");
            var textBoxFocusBorderTextBox = appearancePanel.FindControl<TextBox>("TextBoxFocusBorderTextBox");
            var textBoxFocusBorderPreview = appearancePanel.FindControl<Border>("TextBoxFocusBorderPreview");

            // Wire up all color text boxes with real-time updates (use Instance so lambdas always see current)
            if (panelBackgroundTextBox != null && panelBackgroundPreview != null)
                WireUpColorTextBox(panelBackgroundTextBox, panelBackgroundPreview,
                    () => GameSettings.Instance.PanelBackgroundColor,
                    (color) => { GameSettings.Instance.PanelBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (panelBorderTextBox != null && panelBorderPreview != null)
                WireUpColorTextBox(panelBorderTextBox, panelBorderPreview,
                    () => GameSettings.Instance.PanelBorderColor,
                    (color) => { GameSettings.Instance.PanelBorderColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.BorderBrush = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (panelTextTextBox != null && panelTextPreview != null)
                WireUpColorTextBox(panelTextTextBox, panelTextPreview,
                    () => GameSettings.Instance.PanelTextColor,
                    (color) => { GameSettings.Instance.PanelTextColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (settingsBackgroundTextBox != null && settingsBackgroundPreview != null)
                WireUpColorTextBox(settingsBackgroundTextBox, settingsBackgroundPreview,
                    () => GameSettings.Instance.SettingsBackgroundColor,
                    (color) => { GameSettings.Instance.SettingsBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (settingsTitleTextBox != null && settingsTitlePreview != null)
                WireUpColorTextBox(settingsTitleTextBox, settingsTitlePreview,
                    () => GameSettings.Instance.SettingsTitleColor,
                    (color) => { GameSettings.Instance.SettingsTitleColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (listBoxSelectedTextBox != null && listBoxSelectedPreview != null)
                WireUpColorTextBox(listBoxSelectedTextBox, listBoxSelectedPreview,
                    () => GameSettings.Instance.ListBoxSelectedColor,
                    (color) => { GameSettings.Instance.ListBoxSelectedColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (listBoxSelectedBackgroundTextBox != null && listBoxSelectedBackgroundPreview != null)
                WireUpColorTextBox(listBoxSelectedBackgroundTextBox, listBoxSelectedBackgroundPreview,
                    () => GameSettings.Instance.ListBoxSelectedBackgroundColor,
                    (color) => { GameSettings.Instance.ListBoxSelectedBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (listBoxHoverBackgroundTextBox != null && listBoxHoverBackgroundPreview != null)
                WireUpColorTextBox(listBoxHoverBackgroundTextBox, listBoxHoverBackgroundPreview,
                    () => GameSettings.Instance.ListBoxHoverBackgroundColor,
                    (color) => { GameSettings.Instance.ListBoxHoverBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (buttonPrimaryTextBox != null && buttonPrimaryPreview != null)
                WireUpColorTextBox(buttonPrimaryTextBox, buttonPrimaryPreview,
                    () => GameSettings.Instance.ButtonPrimaryColor,
                    (color) => { GameSettings.Instance.ButtonPrimaryColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (buttonSecondaryTextBox != null && buttonSecondaryPreview != null)
                WireUpColorTextBox(buttonSecondaryTextBox, buttonSecondaryPreview,
                    () => GameSettings.Instance.ButtonSecondaryColor,
                    (color) => { GameSettings.Instance.ButtonSecondaryColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (buttonBackTextBox != null && buttonBackPreview != null)
                WireUpColorTextBox(buttonBackTextBox, buttonBackPreview,
                    () => GameSettings.Instance.ButtonBackColor,
                    (color) => { GameSettings.Instance.ButtonBackColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxTextColorTextBox != null && textBoxTextColorPreview != null)
                WireUpColorTextBox(textBoxTextColorTextBox, textBoxTextColorPreview,
                    () => GameSettings.Instance.TextBoxTextColor,
                    (color) => { GameSettings.Instance.TextBoxTextColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (textBoxBackgroundTextBox != null && textBoxBackgroundPreview != null)
                WireUpColorTextBox(textBoxBackgroundTextBox, textBoxBackgroundPreview,
                    () => GameSettings.Instance.TextBoxBackgroundColor,
                    (color) => { GameSettings.Instance.TextBoxBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxHoverBackgroundTextBox != null && textBoxHoverBackgroundPreview != null)
                WireUpColorTextBox(textBoxHoverBackgroundTextBox, textBoxHoverBackgroundPreview,
                    () => GameSettings.Instance.TextBoxHoverBackgroundColor,
                    (color) => { GameSettings.Instance.TextBoxHoverBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxBorderTextBox != null && textBoxBorderPreview != null)
                WireUpColorTextBox(textBoxBorderTextBox, textBoxBorderPreview,
                    () => GameSettings.Instance.TextBoxBorderColor,
                    (color) => { GameSettings.Instance.TextBoxBorderColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.BorderBrush = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxFocusBorderTextBox != null && textBoxFocusBorderPreview != null)
                WireUpColorTextBox(textBoxFocusBorderTextBox, textBoxFocusBorderPreview,
                    () => GameSettings.Instance.TextBoxFocusBorderColor,
                    (color) => { GameSettings.Instance.TextBoxFocusBorderColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.BorderBrush = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            // Wire up subsequent line darkening controls
            var subsequentLineDarkeningSlider = appearancePanel.FindControl<Slider>("SubsequentLineDarkeningSlider");
            var subsequentLineDarkeningTextBox = appearancePanel.FindControl<TextBox>("SubsequentLineDarkeningTextBox");
            
            if (subsequentLineDarkeningSlider != null && subsequentLineDarkeningTextBox != null)
            {
                // Load initial value from UIConfiguration
                var uiConfig = UIConfiguration.LoadFromFile();
                subsequentLineDarkeningSlider.Value = uiConfig.SubsequentLineDarkening;
                subsequentLineDarkeningTextBox.Text = uiConfig.SubsequentLineDarkening.ToString("F2");
                
                // Bind slider to textbox
                subsequentLineDarkeningSlider.ValueChanged += (s, e) =>
                {
                    subsequentLineDarkeningTextBox.Text = e.NewValue.ToString("F2");
                    SaveSubsequentLineDarkening(e.NewValue);
                };
                
                // Bind textbox to slider with validation
                subsequentLineDarkeningTextBox.LostFocus += (s, e) =>
                {
                    if (double.TryParse(subsequentLineDarkeningTextBox.Text, out double value))
                    {
                        value = Math.Max(0.0, Math.Min(1.0, value)); // Clamp to 0-1
                        subsequentLineDarkeningSlider.Value = value;
                        subsequentLineDarkeningTextBox.Text = value.ToString("F2");
                        SaveSubsequentLineDarkening(value);
                    }
                    else
                    {
                        // Reset to current slider value if invalid
                        subsequentLineDarkeningTextBox.Text = subsequentLineDarkeningSlider.Value.ToString("F2");
                    }
                };
            }

            // Load settings once when panel is wired. Do not subscribe to Loaded: Loaded can fire again on
            // layout/focus (e.g. when user clicks Save), which would overwrite user edits with stale values.
            // Single deferred post so FindControl works when the panel is in the visual tree.
            Dispatcher.UIThread.Post(() =>
            {
                LoadSettings(appearancePanel);
                colorManager?.ApplyColors();
            }, DispatcherPriority.Loaded);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not AppearanceSettingsPanel appearancePanel) return;

            var panelBackgroundTextBox = appearancePanel.FindControl<TextBox>("PanelBackgroundTextBox");
            var panelBorderTextBox = appearancePanel.FindControl<TextBox>("PanelBorderTextBox");
            var panelTextTextBox = appearancePanel.FindControl<TextBox>("PanelTextTextBox");
            var settingsBackgroundTextBox = appearancePanel.FindControl<TextBox>("SettingsBackgroundTextBox");
            var settingsTitleTextBox = appearancePanel.FindControl<TextBox>("SettingsTitleTextBox");
            var listBoxSelectedTextBox = appearancePanel.FindControl<TextBox>("ListBoxSelectedTextBox");
            var listBoxSelectedBackgroundTextBox = appearancePanel.FindControl<TextBox>("ListBoxSelectedBackgroundTextBox");
            var listBoxHoverBackgroundTextBox = appearancePanel.FindControl<TextBox>("ListBoxHoverBackgroundTextBox");
            var buttonPrimaryTextBox = appearancePanel.FindControl<TextBox>("ButtonPrimaryTextBox");
            var buttonSecondaryTextBox = appearancePanel.FindControl<TextBox>("ButtonSecondaryTextBox");
            var buttonBackTextBox = appearancePanel.FindControl<TextBox>("ButtonBackTextBox");
            var textBoxTextColorTextBox = appearancePanel.FindControl<TextBox>("TextBoxTextColorTextBox");
            var textBoxBackgroundTextBox = appearancePanel.FindControl<TextBox>("TextBoxBackgroundTextBox");
            var textBoxHoverBackgroundTextBox = appearancePanel.FindControl<TextBox>("TextBoxHoverBackgroundTextBox");
            var textBoxBorderTextBox = appearancePanel.FindControl<TextBox>("TextBoxBorderTextBox");
            var textBoxFocusBorderTextBox = appearancePanel.FindControl<TextBox>("TextBoxFocusBorderTextBox");

            if (panelBackgroundTextBox != null) panelBackgroundTextBox.Text = GameSettings.Instance.PanelBackgroundColor;
            if (panelBorderTextBox != null) panelBorderTextBox.Text = GameSettings.Instance.PanelBorderColor;
            if (panelTextTextBox != null) panelTextTextBox.Text = GameSettings.Instance.PanelTextColor;
            if (settingsBackgroundTextBox != null) settingsBackgroundTextBox.Text = GameSettings.Instance.SettingsBackgroundColor;
            if (settingsTitleTextBox != null) settingsTitleTextBox.Text = GameSettings.Instance.SettingsTitleColor;
            if (listBoxSelectedTextBox != null) listBoxSelectedTextBox.Text = GameSettings.Instance.ListBoxSelectedColor;
            if (listBoxSelectedBackgroundTextBox != null) listBoxSelectedBackgroundTextBox.Text = GameSettings.Instance.ListBoxSelectedBackgroundColor;
            if (listBoxHoverBackgroundTextBox != null) listBoxHoverBackgroundTextBox.Text = GameSettings.Instance.ListBoxHoverBackgroundColor;
            if (buttonPrimaryTextBox != null) buttonPrimaryTextBox.Text = GameSettings.Instance.ButtonPrimaryColor;
            if (buttonSecondaryTextBox != null) buttonSecondaryTextBox.Text = GameSettings.Instance.ButtonSecondaryColor;
            if (buttonBackTextBox != null) buttonBackTextBox.Text = GameSettings.Instance.ButtonBackColor;
            if (textBoxTextColorTextBox != null) textBoxTextColorTextBox.Text = GameSettings.Instance.TextBoxTextColor;
            if (textBoxBackgroundTextBox != null) textBoxBackgroundTextBox.Text = GameSettings.Instance.TextBoxBackgroundColor;
            if (textBoxHoverBackgroundTextBox != null) textBoxHoverBackgroundTextBox.Text = GameSettings.Instance.TextBoxHoverBackgroundColor;
            if (textBoxBorderTextBox != null) textBoxBorderTextBox.Text = GameSettings.Instance.TextBoxBorderColor;
            if (textBoxFocusBorderTextBox != null) textBoxFocusBorderTextBox.Text = GameSettings.Instance.TextBoxFocusBorderColor;
            
            // Load subsequent line darkening setting
            var subsequentLineDarkeningSlider = appearancePanel.FindControl<Slider>("SubsequentLineDarkeningSlider");
            var subsequentLineDarkeningTextBox = appearancePanel.FindControl<TextBox>("SubsequentLineDarkeningTextBox");
            
            if (subsequentLineDarkeningSlider != null && subsequentLineDarkeningTextBox != null)
            {
                var uiConfig = UIConfiguration.LoadFromFile();
                subsequentLineDarkeningSlider.Value = uiConfig.SubsequentLineDarkening;
                subsequentLineDarkeningTextBox.Text = uiConfig.SubsequentLineDarkening.ToString("F2");
            }
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not AppearanceSettingsPanel appearancePanel) return;
            var panelBackgroundTextBox = appearancePanel.FindControl<TextBox>("PanelBackgroundTextBox");
            var panelBorderTextBox = appearancePanel.FindControl<TextBox>("PanelBorderTextBox");
            var panelTextTextBox = appearancePanel.FindControl<TextBox>("PanelTextTextBox");
            var settingsBackgroundTextBox = appearancePanel.FindControl<TextBox>("SettingsBackgroundTextBox");
            var settingsTitleTextBox = appearancePanel.FindControl<TextBox>("SettingsTitleTextBox");
            var listBoxSelectedTextBox = appearancePanel.FindControl<TextBox>("ListBoxSelectedTextBox");
            var listBoxSelectedBackgroundTextBox = appearancePanel.FindControl<TextBox>("ListBoxSelectedBackgroundTextBox");
            var listBoxHoverBackgroundTextBox = appearancePanel.FindControl<TextBox>("ListBoxHoverBackgroundTextBox");
            var buttonPrimaryTextBox = appearancePanel.FindControl<TextBox>("ButtonPrimaryTextBox");
            var buttonSecondaryTextBox = appearancePanel.FindControl<TextBox>("ButtonSecondaryTextBox");
            var buttonBackTextBox = appearancePanel.FindControl<TextBox>("ButtonBackTextBox");
            var textBoxTextColorTextBox = appearancePanel.FindControl<TextBox>("TextBoxTextColorTextBox");
            var textBoxBackgroundTextBox = appearancePanel.FindControl<TextBox>("TextBoxBackgroundTextBox");
            var textBoxHoverBackgroundTextBox = appearancePanel.FindControl<TextBox>("TextBoxHoverBackgroundTextBox");
            var textBoxBorderTextBox = appearancePanel.FindControl<TextBox>("TextBoxBorderTextBox");
            var textBoxFocusBorderTextBox = appearancePanel.FindControl<TextBox>("TextBoxFocusBorderTextBox");
            if (panelBackgroundTextBox != null && !string.IsNullOrEmpty(panelBackgroundTextBox.Text)) GameSettings.Instance.PanelBackgroundColor = panelBackgroundTextBox.Text;
            if (panelBorderTextBox != null && !string.IsNullOrEmpty(panelBorderTextBox.Text)) GameSettings.Instance.PanelBorderColor = panelBorderTextBox.Text;
            if (panelTextTextBox != null && !string.IsNullOrEmpty(panelTextTextBox.Text)) GameSettings.Instance.PanelTextColor = panelTextTextBox.Text;
            if (settingsBackgroundTextBox != null && !string.IsNullOrEmpty(settingsBackgroundTextBox.Text)) GameSettings.Instance.SettingsBackgroundColor = settingsBackgroundTextBox.Text;
            if (settingsTitleTextBox != null && !string.IsNullOrEmpty(settingsTitleTextBox.Text)) GameSettings.Instance.SettingsTitleColor = settingsTitleTextBox.Text;
            if (listBoxSelectedTextBox != null && !string.IsNullOrEmpty(listBoxSelectedTextBox.Text)) GameSettings.Instance.ListBoxSelectedColor = listBoxSelectedTextBox.Text;
            if (listBoxSelectedBackgroundTextBox != null && !string.IsNullOrEmpty(listBoxSelectedBackgroundTextBox.Text)) GameSettings.Instance.ListBoxSelectedBackgroundColor = listBoxSelectedBackgroundTextBox.Text;
            if (listBoxHoverBackgroundTextBox != null && !string.IsNullOrEmpty(listBoxHoverBackgroundTextBox.Text)) GameSettings.Instance.ListBoxHoverBackgroundColor = listBoxHoverBackgroundTextBox.Text;
            if (buttonPrimaryTextBox != null && !string.IsNullOrEmpty(buttonPrimaryTextBox.Text)) GameSettings.Instance.ButtonPrimaryColor = buttonPrimaryTextBox.Text;
            if (buttonSecondaryTextBox != null && !string.IsNullOrEmpty(buttonSecondaryTextBox.Text)) GameSettings.Instance.ButtonSecondaryColor = buttonSecondaryTextBox.Text;
            if (buttonBackTextBox != null && !string.IsNullOrEmpty(buttonBackTextBox.Text)) GameSettings.Instance.ButtonBackColor = buttonBackTextBox.Text;
            if (textBoxTextColorTextBox != null && !string.IsNullOrEmpty(textBoxTextColorTextBox.Text)) GameSettings.Instance.TextBoxTextColor = textBoxTextColorTextBox.Text;
            if (textBoxBackgroundTextBox != null && !string.IsNullOrEmpty(textBoxBackgroundTextBox.Text)) GameSettings.Instance.TextBoxBackgroundColor = textBoxBackgroundTextBox.Text;
            if (textBoxHoverBackgroundTextBox != null && !string.IsNullOrEmpty(textBoxHoverBackgroundTextBox.Text)) GameSettings.Instance.TextBoxHoverBackgroundColor = textBoxHoverBackgroundTextBox.Text;
            if (textBoxBorderTextBox != null && !string.IsNullOrEmpty(textBoxBorderTextBox.Text)) GameSettings.Instance.TextBoxBorderColor = textBoxBorderTextBox.Text;
            if (textBoxFocusBorderTextBox != null && !string.IsNullOrEmpty(textBoxFocusBorderTextBox.Text)) GameSettings.Instance.TextBoxFocusBorderColor = textBoxFocusBorderTextBox.Text;
            var subsequentLineDarkeningSlider = appearancePanel.FindControl<Slider>("SubsequentLineDarkeningSlider");
            if (subsequentLineDarkeningSlider != null)
                SaveSubsequentLineDarkening(subsequentLineDarkeningSlider.Value);
            // Orchestrator persists GameSettings once at end of save; do not call GameSettings.Instance.SaveSettings() here.
        }
        
        private void SaveSubsequentLineDarkening(double value)
        {
            try
            {
                var uiConfig = UIConfiguration.LoadFromFile();
                uiConfig.SubsequentLineDarkening = value;
                
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
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving subsequent line darkening: {ex.Message}");
            }
        }

        private void WireUpColorTextBox(TextBox textBox, Border preview,
            System.Func<string> getColor, System.Action<string> setColor, System.Action<Border, string> updatePreview)
        {
            if (textBox == null || preview == null) return;

            // Load initial value
            var initialColor = getColor();
            textBox.Text = initialColor;
            updatePreview(preview, initialColor);

            // Real-time updates as user types
            textBox.TextChanged += (s, e) =>
            {
                var color = textBox.Text ?? "";
                if (SettingsColorManager.IsValidHexColor(color))
                {
                    setColor(color);
                    updatePreview(preview, color);
                }
            };

            // Validate and fix on lost focus
            textBox.LostFocus += (s, e) =>
            {
                var color = textBox.Text ?? "";
                if (!SettingsColorManager.IsValidHexColor(color))
                {
                    // Reset to current setting if invalid
                    color = getColor();
                    textBox.Text = color;
                }
                setColor(color);
                updatePreview(preview, color);
            };
        }
    }
}

