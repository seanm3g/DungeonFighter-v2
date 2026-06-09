using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.Config.TextDelay;

namespace RPGGame.Config
{
    /// <summary>
    /// Configuration for chunked text reveal presets
    /// </summary>
    public class ChunkedTextRevealPreset
    {
        public int BaseDelayPerCharMs { get; set; } = 30;
        public int MinDelayMs { get; set; } = 500;
        public int MaxDelayMs { get; set; } = 4000;
        public string Strategy { get; set; } = "Sentence";
    }

    /// <summary>
    /// Configuration for progressive menu delays
    /// </summary>
    public class ProgressiveMenuDelaysConfig
    {
        public int BaseMenuDelay { get; set; } = 25;
        public int ProgressiveReductionRate { get; set; } = 1;
        public int ProgressiveThreshold { get; set; } = 20;
    }

    /// <summary>
    /// Per d20 roll pacing for region travel: each point below 20 adds extra delay and summary minutes.
    /// </summary>
    public class TravelRouteRollPacingConfig
    {
        public int StepDelayBaseMs { get; set; } = 750;
        public int StepExtraDelayMsPerPointBelow20 { get; set; } = 100;
        public int SummaryBaseMinutes { get; set; } = 4;
        public int SummaryExtraMinutesPerPointBelow20 { get; set; } = 1;
    }

    /// <summary>
    /// Facade for unified text delay configuration system
    /// Loads all delay values from TextDelayConfig.json
    /// 
    /// Refactored from 455 lines to ~150 lines using Facade pattern.
    /// Delegates to:
    /// - TextDelayLoader: JSON loading and saving
    /// - DelayCalculator: Delay calculation and retrieval
    /// - PresetManager: Preset management
    /// </summary>
    public static class TextDelayConfiguration
    {
        private static bool _configLoaded = false;
        private static readonly object _lockObject = new object();
        private static TextDelayLoader.TextDelayConfigData? _configData;

        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        private static void LoadConfig()
        {
            if (_configLoaded && _configData != null) return;

            lock (_lockObject)
            {
                if (_configLoaded && _configData != null) return;

                _configData = TextDelayLoader.LoadConfig();
                _configLoaded = true;
            }
        }

        /// <summary>
        /// Gets the current config data (loads if needed)
        /// </summary>
        private static TextDelayLoader.TextDelayConfigData GetConfigData()
        {
            LoadConfig();
            return _configData!;
        }

        /// <summary>
        /// Gets the delay for a specific message type
        /// </summary>
        public static int GetMessageTypeDelay(UIMessageType messageType)
        {
            var configData = GetConfigData();
            return DelayCalculator.GetMessageTypeDelay(messageType, configData);
        }

        /// <summary>
        /// Gets a chunked text reveal preset configuration
        /// </summary>
        public static ChunkedTextRevealPreset? GetChunkedTextRevealPreset(string presetName)
        {
            var configData = GetConfigData();
            return PresetManager.GetChunkedTextRevealPreset(presetName, configData);
        }

        /// <summary>
        /// Gets the action delay (delay after complete action)
        /// </summary>
        public static int GetActionDelayMs()
        {
            var configData = GetConfigData();
            return DelayCalculator.GetActionDelayMs(configData);
        }

        /// <summary>
        /// Gets the message delay (delay between messages within action)
        /// </summary>
        public static int GetMessageDelayMs()
        {
            var configData = GetConfigData();
            return DelayCalculator.GetMessageDelayMs(configData);
        }

        /// <summary>
        /// Multiplier applied to combat log delays during the pre-weapon Training Ground tutorial (2 = twice as long as default).
        /// </summary>
        public static double GetTutorialCombatDelayMultiplier()
        {
            var configData = GetConfigData();
            return configData.TutorialCombatDelayMultiplier > 0
                ? configData.TutorialCombatDelayMultiplier
                : RPGGame.GameConstants.TutorialCombatDelayMultiplier;
        }

        /// <summary>
        /// Gets the environmental line delay (delay between lines in environmental actions)
        /// </summary>
        public static int GetEnvironmentalLineDelay()
        {
            var configData = GetConfigData();
            return configData.EnvironmentalLineDelay;
        }

        /// <summary>
        /// Gets the progressive menu delays configuration
        /// </summary>
        public static ProgressiveMenuDelaysConfig GetProgressiveMenuDelays()
        {
            var configData = GetConfigData();
            return PresetManager.GetProgressiveMenuDelays(configData);
        }

        /// <summary>
        /// Gets whether GUI delays are enabled
        /// </summary>
        public static bool GetEnableGuiDelays()
        {
            var configData = GetConfigData();
            return DelayCalculator.GetEnableGuiDelays(configData);
        }

        /// <summary>
        /// Gets whether console delays are enabled
        /// </summary>
        public static bool GetEnableConsoleDelays()
        {
            var configData = GetConfigData();
            return DelayCalculator.GetEnableConsoleDelays(configData);
        }

        /// <summary>
        /// Pacing for each region-travel d20 step (reveal pause and summary minutes).
        /// </summary>
        public static TravelRouteRollPacingConfig GetTravelRouteRollPacing()
        {
            var configData = GetConfigData();
            return configData.TravelRouteRollPacing;
        }

        /// <summary>
        /// Sets region travel roll pacing and persists to TextDelayConfig.json.
        /// </summary>
        public static void SetTravelRouteRollPacing(TravelRouteRollPacingConfig config)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                var next = config ?? new TravelRouteRollPacingConfig();
                next.StepDelayBaseMs = Math.Max(0, next.StepDelayBaseMs);
                next.StepExtraDelayMsPerPointBelow20 = Math.Max(0, next.StepExtraDelayMsPerPointBelow20);
                next.SummaryBaseMinutes = Math.Max(0, next.SummaryBaseMinutes);
                next.SummaryExtraMinutesPerPointBelow20 = Math.Max(0, next.SummaryExtraMinutesPerPointBelow20);
                configData.TravelRouteRollPacing = next;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets the delay for a specific message type and saves to config
        /// </summary>
        public static void SetMessageTypeDelay(UIMessageType messageType, int delayMs)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                configData.MessageTypeDelays[messageType] = delayMs;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets a chunked text reveal preset and saves to config
        /// </summary>
        public static void SetChunkedTextRevealPreset(string presetName, ChunkedTextRevealPreset preset)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                PresetManager.SetChunkedTextRevealPreset(presetName, preset, configData);
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets the action delay and saves to config
        /// </summary>
        public static void SetActionDelayMs(int delayMs)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                configData.ActionDelayMs = delayMs;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets the message delay and saves to config
        /// </summary>
        public static void SetMessageDelayMs(int delayMs)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                configData.MessageDelayMs = delayMs;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets the Training Ground tutorial combat delay multiplier and saves to config.
        /// </summary>
        public static void SetTutorialCombatDelayMultiplier(double multiplier)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                configData.TutorialCombatDelayMultiplier = Math.Clamp(multiplier, 0.1, 10.0);
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets the environmental line delay and saves to config
        /// </summary>
        public static void SetEnvironmentalLineDelay(int delayMs)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                configData.EnvironmentalLineDelay = delayMs;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets the progressive menu delays and saves to config
        /// </summary>
        public static void SetProgressiveMenuDelays(ProgressiveMenuDelaysConfig config)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                PresetManager.SetProgressiveMenuDelays(config, configData);
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets whether GUI delays are enabled and saves to config
        /// </summary>
        public static void SetEnableGuiDelays(bool enabled)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                configData.EnableGuiDelays = enabled;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Sets whether console delays are enabled and saves to config
        /// </summary>
        public static void SetEnableConsoleDelays(bool enabled)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                configData.EnableConsoleDelays = enabled;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// Persists the current in-memory configuration to the config file.
        /// Call after updating multiple settings from the UI so all changes are saved in one write.
        /// </summary>
        public static void SaveCurrentConfigToFile()
        {
            var configData = GetConfigData();
            TextDelayLoader.SaveConfig(configData);
        }

        /// <summary>
        /// Forces a reload of the configuration from file
        /// </summary>
        public static void ReloadConfig()
        {
            lock (_lockObject)
            {
                _configLoaded = false;
                _configData = null;
                LoadConfig();
            }
        }
    }
}

