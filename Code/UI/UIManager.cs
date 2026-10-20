using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Helpers;
using RPGGame.UI.Services;
using RPGGame.Utils;
using RPGGame.Config;

namespace RPGGame
{
    /// <summary>
    /// Centralized UI management system that handles all console output, formatting, and delays
    /// Supports Caves of Qud-style color markup
    /// 
    /// This is now a facade coordinating four specialized managers:
    /// - UIOutputManager: Handles console/custom UI output
    /// - UIDelayManager: Manages timing and delays
    /// - UIColoredTextManager: Handles colored text operations
    /// - UIMessageBuilder: Builds formatted combat/healing/effect messages
    /// </summary>
    public static class UIManager
    {
        // Flag to disable all UI output during balance analysis
        public static bool DisableAllUIOutput = false;
        
        // Flag to enable color markup parsing
        public static bool EnableColorMarkup = true;
        
        // Custom UI Manager for non-console interfaces (like Avalonia)
        private static IUIManager? _customUIManager = null;

        // Headless game running beside the live one (Stable fighters): its async flow sees its own UI manager
        // and writes nothing through the shared output managers, so the live screen is untouched.
        private static readonly System.Threading.AsyncLocal<IUIManager?> _scopedUIManager = new();
        
        // Flag to enable/disable delays
        public static bool EnableDelays = true;
        
        // Specialized managers
        private static UIOutputManager? _outputManager = null;
        private static UIDelayManager? _delayManager = null;
        private static UIColoredTextManager? _coloredTextManager = null;
        private static UIMessageBuilder? _messageBuilder = null;

        /// <summary>
        /// Gets or creates the UIOutputManager
        /// </summary>
        private static UIOutputManager OutputManager
        {
            get
            {
                if (_outputManager == null)
                {
                    _outputManager = new UIOutputManager(_customUIManager);
                }
                return _outputManager;
            }
        }

        /// <summary>
        /// Gets or creates the UIDelayManager
        /// </summary>
        private static UIDelayManager DelayManager
        {
            get
            {
                if (_delayManager == null)
                {
                    _delayManager = new UIDelayManager();
                }
                return _delayManager;
            }
        }

        /// <summary>
        /// Gets or creates the UIColoredTextManager
        /// </summary>
        private static UIColoredTextManager ColoredTextManager
        {
            get
            {
                if (_coloredTextManager == null)
                {
                    _coloredTextManager = new UIColoredTextManager(OutputManager, DelayManager);
                }
                return _coloredTextManager;
            }
        }

        /// <summary>
        /// Gets or creates the UIMessageBuilder
        /// </summary>
        private static UIMessageBuilder MessageBuilder
        {
            get
            {
                if (_messageBuilder == null)
                {
                    _messageBuilder = new UIMessageBuilder(ColoredTextManager);
                }
                return _messageBuilder;
            }
        }
        
        /// <summary>
        /// Sets a custom UI manager for non-console interfaces (like Avalonia)
        /// </summary>
        public static void SetCustomUIManager(IUIManager? customUIManager)
        {
            _customUIManager = customUIManager;
            // Reset managers to use the new custom manager
            _outputManager = null;
            _coloredTextManager = null;
            _messageBuilder = null;
        }
        
        /// <summary>
        /// Gets the custom UI manager if one is set
        /// </summary>
        public static IUIManager? GetCustomUIManager()
        {
            return _scopedUIManager.Value ?? _customUIManager;
        }

        /// <summary>
        /// True when shared UI output is off: <see cref="DisableAllUIOutput"/>, or inside a <see cref="BeginScopedUIManager"/> flow.
        /// </summary>
        public static bool IsOutputDisabled => DisableAllUIOutput || _scopedUIManager.Value != null;

        /// <summary>
        /// For the current async flow only, <see cref="GetCustomUIManager"/> returns <paramref name="uiManager"/> and the
        /// static write helpers are silent. Lets a headless game run without drawing on the live canvas. Dispose to restore.
        /// </summary>
        public static IDisposable BeginScopedUIManager(IUIManager uiManager)
        {
            var previous = _scopedUIManager.Value;
            _scopedUIManager.Value = uiManager;
            return new ScopedUIManagerRestore(previous);
        }

        private sealed class ScopedUIManagerRestore : IDisposable
        {
            private readonly IUIManager? _previous;
            public ScopedUIManagerRestore(IUIManager? previous) => _previous = previous;
            public void Dispose() => _scopedUIManager.Value = _previous;
        }
        
        /// <summary>
        /// Reloads the UI configuration (resets managers)
        /// </summary>
        public static void ReloadConfiguration()
        {
            // Reset managers to reinitialize with current settings
            _outputManager = null;
            _delayManager = null;
            _coloredTextManager = null;
            _messageBuilder = null;
        }
        
        /// <summary>
        /// Writes a line to console with optional delay and color markup support
        /// </summary>
        /// <param name="message">Message to display (supports &X and {{template|text}} markup)</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () =>
            {
                OutputManager.WriteLine(message, messageType);
                DelayManager.ApplyDelay(messageType);
            });
        }
        
        public static void Write(string message) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => OutputManager.Write(message));
        
        public static void WriteSystemLine(string message) => WriteLine(message, UIMessageType.System);
        public static void WriteMenuLine(string message)
        {
            UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () =>
            {
                OutputManager.WriteMenuLine(message);
                DelayManager.ApplyProgressiveMenuDelay();
            });
        }
        public static void WriteTitleLine(string message) => WriteLine(message, UIMessageType.Title);
        public static void WriteDungeonLine(string message) => WriteLine(message, UIMessageType.System);
        public static void WriteRoomLine(string message) => WriteLine(message, UIMessageType.System);
        public static void WriteEnemyLine(string message) => WriteLine(message, UIMessageType.System);
        public static void WriteRoomClearedLine(string message) => WriteLine(message, UIMessageType.System);
        public static void WriteEffectLine(string message) => WriteLine(message, UIMessageType.EffectMessage);
        
        /// <summary>
        /// Resets Actor tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle() => OutputManager.ResetForNewBattle();
        public static void ApplyDelay(UIMessageType messageType) => DelayManager.ApplyDelay(messageType);
        public static void ResetMenuDelayCounter() => DelayManager.ResetMenuDelayCounter();
        public static int GetConsecutiveMenuLineCount() => DelayManager.GetConsecutiveMenuLineCount();
        public static int GetBaseMenuDelay() => DelayManager.GetBaseMenuDelay();
        public static void WriteBlankLine() 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => OutputManager.WriteBlankLine());
        public static void WriteChunked(string message, ChunkedTextReveal.RevealConfig? config = null) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => OutputManager.WriteChunked(message, config));
        
        /// <summary>
        /// Writes dungeon exploration text with chunked reveal (optimized for dungeon content)
        /// </summary>
        /// <param name="message">The dungeon text to reveal</param>
        public static void WriteDungeonChunked(string message)
        {
            WriteChunked(message, new ChunkedTextReveal.RevealConfig
            {
                Strategy = ChunkedTextReveal.ChunkStrategy.Semantic,
                BaseDelayPerCharMs = 25,
                MinDelayMs = 800,
                MaxDelayMs = 3000
            });
        }
        
        /// <summary>
        /// Writes room description text with chunked reveal
        /// Loads configuration from TextDelayConfig.json
        /// </summary>
        /// <param name="message">The room description to reveal</param>
        public static void WriteRoomChunked(string message)
        {
            var preset = Config.TextDelayConfiguration.GetChunkedTextRevealPreset("Room");
            ChunkedTextReveal.RevealConfig config;
            if (preset != null)
            {
                config = new ChunkedTextReveal.RevealConfig
                {
                    Strategy = ParseChunkStrategy(preset.Strategy),
                    BaseDelayPerCharMs = preset.BaseDelayPerCharMs,
                    MinDelayMs = preset.MinDelayMs,
                    MaxDelayMs = preset.MaxDelayMs
                };
            }
            else
            {
                config = new ChunkedTextReveal.RevealConfig
                {
                    Strategy = ChunkedTextReveal.ChunkStrategy.Sentence,
                    BaseDelayPerCharMs = 30,
                    MinDelayMs = 1000,
                    MaxDelayMs = 3000
                };
            }
            WriteChunked(message, config);
        }
        
        // ===== COLORED TEXT SYSTEM METHODS =====
        
        public static void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => ColoredTextManager.WriteColoredText(coloredText, messageType));
        public static void WriteColoredText(List<ColoredText> coloredTexts, UIMessageType messageType = UIMessageType.System) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => ColoredTextManager.WriteColoredText(coloredTexts, messageType));
        public static void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => ColoredTextManager.WriteLineColoredText(coloredText, messageType));
        public static void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => ColoredTextManager.WriteColoredSegments(segments, messageType));
        public static void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => ColoredTextManager.WriteLineColoredSegments(segments, messageType));
        public static void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => ColoredTextManager.WriteColoredTextBuilder(builder, messageType));
        public static void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => ColoredTextManager.WriteLineColoredTextBuilder(builder, messageType));
        
        // ===== COMBAT MESSAGE BUILDING METHODS =====
        
        /// <summary>
        /// Creates a combat message using the new color system
        /// </summary>
        /// <param name="attacker">Name of the attacker</param>
        /// <param name="action">Action performed</param>
        /// <param name="target">Name of the target</param>
        /// <param name="damage">Damage amount (optional)</param>
        /// <param name="isCritical">Whether this is a critical hit</param>
        /// <param name="isMiss">Whether this is a miss</param>
        /// <param name="isBlock">Whether this is blocked</param>
        /// <param name="isDodge">Whether this is dodged</param>
        public static void WriteCombatMessage(string attacker, string action, string target, int? damage = null, 
            bool isCritical = false, bool isMiss = false, bool isBlock = false, bool isDodge = false) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => MessageBuilder.WriteCombatMessage(attacker, action, target, damage, isCritical, isMiss, isBlock, isDodge));
        public static void WriteHealingMessage(string healer, string target, int amount) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => MessageBuilder.WriteHealingMessage(healer, target, amount));
        public static void WriteStatusEffectMessage(string target, string effect, bool isApplied = true) 
            => UIMethodHelper.ExecuteIfEnabled(IsOutputDisabled, () => MessageBuilder.WriteStatusEffectMessage(target, effect, isApplied));
        
        /// <summary>
        /// Parses a string strategy name to ChunkStrategy enum
        /// </summary>
        private static ChunkedTextReveal.ChunkStrategy ParseChunkStrategy(string strategyName)
        {
            if (Enum.TryParse<ChunkedTextReveal.ChunkStrategy>(strategyName, true, out var strategy))
            {
                return strategy;
            }
            return ChunkedTextReveal.ChunkStrategy.Sentence; // Default fallback
        }
    }
}


