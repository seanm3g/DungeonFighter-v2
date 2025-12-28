using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        private GameStateManager? stateManager;
        
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
            DisplayMode currentMode,
            GameStateManager? stateManager = null)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            this.layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
            this.renderStateManager = renderStateManager ?? throw new ArgumentNullException(nameof(renderStateManager));
            this.contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            this.currentMode = currentMode ?? throw new ArgumentNullException(nameof(currentMode));
            this.stateManager = stateManager;
        }
        
        /// <summary>
        /// Sets an external render callback for cases where external renderer handles rendering
        /// </summary>
        public void SetExternalRenderCallback(System.Action? renderCallback)
        {
            externalRenderCallback = renderCallback;
        }
        
        /// <summary>
        /// Sets the game state manager (called after construction when state manager is available)
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager;
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
                timing.ScheduleRender(() => PerformRender());
            }
        }
        
        /// <summary>
        /// Performs the actual rendering operation
        /// Uses RenderStateManager to determine what needs rendering
        /// </summary>
        /// <param name="force">If true, bypasses the NeedsRender check and always renders (used for animation updates)</param>
        public void PerformRender(bool force = false)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "RenderCoordinator.cs:PerformRender", message = "Entry", data = new { force = force, bufferCount = buffer.Count }, sessionId = "debug-session", runId = "run2", hypothesisId = "H5" }) + "\n"); } catch { }
            // #endregion
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
            
            var state = renderStateManager.GetRenderState(buffer, contextManager, stateManager);
            var activeCharacter = stateManager?.GetActiveCharacter();
            var currentCharacter = contextManager.GetCurrentCharacter();
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "RenderCoordinator.cs:PerformRender", message = "Render state", data = new { currentCharacter = currentCharacter?.Name, activeCharacter = activeCharacter?.Name, charactersMatch = currentCharacter == activeCharacter, bufferCount = buffer.Count, needsRender = state.NeedsRender }, sessionId = "debug-session", runId = "run2", hypothesisId = "H5" }) + "\n"); } catch { }
            // #endregion
            
            // CRITICAL: Clear enemy if character doesn't match active character OR if there's no external callback
            // This prevents background combat enemies from being displayed even if callback hasn't been cleared yet
            if (state.CurrentEnemy != null && stateManager != null && state.CurrentCharacter != null)
            {
                var currentActiveCharacter = stateManager.GetActiveCharacter();
                bool characterMatches = state.CurrentCharacter == currentActiveCharacter;
                
                // CRITICAL: Only clear enemy if character doesn't match
                // Don't clear based on callback - callback may not be set yet when combat first starts
                // The callback is set up in RenderCombat, which happens after StartEnemyEncounter sets the enemy
                if (!characterMatches)
                {
                    // Character doesn't match = enemy is from background combat
                    // Clear it to prevent rendering
                    state.CurrentEnemy = null;
                    // Also clear from context manager to prevent it from being used in future renders
                    contextManager.ClearCurrentEnemy();
                }
                // Note: We don't clear enemy if character matches but no callback - this handles the case
                // where combat is starting and the callback hasn't been set up yet
            }
            // Note: We don't clear enemy if we can't check character and there's no callback
            // This prevents clearing the enemy when combat is starting and the callback hasn't been set up yet
            // The character-based checks above are sufficient to prevent background combat enemies
            
            // If we're in combat mode but external callback is null, that means combat rendering was blocked
            // (character is inactive). Don't render the buffer to prevent showing background combat.
            if (currentMode is CombatDisplayMode && externalRenderCallback == null)
            {
                // Combat mode but no external callback = character is inactive, don't render
                lock (renderLock)
                {
                    isRendering = false;
                }
                return;
            }
            
            // CRITICAL: Skip rendering if we're in a menu state OR if stateManager is null (title screen)
            // Menu screens and title screen handle their own rendering and don't use the display buffer
            // This prevents the center panel from being cleared when menus/title screen are displayed
            // EXCEPTION: Allow forced rendering in Settings state for test output display
            var currentState = stateManager?.CurrentState;
            if (DisplayStateCoordinator.ShouldSuppressRendering(currentState, stateManager))
            {
                // Allow forced rendering in Settings state (for test output from Settings panel)
                if (force && currentState == GameState.Settings)
                {
                    // Continue with rendering - test output should be visible even in Settings
                }
                else
                {
                    // StateManager is null (title screen) or menu state - don't render display buffer
                    lock (renderLock)
                    {
                        isRendering = false;
                    }
                    return;
                }
            }
            
            // Only check NeedsRender if not forcing (force is used for animation updates)
            if (!force && !state.NeedsRender)
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
                        // FINAL CHECK: Ensure enemy is null if character is not active
                        // This is a last-ditch check to prevent background combat enemies from rendering
                        Enemy? enemyToRender = state.CurrentEnemy;
                        if (enemyToRender != null && stateManager != null && state.CurrentCharacter != null)
                        {
                            var currentActiveCharacter = stateManager.GetActiveCharacter();
                            if (state.CurrentCharacter != currentActiveCharacter)
                            {
                                // Character is not active - don't render enemy
                                // Also clear it from context manager to prevent it from being used in future renders
                                contextManager.ClearCurrentEnemy();
                                enemyToRender = null;
                            }
                            else if (externalRenderCallback == null)
                            {
                                // No external callback means no active combat - clear enemy even if character matches
                                // This handles the case where combat ended but enemy context wasn't cleared
                                contextManager.ClearCurrentEnemy();
                                enemyToRender = null;
                            }
                        }
                        else if (enemyToRender != null && externalRenderCallback == null)
                        {
                            // No callback and no character check possible - clear enemy to be safe
                            contextManager.ClearCurrentEnemy();
                            enemyToRender = null;
                        }
                        
                        // Full layout render
                        string title = TitleResolver.DetermineTitle(state.CurrentCharacter, enemyToRender);
                        layoutManager.RenderLayout(
                            state.CurrentCharacter,
                            (x, y, w, h) => renderer.Render(buffer, x, y, w, h),
                            title,
                            enemyToRender,  // Use the checked enemy (may be null)
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

