using System;
using System.Collections.Generic;
using System.Threading;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Centralized UI management system that handles all console output, formatting, and delays
    /// Supports Caves of Qud-style color markup
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
        
        // Progressive delay system for menu lines
        private static int consecutiveMenuLines = 0;
        private static int baseMenuDelay = 0;
        
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
        /// Sets a custom UI manager for non-console interfaces (like Avalonia)
        /// </summary>
        public static void SetCustomUIManager(IUIManager? customUIManager)
        {
            _customUIManager = customUIManager;
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
        }
        
        /// <summary>
        /// Writes a line to console with optional delay and color markup support
        /// </summary>
        /// <param name="message">Message to display (supports &X and {{template|text}} markup)</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteLine(message, messageType);
                return;
            }
            
            if (EnableColorMarkup && ColorParser.HasColorMarkup(message))
            {
                ColoredConsoleWriter.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            
            ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes text to console without newline with color markup support
        /// </summary>
        /// <param name="message">Message to display (supports &X and {{template|text}} markup)</param>
        public static void Write(string message)
        {
            if (DisableAllUIOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.Write(message);
                return;
            }
            
            if (EnableColorMarkup && ColorParser.HasColorMarkup(message))
            {
                ColoredConsoleWriter.Write(message);
            }
            else
            {
                Console.Write(message);
            }
        }
        
        
        
        /// <summary>
        /// Writes a system message (no entity tracking)
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
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteMenuLine(message);
                return;
            }
            
            if (EnableColorMarkup && ColorParser.HasColorMarkup(message))
            {
                ColoredConsoleWriter.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            
            ApplyProgressiveMenuDelay();
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
        /// Resets entity tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.ResetForNewBattle();
                return;
            }
            
            // Entity tracking is now handled by BlockDisplayManager
        }
        
        /// <summary>
        /// Applies appropriate delay based on message type using the beat-based timing system
        /// </summary>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void ApplyDelay(UIMessageType messageType)
        {
            int delayMs = UIConfig.GetEffectiveDelay(messageType);
            
            if (delayMs > 0)
            {
                Thread.Sleep(delayMs);
            }
        }
        
        /// <summary>
        /// Applies progressive menu delay - reduces delay by 1ms for each consecutive menu line
        /// After 20 lines, slowly ramps delay down by 1ms each line
        /// </summary>
        private static void ApplyProgressiveMenuDelay()
        {
            // Get base menu delay from configuration
            int baseDelay = UIConfig.BeatTiming.GetMenuDelay();
            
            // Store base delay on first menu line
            if (consecutiveMenuLines == 0)
            {
                baseMenuDelay = baseDelay;
            }
            
            int progressiveDelay;
            
            if (consecutiveMenuLines < 20)
            {
                // First 20 lines: normal progressive reduction (base delay minus 1ms per line)
                progressiveDelay = Math.Max(0, baseMenuDelay - consecutiveMenuLines);
            }
            else
            {
                // After 20 lines: slowly ramp down by 1ms each line
                // Start from the delay at line 20, then reduce by 1ms each subsequent line
                int delayAtLine20 = Math.Max(0, baseMenuDelay - 19); // Delay at line 20 (0-indexed, so line 20 is index 19)
                progressiveDelay = Math.Max(0, delayAtLine20 - (consecutiveMenuLines - 20));
            }
            
            // Apply the delay
            if (progressiveDelay > 0)
            {
                Thread.Sleep(progressiveDelay);
            }
            
            // Increment consecutive menu line counter
            consecutiveMenuLines++;
        }
        
        /// <summary>
        /// Resets the progressive menu delay counter (call when menu section is complete)
        /// </summary>
        public static void ResetMenuDelayCounter()
        {
            consecutiveMenuLines = 0;
            baseMenuDelay = 0;
        }
        
        /// <summary>
        /// Gets the current consecutive menu line count (for testing/debugging)
        /// </summary>
        public static int GetConsecutiveMenuLineCount()
        {
            return consecutiveMenuLines;
        }
        
        /// <summary>
        /// Gets the current base menu delay (for testing/debugging)
        /// </summary>
        public static int GetBaseMenuDelay()
        {
            return baseMenuDelay;
        }
        
        
        /// <summary>
        /// Writes a blank line without any delay
        /// </summary>
        public static void WriteBlankLine()
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteBlankLine();
                return;
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// Writes text with chunked reveal (progressive text display)
        /// Text appears chunk by chunk with delays proportional to chunk length
        /// </summary>
        /// <param name="message">The text to reveal in chunks</param>
        /// <param name="config">Optional configuration for chunked reveal</param>
        public static void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteChunked(message, config);
                return;
            }
            
            // Use ChunkedTextReveal for console output
            UI.ChunkedTextReveal.RevealText(message, config);
        }
        
        /// <summary>
        /// Writes dungeon exploration text with chunked reveal (optimized for dungeon content)
        /// </summary>
        /// <param name="message">The dungeon text to reveal</param>
        public static void WriteDungeonChunked(string message)
        {
            WriteChunked(message, new UI.ChunkedTextReveal.RevealConfig
            {
                Strategy = UI.ChunkedTextReveal.ChunkStrategy.Semantic,
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
            WriteChunked(message, new UI.ChunkedTextReveal.RevealConfig
            {
                Strategy = UI.ChunkedTextReveal.ChunkStrategy.Sentence,
                BaseDelayPerCharMs = 30,
                MinDelayMs = 1000,
                MaxDelayMs = 3000
            });
        }
        
        // ===== NEW COLORED TEXT SYSTEM METHODS =====
        
        /// <summary>
        /// Writes colored text using the new ColoredText system
        /// </summary>
        /// <param name="coloredText">ColoredText object to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteColoredText(coloredText, messageType);
                return;
            }
            
            // For console output, use the new system
            var segments = new List<ColoredText> { coloredText };
            ColoredConsoleWriter.WriteSegments(segments);
            
            ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes a list of colored text segments
        /// </summary>
        public static void WriteColoredText(List<ColoredText> coloredTexts, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                foreach (var coloredText in coloredTexts)
                {
                    _customUIManager.WriteColoredText(coloredText, messageType);
                }
                return;
            }
            
            // For console output, use the new system
            ColoredConsoleWriter.WriteSegments(coloredTexts);
            
            ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes colored text using the new ColoredText system with newline
        /// </summary>
        /// <param name="coloredText">ColoredText object to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteLineColoredText(coloredText, messageType);
                return;
            }
            
            // For console output, use the new system
            var segments = new List<ColoredText> { coloredText };
            ColoredConsoleWriter.WriteSegments(segments);
            Console.WriteLine();
            
            ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes colored text segments using the new ColoredText system
        /// </summary>
        /// <param name="segments">List of ColoredText segments to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteColoredSegments(segments, messageType);
                return;
            }
            
            // For console output, use the new system
            ColoredConsoleWriter.WriteSegments(segments);
            
            ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes colored text segments using the new ColoredText system with newline
        /// </summary>
        /// <param name="segments">List of ColoredText segments to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteLineColoredSegments(segments, messageType);
                return;
            }
            
            // For console output, use the new system
            ColoredConsoleWriter.WriteSegments(segments);
            Console.WriteLine();
            
            ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes colored text using the builder pattern
        /// </summary>
        /// <param name="builder">ColoredTextBuilder to build and display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            var segments = builder.Build();
            WriteColoredSegments(segments, messageType);
        }
        
        /// <summary>
        /// Writes colored text using the builder pattern with newline
        /// </summary>
        /// <param name="builder">ColoredTextBuilder to build and display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            var segments = builder.Build();
            WriteLineColoredSegments(segments, messageType);
        }
        
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
            var builder = new ColoredTextBuilder();
            
            // Attacker name
            builder.Add(attacker, ColorPalette.Player);
            builder.AddSpace();
            
            // Action
            if (isMiss)
            {
                builder.AddWithPattern(action, "miss");
            }
            else if (isBlock)
            {
                builder.AddWithPattern(action, "block");
            }
            else if (isDodge)
            {
                builder.AddWithPattern(action, "dodge");
            }
            else
            {
                builder.AddWithPattern(action, "damage");
            }
            
            builder.AddSpace();
            
            // Target name
            builder.Add(target, ColorPalette.Enemy);
            
            // Damage amount
            if (damage.HasValue)
            {
                builder.AddSpace();
                builder.Add(damage.Value.ToString(), isCritical ? ColorPalette.Critical : ColorPalette.Damage);
                builder.AddSpace();
                builder.AddWithPattern("damage", "damage");
            }
            
            WriteLineColoredTextBuilder(builder, UIMessageType.Combat);
        }
        
        /// <summary>
        /// Creates a healing message using the new color system
        /// </summary>
        /// <param name="healer">Name of the healer</param>
        /// <param name="target">Name of the target</param>
        /// <param name="amount">Healing amount</param>
        public static void WriteHealingMessage(string healer, string target, int amount)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(healer, ColorPalette.Player);
            builder.AddSpace();
            builder.AddWithPattern("heals", "healing");
            builder.AddSpace();
            builder.Add(target, ColorPalette.Player);
            builder.AddSpace();
            builder.Add(amount.ToString(), ColorPalette.Healing);
            builder.AddSpace();
            builder.AddWithPattern("health", "healing");
            
            WriteLineColoredTextBuilder(builder, UIMessageType.Combat);
        }
        
        /// <summary>
        /// Creates a status effect message using the new color system
        /// </summary>
        /// <param name="target">Name of the target</param>
        /// <param name="effect">Effect name</param>
        /// <param name="isApplied">Whether the effect is applied or removed</param>
        public static void WriteStatusEffectMessage(string target, string effect, bool isApplied = true)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(target, ColorPalette.Player);
            builder.AddSpace();
            
            if (isApplied)
            {
                builder.AddWithPattern("is affected by", "warning");
            }
            else
            {
                builder.AddWithPattern("is no longer affected by", "success");
            }
            
            builder.AddSpace();
            builder.AddWithPattern(effect, "warning");
            
            WriteLineColoredTextBuilder(builder, UIMessageType.Combat);
        }
        
    }
}
