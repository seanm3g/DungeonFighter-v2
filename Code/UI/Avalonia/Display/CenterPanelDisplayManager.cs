using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display.Helpers;
using RPGGame.UI.Avalonia.Display.Mode;
using RPGGame.UI.Avalonia.Display.Render;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

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
        private RenderStateManager renderStateManager;
        
        private readonly DisplayModeManager modeManager;
        private readonly RenderCoordinator renderCoordinator;
        
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
            this.buffer = new DisplayBuffer(maxLines);
            this.renderer = new DisplayRenderer(textWriter);
            this.renderStateManager = new RenderStateManager();
            
            this.modeManager = new DisplayModeManager(new StandardDisplayMode());
            this.renderCoordinator = new RenderCoordinator(
                canvas,
                renderer,
                layoutManager,
                renderStateManager,
                contextManager,
                buffer,
                modeManager.CurrentMode);
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
            modeManager.SetMode(mode, () => modeManager.Timing.CancelPending());
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
            renderCoordinator.SetExternalRenderCallback(externalRenderCallback);
            renderCoordinator.TriggerRender(modeManager.Timing);
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
            modeManager.Timing.ForceRender(new System.Action(renderCoordinator.PerformRender));
        }
        
        /// <summary>
        /// Clears the display buffer without triggering a render
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void ClearWithoutRender()
        {
            buffer.Clear();
            renderStateManager.Reset();
            // Don't trigger a render - let the menu screen handle its own rendering
        }
        
        /// <summary>
        /// Scrolls the display up
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            // Calculate max scroll offset to ensure we don't scroll beyond valid range
            int maxOffset = renderCoordinator.CalculateMaxScrollOffset();
            ScrollDebugLogger.Log($"CenterPanelDisplayManager.ScrollUp: lines={lines}, maxOffset={maxOffset}, currentOffset={buffer.ManualScrollOffset}, buffer.Count={buffer.Count}");
            buffer.ScrollUp(lines, maxOffset);
            ScrollDebugLogger.Log($"CenterPanelDisplayManager.ScrollUp: newOffset={buffer.ManualScrollOffset}, isManualScrolling={buffer.IsManualScrolling}");
            modeManager.Timing.ForceRender(new System.Action(renderCoordinator.PerformRender));
        }
        
        /// <summary>
        /// Scrolls the display down
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            // Calculate max scroll offset based on current content
            int maxOffset = renderCoordinator.CalculateMaxScrollOffset();
            ScrollDebugLogger.Log($"CenterPanelDisplayManager.ScrollDown: lines={lines}, maxOffset={maxOffset}, currentOffset={buffer.ManualScrollOffset}, buffer.Count={buffer.Count}");
            buffer.ScrollDown(lines, maxOffset);
            ScrollDebugLogger.Log($"CenterPanelDisplayManager.ScrollDown: newOffset={buffer.ManualScrollOffset}, isManualScrolling={buffer.IsManualScrolling}");
            modeManager.Timing.ForceRender(new System.Action(renderCoordinator.PerformRender));
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode
        /// </summary>
        public void ResetScroll()
        {
            buffer.ResetScroll();
            modeManager.Timing.ScheduleRender(new System.Action(renderCoordinator.PerformRender));
        }
        
        /// <summary>
        /// Forces an immediate render (bypasses timing)
        /// </summary>
        public void ForceRender()
        {
            modeManager.Timing.ForceRender(new System.Action(renderCoordinator.PerformRender));
        }
        
        /// <summary>
        /// Forces a full layout render by resetting render state
        /// This ensures the layout is fully rendered even if state manager thinks nothing changed
        /// </summary>
        public void ForceFullLayoutRender()
        {
            renderStateManager.Reset();
            modeManager.Timing.ForceRender(new System.Action(renderCoordinator.PerformRender));
        }
        
        /// <summary>
        /// Cancels any pending renders
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void CancelPendingRenders()
        {
            modeManager.Timing.CancelPending();
        }
        
        private List<string> SplitIntoChunks(string text, UI.ChunkedTextReveal.ChunkStrategy strategy) 
            => ChunkSplitterHelper.SplitIntoChunks(text, strategy);
    }
}

