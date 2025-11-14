using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Threading;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages text display, formatting, and display buffer for the canvas UI
    /// </summary>
    public class CanvasTextManager : ICanvasTextManager
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<string> displayBuffer;
        private readonly int maxLines;
        private readonly ICanvasContextManager contextManager;
        
        public CanvasTextManager(GameCanvasControl canvas, ColoredTextWriter textWriter, ICanvasContextManager contextManager, int maxLines = 100)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.contextManager = contextManager;
            this.maxLines = maxLines;
            this.displayBuffer = new List<string>();
        }
        
        /// <summary>
        /// Gets the current display buffer
        /// </summary>
        public List<string> DisplayBuffer => displayBuffer;
        
        /// <summary>
        /// Gets the maximum number of lines to display
        /// </summary>
        public int MaxLines => maxLines;
        
        /// <summary>
        /// Clears the display buffer
        /// </summary>
        public void ClearDisplayBuffer()
        {
            displayBuffer.Clear();
        }
        
        /// <summary>
        /// Adds a message to the display buffer
        /// </summary>
        /// <param name="message">The message to add</param>
        /// <param name="messageType">The type of message for delay configuration</param>
        public void AddToDisplayBuffer(string message, UIMessageType messageType = UIMessageType.System)
        {
            // Truncate message if too long (use display length to handle color markup)
            if (ColorParser.GetDisplayLength(message) > CanvasLayoutManager.CONTENT_WIDTH)
            {
                // Strip markup before truncating to avoid cutting markup codes in the middle
                string strippedMessage = ColorParser.StripColorMarkup(message);
                message = strippedMessage.Substring(0, Math.Min(strippedMessage.Length, CanvasLayoutManager.CONTENT_WIDTH - 3)) + "...";
            }

            // Prevent consecutive duplicate messages
            if (displayBuffer.Count > 0 && displayBuffer[displayBuffer.Count - 1] == message)
            {
                return; // Skip duplicate
            }

            displayBuffer.Add(message);
            
            // Keep only the last maxLines
            if (displayBuffer.Count > maxLines)
            {
                displayBuffer.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Writes colored text to canvas with color markup support
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        public void WriteLineColored(string message, int x, int y)
        {
            textWriter.WriteLineColored(message, x, y);
        }
        
        /// <summary>
        /// Writes colored text to canvas with color markup support and text wrapping
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="maxWidth">Maximum width for text wrapping</param>
        /// <returns>Number of lines rendered</returns>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            return textWriter.WriteLineColoredWrapped(message, x, y, maxWidth);
        }
        
        /// <summary>
        /// Writes text with chunked reveal (progressive text display)
        /// </summary>
        /// <param name="message">The text to reveal in chunks</param>
        /// <param name="config">Optional configuration for chunked reveal</param>
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            config ??= UI.ChunkedTextReveal.DefaultConfig;
            
            // If chunked reveal is disabled, just write normally
            if (!config.Enabled)
            {
                AddToDisplayBuffer(message);
                return;
            }
            
            // Split text into chunks
            var chunks = SplitIntoChunks(message, config.Strategy);
            
            // Add each chunk to display buffer with delays
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                
                // Add chunk to display buffer
                AddToDisplayBuffer(chunk);
                
                // Add blank line if configured
                if (config.AddBlankLineBetweenChunks && i < chunks.Count - 1)
                {
                    AddToDisplayBuffer("");
                }
                
                // Apply delay if not the last chunk
                if (i < chunks.Count - 1)
                {
                    int delay = CalculateChunkDelay(chunk, config);
                    Thread.Sleep(delay);
                }
            }
        }
        
        /// <summary>
        /// Splits text into chunks based on strategy
        /// </summary>
        /// <param name="text">The text to split</param>
        /// <param name="strategy">The chunking strategy to use</param>
        /// <returns>List of text chunks</returns>
        private List<string> SplitIntoChunks(string text, UI.ChunkedTextReveal.ChunkStrategy strategy)
        {
            var chunks = new List<string>();
            
            switch (strategy)
            {
                case UI.ChunkedTextReveal.ChunkStrategy.Sentence:
                    // Split by sentences
                    chunks = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[.!?])\s+(?=[A-Z\n])")
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    break;
                    
                case UI.ChunkedTextReveal.ChunkStrategy.Paragraph:
                    // Split by paragraphs
                    chunks = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                    break;
                    
                case UI.ChunkedTextReveal.ChunkStrategy.Line:
                    // Split by lines
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    break;
                    
                case UI.ChunkedTextReveal.ChunkStrategy.Semantic:
                    // Split by semantic sections (simple version for now)
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    break;
            }
            
            return chunks;
        }
        
        /// <summary>
        /// Calculates delay for a chunk based on its length
        /// </summary>
        /// <param name="chunk">The text chunk</param>
        /// <param name="config">The reveal configuration</param>
        /// <returns>Delay in milliseconds</returns>
        private int CalculateChunkDelay(string chunk, UI.ChunkedTextReveal.RevealConfig config)
        {
            // Get display length (excluding color markup)
            int displayLength = ColorParser.GetDisplayLength(chunk);
            
            // Calculate delay: base delay per character * number of characters
            int calculatedDelay = displayLength * config.BaseDelayPerCharMs;
            
            // Clamp to min/max
            int delay = Math.Max(config.MinDelayMs, Math.Min(config.MaxDelayMs, calculatedDelay));
            
            return delay;
        }
        
        /// <summary>
        /// Renders the display buffer to the canvas
        /// </summary>
        /// <param name="contentX">X position of content area</param>
        /// <param name="contentY">Y position of content area</param>
        /// <param name="contentWidth">Width of content area</param>
        /// <param name="contentHeight">Height of content area</param>
        public void RenderDisplayBuffer(int contentX, int contentY, int contentWidth, int contentHeight)
        {
            // Dispatch to UI thread to avoid cross-thread issues
            Dispatcher.UIThread.Post(() =>
            {
                int y = contentY;
                int availableWidth = contentWidth - 4; // 2 chars padding on each side
                
                foreach (var line in displayBuffer.TakeLast(maxLines))
                {
                    if (y < contentY + contentHeight - 1)
                    {
                        // Parse and render color markup with text wrapping
                        int linesRendered = WriteLineColoredWrapped(line, contentX + 2, y, availableWidth);
                        y += linesRendered;
                    }
                }
                
                // Refresh the canvas to display the rendered text
                canvas.Refresh();
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Renders the display buffer with persistent layout (for combat and other game phases)
        /// </summary>
        public void RenderDisplayBufferFallback()
        {
            // Dispatch to UI thread to avoid cross-thread issues
            Dispatcher.UIThread.Post(() =>
            {
                // Use persistent layout system instead of simple fallback
                // This ensures combat displays in the center panel while keeping character info visible
                var persistentLayout = new PersistentLayoutManager(canvas);
                var currentCharacter = GetCurrentCharacter();
                var currentEnemy = GetCurrentEnemy();
                var dungeonName = GetCurrentDungeonName();
                var roomName = GetCurrentRoomName();
                
                persistentLayout.RenderLayout(
                    currentCharacter,
                    (contentX, contentY, contentWidth, contentHeight) => {
                        // Render display buffer content in the center panel
                        int y = contentY;
                        foreach (var line in displayBuffer.TakeLast(maxLines))
                        {
                            if (y < contentY + contentHeight - 1)
                            {
                                // Parse and render color markup
                                WriteLineColored(line, contentX + 1, y);
                                y++;
                            }
                        }
                    },
                    "COMBAT",
                    currentEnemy,
                    dungeonName,
                    roomName
                );
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Gets the number of lines in the display buffer
        /// </summary>
        public int BufferLineCount => displayBuffer.Count;
        
        /// <summary>
        /// Gets the last N lines from the display buffer
        /// </summary>
        /// <param name="count">Number of lines to get</param>
        /// <returns>List of the last N lines</returns>
        public List<string> GetLastLines(int count)
        {
            return displayBuffer.TakeLast(count).ToList();
        }
        
        /// <summary>
        /// Clears the display buffer and refreshes the canvas
        /// </summary>
        public void ClearDisplay()
        {
            canvas.Clear();
            displayBuffer.Clear();
            canvas.Refresh();
        }
        
        /// <summary>
        /// Gets the current character from context manager
        /// </summary>
        private Character? GetCurrentCharacter()
        {
            return contextManager.GetCurrentCharacter();
        }
        
        /// <summary>
        /// Gets the current enemy from context manager
        /// </summary>
        private Enemy? GetCurrentEnemy()
        {
            return contextManager.GetCurrentEnemy();
        }
        
        /// <summary>
        /// Gets the current dungeon name from context manager
        /// </summary>
        private string? GetCurrentDungeonName()
        {
            return contextManager.GetDungeonName();
        }
        
        /// <summary>
        /// Gets the current room name from context manager
        /// </summary>
        private string? GetCurrentRoomName()
        {
            return contextManager.GetRoomName();
        }
    }
}
