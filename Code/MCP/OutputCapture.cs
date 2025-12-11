using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.MCP
{
    /// <summary>
    /// Custom IUIManager implementation that captures all game output
    /// for AI context. Stores messages in a buffer that can be retrieved.
    /// </summary>
    public class OutputCapture : IUIManager
    {
        private readonly ConcurrentQueue<string> _outputBuffer = new();
        private readonly object _lock = new object();
        private int _maxBufferSize = 200; // Keep last 200 messages (reduced for token efficiency)
        private const int MaxMessageLength = 500; // Truncate very long messages
        private int _consecutiveMenuLines = 0;
        private int _baseMenuDelay = 25;

        /// <summary>
        /// Gets or sets the maximum number of messages to keep in buffer
        /// </summary>
        public int MaxBufferSize
        {
            get => _maxBufferSize;
            set => _maxBufferSize = Math.Max(1, value);
        }

        /// <summary>
        /// Gets all captured output messages
        /// </summary>
        public List<string> GetOutput()
        {
            lock (_lock)
            {
                return new List<string>(_outputBuffer);
            }
        }

        /// <summary>
        /// Gets the most recent N messages
        /// </summary>
        public List<string> GetRecentOutput(int count = 50)
        {
            lock (_lock)
            {
                var all = new List<string>(_outputBuffer);
                var start = Math.Max(0, all.Count - count);
                return all.GetRange(start, all.Count - start);
            }
        }

        /// <summary>
        /// Clears the output buffer
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                while (_outputBuffer.TryDequeue(out _)) { }
            }
        }

        private void AddMessage(string message)
        {
            // Skip blank lines to save tokens
            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_lock)
            {
                // Truncate very long messages to prevent token bloat
                if (message.Length > MaxMessageLength)
                {
                    message = message.Substring(0, MaxMessageLength) + "... [truncated]";
                }

                _outputBuffer.Enqueue(message);
                // Trim buffer if it exceeds max size
                while (_outputBuffer.Count > _maxBufferSize)
                {
                    _outputBuffer.TryDequeue(out _);
                }
            }
        }

        // IUIManager implementation - all methods capture output
        public void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            AddMessage(message);
        }

        public void Write(string message)
        {
            // Don't add incomplete lines, wait for WriteLine
        }

        public void WriteSystemLine(string message)
        {
            AddMessage($"[System] {message}");
        }

        public void WriteMenuLine(string message)
        {
            _consecutiveMenuLines++;
            AddMessage($"[Menu] {message}");
        }

        public void WriteTitleLine(string message)
        {
            AddMessage($"[Title] {message}");
        }

        public void WriteDungeonLine(string message)
        {
            AddMessage($"[Dungeon] {message}");
        }

        public void WriteRoomLine(string message)
        {
            AddMessage($"[Room] {message}");
        }

        public void WriteEnemyLine(string message)
        {
            AddMessage($"[Enemy] {message}");
        }

        public void WriteRoomClearedLine(string message)
        {
            AddMessage($"[Room Cleared] {message}");
        }

        public void WriteEffectLine(string message)
        {
            AddMessage($"[Effect] {message}");
        }

        public void WriteBlankLine()
        {
            // Skip blank lines to save tokens
            // AddMessage(""); // Commented out to reduce token usage
        }

        public void ResetForNewBattle()
        {
            _consecutiveMenuLines = 0;
        }

        public void ResetMenuDelayCounter()
        {
            _consecutiveMenuLines = 0;
        }

        public int GetConsecutiveMenuLineCount()
        {
            return _consecutiveMenuLines;
        }

        public int GetBaseMenuDelay()
        {
            return _baseMenuDelay;
        }

        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            // For chunked text, we'll just capture the final message
            // The game will call WriteLine after chunking is complete
        }

        public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            AddMessage(coloredText.Text);
        }

        public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            AddMessage(coloredText.Text);
        }

        public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            var combined = string.Join("", segments.ConvertAll(s => s.Text));
            AddMessage(combined);
        }

        public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            var combined = string.Join("", segments.ConvertAll(s => s.Text));
            AddMessage(combined);
        }

        public void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            var text = builder.GetPlainText();
            AddMessage(text);
        }

        public void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            var text = builder.GetPlainText();
            AddMessage(text);
        }
    }
}

