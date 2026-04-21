using System;
using Avalonia.Threading;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Display.Helpers;
using RPGGame.UI.Avalonia.Display.Mode;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;

namespace RPGGame.UI.Avalonia.Display.Render
{

    /// <summary>
    /// Coordinates rendering operations and state management.
    /// </summary>
    public class RenderCoordinator
    {
        private readonly GameCanvasControl canvas;
        private readonly DisplayRenderer renderer;
        private readonly DungeonRenderer dungeonRenderer;
        private readonly PersistentLayoutManager layoutManager;
        private readonly RenderStateManager renderStateManager;
        private readonly ICanvasContextManager contextManager;
        private readonly DisplayBuffer buffer;
        private readonly DisplayModeManager modeManager;
        private GameStateManager? stateManager;
        
        // Render guard to prevent concurrent renders; coalesce extra PerformRender calls while a paint is in flight
        private bool isRendering = false;
        private bool renderPending = false;
        private bool pendingForce = false;
        private readonly object renderLock = new object();
        
        // Callback to trigger combat screen re-render when new messages are added
        private System.Action? externalRenderCallback = null;
        
        public RenderCoordinator(
            GameCanvasControl canvas,
            DisplayRenderer renderer,
            DungeonRenderer dungeonRenderer,
            PersistentLayoutManager layoutManager,
            RenderStateManager renderStateManager,
            ICanvasContextManager contextManager,
            DisplayBuffer buffer,
            DisplayModeManager modeManager,
            GameStateManager? stateManager = null)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            this.dungeonRenderer = dungeonRenderer ?? throw new ArgumentNullException(nameof(dungeonRenderer));
            this.layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
            this.renderStateManager = renderStateManager ?? throw new ArgumentNullException(nameof(renderStateManager));
            this.contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            this.modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
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
            lock (renderLock)
            {
                if (isRendering)
                {
                    renderPending = true;
                    if (force)
                        pendingForce = true;
                    return;
                }
                isRendering = true;
            }

            // If we're in combat mode but external callback is null, that means combat rendering was blocked
            // (character is inactive). Don't render the buffer to prevent showing background combat.
            // Use live mode from modeManager — the initial DisplayMode snapshot was never updated after SetMode(Combat).
            if (modeManager.CurrentMode is CombatDisplayMode && externalRenderCallback == null)
            {
                ReleaseRenderingAndProcessPending();
                return;
            }

            // CRITICAL: Skip rendering if we're in a menu state OR if stateManager is null (title screen)
            var currentState = stateManager?.CurrentState;
            if (DisplayStateCoordinator.ShouldSuppressRendering(currentState, stateManager))
            {
                if (!(force && currentState == GameState.Settings))
                {
                    ReleaseRenderingAndProcessPending();
                    return;
                }
            }

            var state = renderStateManager.GetRenderState(buffer, contextManager, stateManager);
            if (!force && !state.NeedsRender)
            {
                ReleaseRenderingAndProcessPending();
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                bool runFollowUp = false;
                bool followUpForce = false;
                try
                {
                    // Resolve layout inputs on the UI thread so action strip uses the same character/context as the live buffer
                    if (modeManager.CurrentMode is CombatDisplayMode && externalRenderCallback == null)
                        return;

                    var paintState = stateManager?.CurrentState;
                    if (DisplayStateCoordinator.ShouldSuppressRendering(paintState, stateManager))
                    {
                        if (!(force && paintState == GameState.Settings))
                            return;
                    }

                    ClearEnemyFromContextIfBackgroundCombat();

                    var state = renderStateManager.GetRenderState(buffer, contextManager, stateManager);

                    if (!force && !state.NeedsRender)
                        return;

                    bool shouldClearCanvas = renderStateManager.ShouldClearCanvas(state, modeManager.CurrentMode);

                    Enemy? enemyToRender;
                    Character? layoutCharacter;
                    string title;

                    if (paintState == GameState.ActionInteractionLab)
                    {
                        var lab = ActionInteractionLabSession.Current;
                        if (lab != null)
                        {
                            layoutCharacter = lab.LabPlayer;
                            enemyToRender = lab.LabEnemy;
                            title = "COMBAT";
                        }
                        else
                        {
                            enemyToRender = state.CurrentEnemy;
                            layoutCharacter = ResolveLayoutCharacter(state, stateManager);
                            title = TitleResolver.DetermineTitle(layoutCharacter, enemyToRender);
                        }
                    }
                    else
                    {
                        enemyToRender = state.CurrentEnemy;
                        layoutCharacter = ResolveLayoutCharacter(state, stateManager);
                        title = TitleResolver.DetermineTitle(layoutCharacter, enemyToRender);
                    }

                    bool labEnemyLevelHover = paintState == GameState.ActionInteractionLab && ActionInteractionLabSession.Current != null;
                    layoutManager.RenderLayout(
                        layoutCharacter,
                        (x, y, w, h) => { renderer.Render(buffer, x, y, w, h); },
                        title,
                        enemyToRender,
                        state.DungeonName,
                        state.RoomName,
                        clearCanvas: shouldClearCanvas,
                        usePersistentChrome: true,
                        inventoryComboRightPanel: false,
                        registerActionLabEnemyLevelHover: labEnemyLevelHover);

                    // Draw strip after left/center/right chrome so the right panel cannot paint over it when columns overlap.
                    if (layoutCharacter != null)
                    {
                        dungeonRenderer.RenderActionInfoStrip(layoutCharacter);
                        canvas.Refresh();
                    }

                    renderStateManager.RecordRender(buffer, contextManager);
                }
                finally
                {
                    lock (renderLock)
                    {
                        isRendering = false;
                        if (renderPending)
                        {
                            renderPending = false;
                            runFollowUp = true;
                            followUpForce = pendingForce;
                            pendingForce = false;
                        }
                    }
                    if (runFollowUp)
                        PerformRender(force: followUpForce);
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Removes enemy from context when it belongs to background combat (non-active character).
        /// Must run before <see cref="RenderStateManager.GetRenderState"/> so layout and strip match context.
        /// </summary>
        private void ClearEnemyFromContextIfBackgroundCombat()
        {
            var currentEnemy = contextManager.GetCurrentEnemy();
            var currentCharacter = contextManager.GetCurrentCharacter();
            if (currentEnemy == null || stateManager == null || currentCharacter == null)
                return;
            if (!DisplayStateCoordinator.IsCharacterActive(currentCharacter, stateManager))
                contextManager.ClearCurrentEnemy();
        }

        /// <summary>
        /// Resolves which <see cref="Character"/> to use for title and action-info strip.
        /// Prefer the registered active character so a stale <see cref="ICanvasContextManager"/> reference
        /// cannot override the gameplay instance (wrong or empty combo on the strip).
        /// </summary>
        internal static Character? ResolveLayoutCharacter(RenderState state, GameStateManager? stateManager)
        {
            return stateManager?.GetActiveCharacter() ?? state.CurrentCharacter;
        }

        private void ReleaseRenderingAndProcessPending()
        {
            bool followUp;
            bool followUpForce;
            lock (renderLock)
            {
                isRendering = false;
                followUp = renderPending;
                followUpForce = pendingForce;
                renderPending = false;
                pendingForce = false;
            }
            if (followUpForce)
                PerformRender(force: true);
            else if (followUp)
                PerformRender(force: false);
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

