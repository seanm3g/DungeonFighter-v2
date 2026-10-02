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
        private const string ConfigFileName = "TextDelayConfig.json";

        /// <summary>Absolute path written by the most recent successful save, for UI confirmation/debugging.</summary>
        public static string? LastSavedConfigPath { get; private set; }

        public static string GetConfigPath()
        {
            return RPGGame.GameConstants.GetGameDataFilePath(ConfigFileName);
        }

        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        public static TextDelayConfigData LoadConfig()
        {
            var configData = new TextDelayConfigData();

            try
            {
                string configPath = GetConfigPath();
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
                        if (combatDelays.TryGetProperty("TutorialCombatDelayMultiplier", out var tutorialCombatDelayMultiplier))
                            configData.TutorialCombatDelayMultiplier = tutorialCombatDelayMultiplier.GetDouble();
                        if (combatDelays.TryGetProperty("SequenceHudDelayMultiplier", out var sequenceHudDelayMultiplier))
                            configData.SequenceHudDelayMultiplier = sequenceHudDelayMultiplier.GetDouble();
                        if (combatDelays.TryGetProperty("NarrativeCharRevealMs", out var narrativeCharRevealMs))
                            configData.NarrativeCharRevealMs = narrativeCharRevealMs.GetInt32();
                        if (combatDelays.TryGetProperty("NarrativeCharRevealRampChars", out var narrativeCharRevealRampChars))
                            configData.NarrativeCharRevealRampChars = narrativeCharRevealRampChars.GetInt32();
                        if (combatDelays.TryGetProperty("NarrativeCharRevealMaxMs", out var narrativeCharRevealMaxMs))
                            configData.NarrativeCharRevealMaxMs = narrativeCharRevealMaxMs.GetInt32();
                        if (combatDelays.TryGetProperty("NarrativeSentencePauseMs", out var narrativeSentencePauseMs))
                            configData.NarrativeSentencePauseMs = narrativeSentencePauseMs.GetInt32();
                    }

                    // Character reveal rhythm (preferred); seed from legacy narrative if missing.
                    if (config.TryGetProperty("CharacterRevealRhythm", out var characterRevealRhythm))
                    {
                        var loaded = JsonSerializer.Deserialize<CharacterRevealRhythmConfig>(characterRevealRhythm.GetRawText());
                        if (loaded != null)
                        {
                            configData.CharacterRevealRhythm = loaded;
                            configData.CharacterRevealRhythmLoaded = true;
                        }
                    }

                    // Load progressive menu delays
                    if (config.TryGetProperty("ProgressiveMenuDelays", out var progressiveMenuDelays))
                    {
                        configData.ProgressiveMenuDelays = JsonSerializer.Deserialize<ProgressiveMenuDelaysConfig>(progressiveMenuDelays.GetRawText()) 
                            ?? new ProgressiveMenuDelaysConfig();
                    }

                    if (config.TryGetProperty("TravelRouteRollPacing", out var travelRouteRollPacing))
                    {
                        var loaded = JsonSerializer.Deserialize<TravelRouteRollPacingConfig>(travelRouteRollPacing.GetRawText());
                        if (loaded != null)
                            configData.TravelRouteRollPacing = loaded;
                    }

                    // Load environmental line delay
                    if (config.TryGetProperty("EnvironmentalLineDelay", out var environmentalLineDelay))
                        configData.EnvironmentalLineDelay = environmentalLineDelay.GetInt32();

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
            // Default environmental line delay (500ms)
            if (configData.EnvironmentalLineDelay == 0)
            {
                configData.EnvironmentalLineDelay = 500;
            }

            if (configData.TutorialCombatDelayMultiplier <= 0)
            {
                configData.TutorialCombatDelayMultiplier = RPGGame.GameConstants.TutorialCombatDelayMultiplier;
            }

            if (configData.SequenceHudDelayMultiplier <= 0)
            {
                configData.SequenceHudDelayMultiplier = RPGGame.GameConstants.SequenceHudDelayMultiplier;
            }

            if (configData.NarrativeCharRevealMs <= 0)
            {
                configData.NarrativeCharRevealMs = RPGGame.GameConstants.NarrativeCharRevealMs;
            }

            if (configData.NarrativeCharRevealRampChars <= 0)
            {
                configData.NarrativeCharRevealRampChars = RPGGame.GameConstants.NarrativeCharRevealRampChars;
            }

            if (configData.NarrativeCharRevealMaxMs <= 0)
            {
                configData.NarrativeCharRevealMaxMs = RPGGame.GameConstants.NarrativeCharRevealMaxMs;
            }

            if (configData.NarrativeSentencePauseMs <= 0)
            {
                configData.NarrativeSentencePauseMs = RPGGame.GameConstants.NarrativeSentencePauseMs;
            }

            if (!configData.CharacterRevealRhythmLoaded)
            {
                // Prefer seeding from legacy CombatDelays narrative knobs when the new block was absent.
                configData.CharacterRevealRhythm = CharacterRevealRhythmConfig.FromLegacyNarrative(
                    configData.NarrativeCharRevealMs,
                    configData.NarrativeCharRevealRampChars,
                    configData.NarrativeCharRevealMaxMs,
                    configData.NarrativeSentencePauseMs);
                configData.CharacterRevealRhythmLoaded = true;
            }
            else
            {
                SanitizeLoadedRhythm(configData);
            }

            // Mirror rhythm → legacy fields so older getters/saves stay coherent.
            configData.NarrativeCharRevealMs = configData.CharacterRevealRhythm.BaseCharDelayMs;
            configData.NarrativeCharRevealRampChars = configData.CharacterRevealRhythm.BattleRampChars;
            configData.NarrativeCharRevealMaxMs = configData.CharacterRevealRhythm.MaxCharDelayMs;
            configData.NarrativeSentencePauseMs = configData.CharacterRevealRhythm.SentencePauseMs;

            // Default message type delays
            if (configData.MessageTypeDelays.Count == 0)
            {
                configData.MessageTypeDelays[UIMessageType.Combat] = 100;
                configData.MessageTypeDelays[UIMessageType.System] = 100;
                configData.MessageTypeDelays[UIMessageType.Menu] = 25;
                configData.MessageTypeDelays[UIMessageType.Title] = 400;
                configData.MessageTypeDelays[UIMessageType.MainTitle] = 0;
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

        private static void SanitizeLoadedRhythm(TextDelayConfigData configData)
        {
            var rhythm = configData.CharacterRevealRhythm ?? CharacterRevealRhythmConfig.CreateDefault();
            if (rhythm.BaseCharDelayMs <= 0)
                rhythm.BaseCharDelayMs = configData.NarrativeCharRevealMs > 0
                    ? configData.NarrativeCharRevealMs
                    : RPGGame.GameConstants.NarrativeCharRevealMs;
            if (rhythm.MinCharDelayMs < 0)
                rhythm.MinCharDelayMs = 0;
            if (rhythm.MaxCharDelayMs < rhythm.MinCharDelayMs)
                rhythm.MaxCharDelayMs = Math.Max(rhythm.MinCharDelayMs, 40);
            if (rhythm.SentencePauseMs <= 0)
                rhythm.SentencePauseMs = configData.NarrativeSentencePauseMs > 0
                    ? configData.NarrativeSentencePauseMs
                    : RPGGame.GameConstants.NarrativeSentencePauseMs;
            if (rhythm.ParagraphTargetMs < 0)
                rhythm.ParagraphTargetMs = RPGGame.GameConstants.NarrativeParagraphTargetMs;
            if (rhythm.SentenceReferenceChars <= 0)
                rhythm.SentenceReferenceChars = 40;
            if (rhythm.WordReferenceChars <= 0)
                rhythm.WordReferenceChars = 6;
            if (rhythm.BattleRampChars <= 0)
                rhythm.BattleRampChars = configData.NarrativeCharRevealRampChars > 0
                    ? configData.NarrativeCharRevealRampChars
                    : RPGGame.GameConstants.NarrativeCharRevealRampChars;
            rhythm.WordEmphasisPreset = CharacterRevealRhythmCalculator.NormalizePresetName(rhythm.WordEmphasisPreset);
            configData.CharacterRevealRhythm = rhythm;
        }

        /// <summary>
        /// Saves the configuration to JSON file
        /// </summary>
        public static void SaveConfig(TextDelayConfigData configData)
        {
            try
            {
                string configPath = GetConfigPath();
                string? dir = System.IO.Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                
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
                    MessageDelayMs = configData.MessageDelayMs,
                    TutorialCombatDelayMultiplier = configData.TutorialCombatDelayMultiplier,
                    SequenceHudDelayMultiplier = configData.SequenceHudDelayMultiplier,
                    NarrativeCharRevealMs = configData.CharacterRevealRhythm.BaseCharDelayMs,
                    NarrativeCharRevealRampChars = configData.CharacterRevealRhythm.BattleRampChars,
                    NarrativeCharRevealMaxMs = configData.CharacterRevealRhythm.MaxCharDelayMs,
                    NarrativeSentencePauseMs = configData.CharacterRevealRhythm.SentencePauseMs
                };

                saveData.CharacterRevealRhythm = new CharacterRevealRhythmConfig
                {
                    Enabled = configData.CharacterRevealRhythm.Enabled,
                    ParagraphTargetMs = configData.CharacterRevealRhythm.ParagraphTargetMs,
                    BaseCharDelayMs = configData.CharacterRevealRhythm.BaseCharDelayMs,
                    MinCharDelayMs = configData.CharacterRevealRhythm.MinCharDelayMs,
                    MaxCharDelayMs = configData.CharacterRevealRhythm.MaxCharDelayMs,
                    SentencePauseMs = configData.CharacterRevealRhythm.SentencePauseMs,
                    SentenceReferenceChars = configData.CharacterRevealRhythm.SentenceReferenceChars,
                    SentenceScaleMin = configData.CharacterRevealRhythm.SentenceScaleMin,
                    SentenceScaleMax = configData.CharacterRevealRhythm.SentenceScaleMax,
                    WordReferenceChars = configData.CharacterRevealRhythm.WordReferenceChars,
                    WordScaleMin = configData.CharacterRevealRhythm.WordScaleMin,
                    WordScaleMax = configData.CharacterRevealRhythm.WordScaleMax,
                    WordEmphasisPreset = configData.CharacterRevealRhythm.WordEmphasisPreset,
                    WordBeginWeight = configData.CharacterRevealRhythm.WordBeginWeight,
                    WordMidWeight = configData.CharacterRevealRhythm.WordMidWeight,
                    WordEndWeight = configData.CharacterRevealRhythm.WordEndWeight,
                    BattleRampChars = configData.CharacterRevealRhythm.BattleRampChars
                };
                
                // Copy progressive menu delays
                saveData.ProgressiveMenuDelays = new ProgressiveMenuDelaysConfig
                {
                    BaseMenuDelay = configData.ProgressiveMenuDelays.BaseMenuDelay,
                    ProgressiveReductionRate = configData.ProgressiveMenuDelays.ProgressiveReductionRate,
                    ProgressiveThreshold = configData.ProgressiveMenuDelays.ProgressiveThreshold
                };

                saveData.TravelRouteRollPacing = new TravelRouteRollPacingConfig
                {
                    StepDelayBaseMs = configData.TravelRouteRollPacing.StepDelayBaseMs,
                    StepExtraDelayMsPerPointBelow20 = configData.TravelRouteRollPacing.StepExtraDelayMsPerPointBelow20,
                    SummaryBaseMinutes = configData.TravelRouteRollPacing.SummaryBaseMinutes,
                    SummaryExtraMinutesPerPointBelow20 = configData.TravelRouteRollPacing.SummaryExtraMinutesPerPointBelow20
                };
                
                // Add environmental line delay
                saveData.EnvironmentalLineDelay = configData.EnvironmentalLineDelay;

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
                LastSavedConfigPath = configPath;
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
            public int ActionDelayMs { get; set; } = 3000;
            public int MessageDelayMs { get; set; } = 200;
            public double TutorialCombatDelayMultiplier { get; set; } = RPGGame.GameConstants.TutorialCombatDelayMultiplier;
            public double SequenceHudDelayMultiplier { get; set; } = RPGGame.GameConstants.SequenceHudDelayMultiplier;
            public int NarrativeCharRevealMs { get; set; } = RPGGame.GameConstants.NarrativeCharRevealMs;
            public int NarrativeCharRevealRampChars { get; set; } = RPGGame.GameConstants.NarrativeCharRevealRampChars;
            public int NarrativeCharRevealMaxMs { get; set; } = RPGGame.GameConstants.NarrativeCharRevealMaxMs;
            public int NarrativeSentencePauseMs { get; set; } = RPGGame.GameConstants.NarrativeSentencePauseMs;
            public CharacterRevealRhythmConfig CharacterRevealRhythm { get; set; } = CharacterRevealRhythmConfig.CreateDefault();
            /// <summary>True when CharacterRevealRhythm was present in JSON (vs seeded from legacy).</summary>
            public bool CharacterRevealRhythmLoaded { get; set; }
            public int EnvironmentalLineDelay { get; set; } = 500;
            public ProgressiveMenuDelaysConfig ProgressiveMenuDelays { get; set; } = new ProgressiveMenuDelaysConfig();
            public TravelRouteRollPacingConfig TravelRouteRollPacing { get; set; } = new TravelRouteRollPacingConfig();
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
            public CharacterRevealRhythmConfig CharacterRevealRhythm { get; set; } = CharacterRevealRhythmConfig.CreateDefault();
            public int EnvironmentalLineDelay { get; set; } = 500;
            public ProgressiveMenuDelaysConfig ProgressiveMenuDelays { get; set; } = new ProgressiveMenuDelaysConfig();
            public TravelRouteRollPacingConfig TravelRouteRollPacing { get; set; } = new TravelRouteRollPacingConfig();
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
            public double TutorialCombatDelayMultiplier { get; set; } = RPGGame.GameConstants.TutorialCombatDelayMultiplier;
            public double SequenceHudDelayMultiplier { get; set; } = RPGGame.GameConstants.SequenceHudDelayMultiplier;
            public int NarrativeCharRevealMs { get; set; } = RPGGame.GameConstants.NarrativeCharRevealMs;
            public int NarrativeCharRevealRampChars { get; set; } = RPGGame.GameConstants.NarrativeCharRevealRampChars;
            public int NarrativeCharRevealMaxMs { get; set; } = RPGGame.GameConstants.NarrativeCharRevealMaxMs;
            public int NarrativeSentencePauseMs { get; set; } = RPGGame.GameConstants.NarrativeSentencePauseMs;
        }
    }
}

