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
    /// Sentence/word-aware character reveal rhythm for F7 narrative typewriter (and future prose consumers).
    /// </summary>
    public class CharacterRevealRhythmConfig
    {
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Target ms for typing the full open F7 paragraph (char delays). When &gt; 0 and Enabled,
        /// delays are subdivided equally sentence → word → character. Set 0 to use legacy
        /// BaseCharDelayMs × scales × curve + battle ramp.
        /// </summary>
        public int ParagraphTargetMs { get; set; } = RPGGame.GameConstants.NarrativeParagraphTargetMs;
        public int BaseCharDelayMs { get; set; } = RPGGame.GameConstants.NarrativeCharRevealMs;
        public int MinCharDelayMs { get; set; } = 1;
        public int MaxCharDelayMs { get; set; } = 40;
        public int SentencePauseMs { get; set; } = RPGGame.GameConstants.NarrativeSentencePauseMs;
        public int SentenceReferenceChars { get; set; } = 40;
        public double SentenceScaleMin { get; set; } = 0.5;
        public double SentenceScaleMax { get; set; } = 2.0;
        public int WordReferenceChars { get; set; } = 6;
        public double WordScaleMin { get; set; } = 0.75;
        public double WordScaleMax { get; set; } = 1.5;
        public string WordEmphasisPreset { get; set; } = CharacterRevealRhythmCalculator.PresetFlat;
        public double WordBeginWeight { get; set; } = 1.0;
        public double WordMidWeight { get; set; } = 1.0;
        public double WordEndWeight { get; set; } = 1.0;
        public int BattleRampChars { get; set; } = RPGGame.GameConstants.NarrativeCharRevealRampChars;

        public static CharacterRevealRhythmConfig CreateDefault() => new CharacterRevealRhythmConfig();

        /// <summary>
        /// Seeds rhythm knobs from legacy CombatDelays narrative fields when the new block is absent.
        /// </summary>
        public static CharacterRevealRhythmConfig FromLegacyNarrative(
            int baseMs,
            int rampChars,
            int maxMs,
            int sentencePauseMs)
        {
            return new CharacterRevealRhythmConfig
            {
                Enabled = true,
                ParagraphTargetMs = RPGGame.GameConstants.NarrativeParagraphTargetMs,
                BaseCharDelayMs = baseMs > 0 ? baseMs : RPGGame.GameConstants.NarrativeCharRevealMs,
                MinCharDelayMs = 1,
                MaxCharDelayMs = maxMs > 0 ? Math.Max(maxMs, 40) : 40,
                SentencePauseMs = sentencePauseMs > 0 ? sentencePauseMs : RPGGame.GameConstants.NarrativeSentencePauseMs,
                BattleRampChars = rampChars > 0 ? rampChars : RPGGame.GameConstants.NarrativeCharRevealRampChars
            };
        }
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
        /// Sequence HUD beat gap vs <see cref="GetMessageDelayMs"/> (1.5 = 50% slower than the combat log).
        /// </summary>
        public static double GetSequenceHudDelayMultiplier()
        {
            var configData = GetConfigData();
            return configData.SequenceHudDelayMultiplier > 0
                ? configData.SequenceHudDelayMultiplier
                : RPGGame.GameConstants.SequenceHudDelayMultiplier;
        }

        /// <summary>
        /// Full character-reveal rhythm config (sentence/word curves + battle ramp).
        /// </summary>
        public static CharacterRevealRhythmConfig GetCharacterRevealRhythm()
        {
            var configData = GetConfigData();
            return CloneRhythm(configData.CharacterRevealRhythm);
        }

        /// <summary>
        /// Persists character-reveal rhythm settings to TextDelayConfig.json and mirrors legacy narrative fields.
        /// </summary>
        public static void SetCharacterRevealRhythm(CharacterRevealRhythmConfig config)
        {
            var configData = GetConfigData();
            lock (_lockObject)
            {
                var next = SanitizeRhythm(config);
                configData.CharacterRevealRhythm = next;
                // Keep legacy CombatDelays narrative fields in sync for older readers / save shape.
                configData.NarrativeCharRevealMs = next.BaseCharDelayMs;
                configData.NarrativeCharRevealRampChars = next.BattleRampChars;
                configData.NarrativeCharRevealMaxMs = next.MaxCharDelayMs;
                configData.NarrativeSentencePauseMs = next.SentencePauseMs;
                TextDelayLoader.SaveConfig(configData);
            }
        }

        /// <summary>
        /// F7 narrative combat-log typewriter delay per character at battle start (ms).
        /// Prefer <see cref="GetCharacterRevealRhythm"/> for full rhythm knobs.
        /// </summary>
        public static int GetNarrativeCharRevealMs()
        {
            var rhythm = GetConfigData().CharacterRevealRhythm;
            return rhythm.BaseCharDelayMs > 0
                ? rhythm.BaseCharDelayMs
                : RPGGame.GameConstants.NarrativeCharRevealMs;
        }

        /// <summary>
        /// Characters typed before F7 narrative char delay increases by 1ms.
        /// </summary>
        public static int GetNarrativeCharRevealRampChars()
        {
            var rhythm = GetConfigData().CharacterRevealRhythm;
            return rhythm.BattleRampChars > 0
                ? rhythm.BattleRampChars
                : RPGGame.GameConstants.NarrativeCharRevealRampChars;
        }

        /// <summary>
        /// Ceiling for F7 narrative char delay after rhythm + pacing ramp (ms).
        /// </summary>
        public static int GetNarrativeCharRevealMaxMs()
        {
            var rhythm = GetConfigData().CharacterRevealRhythm;
            return rhythm.MaxCharDelayMs > 0
                ? rhythm.MaxCharDelayMs
                : RPGGame.GameConstants.NarrativeCharRevealMaxMs;
        }

        /// <summary>
        /// Pause between F7 narrative sentences (ms).
        /// </summary>
        public static int GetNarrativeSentencePauseMs()
        {
            var rhythm = GetConfigData().CharacterRevealRhythm;
            return rhythm.SentencePauseMs > 0
                ? rhythm.SentencePauseMs
                : RPGGame.GameConstants.NarrativeSentencePauseMs;
        }

        private static CharacterRevealRhythmConfig CloneRhythm(CharacterRevealRhythmConfig source)
        {
            var s = source ?? CharacterRevealRhythmConfig.CreateDefault();
            return new CharacterRevealRhythmConfig
            {
                Enabled = s.Enabled,
                ParagraphTargetMs = s.ParagraphTargetMs,
                BaseCharDelayMs = s.BaseCharDelayMs,
                MinCharDelayMs = s.MinCharDelayMs,
                MaxCharDelayMs = s.MaxCharDelayMs,
                SentencePauseMs = s.SentencePauseMs,
                SentenceReferenceChars = s.SentenceReferenceChars,
                SentenceScaleMin = s.SentenceScaleMin,
                SentenceScaleMax = s.SentenceScaleMax,
                WordReferenceChars = s.WordReferenceChars,
                WordScaleMin = s.WordScaleMin,
                WordScaleMax = s.WordScaleMax,
                WordEmphasisPreset = s.WordEmphasisPreset,
                WordBeginWeight = s.WordBeginWeight,
                WordMidWeight = s.WordMidWeight,
                WordEndWeight = s.WordEndWeight,
                BattleRampChars = s.BattleRampChars
            };
        }

        private static CharacterRevealRhythmConfig SanitizeRhythm(CharacterRevealRhythmConfig? config)
        {
            var next = CloneRhythm(config ?? CharacterRevealRhythmConfig.CreateDefault());
            next.ParagraphTargetMs = Math.Max(0, next.ParagraphTargetMs);
            next.BaseCharDelayMs = Math.Max(0, next.BaseCharDelayMs);
            next.MinCharDelayMs = Math.Max(0, next.MinCharDelayMs);
            next.MaxCharDelayMs = Math.Max(next.MinCharDelayMs, next.MaxCharDelayMs);
            next.SentencePauseMs = Math.Max(0, next.SentencePauseMs);
            next.SentenceReferenceChars = Math.Max(1, next.SentenceReferenceChars);
            next.WordReferenceChars = Math.Max(1, next.WordReferenceChars);
            next.BattleRampChars = Math.Max(1, next.BattleRampChars);
            next.WordEmphasisPreset = CharacterRevealRhythmCalculator.NormalizePresetName(next.WordEmphasisPreset);
            if (next.SentenceScaleMin > next.SentenceScaleMax)
                (next.SentenceScaleMin, next.SentenceScaleMax) = (next.SentenceScaleMax, next.SentenceScaleMin);
            if (next.WordScaleMin > next.WordScaleMax)
                (next.WordScaleMin, next.WordScaleMax) = (next.WordScaleMax, next.WordScaleMin);
            return next;
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

