using System;
using Avalonia.Controls;
using RPGGame;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Manages loading and saving of animation settings (brightness mask and undulation)
    /// Extracted from SettingsManager to separate animation configuration logic
    /// </summary>
    public class AnimationSettingsManager
    {
        private readonly Action<string, bool>? showStatusMessage;

        public AnimationSettingsManager(Action<string, bool>? showStatusMessage)
        {
            this.showStatusMessage = showStatusMessage;
        }

        /// <summary>
        /// Loads animation settings from UIConfiguration into UI controls
        /// </summary>
        public void LoadAnimationSettings(
            CheckBox brightnessMaskEnabledCheckBox,
            Slider brightnessMaskIntensitySlider,
            TextBox brightnessMaskIntensityTextBox,
            Slider brightnessMaskWaveLengthSlider,
            TextBox brightnessMaskWaveLengthTextBox,
            TextBox brightnessMaskUpdateIntervalTextBox,
            Slider undulationSpeedSlider,
            TextBox undulationSpeedTextBox,
            Slider undulationWaveLengthSlider,
            TextBox undulationWaveLengthTextBox,
            TextBox undulationIntervalTextBox)
        {
            try
            {
                var uiConfig = UIConfiguration.LoadFromFile();
                var animConfig = uiConfig.DungeonSelectionAnimation;

                // Brightness Mask Settings
                brightnessMaskEnabledCheckBox.IsChecked = animConfig.BrightnessMask.Enabled;
                brightnessMaskIntensitySlider.Value = animConfig.BrightnessMask.Intensity;
                brightnessMaskIntensityTextBox.Text = animConfig.BrightnessMask.Intensity.ToString("F1");
                brightnessMaskWaveLengthSlider.Value = animConfig.BrightnessMask.WaveLength;
                brightnessMaskWaveLengthTextBox.Text = animConfig.BrightnessMask.WaveLength.ToString("F1");
                brightnessMaskUpdateIntervalTextBox.Text = animConfig.BrightnessMask.UpdateIntervalMs.ToString();

                // Undulation Settings
                undulationSpeedSlider.Value = animConfig.UndulationSpeed;
                undulationSpeedTextBox.Text = animConfig.UndulationSpeed.ToString("F3");
                undulationWaveLengthSlider.Value = animConfig.UndulationWaveLength;
                undulationWaveLengthTextBox.Text = animConfig.UndulationWaveLength.ToString("F1");
                undulationIntervalTextBox.Text = animConfig.UndulationIntervalMs.ToString();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading animation settings: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Saves animation settings from UI controls to UIConfiguration
        /// </summary>
        public void SaveAnimationSettings(
            CheckBox brightnessMaskEnabledCheckBox,
            Slider brightnessMaskIntensitySlider,
            Slider brightnessMaskWaveLengthSlider,
            TextBox brightnessMaskUpdateIntervalTextBox,
            Slider undulationSpeedSlider,
            Slider undulationWaveLengthSlider,
            TextBox undulationIntervalTextBox)
        {
            try
            {
                var uiConfig = UIConfiguration.LoadFromFile();

                // Update brightness mask settings
                uiConfig.DungeonSelectionAnimation.BrightnessMask.Enabled = brightnessMaskEnabledCheckBox.IsChecked ?? false;
                uiConfig.DungeonSelectionAnimation.BrightnessMask.Intensity = (float)brightnessMaskIntensitySlider.Value;
                uiConfig.DungeonSelectionAnimation.BrightnessMask.WaveLength = (float)brightnessMaskWaveLengthSlider.Value;
                
                if (int.TryParse(brightnessMaskUpdateIntervalTextBox.Text, out int brightnessMaskInterval))
                {
                    uiConfig.DungeonSelectionAnimation.BrightnessMask.UpdateIntervalMs = Math.Max(10, brightnessMaskInterval);
                }

                // Update undulation settings
                uiConfig.DungeonSelectionAnimation.UndulationSpeed = undulationSpeedSlider.Value;
                uiConfig.DungeonSelectionAnimation.UndulationWaveLength = (float)undulationWaveLengthSlider.Value;
                
                if (int.TryParse(undulationIntervalTextBox.Text, out int undulationInterval))
                {
                    uiConfig.DungeonSelectionAnimation.UndulationIntervalMs = Math.Max(10, undulationInterval);
                }

                // Save to file
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
                
                // Trigger real-time update of animation configuration
                // This will be called from SettingsPanel if CanvasUICoordinator is available
                OnConfigurationUpdated?.Invoke();

                showStatusMessage?.Invoke("Animation settings saved successfully!", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving animation settings: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Event fired when animation configuration is updated
        /// Allows SettingsPanel to notify CanvasAnimationManager to reload config
        /// </summary>
        public event System.Action? OnConfigurationUpdated;

        /// <summary>
        /// Gets current animation settings for real-time updates
        /// </summary>
        public (bool brightnessMaskEnabled, float intensity, float waveLength, int updateInterval, double undulationSpeed, float undulationWaveLength, int undulationInterval) GetCurrentSettings()
        {
            var uiConfig = UIConfiguration.LoadFromFile();
            var animConfig = uiConfig.DungeonSelectionAnimation;
            
            return (
                animConfig.BrightnessMask.Enabled,
                animConfig.BrightnessMask.Intensity,
                animConfig.BrightnessMask.WaveLength,
                animConfig.BrightnessMask.UpdateIntervalMs,
                animConfig.UndulationSpeed,
                animConfig.UndulationWaveLength,
                animConfig.UndulationIntervalMs
            );
        }
    }
}

