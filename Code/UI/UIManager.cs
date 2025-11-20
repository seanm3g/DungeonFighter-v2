using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

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
        
        private static UIConfiguration? _uiConfig = null;
        
        // Specialized managers
        private static UIOutputManager? _outputManager = null;
        private static UIDelayManager? _delayManager = null;
        private static UIColoredTextManager? _coloredTextManager = null;
        private static UIMessageBuilder? _messageBuilder = null;
        
        /// <summary>
        /// Gets the current UI configuration
        /// </summary>
        public static UIConfiguration UIConfig
        {
            get
            {
                if (_uiConfig == null)
                {
                    _uiConfig = UIConfiguration.LoadFromFile();
                }
                return _uiConfig;
            }
        }

        /// <summary>
        /// Gets or creates the UIOutputManager
        /// </summary>
        private static UIOutputManager OutputManager
        {
            get
            {
                if (_outputManager == null)
                {
                    _outputManager = new UIOutputManager(_customUIManager, UIConfig);
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
                    _delayManager = new UIDelayManager(UIConfig);
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
            return _customUIManager;
        }
        
        /// <summary>
        /// Reloads the UI configuration from file
        /// </summary>
        public static void ReloadConfiguration()
        {
            _uiConfig = UIConfiguration.LoadFromFile();
            // Reset managers to use the new configuration
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
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            OutputManager.WriteLine(message, messageType);
            DelayManager.ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes text to console without newline with color markup support
        /// </summary>
        /// <param name="message">Message to display (supports &X and {{template|text}} markup)</param>
        public static void Write(string message)
        {
            if (DisableAllUIOutput) return;
            
            OutputManager.Write(message);
        }
        
        
        
        /// <summary>
        /// Writes a system message (no Actor tracking)
        /// </summary>
        /// <param name="message">System message to display</param>
        public static void WriteSystemLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a menu line with progressive delay reduction (speeds up with each consecutive line)
        /// Supports color markup
        /// </summary>
        /// <param name="message">Menu message to display (supports &X and {{template|text}} markup)</param>
        public static void WriteMenuLine(string message)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            OutputManager.WriteMenuLine(message);
            DelayManager.ApplyProgressiveMenuDelay();
        }
        
        /// <summary>
        /// Writes a title line with title delay
        /// </summary>
        /// <param name="message">Title message to display</param>
        public static void WriteTitleLine(string message)
        {
            WriteLine(message, UIMessageType.Title);
        }
        
        /// <summary>
        /// Writes a dungeon-related message with system delay
        /// </summary>
        /// <param name="message">Dungeon message to display</param>
        public static void WriteDungeonLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a room-related message with system delay
        /// </summary>
        /// <param name="message">Room message to display</param>
        public static void WriteRoomLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes an enemy encounter message with system delay
        /// </summary>
        /// <param name="message">Enemy encounter message to display</param>
        public static void WriteEnemyLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a room cleared message with system delay
        /// </summary>
        /// <param name="message">Room cleared message to display</param>
        public static void WriteRoomClearedLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a status effect message (stun, poison, bleed, etc.) with effect message delay
        /// </summary>
        /// <param name="message">Status effect message to display</param>
        public static void WriteEffectLine(string message)
        {
            WriteLine(message, UIMessageType.EffectMessage);
        }
        
        /// <summary>
        /// Resets Actor tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            OutputManager.ResetForNewBattle();
        }
        
        /// <summary>
        /// Applies appropriate delay based on message type using the beat-based timing system
        /// </summary>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void ApplyDelay(UIMessageType messageType)
        {
            DelayManager.ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Resets the progressive menu delay counter (call when menu section is complete)
        /// </summary>
        public static void ResetMenuDelayCounter()
        {
            DelayManager.ResetMenuDelayCounter();
        }
        
        /// <summary>
        /// Gets the current consecutive menu line count (for testing/debugging)
        /// </summary>
        public static int GetConsecutiveMenuLineCount()
        {
            return DelayManager.GetConsecutiveMenuLineCount();
        }
        
        /// <summary>
        /// Gets the current base menu delay (for testing/debugging)
        /// </summary>
        public static int GetBaseMenuDelay()
        {
            return DelayManager.GetBaseMenuDelay();
        }
        
        /// <summary>
        /// Writes a blank line without any delay
        /// </summary>
        public static void WriteBlankLine()
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            OutputManager.WriteBlankLine();
        }
        
        /// <summary>
        /// Writes text with chunked reveal (progressive text display)
        /// Text appears chunk by chunk with delays proportional to chunk length
        /// </summary>
        /// <param name="message">The text to reveal in chunks</param>
        /// <param name="config">Optional configuration for chunked reveal</param>
        public static void WriteChunked(string message, ChunkedTextReveal.RevealConfig? config = null)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            OutputManager.WriteChunked(message, config);
        }
        
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
        /// </summary>
        /// <param name="message">The room description to reveal</param>
        public static void WriteRoomChunked(string message)
        {
            WriteChunked(message, new ChunkedTextReveal.RevealConfig
            {
                Strategy = ChunkedTextReveal.ChunkStrategy.Sentence,
                BaseDelayPerCharMs = 30,
                MinDelayMs = 1000,
                MaxDelayMs = 3000
            });
        }
        
        // ===== COLORED TEXT SYSTEM METHODS =====
        
        /// <summary>
        /// Writes colored text using the new ColoredText system
        /// </summary>
        /// <param name="coloredText">ColoredText object to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            ColoredTextManager.WriteColoredText(coloredText, messageType);
        }
        
        /// <summary>
        /// Writes a list of colored text segments
        /// </summary>
        public static void WriteColoredText(List<ColoredText> coloredTexts, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            ColoredTextManager.WriteColoredText(coloredTexts, messageType);
        }
        
        /// <summary>
        /// Writes colored text using the new ColoredText system with newline
        /// </summary>
        /// <param name="coloredText">ColoredText object to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            ColoredTextManager.WriteLineColoredText(coloredText, messageType);
        }
        
        /// <summary>
        /// Writes colored text segments using the new ColoredText system
        /// </summary>
        /// <param name="segments">List of ColoredText segments to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            ColoredTextManager.WriteColoredSegments(segments, messageType);
        }
        
        /// <summary>
        /// Writes colored text segments using the new ColoredText system with newline
        /// </summary>
        /// <param name="segments">List of ColoredText segments to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            ColoredTextManager.WriteLineColoredSegments(segments, messageType);
        }
        
        /// <summary>
        /// Writes colored text using the builder pattern
        /// </summary>
        /// <param name="builder">ColoredTextBuilder to build and display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            ColoredTextManager.WriteColoredTextBuilder(builder, messageType);
        }
        
        /// <summary>
        /// Writes colored text using the builder pattern with newline
        /// </summary>
        /// <param name="builder">ColoredTextBuilder to build and display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            ColoredTextManager.WriteLineColoredTextBuilder(builder, messageType);
        }
        
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
        {
            MessageBuilder.WriteCombatMessage(attacker, action, target, damage, isCritical, isMiss, isBlock, isDodge);
        }
        
        /// <summary>
        /// Creates a healing message using the new color system
        /// </summary>
        /// <param name="healer">Name of the healer</param>
        /// <param name="target">Name of the target</param>
        /// <param name="amount">Healing amount</param>
        public static void WriteHealingMessage(string healer, string target, int amount)
        {
            MessageBuilder.WriteHealingMessage(healer, target, amount);
        }
        
        /// <summary>
        /// Creates a status effect message using the new color system
        /// </summary>
        /// <param name="target">Name of the target</param>
        /// <param name="effect">Effect name</param>
        /// <param name="isApplied">Whether the effect is applied or removed</param>
        public static void WriteStatusEffectMessage(string target, string effect, bool isApplied = true)
        {
            MessageBuilder.WriteStatusEffectMessage(target, effect, isApplied);
        }
        
    }
}


