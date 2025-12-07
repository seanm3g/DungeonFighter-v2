namespace RPGGame.UI.Avalonia.Display.Buffer
{
    using System;

    /// <summary>
    /// Manages scroll state for the display buffer.
    /// Handles scroll position tracking and state transitions.
    /// </summary>
    public class ScrollStateManager
    {
        // Scroll state
        private int manualScrollOffset = 0;
        private bool isManualScrolling = false;
        private int lastBufferCountWhenScrolling = 0;
        private bool isStuckAtTop = false; // Track if user scrolled to top and wants to stay there
        
        /// <summary>
        /// Gets the manual scroll offset
        /// </summary>
        public int ManualScrollOffset => manualScrollOffset;
        
        /// <summary>
        /// Gets whether manual scrolling is active
        /// </summary>
        public bool IsManualScrolling => isManualScrolling;
        
        /// <summary>
        /// Checks if user was at bottom before adding message
        /// </summary>
        public bool WasAtBottom()
        {
            return !isManualScrolling || (lastBufferCountWhenScrolling == 0);
        }
        
        /// <summary>
        /// Checks if user was at top before adding message
        /// </summary>
        public bool WasAtTop()
        {
            return isManualScrolling && manualScrollOffset == 0 && !WasAtBottom();
        }
        
        /// <summary>
        /// Updates scroll state after adding a message
        /// </summary>
        public void UpdateAfterAdd(bool wasAtTop, bool wasAtBottom, int newMessageCount)
        {
            lastBufferCountWhenScrolling = newMessageCount;
            
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
            }
            else if (wasAtBottom)
            {
                // User is at bottom - keep auto-scroll
                isManualScrolling = false;
                manualScrollOffset = 0;
                isStuckAtTop = false;
            }
            // else: User is in middle - keep current offset (don't change scroll state)
        }
        
        /// <summary>
        /// Updates scroll state after adding a message (uses current buffer count)
        /// </summary>
        public void UpdateAfterAdd(bool wasAtTop, bool wasAtBottom)
        {
            // This overload is used when buffer count is managed externally
            // The caller should update lastBufferCountWhenScrolling separately if needed
            if (wasAtTop || isStuckAtTop)
            {
                isManualScrolling = true;
                manualScrollOffset = 0;
                isStuckAtTop = true;
            }
            else if (wasAtBottom)
            {
                isManualScrolling = false;
                manualScrollOffset = 0;
                isStuckAtTop = false;
            }
        }
        
        /// <summary>
        /// Updates the buffer count when scrolling
        /// </summary>
        public void UpdateBufferCount(int count)
        {
            lastBufferCountWhenScrolling = count;
        }
        
        /// <summary>
        /// Scrolls up (shows older content)
        /// </summary>
        public void ScrollUp(int lines, int maxScrollOffset, int bufferCount)
        {
            lastBufferCountWhenScrolling = bufferCount;
            
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
        public void ScrollDown(int lines, int maxScrollOffset, int bufferCount)
        {
            lastBufferCountWhenScrolling = bufferCount;
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
        public void ResetScroll(int bufferCount)
        {
            isManualScrolling = false;
            manualScrollOffset = 0;
            isStuckAtTop = false;
            lastBufferCountWhenScrolling = bufferCount;
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
        
        /// <summary>
        /// Resets all scroll state
        /// </summary>
        public void Reset()
        {
            isManualScrolling = false;
            manualScrollOffset = 0;
            isStuckAtTop = false;
            lastBufferCountWhenScrolling = 0;
        }
    }
}

