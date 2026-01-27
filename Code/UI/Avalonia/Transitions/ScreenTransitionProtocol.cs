using System;
using RPGGame;
using RPGGame.UI.Avalonia.Transitions;

namespace RPGGame.UI.Avalonia.Transitions
{
    /// <summary>
    /// Standardized protocol for screen transitions.
    /// Ensures consistent behavior across all screen transitions by following
    /// a well-defined sequence of operations.
    /// 
    /// Protocol Sequence:
    /// 1. Transition state FIRST (prevents reactive systems from interfering)
    /// 2. Suppress display buffer rendering
    /// 3. Clear interactive elements
    /// 4. Clear context (enemy, dungeon, room) if needed
    /// 5. Set character (if provided)
    /// 6. Explicitly clear canvas (via CanvasUICoordinator.Clear())
    /// 7. Render screen (render methods should pass clearCanvas: false to avoid double-clearing)
    /// 8. Force refresh
    /// 9. DO NOT restore display buffer (handled by DisplayBufferManager)
    /// 
    /// IMPORTANT: Since this protocol clears the canvas at step 6, render methods called
    /// through this protocol should pass clearCanvas: false to RenderWithLayout to avoid
    /// double-clearing. The canvas is already cleared by this protocol.
    /// </summary>
    public static class ScreenTransitionProtocol
    {
        /// <summary>
        /// Transitions to a menu screen using the standardized protocol.
        /// </summary>
        /// <param name="stateManager">Game state manager</param>
        /// <param name="canvasUI">Canvas UI coordinator</param>
        /// <param name="context">Screen transition context with all required parameters</param>
        public static void TransitionToMenuScreen(
            GameStateManager stateManager,
            CanvasUICoordinator canvasUI,
            ScreenTransitionContext context)
        {
            if (stateManager == null)
                throw new ArgumentNullException(nameof(stateManager));
            if (canvasUI == null)
                throw new ArgumentNullException(nameof(canvasUI));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // STEP 1: Transition state FIRST
            // This prevents reactive systems (display buffer, animations) from interfering
            stateManager.TransitionToState(context.TargetState);

            // STEP 2: Suppress display buffer rendering
            // Prevents combat log from auto-rendering and clearing menu
            canvasUI.SuppressDisplayBufferRendering();
            canvasUI.ClearDisplayBufferWithoutRender();

            // STEP 3: Clear interactive elements
            canvasUI.ClearClickableElements();

            // STEP 3.5: Clear any loading animations/status
            // Ensures clean menu display without lingering loading messages
            canvasUI.ClearLoadingStatus();

            // STEP 4: Clear context (if needed)
            if (context.ClearEnemyContext)
                canvasUI.ClearCurrentEnemy();
            if (context.ClearDungeonContext)
            {
                canvasUI.SetDungeonName(null);
                canvasUI.SetRoomName(null);
            }

            // STEP 5: Set character (if provided)
            if (context.Character != null)
                canvasUI.SetCharacter(context.Character);

            // STEP 6: Explicitly clear canvas
            // Ensures clean transition regardless of title changes
            canvasUI.Clear();

            // STEP 7: Render screen
            context.RenderAction(canvasUI);

            // STEP 8: Force refresh
            // Ensures screen is displayed immediately
            canvasUI.Refresh();
            
            // Track the rendered screen state to prevent unnecessary re-renders
            canvasUI.SetLastRenderedScreenState(context.TargetState);

            // STEP 9: DO NOT restore display buffer
            // Menu screens keep display buffer suppressed
            // DisplayBufferManager will handle restoration when entering non-menu states
        }

        /// <summary>
        /// Convenience overload that creates a context from individual parameters.
        /// </summary>
        public static void TransitionToMenuScreen(
            GameStateManager stateManager,
            CanvasUICoordinator canvasUI,
            GameState targetState,
            Action<CanvasUICoordinator> renderAction,
            Character? character = null,
            bool clearEnemyContext = true,
            bool clearDungeonContext = false)
        {
            var context = new ScreenTransitionContext(
                targetState,
                renderAction,
                character,
                clearEnemyContext,
                clearDungeonContext);

            TransitionToMenuScreen(stateManager, canvasUI, context);
        }
    }
}

