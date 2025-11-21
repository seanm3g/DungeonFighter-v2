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
        
        // Manual scroll offset for user-controlled scrolling
        private int manualScrollOffset = 0;
        private bool isManualScrolling = false;
        private int lastBufferCountWhenScrolling = 0;
        private int lastManualScrollOffset = -1; // Track scroll offset changes to force re-render
        
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
            // Reset scroll state when buffer is cleared
            isManualScrolling = false;
            manualScrollOffset = 0;
            lastBufferCountWhenScrolling = 0;
            lastManualScrollOffset = -1;
            // Reset render cache when buffer is cleared
            layoutInitialized = false;
            lastBufferCount = 0;
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

            bool wasAtBottom = !isManualScrolling || (displayBuffer.Count == lastBufferCountWhenScrolling);
            
            displayBuffer.Add(message);
            
            // Keep only the last maxLines
            if (displayBuffer.Count > maxLines)
            {
                displayBuffer.RemoveAt(0);
            }
            
            // If new text was added and user was at the bottom (or not manually scrolling), auto-scroll to bottom
            if (wasAtBottom)
            {
                isManualScrolling = false;
                manualScrollOffset = 0;
                lastBufferCountWhenScrolling = displayBuffer.Count;
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
        
        public void WriteLineColoredSegments(List<ColoredText> segments, int x, int y)
        {
            textWriter.RenderSegments(segments, x, y);
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
                    // Use TrimEnd() instead of Trim() to preserve leading spaces (e.g., for indented roll info)
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.TrimEnd())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    break;
                    
                case UI.ChunkedTextReveal.ChunkStrategy.Semantic:
                    // Split by semantic sections (simple version for now)
                    // Use TrimEnd() instead of Trim() to preserve leading spaces (e.g., for indented roll info)
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.TrimEnd())
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
                var linesToRender = displayBuffer.TakeLast(maxLines).ToList();
                if (linesToRender.Count == 0)
                {
                    canvas.Refresh();
                    return;
                }
                
                int availableWidth = contentWidth - 4; // 2 chars padding on each side
                
                // Calculate total height needed for all lines (accounting for text wrapping and newlines)
                int totalHeight = 0;
                var lineHeights = new List<int>();
                foreach (var line in linesToRender)
                {
                    int linesNeeded = CalculateWrappedLineCount(line, availableWidth);
                    lineHeights.Add(linesNeeded);
                    totalHeight += linesNeeded;
                }
                
                // Calculate scroll offset: if content exceeds viewport, scroll to show bottom
                int scrollOffset = 0;
                if (totalHeight > contentHeight)
                {
                    // Scroll to bottom: start rendering from a position that shows the last contentHeight lines
                    scrollOffset = totalHeight - contentHeight;
                }
                
                // Render lines, starting from the scroll offset position
                int y = contentY;
                int currentHeight = 0;
                for (int i = 0; i < linesToRender.Count; i++)
                {
                    var line = linesToRender[i];
                    int linesNeeded = lineHeights[i];
                    
                    // Skip lines until we reach the scroll offset
                    if (currentHeight + linesNeeded <= scrollOffset)
                    {
                        currentHeight += linesNeeded;
                        continue; // Skip this line, it's above the viewport
                    }
                    
                    // Render this line if it fits in the viewport
                    if (y < contentY + contentHeight - 1)
                    {
                        // Parse and render color markup with text wrapping
                        int linesRendered = WriteLineColoredWrapped(line, contentX + 2, y, availableWidth);
                        y += linesRendered;
                    }
                    else
                    {
                        // No more room in viewport
                        break;
                    }
                }
                
                // Refresh the canvas to display the rendered text
                canvas.Refresh();
            }, DispatcherPriority.Background);
        }
        
        // Cache for layout state to avoid unnecessary full re-renders
        private Character? lastRenderedCharacter;
        private Enemy? lastRenderedEnemy;
        private string? lastRenderedDungeonName;
        private string? lastRenderedRoomName;
        private bool layoutInitialized = false;
        private int lastBufferCount = 0;
        private System.Threading.Timer? debounceTimer;
        private readonly object renderLock = new object();
        
        /// <summary>
        /// Renders the display buffer with persistent layout (for combat and other game phases)
        /// Optimized to only re-render what's necessary to prevent flickering
        /// Uses debouncing to batch rapid text updates (waits 16ms to batch multiple updates)
        /// </summary>
        public void RenderDisplayBufferFallback()
        {
            // Cancel any pending debounced render
            lock (renderLock)
            {
                debounceTimer?.Dispose();
                
                // Debounce rapid updates - wait 16ms (60fps) before rendering
                // This batches multiple text updates together to reduce flickering
                debounceTimer = new System.Threading.Timer(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        PerformRender();
                    }, DispatcherPriority.Normal);
                }, null, 16, System.Threading.Timeout.Infinite);
            }
        }
        
        /// <summary>
        /// Performs the actual rendering operation
        /// </summary>
        private void PerformRender()
        {
            var currentCharacter = GetCurrentCharacter();
            var currentEnemy = GetCurrentEnemy();
            var dungeonName = GetCurrentDungeonName();
            var roomName = GetCurrentRoomName();
            
            // Check if we need to re-render the full layout (character/enemy/dungeon changed)
            bool needsFullRender = !layoutInitialized ||
                !ReferenceEquals(currentCharacter, lastRenderedCharacter) ||
                !ReferenceEquals(currentEnemy, lastRenderedEnemy) ||
                dungeonName != lastRenderedDungeonName ||
                roomName != lastRenderedRoomName;
            
            // Check if buffer changed (new text added)
            bool bufferChanged = displayBuffer.Count != lastBufferCount;
            
            // Check if scroll offset changed (user scrolled)
            bool scrollChanged = manualScrollOffset != lastManualScrollOffset;
            
            // Determine if this is a major state change (different dungeon/room/character) vs minor (just enemy changed)
            // For minor changes (like transitioning to combat in same dungeon/room), don't clear - just update
            bool isMajorStateChange = !layoutInitialized ||
                !ReferenceEquals(currentCharacter, lastRenderedCharacter) ||
                dungeonName != lastRenderedDungeonName ||
                roomName != lastRenderedRoomName;
            
            // Only clear canvas on major state changes (new dungeon, new room, new character)
            // For combat transitions within same dungeon/room, preserve existing content
            bool shouldClearCanvas = isMajorStateChange;
            
            // Re-render if: full render needed, buffer changed, or scroll position changed
            if (needsFullRender || bufferChanged || scrollChanged)
            {
                var persistentLayout = new PersistentLayoutManager(canvas);
                
                // Only clear canvas and re-render full layout on major state changes
                // For combat transitions within same dungeon/room, preserve existing content
                if (shouldClearCanvas)
                {
                    // Major state change - clear and render full layout
                    persistentLayout.RenderLayout(
                        currentCharacter,
                        (contentX, contentY, contentWidth, contentHeight) => {
                            // Render display buffer content in the center panel
                            RenderCenterContent(contentX, contentY, contentWidth, contentHeight);
                        },
                        "COMBAT",
                        currentEnemy,
                        dungeonName,
                        roomName,
                        clearCanvas: true
                    );
                }
                else
                {
                    // Minor state change (just enemy changed or buffer updated)
                    // Still do a full render, but the display buffer already contains all pre-combat info
                    // This ensures everything is rendered correctly with the updated enemy info
                    persistentLayout.RenderLayout(
                        currentCharacter,
                        (contentX, contentY, contentWidth, contentHeight) => {
                            // Render display buffer content in the center panel
                            // Display buffer already contains all pre-combat info, so it will all be rendered
                            RenderCenterContent(contentX, contentY, contentWidth, contentHeight);
                        },
                        "COMBAT",
                        currentEnemy,
                        dungeonName,
                        roomName,
                        clearCanvas: true
                    );
                }
                
                // Update cache
                lastRenderedCharacter = currentCharacter;
                lastRenderedEnemy = currentEnemy;
                lastRenderedDungeonName = dungeonName;
                lastRenderedRoomName = roomName;
                lastBufferCount = displayBuffer.Count;
                lastManualScrollOffset = manualScrollOffset; // Track scroll offset
                layoutInitialized = true;
            }
        }
        
        /// <summary>
        /// Calculates how many lines a message will take when wrapped
        /// Accounts for newlines and text wrapping
        /// </summary>
        private int CalculateWrappedLineCount(string message, int maxWidth)
        {
            if (string.IsNullOrEmpty(message))
                return 0;
            
            // Split on newlines first (like WriteLineColoredWrapped does)
            var newlineSplit = message.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int totalLines = 0;
            
            foreach (var line in newlineSplit)
            {
                if (string.IsNullOrEmpty(line))
                {
                    totalLines++; // Empty lines still take one line
                    continue;
                }
                
                // Calculate how many wrapped lines this line will take
                // Use display length (excluding color markup) for accurate calculation
                int displayLength = ColorParser.GetDisplayLength(line);
                int wrappedLines = Math.Max(1, (int)Math.Ceiling((double)displayLength / maxWidth));
                totalLines += wrappedLines;
            }
            
            return totalLines;
        }
        
        /// <summary>
        /// Renders the center content area (combat text)
        /// Uses manual scroll offset if user is scrolling, otherwise auto-scrolls to bottom
        /// </summary>
        private void RenderCenterContent(int contentX, int contentY, int contentWidth, int contentHeight)
        {
            var linesToRender = displayBuffer.TakeLast(maxLines).ToList();
            if (linesToRender.Count == 0)
                return;
            
            int availableWidth = contentWidth - 2;
            
            // Calculate total height needed for all lines (accounting for text wrapping and newlines)
            int totalHeight = 0;
            var lineHeights = new List<int>();
            foreach (var line in linesToRender)
            {
                int linesNeeded = CalculateWrappedLineCount(line, availableWidth);
                lineHeights.Add(linesNeeded);
                totalHeight += linesNeeded;
            }
            
            // Calculate scroll offset
            int scrollOffset = 0;
            if (isManualScrolling)
            {
                // Use manual scroll offset, clamped to valid range
                int maxScrollOffset = Math.Max(0, totalHeight - contentHeight);
                scrollOffset = Math.Max(0, Math.Min(manualScrollOffset, maxScrollOffset));
                // Update manualScrollOffset to the clamped value
                manualScrollOffset = scrollOffset;
            }
            else if (totalHeight > contentHeight)
            {
                // Auto-scroll to bottom: start rendering from a position that shows the last contentHeight lines
                scrollOffset = totalHeight - contentHeight;
                manualScrollOffset = scrollOffset; // Keep manual offset in sync
            }
            else
            {
                // Content fits in viewport, no scrolling needed
                manualScrollOffset = 0;
            }
            
            // Render lines, starting from the scroll offset position
            int y = contentY;
            int currentHeight = 0;
            for (int i = 0; i < linesToRender.Count; i++)
            {
                var line = linesToRender[i];
                int linesNeeded = lineHeights[i];
                
                // Skip lines until we reach the scroll offset
                if (currentHeight + linesNeeded <= scrollOffset)
                {
                    currentHeight += linesNeeded;
                    continue; // Skip this line, it's above the viewport
                }
                
                // Render this line if it fits in the viewport
                if (y < contentY + contentHeight)
                {
                    // Parse and render color markup with text wrapping
                    int linesRendered = WriteLineColoredWrapped(line, contentX + 1, y, availableWidth);
                    y += linesRendered;
                }
                else
                {
                    // No more room in viewport
                    break;
                }
            }
        }
        
        /// <summary>
        /// Scrolls the display buffer up (shows older content)
        /// </summary>
        /// <param name="lines">Number of lines to scroll up</param>
        public void ScrollUp(int lines = 3)
        {
            isManualScrolling = true;
            lastBufferCountWhenScrolling = displayBuffer.Count;
            
            // Calculate current total height to determine max scroll
            int contentHeight = CanvasLayoutManager.CONTENT_HEIGHT;
            int availableWidth = CanvasLayoutManager.CONTENT_WIDTH - 2;
            
            var linesToRender = displayBuffer.TakeLast(maxLines).ToList();
            int totalHeight = 0;
            foreach (var line in linesToRender)
            {
                totalHeight += CalculateWrappedLineCount(line, availableWidth);
            }
            
            int maxScrollOffset = Math.Max(0, totalHeight - contentHeight);
            
            // Scroll up by decreasing the offset (showing content higher up)
            int oldOffset = manualScrollOffset;
            manualScrollOffset = Math.Max(0, manualScrollOffset - lines);
            
            // Only trigger re-render if scroll offset actually changed
            if (manualScrollOffset != oldOffset)
            {
                // Force immediate render for scroll operations (bypass debouncing)
                Dispatcher.UIThread.Post(() =>
                {
                    PerformRender();
                }, DispatcherPriority.Normal);
            }
        }
        
        /// <summary>
        /// Scrolls the display buffer down (shows newer content)
        /// </summary>
        /// <param name="lines">Number of lines to scroll down</param>
        public void ScrollDown(int lines = 3)
        {
            isManualScrolling = true;
            lastBufferCountWhenScrolling = displayBuffer.Count;
            
            // Calculate current total height to determine max scroll
            int contentHeight = CanvasLayoutManager.CONTENT_HEIGHT;
            int availableWidth = CanvasLayoutManager.CONTENT_WIDTH - 2;
            
            var linesToRender = displayBuffer.TakeLast(maxLines).ToList();
            int totalHeight = 0;
            foreach (var line in linesToRender)
            {
                totalHeight += CalculateWrappedLineCount(line, availableWidth);
            }
            
            int maxScrollOffset = Math.Max(0, totalHeight - contentHeight);
            
            // Scroll down by increasing the offset (showing content lower down)
            int oldOffset = manualScrollOffset;
            manualScrollOffset = Math.Min(maxScrollOffset, manualScrollOffset + lines);
            
            // If we've scrolled to the bottom, switch back to auto-scroll mode
            if (manualScrollOffset >= maxScrollOffset)
            {
                isManualScrolling = false;
                manualScrollOffset = 0;
            }
            
            // Only trigger re-render if scroll offset actually changed
            if (manualScrollOffset != oldOffset)
            {
                // Force immediate render for scroll operations (bypass debouncing)
                Dispatcher.UIThread.Post(() =>
                {
                    PerformRender();
                }, DispatcherPriority.Normal);
            }
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode (scrolls to bottom)
        /// </summary>
        public void ResetScroll()
        {
            isManualScrolling = false;
            manualScrollOffset = 0;
            lastBufferCountWhenScrolling = displayBuffer.Count;
            RenderDisplayBufferFallback();
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
            // Cancel any pending debounced renders
            lock (renderLock)
            {
                debounceTimer?.Dispose();
                debounceTimer = null;
            }
            
            canvas.Clear();
            displayBuffer.Clear();
            
            // Reset scroll state
            isManualScrolling = false;
            manualScrollOffset = 0;
            lastBufferCountWhenScrolling = 0;
            
            // Reset render cache
            layoutInitialized = false;
            lastBufferCount = 0;
            lastManualScrollOffset = -1;
            lastRenderedCharacter = null;
            lastRenderedEnemy = null;
            lastRenderedDungeonName = null;
            lastRenderedRoomName = null;
            
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
