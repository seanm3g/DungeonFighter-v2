using System;
using System.Collections.Generic;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Display.Helpers;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display.Render
{

    /// <summary>
    /// Coordinates rendering operations and state management.
    /// </summary>
    public class RenderCoordinator
    {
        private readonly GameCanvasControl canvas;
        private readonly DisplayRenderer renderer;
        private readonly PersistentLayoutManager layoutManager;
        private readonly RenderStateManager renderStateManager;
        private readonly ICanvasContextManager contextManager;
        private readonly DisplayBuffer buffer;
        private readonly DisplayMode currentMode;
        
        // Render guard to prevent concurrent renders
        private bool isRendering = false;
        private readonly object renderLock = new object();
        
        // Callback to trigger combat screen re-render when new messages are added
        private System.Action? externalRenderCallback = null;
        
        public RenderCoordinator(
            GameCanvasControl canvas,
            DisplayRenderer renderer,
            PersistentLayoutManager layoutManager,
            RenderStateManager renderStateManager,
            ICanvasContextManager contextManager,
            DisplayBuffer buffer,
            DisplayMode currentMode)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            this.layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
            this.renderStateManager = renderStateManager ?? throw new ArgumentNullException(nameof(renderStateManager));
            this.contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            this.currentMode = currentMode ?? throw new ArgumentNullException(nameof(currentMode));
        }
        
        /// <summary>
        /// Sets an external render callback for cases where external renderer handles rendering
        /// </summary>
        public void SetExternalRenderCallback(System.Action? renderCallback)
        {
            externalRenderCallback = renderCallback;
        }
        
        /// <summary>
        /// Triggers a render (used by batch transactions and explicit render calls)
        /// </summary>
        public void TriggerRender(DisplayTiming timing)
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
        /// Performs the actual rendering operation
        /// Uses RenderStateManager to determine what needs rendering
        /// </summary>
        public void PerformRender()
        {
            // Prevent concurrent renders
            lock (renderLock)
            {
                if (isRendering)
                {
                    // Already rendering, skip this call
                    return;
                }
                isRendering = true;
            }
            
            var state = renderStateManager.GetRenderState(buffer, contextManager);
            
            if (!state.NeedsRender)
            {
                // Nothing to render, release lock
                lock (renderLock)
                {
                    isRendering = false;
                }
                return;
            }
            
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Get center content area dimensions from layout manager
                    var (contentX, contentY, contentWidth, contentHeight) = layoutManager.GetCenterContentArea();
                    
                    // Determine if we should clear canvas
                    bool shouldClearCanvas = renderStateManager.ShouldClearCanvas(state, currentMode);
                    
                    if (shouldClearCanvas || state.NeedsFullLayout)
                    {
                        // Full layout render
                        string title = TitleResolver.DetermineTitle(state.CurrentCharacter, state.CurrentEnemy);
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
                }
                finally
                {
                    // Release render lock when done (on UI thread)
                    lock (renderLock)
                    {
                        isRendering = false;
                    }
                }
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Calculates the maximum scroll offset based on current buffer content
        /// </summary>
        public int CalculateMaxScrollOffset()
        {
            // Get center content area dimensions from layout manager
            var (contentX, contentY, contentWidth, contentHeight) = layoutManager.GetCenterContentArea();
            
            // Use DisplayRenderer's calculation method for accurate results
            // This matches the actual rendering logic
            return renderer.CalculateMaxScrollOffset(buffer, contentWidth, contentHeight);
        }
    }
}

