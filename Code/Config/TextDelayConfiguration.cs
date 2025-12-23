using System;
using System.Collections.Generic;
using System.Text.Json;
using RPGGame.UI;

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
    /// Unified text delay configuration system
    /// Loads all delay values from TextDelayConfig.json
    /// </summary>
    public static class TextDelayConfiguration
    {
        private static bool _configLoaded = false;
        private static readonly object _lockObject = new object();

        // Message type delays
        private static Dictionary<UIMessageType, int> _messageTypeDelays = new Dictionary<UIMessageType, int>();

        // Chunked text reveal presets
        private static Dictionary<string, ChunkedTextRevealPreset> _chunkedTextRevealPresets = new Dictionary<string, ChunkedTextRevealPreset>();

        // Combat delays
        private static int _actionDelayMs = 1000;
        private static int _messageDelayMs = 200;

        // Progressive menu delays
        private static ProgressiveMenuDelaysConfig _progressiveMenuDelays = new ProgressiveMenuDelaysConfig();

        // Enable flags
        private static bool _enableGuiDelays = true;
        private static bool _enableConsoleDelays = true;

        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        private static void LoadConfig()
        {
            if (_configLoaded) return;

            lock (_lockObject)
            {
                if (_configLoaded) return;

                try
                {
                    string configPath = "GameData/TextDelayConfig.json";
                    if (System.IO.File.Exists(configPath))
                    {
                        string jsonContent = System.IO.File.ReadAllText(configPath);
                        var config = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                        // Load message type delays
                        if (config.TryGetProperty("MessageTypeDelays", out var messageTypeDelays))
                        {
                            _messageTypeDelays = new Dictionary<UIMessageType, int>();
                            foreach (var prop in messageTypeDelays.EnumerateObject())
                            {
                                if (Enum.TryParse<UIMessageType>(prop.Name, out var messageType))
                                {
                                    _messageTypeDelays[messageType] = prop.Value.GetInt32();
                                }
                            }
                        }

                        // Load chunked text reveal presets
                        if (config.TryGetProperty("ChunkedTextReveal", out var chunkedTextReveal))
                        {
                            _chunkedTextRevealPresets = new Dictionary<string, ChunkedTextRevealPreset>();
                            foreach (var prop in chunkedTextReveal.EnumerateObject())
                            {
                                var preset = JsonSerializer.Deserialize<ChunkedTextRevealPreset>(prop.Value.GetRawText());
                                if (preset != null)
                                {
                                    _chunkedTextRevealPresets[prop.Name] = preset;
                                }
                            }
                        }

                        // Load combat delays
                        if (config.TryGetProperty("CombatDelays", out var combatDelays))
                        {
                            if (combatDelays.TryGetProperty("ActionDelayMs", out var actionDelay))
                                _actionDelayMs = actionDelay.GetInt32();
                            if (combatDelays.TryGetProperty("MessageDelayMs", out var messageDelay))
                                _messageDelayMs = messageDelay.GetInt32();
                        }

                        // Load progressive menu delays
                        if (config.TryGetProperty("ProgressiveMenuDelays", out var progressiveMenuDelays))
                        {
                            _progressiveMenuDelays = JsonSerializer.Deserialize<ProgressiveMenuDelaysConfig>(progressiveMenuDelays.GetRawText()) 
                                ?? new ProgressiveMenuDelaysConfig();
                        }

                        // Load enable flags
                        if (config.TryGetProperty("EnableGuiDelays", out var enableGui))
                            _enableGuiDelays = enableGui.GetBoolean();
                        if (config.TryGetProperty("EnableConsoleDelays", out var enableConsole))
                            _enableConsoleDelays = enableConsole.GetBoolean();
                    }
                }
                catch (Exception ex)
                {
                    // If config loading fails, use default values
                    // Note: We can't use UIManager here due to circular dependency, so we'll just use defaults
                    // The error will be silent but defaults will be used
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not load text delay config: {ex.Message}");
                }

                // Initialize defaults if not loaded from config
                InitializeDefaults();

                _configLoaded = true;
            }
        }

        /// <summary>
        /// Initializes default values if not loaded from config
        /// </summary>
        private static void InitializeDefaults()
        {
            // Default message type delays
            if (_messageTypeDelays.Count == 0)
            {
                _messageTypeDelays[UIMessageType.Combat] = 100;
                _messageTypeDelays[UIMessageType.System] = 100;
                _messageTypeDelays[UIMessageType.Menu] = 25;
                _messageTypeDelays[UIMessageType.Title] = 400;
                _messageTypeDelays[UIMessageType.MainTitle] = 400;
                _messageTypeDelays[UIMessageType.Environmental] = 150;
                _messageTypeDelays[UIMessageType.EffectMessage] = 50;
                _messageTypeDelays[UIMessageType.DamageOverTime] = 50;
                _messageTypeDelays[UIMessageType.Encounter] = 67;
                _messageTypeDelays[UIMessageType.RollInfo] = 5;
            }

            // Default chunked text reveal presets
            if (_chunkedTextRevealPresets.Count == 0)
            {
                _chunkedTextRevealPresets["Combat"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 20,
                    MinDelayMs = 500,
                    MaxDelayMs = 2000,
                    Strategy = "Line"
                };
                _chunkedTextRevealPresets["Dungeon"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 25,
                    MinDelayMs = 800,
                    MaxDelayMs = 3000,
                    Strategy = "Semantic"
                };
                _chunkedTextRevealPresets["Room"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 30,
                    MinDelayMs = 1000,
                    MaxDelayMs = 3000,
                    Strategy = "Sentence"
                };
                _chunkedTextRevealPresets["Narrative"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 25,
                    MinDelayMs = 400,
                    MaxDelayMs = 2000,
                    Strategy = "Sentence"
                };
                _chunkedTextRevealPresets["Default"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 30,
                    MinDelayMs = 500,
                    MaxDelayMs = 4000,
                    Strategy = "Sentence"
                };
            }
        }

        /// <summary>
        /// Gets the delay for a specific message type
        /// </summary>
        public static int GetMessageTypeDelay(UIMessageType messageType)
        {
            LoadConfig();
            return _messageTypeDelays.TryGetValue(messageType, out var delay) ? delay : 0;
        }

        /// <summary>
        /// Gets a chunked text reveal preset configuration
        /// </summary>
        public static ChunkedTextRevealPreset? GetChunkedTextRevealPreset(string presetName)
        {
            LoadConfig();
            return _chunkedTextRevealPresets.TryGetValue(presetName, out var preset) ? preset : null;
        }

        /// <summary>
        /// Gets the action delay (delay after complete action)
        /// </summary>
        public static int GetActionDelayMs()
        {
            LoadConfig();
            return _actionDelayMs;
        }

        /// <summary>
        /// Gets the message delay (delay between messages within action)
        /// </summary>
        public static int GetMessageDelayMs()
        {
            LoadConfig();
            return _messageDelayMs;
        }

        /// <summary>
        /// Gets the progressive menu delays configuration
        /// </summary>
        public static ProgressiveMenuDelaysConfig GetProgressiveMenuDelays()
        {
            LoadConfig();
            return _progressiveMenuDelays;
        }

        /// <summary>
        /// Gets whether GUI delays are enabled
        /// </summary>
        public static bool GetEnableGuiDelays()
        {
            LoadConfig();
            return _enableGuiDelays;
        }

        /// <summary>
        /// Gets whether console delays are enabled
        /// </summary>
        public static bool GetEnableConsoleDelays()
        {
            LoadConfig();
            return _enableConsoleDelays;
        }

        /// <summary>
        /// Sets the delay for a specific message type and saves to config
        /// </summary>
        public static void SetMessageTypeDelay(UIMessageType messageType, int delayMs)
        {
            LoadConfig();
            lock (_lockObject)
            {
                _messageTypeDelays[messageType] = delayMs;
                SaveConfig();
            }
        }

        /// <summary>
        /// Sets a chunked text reveal preset and saves to config
        /// </summary>
        public static void SetChunkedTextRevealPreset(string presetName, ChunkedTextRevealPreset preset)
        {
            LoadConfig();
            lock (_lockObject)
            {
                _chunkedTextRevealPresets[presetName] = preset;
                SaveConfig();
            }
        }

        /// <summary>
        /// Sets the action delay and saves to config
        /// </summary>
        public static void SetActionDelayMs(int delayMs)
        {
            LoadConfig();
            lock (_lockObject)
            {
                _actionDelayMs = delayMs;
                SaveConfig();
            }
        }

        /// <summary>
        /// Sets the message delay and saves to config
        /// </summary>
        public static void SetMessageDelayMs(int delayMs)
        {
            LoadConfig();
            lock (_lockObject)
            {
                _messageDelayMs = delayMs;
                SaveConfig();
            }
        }

        /// <summary>
        /// Sets the progressive menu delays and saves to config
        /// </summary>
        public static void SetProgressiveMenuDelays(ProgressiveMenuDelaysConfig config)
        {
            LoadConfig();
            lock (_lockObject)
            {
                _progressiveMenuDelays = config;
                SaveConfig();
            }
        }

        /// <summary>
        /// Sets whether GUI delays are enabled and saves to config
        /// </summary>
        public static void SetEnableGuiDelays(bool enabled)
        {
            LoadConfig();
            lock (_lockObject)
            {
                _enableGuiDelays = enabled;
                SaveConfig();
            }
        }

        /// <summary>
        /// Sets whether console delays are enabled and saves to config
        /// </summary>
        public static void SetEnableConsoleDelays(bool enabled)
        {
            LoadConfig();
            lock (_lockObject)
            {
                _enableConsoleDelays = enabled;
                SaveConfig();
            }
        }

        /// <summary>
        /// Configuration structure for JSON serialization
        /// </summary>
        private class TextDelayConfigData
        {
            public Dictionary<string, int> MessageTypeDelays { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, ChunkedTextRevealPreset> ChunkedTextReveal { get; set; } = new Dictionary<string, ChunkedTextRevealPreset>();
            public CombatDelaysData CombatDelays { get; set; } = new CombatDelaysData();
            public ProgressiveMenuDelaysConfig ProgressiveMenuDelays { get; set; } = new ProgressiveMenuDelaysConfig();
            public bool EnableGuiDelays { get; set; } = true;
            public bool EnableConsoleDelays { get; set; } = true;
        }

        /// <summary>
        /// Combat delays data structure
        /// </summary>
        private class CombatDelaysData
        {
            public int ActionDelayMs { get; set; }
            public int MessageDelayMs { get; set; }
        }

        /// <summary>
        /// Saves the current configuration to JSON file
        /// </summary>
        private static void SaveConfig()
        {
            try
            {
                string configPath = "GameData/TextDelayConfig.json";
                
                var configData = new TextDelayConfigData();
                
                // Build message type delays dictionary
                foreach (var kvp in _messageTypeDelays)
                {
                    configData.MessageTypeDelays[kvp.Key.ToString()] = kvp.Value;
                }
                
                // Copy chunked text reveal presets
                configData.ChunkedTextReveal = new Dictionary<string, ChunkedTextRevealPreset>();
                foreach (var kvp in _chunkedTextRevealPresets)
                {
                    configData.ChunkedTextReveal[kvp.Key] = new ChunkedTextRevealPreset
                    {
                        BaseDelayPerCharMs = kvp.Value.BaseDelayPerCharMs,
                        MinDelayMs = kvp.Value.MinDelayMs,
                        MaxDelayMs = kvp.Value.MaxDelayMs,
                        Strategy = kvp.Value.Strategy
                    };
                }
                
                // Build combat delays
                configData.CombatDelays = new CombatDelaysData
                {
                    ActionDelayMs = _actionDelayMs,
                    MessageDelayMs = _messageDelayMs
                };
                
                // Copy progressive menu delays
                configData.ProgressiveMenuDelays = new ProgressiveMenuDelaysConfig
                {
                    BaseMenuDelay = _progressiveMenuDelays.BaseMenuDelay,
                    ProgressiveReductionRate = _progressiveMenuDelays.ProgressiveReductionRate,
                    ProgressiveThreshold = _progressiveMenuDelays.ProgressiveThreshold
                };
                
                // Add enable flags
                configData.EnableGuiDelays = _enableGuiDelays;
                configData.EnableConsoleDelays = _enableConsoleDelays;
                
                // Serialize to JSON with indentation
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonContent = JsonSerializer.Serialize(configData, options);
                
                // Write to file
                System.IO.File.WriteAllText(configPath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving text delay config: {ex.Message}");
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
                LoadConfig();
            }
        }
    }
}

