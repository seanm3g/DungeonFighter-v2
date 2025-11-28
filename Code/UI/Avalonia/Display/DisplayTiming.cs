using System;
using System.Threading;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Manages timing and debouncing for display updates
    /// Uses a single unified timing system for all display modes
    /// </summary>
    public class DisplayTiming
    {
        private readonly DisplayMode mode;
        private System.Threading.Timer? debounceTimer = null;
        private System.Action? pendingRender = null;
        private DateTime lastRenderTime = DateTime.MinValue;
        private readonly object timingLock = new object();
        
        public DisplayTiming(DisplayMode mode)
        {
            this.mode = mode;
        }
        
        /// <summary>
        /// Schedules a render action with debouncing
        /// Multiple rapid calls will be batched into a single render
        /// </summary>
        public void ScheduleRender(System.Action renderAction)
        {
            if (renderAction == null) return;
            
            lock (timingLock)
            {
                // Store the latest render action (allows batching)
                pendingRender = renderAction;
                
                // Cancel any pending render timer
                debounceTimer?.Dispose();
                
                var now = DateTime.Now;
                var timeSinceLastRender = (now - lastRenderTime).TotalMilliseconds;
                
                // Calculate delay based on mode
                int delayMs = 0;
                
                if (mode.DebounceMs > 0)
                {
                    // Use debounce delay
                    if (timeSinceLastRender < mode.DebounceMs)
                    {
                        delayMs = mode.DebounceMs - (int)timeSinceLastRender;
                    }
                }
                
                // Apply minimum render delay if needed
                if (mode.MinRenderDelayMs > 0 && timeSinceLastRender < mode.MinRenderDelayMs)
                {
                    int minDelay = mode.MinRenderDelayMs - (int)timeSinceLastRender;
                    delayMs = Math.Max(delayMs, minDelay);
                }
                
                if (delayMs <= 0)
                {
                    // Render immediately
                    lastRenderTime = now;
                    var actionToExecute = pendingRender;
                    pendingRender = null;
                    actionToExecute?.Invoke();
                }
                else
                {
                    // Schedule render after delay
                    debounceTimer = new System.Threading.Timer(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            lock (timingLock)
                            {
                                lastRenderTime = DateTime.Now;
                                var actionToExecute = pendingRender;
                                pendingRender = null;
                                actionToExecute?.Invoke();
                            }
                        }, DispatcherPriority.Background);
                    }, null, delayMs, Timeout.Infinite);
                }
            }
        }
        
        /// <summary>
        /// Forces an immediate render (bypasses debouncing)
        /// </summary>
        public void ForceRender(System.Action renderAction)
        {
            if (renderAction == null) return;
            
            lock (timingLock)
            {
                // Cancel any pending render
                debounceTimer?.Dispose();
                debounceTimer = null;
                pendingRender = null;
                
                lastRenderTime = DateTime.Now;
                renderAction.Invoke();
            }
        }
        
        /// <summary>
        /// Cancels any pending renders
        /// </summary>
        public void CancelPending()
        {
            lock (timingLock)
            {
                debounceTimer?.Dispose();
                debounceTimer = null;
                pendingRender = null;
            }
        }
        
        /// <summary>
        /// Updates the display mode (and resets timing state)
        /// </summary>
        public void SetMode(DisplayMode newMode)
        {
            lock (timingLock)
            {
                CancelPending();
                // Note: mode is readonly, so we'd need to recreate DisplayTiming
                // For now, this is a placeholder for future mode switching
            }
        }
    }
}

