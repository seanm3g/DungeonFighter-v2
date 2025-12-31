using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Handles wiring and loading for the Appearance settings panel
    /// </summary>
    public class AppearancePanelHandler : ISettingsPanelHandler
    {
        private readonly GameSettings settings;
        private readonly SettingsColorManager? colorManager;

        public string PanelType => "Appearance";

        public AppearancePanelHandler(GameSettings settings, SettingsColorManager? colorManager)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
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

            // Wire up all color text boxes with real-time updates
            if (panelBackgroundTextBox != null && panelBackgroundPreview != null)
                WireUpColorTextBox(panelBackgroundTextBox, panelBackgroundPreview,
                    () => settings.PanelBackgroundColor,
                    (color) => { settings.PanelBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (panelBorderTextBox != null && panelBorderPreview != null)
                WireUpColorTextBox(panelBorderTextBox, panelBorderPreview,
                    () => settings.PanelBorderColor,
                    (color) => { settings.PanelBorderColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.BorderBrush = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (panelTextTextBox != null && panelTextPreview != null)
                WireUpColorTextBox(panelTextTextBox, panelTextPreview,
                    () => settings.PanelTextColor,
                    (color) => { settings.PanelTextColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (settingsBackgroundTextBox != null && settingsBackgroundPreview != null)
                WireUpColorTextBox(settingsBackgroundTextBox, settingsBackgroundPreview,
                    () => settings.SettingsBackgroundColor,
                    (color) => { settings.SettingsBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (settingsTitleTextBox != null && settingsTitlePreview != null)
                WireUpColorTextBox(settingsTitleTextBox, settingsTitlePreview,
                    () => settings.SettingsTitleColor,
                    (color) => { settings.SettingsTitleColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (listBoxSelectedTextBox != null && listBoxSelectedPreview != null)
                WireUpColorTextBox(listBoxSelectedTextBox, listBoxSelectedPreview,
                    () => settings.ListBoxSelectedColor,
                    (color) => { settings.ListBoxSelectedColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (listBoxSelectedBackgroundTextBox != null && listBoxSelectedBackgroundPreview != null)
                WireUpColorTextBox(listBoxSelectedBackgroundTextBox, listBoxSelectedBackgroundPreview,
                    () => settings.ListBoxSelectedBackgroundColor,
                    (color) => { settings.ListBoxSelectedBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (listBoxHoverBackgroundTextBox != null && listBoxHoverBackgroundPreview != null)
                WireUpColorTextBox(listBoxHoverBackgroundTextBox, listBoxHoverBackgroundPreview,
                    () => settings.ListBoxHoverBackgroundColor,
                    (color) => { settings.ListBoxHoverBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (buttonPrimaryTextBox != null && buttonPrimaryPreview != null)
                WireUpColorTextBox(buttonPrimaryTextBox, buttonPrimaryPreview,
                    () => settings.ButtonPrimaryColor,
                    (color) => { settings.ButtonPrimaryColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (buttonSecondaryTextBox != null && buttonSecondaryPreview != null)
                WireUpColorTextBox(buttonSecondaryTextBox, buttonSecondaryPreview,
                    () => settings.ButtonSecondaryColor,
                    (color) => { settings.ButtonSecondaryColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (buttonBackTextBox != null && buttonBackPreview != null)
                WireUpColorTextBox(buttonBackTextBox, buttonBackPreview,
                    () => settings.ButtonBackColor,
                    (color) => { settings.ButtonBackColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxTextColorTextBox != null && textBoxTextColorPreview != null)
                WireUpColorTextBox(textBoxTextColorTextBox, textBoxTextColorPreview,
                    () => settings.TextBoxTextColor,
                    (color) => { settings.TextBoxTextColor = color; colorManager.ApplyColors(); },
                    (preview, color) =>
                    {
                        var textBlock = preview.Child as TextBlock;
                        if (textBlock != null)
                            textBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(color));
                    });

            if (textBoxBackgroundTextBox != null && textBoxBackgroundPreview != null)
                WireUpColorTextBox(textBoxBackgroundTextBox, textBoxBackgroundPreview,
                    () => settings.TextBoxBackgroundColor,
                    (color) => { settings.TextBoxBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxHoverBackgroundTextBox != null && textBoxHoverBackgroundPreview != null)
                WireUpColorTextBox(textBoxHoverBackgroundTextBox, textBoxHoverBackgroundPreview,
                    () => settings.TextBoxHoverBackgroundColor,
                    (color) => { settings.TextBoxHoverBackgroundColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.Background = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxBorderTextBox != null && textBoxBorderPreview != null)
                WireUpColorTextBox(textBoxBorderTextBox, textBoxBorderPreview,
                    () => settings.TextBoxBorderColor,
                    (color) => { settings.TextBoxBorderColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.BorderBrush = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            if (textBoxFocusBorderTextBox != null && textBoxFocusBorderPreview != null)
                WireUpColorTextBox(textBoxFocusBorderTextBox, textBoxFocusBorderPreview,
                    () => settings.TextBoxFocusBorderColor,
                    (color) => { settings.TextBoxFocusBorderColor = color; colorManager.ApplyColors(); },
                    (preview, color) => preview.BorderBrush = new SolidColorBrush(SettingsColorManager.ParseColor(color)));

            // Load current settings
            appearancePanel.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LoadSettings(appearancePanel);
                    colorManager?.ApplyColors();
                }, DispatcherPriority.Loaded);
            };
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

            if (panelBackgroundTextBox != null) panelBackgroundTextBox.Text = settings.PanelBackgroundColor;
            if (panelBorderTextBox != null) panelBorderTextBox.Text = settings.PanelBorderColor;
            if (panelTextTextBox != null) panelTextTextBox.Text = settings.PanelTextColor;
            if (settingsBackgroundTextBox != null) settingsBackgroundTextBox.Text = settings.SettingsBackgroundColor;
            if (settingsTitleTextBox != null) settingsTitleTextBox.Text = settings.SettingsTitleColor;
            if (listBoxSelectedTextBox != null) listBoxSelectedTextBox.Text = settings.ListBoxSelectedColor;
            if (listBoxSelectedBackgroundTextBox != null) listBoxSelectedBackgroundTextBox.Text = settings.ListBoxSelectedBackgroundColor;
            if (listBoxHoverBackgroundTextBox != null) listBoxHoverBackgroundTextBox.Text = settings.ListBoxHoverBackgroundColor;
            if (buttonPrimaryTextBox != null) buttonPrimaryTextBox.Text = settings.ButtonPrimaryColor;
            if (buttonSecondaryTextBox != null) buttonSecondaryTextBox.Text = settings.ButtonSecondaryColor;
            if (buttonBackTextBox != null) buttonBackTextBox.Text = settings.ButtonBackColor;
            if (textBoxTextColorTextBox != null) textBoxTextColorTextBox.Text = settings.TextBoxTextColor;
            if (textBoxBackgroundTextBox != null) textBoxBackgroundTextBox.Text = settings.TextBoxBackgroundColor;
            if (textBoxHoverBackgroundTextBox != null) textBoxHoverBackgroundTextBox.Text = settings.TextBoxHoverBackgroundColor;
            if (textBoxBorderTextBox != null) textBoxBorderTextBox.Text = settings.TextBoxBorderColor;
            if (textBoxFocusBorderTextBox != null) textBoxFocusBorderTextBox.Text = settings.TextBoxFocusBorderColor;
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

