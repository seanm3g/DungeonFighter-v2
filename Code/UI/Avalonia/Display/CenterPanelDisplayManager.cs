using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display.Helpers;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Unified display manager for center panel content
    /// Single rendering path for all game modes (combat, menus, exploration, etc.)
    /// </summary>
    public class CenterPanelDisplayManager
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly ICanvasContextManager contextManager;
        private readonly PersistentLayoutManager layoutManager;
        
        // Core components
        private DisplayBuffer buffer;
        private DisplayRenderer renderer;
        private DisplayTiming timing;
        private DisplayMode currentMode;
        private RenderStateManager renderStateManager;
        
        // Callback to trigger combat screen re-render when new messages are added
        // Used when external renderer (like combat screen) handles rendering
        private System.Action? externalRenderCallback = null;
        
        public CenterPanelDisplayManager(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            ICanvasContextManager contextManager,
            int maxLines = 100)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.contextManager = contextManager;
            this.layoutManager = new PersistentLayoutManager(canvas);
            
            // Initialize with standard mode
            this.currentMode = new StandardDisplayMode();
            this.buffer = new DisplayBuffer(maxLines);
            this.renderer = new DisplayRenderer(textWriter);
            this.timing = new DisplayTiming(currentMode);
            this.renderStateManager = new RenderStateManager();
        }
        
        /// <summary>
        /// Gets the display buffer
        /// </summary>
        public DisplayBuffer Buffer => buffer;
        
        /// <summary>
        /// Sets the display mode (Standard, Combat, Menu, etc.)
        /// </summary>
        public void SetMode(DisplayMode mode)
        {
            if (mode == null) return;
            
            // If switching modes, cancel any pending renders
            if (mode.GetType() != currentMode.GetType())
            {
                timing.CancelPending();
            }
            
            currentMode = mode;
            timing = new DisplayTiming(mode);
        }
        
        /// <summary>
        /// Sets an external render callback for cases where external renderer handles rendering
        /// When set, auto-rendering triggers the callback instead of PerformRender()
        /// Used for combat screen and other specialized rendering scenarios
        /// </summary>
        public void SetExternalRenderCallback(System.Action? renderCallback)
        {
            externalRenderCallback = renderCallback;
        }
        
        /// <summary>
        /// Starts a batch transaction for adding multiple messages
        /// Messages are added to the buffer but render is only triggered when transaction completes
        /// </summary>
        /// <param name="autoRender">If true, automatically triggers render when transaction completes. If false, caller must call Render() explicitly.</param>
        public DisplayBatchTransaction StartBatch(bool autoRender = true)
        {
            return new DisplayBatchTransaction(this, autoRender);
        }
        
        /// <summary>
        /// Triggers a render (used by batch transactions and explicit render calls)
        /// </summary>
        internal void TriggerRender()
        {
            if (externalRenderCallback != null)
            {
                // External renderer handles rendering (e.g., combat screen)
                timing.ScheduleRender(externalRenderCallback);
            }
            else
            {
                // Standard auto-render
                timing.ScheduleRender(new System.Action(PerformRender));
            }
        }
        
        /// <summary>
        /// Adds a message to the display buffer and schedules a render
        /// </summary>
        public void AddMessage(string message, UIMessageType messageType = UIMessageType.System)
        {
            buffer.Add(message);
            TriggerRender();
        }
        
        /// <summary>
        /// Adds structured ColoredText segments to the display buffer and schedules a render
        /// This eliminates round-trip conversions by storing structured data directly
        /// </summary>
        public void AddMessage(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            buffer.Add(segments);
            TriggerRender();
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer and schedules a render
        /// </summary>
        public void AddMessages(IEnumerable<string> messages)
        {
            buffer.AddRange(messages);
            TriggerRender();
        }
        
        /// <summary>
        /// Adds multiple structured ColoredText segment lists to the display buffer and schedules a render
        /// This eliminates round-trip conversions by storing structured data directly
        /// </summary>
        public void AddMessages(IEnumerable<List<ColoredText>> segmentsList)
        {
            buffer.AddRange(segmentsList);
            TriggerRender();
        }
        
        public void AddMessageBatch(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            buffer.AddRange(messages);
            BatchOperationHelper.ScheduleRenderWithDelay(TriggerRender, delayAfterBatchMs);
        }
        
        public void AddMessageBatch(IEnumerable<List<ColoredText>> segmentsList, int delayAfterBatchMs = 0)
        {
            buffer.AddRange(segmentsList);
            BatchOperationHelper.ScheduleRenderWithDelay(TriggerRender, delayAfterBatchMs);
        }
        
        public async Task AddMessageBatchAsync(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            buffer.AddRange(messages);
            await BatchOperationHelper.ScheduleRenderWithDelayAsync(TriggerRender, delayAfterBatchMs);
        }
        
        public async Task AddMessageBatchAsync(IEnumerable<List<ColoredText>> segmentsList, int delayAfterBatchMs = 0)
        {
            buffer.AddRange(segmentsList);
            await BatchOperationHelper.ScheduleRenderWithDelayAsync(TriggerRender, delayAfterBatchMs);
        }
        
        /// <summary>
        /// Writes chunked text (all chunks added immediately, then rendered)
        /// </summary>
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            config ??= UI.ChunkedTextReveal.DefaultConfig;
            
            if (!config.Enabled)
            {
                buffer.Add(message);
                TriggerRender();
                return;
            }
            
            // Split text into chunks
            var chunks = SplitIntoChunks(message, config.Strategy);
            
            if (chunks.Count == 0)
            {
                return;
            }
            
            // Add all chunks to buffer immediately
            foreach (var chunk in chunks)
            {
                buffer.Add(chunk);
                
                // Add blank line if configured
                if (config.AddBlankLineBetweenChunks && chunk != chunks[chunks.Count - 1])
                {
                    buffer.Add("");
                }
            }
            
            // Schedule single render for all chunks
            TriggerRender();
        }
        
        /// <summary>
        /// Clears the display buffer
        /// </summary>
        public void Clear()
        {
            buffer.Clear();
            renderStateManager.Reset();
            timing.ForceRender(new System.Action(PerformRender));
        }
        
        /// <summary>
        /// Scrolls the display up
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            buffer.ScrollUp(lines);
            timing.ForceRender(new System.Action(PerformRender));
        }
        
        /// <summary>
        /// Scrolls the display down
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            // Calculate max scroll offset based on current content
            int maxOffset = CalculateMaxScrollOffset();
            buffer.ScrollDown(lines, maxOffset);
            timing.ForceRender(new System.Action(PerformRender));
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode
        /// </summary>
        public void ResetScroll()
        {
            buffer.ResetScroll();
            timing.ScheduleRender(new System.Action(PerformRender));
        }
        
        /// <summary>
        /// Forces an immediate render (bypasses timing)
        /// </summary>
        public void ForceRender()
        {
            timing.ForceRender(new System.Action(PerformRender));
        }
        
        /// <summary>
        /// Performs the actual rendering operation
        /// Uses RenderStateManager to determine what needs rendering
        /// </summary>
        private void PerformRender()
        {
            var state = renderStateManager.GetRenderState(buffer, contextManager);
            
            if (!state.NeedsRender)
            {
                return; // Nothing to render
            }
            
            Dispatcher.UIThread.Post(() =>
            {
                // Get center content area dimensions from layout manager
                var (contentX, contentY, contentWidth, contentHeight) = layoutManager.GetCenterContentArea();
                
                // Determine if we should clear canvas
                bool shouldClearCanvas = renderStateManager.ShouldClearCanvas(state, currentMode);
                
                if (shouldClearCanvas || state.NeedsFullLayout)
                {
                    // Full layout render
                    string title = DetermineTitle(state.CurrentCharacter, state.CurrentEnemy);
                    layoutManager.RenderLayout(
                        state.CurrentCharacter,
                        (x, y, w, h) => renderer.Render(buffer, x, y, w, h),
                        title,
                        state.CurrentEnemy,
                        state.DungeonName,
                        state.RoomName,
                        clearCanvas: shouldClearCanvas
                    );
                }
                else
                {
                    // Just update center content
                    renderer.Render(buffer, contentX, contentY, contentWidth, contentHeight);
                    canvas.Refresh();
                }
                
                // Record that render was performed
                renderStateManager.RecordRender(buffer, contextManager);
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Calculates the maximum scroll offset based on current buffer content
        /// </summary>
        private int CalculateMaxScrollOffset()
        {
            // This is a simplified calculation - in practice, we'd need to calculate
            // the total height of all wrapped lines. For now, return a reasonable estimate.
            // The actual calculation happens in DisplayRenderer.Render()
            return Math.Max(0, buffer.Count * 2 - 50); // Rough estimate
        }
        
        /// <summary>
        /// Determines the title based on current game state
        /// </summary>
        private string DetermineTitle(Character? character, Enemy? enemy)
        {
            return TitleResolver.DetermineTitle(character, enemy);
        }
        
        private List<string> SplitIntoChunks(string text, UI.ChunkedTextReveal.ChunkStrategy strategy) 
            => ChunkSplitterHelper.SplitIntoChunks(text, strategy);
    }
}

