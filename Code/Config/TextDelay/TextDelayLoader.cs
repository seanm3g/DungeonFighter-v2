using System;
using System.Collections.Generic;
using System.Text.Json;
using RPGGame.UI;

namespace RPGGame.Config.TextDelay
{
    /// <summary>
    /// Handles loading text delay configuration from JSON files
    /// Extracted from TextDelayConfiguration to separate loading logic
    /// </summary>
    public static class TextDelayLoader
    {
        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        public static TextDelayConfigData LoadConfig()
        {
            var configData = new TextDelayConfigData();

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
                        configData.MessageTypeDelays = new Dictionary<UIMessageType, int>();
                        foreach (var prop in messageTypeDelays.EnumerateObject())
                        {
                            // Try case-insensitive parsing first, then case-sensitive
                            if (Enum.TryParse<UIMessageType>(prop.Name, true, out var messageType) ||
                                Enum.TryParse<UIMessageType>(prop.Name, false, out messageType))
                            {
                                configData.MessageTypeDelays[messageType] = prop.Value.GetInt32();
                            }
                            else
                            {
                                // Log warning for unparseable enum values
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not parse message type '{prop.Name}' from TextDelayConfig.json");
                            }
                        }
                    }

                    // Load chunked text reveal presets
                    if (config.TryGetProperty("ChunkedTextReveal", out var chunkedTextReveal))
                    {
                        configData.ChunkedTextRevealPresets = new Dictionary<string, ChunkedTextRevealPreset>();
                        foreach (var prop in chunkedTextReveal.EnumerateObject())
                        {
                            var preset = JsonSerializer.Deserialize<ChunkedTextRevealPreset>(prop.Value.GetRawText());
                            if (preset != null)
                            {
                                configData.ChunkedTextRevealPresets[prop.Name] = preset;
                            }
                        }
                    }

                    // Load combat delays
                    if (config.TryGetProperty("CombatDelays", out var combatDelays))
                    {
                        if (combatDelays.TryGetProperty("ActionDelayMs", out var actionDelay))
                            configData.ActionDelayMs = actionDelay.GetInt32();
                        if (combatDelays.TryGetProperty("MessageDelayMs", out var messageDelay))
                            configData.MessageDelayMs = messageDelay.GetInt32();
                    }

                    // Load progressive menu delays
                    if (config.TryGetProperty("ProgressiveMenuDelays", out var progressiveMenuDelays))
                    {
                        configData.ProgressiveMenuDelays = JsonSerializer.Deserialize<ProgressiveMenuDelaysConfig>(progressiveMenuDelays.GetRawText()) 
                            ?? new ProgressiveMenuDelaysConfig();
                    }

                    // Load enable flags
                    if (config.TryGetProperty("EnableGuiDelays", out var enableGui))
                        configData.EnableGuiDelays = enableGui.GetBoolean();
                    if (config.TryGetProperty("EnableConsoleDelays", out var enableConsole))
                        configData.EnableConsoleDelays = enableConsole.GetBoolean();
                }
            }
            catch (Exception ex)
            {
                // If config loading fails, use default values
                System.Diagnostics.Debug.WriteLine($"Warning: Could not load text delay config: {ex.Message}");
            }

            // Initialize defaults if not loaded from config
            InitializeDefaults(configData);

            return configData;
        }

        /// <summary>
        /// Initializes default values if not loaded from config
        /// </summary>
        private static void InitializeDefaults(TextDelayConfigData configData)
        {
            // Default message type delays
            if (configData.MessageTypeDelays.Count == 0)
            {
                configData.MessageTypeDelays[UIMessageType.Combat] = 100;
                configData.MessageTypeDelays[UIMessageType.System] = 100;
                configData.MessageTypeDelays[UIMessageType.Menu] = 25;
                configData.MessageTypeDelays[UIMessageType.Title] = 400;
                configData.MessageTypeDelays[UIMessageType.MainTitle] = 400;
                configData.MessageTypeDelays[UIMessageType.Environmental] = 150;
                configData.MessageTypeDelays[UIMessageType.EffectMessage] = 50;
                configData.MessageTypeDelays[UIMessageType.DamageOverTime] = 50;
                configData.MessageTypeDelays[UIMessageType.Encounter] = 67;
                configData.MessageTypeDelays[UIMessageType.RollInfo] = 5;
            }

            // Default chunked text reveal presets
            if (configData.ChunkedTextRevealPresets.Count == 0)
            {
                configData.ChunkedTextRevealPresets["Combat"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 20,
                    MinDelayMs = 500,
                    MaxDelayMs = 2000,
                    Strategy = "Line"
                };
                configData.ChunkedTextRevealPresets["Dungeon"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 25,
                    MinDelayMs = 800,
                    MaxDelayMs = 3000,
                    Strategy = "Semantic"
                };
                configData.ChunkedTextRevealPresets["Room"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 30,
                    MinDelayMs = 1000,
                    MaxDelayMs = 3000,
                    Strategy = "Sentence"
                };
                configData.ChunkedTextRevealPresets["Narrative"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 25,
                    MinDelayMs = 400,
                    MaxDelayMs = 2000,
                    Strategy = "Sentence"
                };
                configData.ChunkedTextRevealPresets["Default"] = new ChunkedTextRevealPreset
                {
                    BaseDelayPerCharMs = 30,
                    MinDelayMs = 500,
                    MaxDelayMs = 4000,
                    Strategy = "Sentence"
                };
            }
        }

        /// <summary>
        /// Saves the configuration to JSON file
        /// </summary>
        public static void SaveConfig(TextDelayConfigData configData)
        {
            try
            {
                string configPath = "GameData/TextDelayConfig.json";
                
                var saveData = new TextDelayConfigSaveData();
                
                // Build message type delays dictionary
                foreach (var kvp in configData.MessageTypeDelays)
                {
                    saveData.MessageTypeDelays[kvp.Key.ToString()] = kvp.Value;
                }
                
                // Copy chunked text reveal presets
                saveData.ChunkedTextReveal = new Dictionary<string, ChunkedTextRevealPreset>();
                foreach (var kvp in configData.ChunkedTextRevealPresets)
                {
                    saveData.ChunkedTextReveal[kvp.Key] = new ChunkedTextRevealPreset
                    {
                        BaseDelayPerCharMs = kvp.Value.BaseDelayPerCharMs,
                        MinDelayMs = kvp.Value.MinDelayMs,
                        MaxDelayMs = kvp.Value.MaxDelayMs,
                        Strategy = kvp.Value.Strategy
                    };
                }
                
                // Build combat delays
                saveData.CombatDelays = new CombatDelaysData
                {
                    ActionDelayMs = configData.ActionDelayMs,
                    MessageDelayMs = configData.MessageDelayMs
                };
                
                // Copy progressive menu delays
                saveData.ProgressiveMenuDelays = new ProgressiveMenuDelaysConfig
                {
                    BaseMenuDelay = configData.ProgressiveMenuDelays.BaseMenuDelay,
                    ProgressiveReductionRate = configData.ProgressiveMenuDelays.ProgressiveReductionRate,
                    ProgressiveThreshold = configData.ProgressiveMenuDelays.ProgressiveThreshold
                };
                
                // Add enable flags
                saveData.EnableGuiDelays = configData.EnableGuiDelays;
                saveData.EnableConsoleDelays = configData.EnableConsoleDelays;
                
                // Serialize to JSON with indentation
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonContent = JsonSerializer.Serialize(saveData, options);
                
                // Write to file
                System.IO.File.WriteAllText(configPath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving text delay config: {ex.Message}");
            }
        }

        /// <summary>
        /// Configuration data structure for internal use
        /// </summary>
        public class TextDelayConfigData
        {
            public Dictionary<UIMessageType, int> MessageTypeDelays { get; set; } = new Dictionary<UIMessageType, int>();
            public Dictionary<string, ChunkedTextRevealPreset> ChunkedTextRevealPresets { get; set; } = new Dictionary<string, ChunkedTextRevealPreset>();
            public int ActionDelayMs { get; set; } = 1000;
            public int MessageDelayMs { get; set; } = 200;
            public ProgressiveMenuDelaysConfig ProgressiveMenuDelays { get; set; } = new ProgressiveMenuDelaysConfig();
            public bool EnableGuiDelays { get; set; } = true;
            public bool EnableConsoleDelays { get; set; } = true;
        }

        /// <summary>
        /// Configuration structure for JSON serialization
        /// </summary>
        private class TextDelayConfigSaveData
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
    }
}

