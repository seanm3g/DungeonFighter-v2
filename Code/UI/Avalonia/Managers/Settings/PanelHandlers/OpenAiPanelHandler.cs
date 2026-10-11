using System.Globalization;
using Avalonia.Controls;
using RPGGame.Config;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>Settings → OpenAI: local API key + victory limerick toggle.</summary>
    public sealed class OpenAiPanelHandler : ISettingsPanelHandler
    {
        public string PanelType => "OpenAI";

        public void WireUp(UserControl panel)
        {
            if (panel is not OpenAiSettingsPanel) return;
            LoadSettings(panel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not OpenAiSettingsPanel openAiPanel) return;

            var prefs = GeneralSettingsStore.Load().OpenAiPreferences ?? new OpenAiPreferences();
            prefs.ValidateAndFix();

            var enabled = openAiPanel.FindControl<CheckBox>("VictoryLimerickEnabledCheckBox");
            if (enabled != null)
                enabled.IsChecked = prefs.VictoryLimerickEnabled;

            var apiKey = openAiPanel.FindControl<TextBox>("ApiKeyTextBox");
            if (apiKey != null)
                apiKey.Text = prefs.ApiKey ?? string.Empty;

            var model = openAiPanel.FindControl<TextBox>("ModelTextBox");
            if (model != null)
                model.Text = prefs.Model ?? OpenAiPreferences.DefaultModel;

            var baseUrl = openAiPanel.FindControl<TextBox>("BaseUrlTextBox");
            if (baseUrl != null)
                baseUrl.Text = prefs.BaseUrl ?? OpenAiPreferences.DefaultBaseUrl;

            var timeout = openAiPanel.FindControl<TextBox>("TimeoutSecondsTextBox");
            if (timeout != null)
                timeout.Text = prefs.TimeoutSeconds.ToString(CultureInfo.InvariantCulture);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not OpenAiSettingsPanel openAiPanel) return;

            var prefs = GeneralSettingsStore.Load().OpenAiPreferences ?? new OpenAiPreferences();

            var enabled = openAiPanel.FindControl<CheckBox>("VictoryLimerickEnabledCheckBox");
            prefs.VictoryLimerickEnabled = enabled?.IsChecked == true;

            var apiKey = openAiPanel.FindControl<TextBox>("ApiKeyTextBox");
            prefs.ApiKey = apiKey?.Text?.Trim() ?? string.Empty;

            var model = openAiPanel.FindControl<TextBox>("ModelTextBox");
            prefs.Model = string.IsNullOrWhiteSpace(model?.Text)
                ? OpenAiPreferences.DefaultModel
                : model!.Text.Trim();

            var baseUrl = openAiPanel.FindControl<TextBox>("BaseUrlTextBox");
            prefs.BaseUrl = string.IsNullOrWhiteSpace(baseUrl?.Text)
                ? OpenAiPreferences.DefaultBaseUrl
                : baseUrl!.Text.Trim();

            var timeout = openAiPanel.FindControl<TextBox>("TimeoutSecondsTextBox");
            if (int.TryParse(timeout?.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) ||
                int.TryParse(timeout?.Text?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out seconds))
            {
                prefs.TimeoutSeconds = seconds;
            }

            prefs.ValidateAndFix();
            GeneralSettingsStore.SaveOpenAiPreferences(prefs);
        }
    }
}
