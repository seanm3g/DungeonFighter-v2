using RPGGame;
using RPGGame.UI.Avalonia.Coordinators;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages state manager setup and state change handling for CanvasUICoordinator.
    /// Extracted from CanvasUICoordinator to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CanvasUIStateManager
    {
        private GameStateManager? stateManager;
        private GameState? lastRenderedScreenState;
        
        /// <summary>
        /// Gets the last rendered screen state.
        /// Used to prevent unnecessary re-renders when already showing the same screen.
        /// </summary>
        public GameState? LastRenderedScreenState => lastRenderedScreenState;
        
        /// <summary>
        /// Sets the last rendered screen state.
        /// Called by ScreenTransitionProtocol after rendering.
        /// </summary>
        public void SetLastRenderedScreenState(GameState state)
        {
            lastRenderedScreenState = state;
        }
        
        /// <summary>
        /// Sets up the state manager and wires up all necessary event handlers and dependencies.
        /// </summary>
        public void SetupStateManager(
            GameStateManager stateManager,
            ICanvasTextManager textManager,
            ICanvasAnimationManager animationManager,
            Display.DisplayBufferManager displayBufferManager,
            CharacterSwitchHandler characterSwitchHandler,
            UIContextCoordinator contextCoordinator,
            GameCanvasControl canvas,
            ICanvasInteractionManager interactionManager,
            System.Action<Character, List<Dungeon>> renderDungeonSelectionCallback)
        {
            this.stateManager = stateManager;
            
            // Update context coordinator with state manager
            contextCoordinator.SetStateManager(stateManager);
            
            // Update state manager in display manager
            if (textManager is CanvasTextManager canvasTextManager)
            {
                // Set state manager on CanvasTextManager (which will update all per-character display managers)
                canvasTextManager.SetStateManager(stateManager);
            }
            
            // Set up animation manager
            if (animationManager is CanvasAnimationManager canvasAnimationManager)
            {
                var dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
                canvasAnimationManager.SetupAnimationManager(dungeonRenderer, renderDungeonSelectionCallback, stateManager);
                
                // Set up crit line re-render callback to trigger display buffer re-renders
                if (textManager is CanvasTextManager textMgr)
                {
                    System.Action critLineReRenderCallback = () =>
                    {
                        textMgr.DisplayManager.ForceRender();
                    };
                    canvasAnimationManager.SetCritLineReRenderCallback(critLineReRenderCallback);
                }
            }
            
            // Set state manager in DisplayBufferManager for automatic state-based management
            displayBufferManager.SetStateManager(stateManager);
            
            // Update character switch handler with state manager
            characterSwitchHandler.SetStateManager(stateManager);
            
            // Subscribe to character switch events for multi-character support
            stateManager.CharacterSwitched += characterSwitchHandler.OnCharacterSwitched;
            
            // Subscribe to state changes
            stateManager.StateChanged += OnStateChanged;
        }
        
        /// <summary>
        /// Handles state changes - settings window stays open regardless of state transitions
        /// </summary>
        private void OnStateChanged(object? sender, StateChangedEventArgs e)
        {
            // Don't close settings window on state changes - it should stay open independently
            // The settings window is a separate pop-out window that doesn't depend on game state
            // Users can interact with the main menu while settings window is open
        }
    }
}
