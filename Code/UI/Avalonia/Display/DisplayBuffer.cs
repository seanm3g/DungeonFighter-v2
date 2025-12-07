using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Display.Buffer;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Manages the display buffer for center panel content.
    /// Facade coordinator that delegates to specialized buffer components.
    /// Handles message storage, truncation, and scroll state.
    /// Now stores structured ColoredText segments to eliminate round-trip conversions.
    /// </summary>
    public class DisplayBuffer
    {
        private readonly BufferStorage storage;
        private readonly ScrollStateManager scrollManager;
        
        public DisplayBuffer(int maxLines = 100, int maxLineWidth = 152)
        {
            this.storage = new BufferStorage(maxLines, maxLineWidth);
            this.scrollManager = new ScrollStateManager();
        }
        
        /// <summary>
        /// Gets all messages in the buffer as structured ColoredText segments
        /// </summary>
        public IReadOnlyList<List<ColoredText>> Messages => storage.Messages;
        
        /// <summary>
        /// Gets all messages as strings (for backwards compatibility during migration)
        /// </summary>
        public IReadOnlyList<string> MessagesAsStrings => storage.Messages.Select(segments => 
            segments == null || segments.Count == 0 
                ? "" 
                : ColoredTextRenderer.RenderAsPlainText(segments)
        ).ToList();
        
        /// <summary>
        /// Gets the current number of messages
        /// </summary>
        public int Count => storage.Count;
        
        /// <summary>
        /// Gets the maximum number of lines
        /// </summary>
        public int MaxLines => storage.MaxLines;
        
        /// <summary>
        /// Gets the manual scroll offset
        /// </summary>
        public int ManualScrollOffset => scrollManager.ManualScrollOffset;
        
        /// <summary>
        /// Gets whether manual scrolling is active
        /// </summary>
        public bool IsManualScrolling => scrollManager.IsManualScrolling;
        
        /// <summary>
        /// Adds a ColoredText segment list to the buffer (preferred method)
        /// Stores structured data directly to eliminate round-trip conversions
        /// </summary>
        public void Add(List<ColoredText> segments)
        {
            scrollManager.UpdateBufferCount(storage.Count);
            storage.Add(segments, scrollManager);
        }
        
        /// <summary>
        /// Adds a message to the buffer (string version - parses to ColoredText for storage)
        /// </summary>
        public void Add(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                scrollManager.UpdateBufferCount(storage.Count);
                storage.Add(new List<ColoredText>(), scrollManager);
                return;
            }
            
            // Parse string to ColoredText segments for structured storage
            var segments = ColoredTextParser.Parse(message);
            Add(segments);
        }
        
        /// <summary>
        /// Adds multiple ColoredText segment lists to the buffer (preferred method)
        /// Stores structured data directly
        /// </summary>
        public void AddRange(IEnumerable<List<ColoredText>> segmentsList)
        {
            scrollManager.UpdateBufferCount(storage.Count);
            storage.AddRange(segmentsList, scrollManager);
        }
        
        /// <summary>
        /// Adds multiple messages to the buffer (string version - parses to ColoredText for storage)
        /// Optimized batch processing - processes all messages at once instead of individually
        /// </summary>
        public void AddRange(IEnumerable<string> messagesToAdd)
        {
            if (messagesToAdd == null)
                return;
            
            // Parse all strings to ColoredText segments
            var segmentsList = messagesToAdd.Select(msg => 
                string.IsNullOrEmpty(msg) 
                    ? new List<ColoredText>() 
                    : ColoredTextParser.Parse(msg)
            ).ToList();
            
            AddRange(segmentsList);
        }
        
        /// <summary>
        /// Clears all messages from the buffer
        /// </summary>
        public void Clear()
        {
            storage.Clear(scrollManager);
        }
        
        /// <summary>
        /// Gets the last N messages as structured ColoredText segments
        /// </summary>
        public List<List<ColoredText>> GetLast(int count)
        {
            return storage.GetLast(count);
        }
        
        /// <summary>
        /// Gets the last N messages as strings (for backwards compatibility during migration)
        /// </summary>
        public List<string> GetLastAsStrings(int count)
        {
            return storage.Messages.TakeLast(count).Select(segments => 
                segments == null || segments.Count == 0 
                    ? "" 
                    : ColoredTextRenderer.RenderAsPlainText(segments)
            ).ToList();
        }
        
        /// <summary>
        /// Scrolls up (shows older content)
        /// </summary>
        public void ScrollUp(int lines = 3, int maxScrollOffset = 0)
        {
            scrollManager.ScrollUp(lines, maxScrollOffset, storage.Count);
        }
        
        /// <summary>
        /// Scrolls down (shows newer content)
        /// </summary>
        public void ScrollDown(int lines = 3, int maxScrollOffset = 0)
        {
            scrollManager.ScrollDown(lines, maxScrollOffset, storage.Count);
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode (scrolls to bottom)
        /// </summary>
        public void ResetScroll()
        {
            scrollManager.ResetScroll(storage.Count);
        }
        
        /// <summary>
        /// Sets the manual scroll offset (used when calculating scroll position)
        /// Only updates if the offset is actually different to prevent unnecessary state changes
        /// </summary>
        public void SetScrollOffset(int offset, int maxOffset)
        {
            scrollManager.SetScrollOffset(offset, maxOffset);
        }
    }
}

