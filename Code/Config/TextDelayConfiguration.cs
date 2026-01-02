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

