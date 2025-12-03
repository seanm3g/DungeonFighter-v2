using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Manages the display buffer for center panel content
    /// Handles message storage, truncation, and scroll state
    /// Now stores structured ColoredText segments to eliminate round-trip conversions
    /// </summary>
    public class DisplayBuffer
    {
        private readonly List<List<ColoredText>> messages;
        private readonly int maxLines;
        private readonly int maxLineWidth;
        
        // Scroll state
        private int manualScrollOffset = 0;
        private bool isManualScrolling = false;
        private int lastBufferCountWhenScrolling = 0;
        private bool isStuckAtTop = false; // Track if user scrolled to top and wants to stay there
        
        public DisplayBuffer(int maxLines = 100, int maxLineWidth = 152)
        {
            this.messages = new List<List<ColoredText>>();
            this.maxLines = maxLines;
            this.maxLineWidth = maxLineWidth;
        }
        
        /// <summary>
        /// Gets all messages in the buffer as structured ColoredText segments
        /// </summary>
        public IReadOnlyList<List<ColoredText>> Messages => messages;
        
        /// <summary>
        /// Gets all messages as strings (for backwards compatibility during migration)
        /// </summary>
        public IReadOnlyList<string> MessagesAsStrings => messages.Select(segments => 
            segments == null || segments.Count == 0 
                ? "" 
                : ColoredTextRenderer.RenderAsPlainText(segments)
        ).ToList();
        
        /// <summary>
        /// Gets the current number of messages
        /// </summary>
        public int Count => messages.Count;
        
        /// <summary>
        /// Gets the maximum number of lines
        /// </summary>
        public int MaxLines => maxLines;
        
        /// <summary>
        /// Gets the manual scroll offset
        /// </summary>
        public int ManualScrollOffset => manualScrollOffset;
        
        /// <summary>
        /// Gets whether manual scrolling is active
        /// </summary>
        public bool IsManualScrolling => isManualScrolling;
        
        /// <summary>
        /// Adds a ColoredText segment list to the buffer (preferred method)
        /// Stores structured data directly to eliminate round-trip conversions
        /// </summary>
        public void Add(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                AddEmpty();
                return;
            }
            
            // Truncate if too long
            var displayLength = ColoredTextRenderer.GetDisplayLength(segments);
            if (displayLength > maxLineWidth)
            {
                segments = ColoredTextRenderer.Truncate(segments, maxLineWidth - 3);
                // Add "..." as a final segment
                segments.Add(new ColoredText("...", Colors.White));
            }
            
            // Prevent consecutive duplicate messages
            // BUT: Allow blank lines to be added even if previous was blank (spacing needs multiple blanks)
            if (messages.Count > 0 && AreSegmentsEqual(messages[messages.Count - 1], segments) && segments.Count > 0)
            {
                return; // Skip duplicate (but not blank lines)
            }
            
            bool wasAtBottom = !isManualScrolling || (messages.Count == lastBufferCountWhenScrolling);
            bool wasAtTop = isManualScrolling && manualScrollOffset == 0 && !wasAtBottom;
            
            messages.Add(new List<ColoredText>(segments));
            
            // Keep only the last maxLines
            if (messages.Count > maxLines)
            {
                messages.RemoveAt(0);
            }
            
            // Preserve scroll position based on where user was:
            // - If at top, stay at top (offset = 0)
            // - If at bottom, stay at bottom (auto-scroll)
            // - If in middle, keep current offset
            if (wasAtTop || isStuckAtTop)
            {
                // User is at top - keep them at top
                isManualScrolling = true;
                manualScrollOffset = 0;
                isStuckAtTop = true;
                lastBufferCountWhenScrolling = messages.Count;
            }
            else if (wasAtBottom)
            {
                // User is at bottom - keep auto-scroll
                isManualScrolling = false;
                manualScrollOffset = 0;
                isStuckAtTop = false;
                lastBufferCountWhenScrolling = messages.Count;
            }
            else
            {
                // User is in middle - keep current offset (don't change scroll state)
                lastBufferCountWhenScrolling = messages.Count;
            }
        }
        
        /// <summary>
        /// Adds a message to the buffer (string version - parses to ColoredText for storage)
        /// </summary>
        public void Add(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                AddEmpty();
                return;
            }
            
            // Parse string to ColoredText segments for structured storage
            var segments = ColoredTextParser.Parse(message);
            Add(segments);
        }
        
        /// <summary>
        /// Adds an empty line to the buffer
        /// </summary>
        private void AddEmpty()
        {
            messages.Add(new List<ColoredText>());
            
            bool wasAtBottom = !isManualScrolling || (messages.Count == lastBufferCountWhenScrolling);
            bool wasAtTop = isManualScrolling && manualScrollOffset == 0 && !wasAtBottom;
            
            // Keep only the last maxLines
            if (messages.Count > maxLines)
            {
                messages.RemoveAt(0);
            }
            
            // Preserve scroll position based on where user was:
            // - If at top, stay at top (offset = 0)
            // - If at bottom, stay at bottom (auto-scroll)
            // - If in middle, keep current offset
            if (wasAtTop || isStuckAtTop)
            {
                // User is at top - keep them at top
                isManualScrolling = true;
                manualScrollOffset = 0;
                isStuckAtTop = true;
                lastBufferCountWhenScrolling = messages.Count;
            }
            else if (wasAtBottom)
            {
                // User is at bottom - keep auto-scroll
                isManualScrolling = false;
                manualScrollOffset = 0;
                isStuckAtTop = false;
                lastBufferCountWhenScrolling = messages.Count;
            }
            else
            {
                // User is in middle - keep current offset (don't change scroll state)
                lastBufferCountWhenScrolling = messages.Count;
            }
        }
        
        /// <summary>
        /// Checks if two segment lists are equal (for duplicate detection)
        /// </summary>
        private bool AreSegmentsEqual(List<ColoredText> segments1, List<ColoredText> segments2)
        {
            if (segments1 == null && segments2 == null) return true;
            if (segments1 == null || segments2 == null) return false;
            if (segments1.Count != segments2.Count) return false;
            
            for (int i = 0; i < segments1.Count; i++)
            {
                if (segments1[i].Text != segments2[i].Text || 
                    segments1[i].Color != segments2[i].Color)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Adds multiple ColoredText segment lists to the buffer (preferred method)
        /// Stores structured data directly
        /// </summary>
        public void AddRange(IEnumerable<List<ColoredText>> segmentsList)
        {
            if (segmentsList == null)
                return;
            
            var segmentsListToAdd = segmentsList.ToList();
            if (segmentsListToAdd.Count == 0) return;
            
            // Check if we were at the bottom or top before adding
            bool wasAtBottom = !isManualScrolling || (messages.Count == lastBufferCountWhenScrolling);
            bool wasAtTop = isManualScrolling && manualScrollOffset == 0 && !wasAtBottom;
            
            // Process all messages in batch
            foreach (var segments in segmentsListToAdd)
            {
                if (segments == null || segments.Count == 0)
                {
                    // Always allow blank lines - they're used for spacing between sections
                    messages.Add(new List<ColoredText>());
                    continue;
                }
                
                // Truncate if too long
                var displayLength = ColoredTextRenderer.GetDisplayLength(segments);
                var processedSegments = segments;
                if (displayLength > maxLineWidth)
                {
                    processedSegments = ColoredTextRenderer.Truncate(segments, maxLineWidth - 3);
                    // Add "..." as a final segment
                    processedSegments.Add(new ColoredText("...", Colors.White));
                }
                
                // Prevent consecutive duplicate messages (only check against last message in buffer)
                // BUT: Allow blank lines to be added even if previous was blank (spacing needs multiple blanks)
                if (messages.Count > 0 && AreSegmentsEqual(messages[messages.Count - 1], processedSegments) && processedSegments.Count > 0)
                {
                    continue; // Skip duplicate (but not blank lines)
                }
                
                messages.Add(new List<ColoredText>(processedSegments));
            }
            
            // Keep only the last maxLines (batch removal)
            if (messages.Count > maxLines)
            {
                int removeCount = messages.Count - maxLines;
                messages.RemoveRange(0, removeCount);
            }
            
            // Preserve scroll position based on where user was:
            // - If at top, stay at top (offset = 0)
            // - If at bottom, stay at bottom (auto-scroll)
            // - If in middle, keep current offset
            if (wasAtTop || isStuckAtTop)
            {
                // User is at top - keep them at top
                isManualScrolling = true;
                manualScrollOffset = 0;
                isStuckAtTop = true;
                lastBufferCountWhenScrolling = messages.Count;
            }
            else if (wasAtBottom)
            {
                // User is at bottom - keep auto-scroll
                isManualScrolling = false;
                manualScrollOffset = 0;
                isStuckAtTop = false;
                lastBufferCountWhenScrolling = messages.Count;
            }
            else
            {
                // User is in middle - keep current offset (don't change scroll state)
                lastBufferCountWhenScrolling = messages.Count;
            }
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
            messages.Clear();
            isManualScrolling = false;
            manualScrollOffset = 0;
            isStuckAtTop = false;
            lastBufferCountWhenScrolling = 0;
        }
        
        /// <summary>
        /// Gets the last N messages as structured ColoredText segments
        /// </summary>
        public List<List<ColoredText>> GetLast(int count)
        {
            return messages.TakeLast(count).Select(segments => new List<ColoredText>(segments)).ToList();
        }
        
        /// <summary>
        /// Gets the last N messages as strings (for backwards compatibility during migration)
        /// </summary>
        public List<string> GetLastAsStrings(int count)
        {
            return messages.TakeLast(count).Select(segments => 
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
            lastBufferCountWhenScrolling = messages.Count;
            
            // If we're transitioning from auto-scroll mode (not manually scrolling yet), 
            // start from the bottom (maxOffset) and immediately scroll up by lines
            // This only happens when first starting to scroll up from auto-scroll
            if (!isManualScrolling && maxScrollOffset > 0)
            {
                // Transitioning from auto-scroll - start from bottom and scroll up
                isManualScrolling = true;
                manualScrollOffset = Math.Max(0, maxScrollOffset - lines);
                isStuckAtTop = false;
            }
            else
            {
                // Already in manual scroll mode - just scroll up
                isManualScrolling = true;
                
                // Calculate new offset
                int newOffset = manualScrollOffset - lines;
                
                // Stop at top (0) - don't wrap around
                if (newOffset < 0)
                {
                    manualScrollOffset = 0;
                    isStuckAtTop = true; // User scrolled to top, mark as stuck
                }
                else
                {
                    // Clamp to valid range: 0 to maxScrollOffset
                    manualScrollOffset = Math.Max(0, Math.Min(maxScrollOffset, newOffset));
                    isStuckAtTop = false; // Not at top anymore
                }
            }
        }
        
        /// <summary>
        /// Scrolls down (shows newer content)
        /// </summary>
        public void ScrollDown(int lines = 3, int maxScrollOffset = 0)
        {
            lastBufferCountWhenScrolling = messages.Count;
            isStuckAtTop = false; // Clear top-stuck flag when user manually scrolls
            
            // If we're at top and not in manual scroll mode yet, start manual scrolling
            if (!isManualScrolling)
            {
                // Already at top in auto-scroll mode - can't scroll down from here
                // Stay in auto-scroll mode
                return;
            }
            
            // Calculate new offset
            int newOffset = manualScrollOffset + lines;
            
            // Stop at bottom (maxScrollOffset) - don't wrap around
            if (newOffset >= maxScrollOffset)
            {
                // Reached bottom - switch back to auto-scroll mode
                isManualScrolling = false;
                manualScrollOffset = 0;
                isStuckAtTop = false; // At bottom, not top
            }
            else
            {
                // Clamp to valid range: 0 to maxScrollOffset
                manualScrollOffset = Math.Max(0, Math.Min(maxScrollOffset, newOffset));
            }
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode (scrolls to bottom)
        /// </summary>
        public void ResetScroll()
        {
            isManualScrolling = false;
            manualScrollOffset = 0;
            isStuckAtTop = false;
            lastBufferCountWhenScrolling = messages.Count;
        }
        
        /// <summary>
        /// Sets the manual scroll offset (used when calculating scroll position)
        /// Only updates if the offset is actually different to prevent unnecessary state changes
        /// </summary>
        public void SetScrollOffset(int offset, int maxOffset)
        {
            // Only update if offset actually changed to prevent resetting scroll state
            if (offset == manualScrollOffset && isManualScrolling)
            {
                return; // No change needed
            }
            
            if (offset >= maxOffset)
            {
                // At or past bottom - switch to auto-scroll only if we weren't already there
                if (isManualScrolling)
                {
                    isManualScrolling = false;
                    manualScrollOffset = 0;
                }
            }
            else
            {
                // Clamp to valid range and maintain manual scrolling state
                int clampedOffset = Math.Max(0, Math.Min(maxOffset, offset));
                if (clampedOffset != manualScrollOffset)
                {
                    isManualScrolling = true;
                    manualScrollOffset = clampedOffset;
                }
            }
        }
    }
}

