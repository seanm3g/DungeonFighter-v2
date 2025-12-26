using System;
using RPGGame;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Centralized manager for display buffer state.
    /// Automatically suppresses display buffer rendering for menu states
    /// and restores it for non-menu states (Combat, Dungeon).
    /// 
    /// This eliminates the need for manual suppression/restoration calls
    /// scattered throughout the codebase.
    /// </summary>
    public class DisplayBufferManager
    {
        private readonly CanvasUICoordinator canvasUI;
        private GameStateManager? stateManager;
        private bool isSubscribed = false;
        private bool isManuallySuppressed = false;

        /// <summary>
        /// Creates a new DisplayBufferManager.
        /// </summary>
        /// <param name="canvasUI">Canvas UI coordinator to manage</param>
        public DisplayBufferManager(CanvasUICoordinator canvasUI)
        {
            this.canvasUI = canvasUI ?? throw new ArgumentNullException(nameof(canvasUI));
        }

        /// <summary>
        /// Sets the game state manager and subscribes to state change events.
        /// Call this when the state manager becomes available.
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            if (this.stateManager != null && isSubscribed)
            {
                // Unsubscribe from previous state manager
                this.stateManager.StateChanged -= OnStateChanged;
                isSubscribed = false;
            }

            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            
            // Subscribe to state change events
            this.stateManager.StateChanged += OnStateChanged;
            isSubscribed = true;

            // Apply initial state
            HandleStateChange(stateManager.CurrentState);
        }

        /// <summary>
        /// Manually suppresses display buffer rendering.
        /// Use this for edge cases where automatic management isn't sufficient.
        /// </summary>
        public void ManuallySuppress()
        {
            isManuallySuppressed = true;
            canvasUI.SuppressDisplayBufferRendering();
        }

        /// <summary>
        /// Manually restores display buffer rendering.
        /// Use this for edge cases where automatic management isn't sufficient.
        /// </summary>
        public void ManuallyRestore()
        {
            isManuallySuppressed = false;
            canvasUI.RestoreDisplayBufferRendering();
        }

        /// <summary>
        /// Clears the display buffer without triggering a render.
        /// </summary>
        public void ClearBufferWithoutRender()
        {
            canvasUI.ClearDisplayBufferWithoutRender();
        }

        /// <summary>
        /// Handles state change events from GameStateManager.
        /// </summary>
        private void OnStateChanged(object? sender, StateChangedEventArgs e)
        {
            HandleStateChange(e.NewState);
        }

        /// <summary>
        /// Handles state changes by automatically managing display buffer suppression/restoration.
        /// </summary>
        private void HandleStateChange(GameState newState)
        {
            // If manually suppressed, don't auto-manage
            if (isManuallySuppressed)
                return;

            // Automatically suppress for menu states
            if (DisplayStateCoordinator.IsMenuState(newState))
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
            }
            // Automatically restore for non-menu states (Combat, Dungeon, etc.)
            else
            {
                canvasUI.RestoreDisplayBufferRendering();
            }
        }

        /// <summary>
        /// Disposes the manager and unsubscribes from events.
        /// </summary>
        public void Dispose()
        {
            if (stateManager != null && isSubscribed)
            {
                stateManager.StateChanged -= OnStateChanged;
                isSubscribed = false;
            }
        }
    }
}

