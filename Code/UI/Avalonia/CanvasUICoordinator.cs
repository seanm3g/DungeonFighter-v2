using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Builders;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Coordinators;
using RPGGame.UI.Avalonia.Utils;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Refactored coordinated UI manager that delegates to specialized coordinators
    /// Replaces the monolithic CanvasUIManager with a clean separation of concerns
    /// </summary>
    public partial class CanvasUICoordinator : IUIManager
    {
        private readonly GameCanvasControl canvas;
        private readonly CanvasRenderer renderer;
        private readonly ICanvasInteractionManager interactionManager;
        private readonly CanvasLayoutManager layoutManager;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasAnimationManager animationManager;
        private readonly ICanvasContextManager contextManager;
        
        // Specialized coordinators (consolidated from 6 to 3)
        private readonly MessageWritingCoordinator messageWritingCoordinator;
        private readonly UtilityCoordinator utilityCoordinator;
        private readonly ColoredTextCoordinator coloredTextCoordinator;
        private readonly BatchOperationCoordinator batchOperationCoordinator;
        private readonly Display.DisplayUpdateCoordinator displayUpdateCoordinator;
        private readonly Display.DisplayBufferManager displayBufferManager;
        private readonly Coordinators.CharacterSwitchHandler characterSwitchHandler;
        private readonly UIContextCoordinator contextCoordinator;
        private readonly CanvasWindowManager windowManager;
        
        private GameStateManager? stateManager = null;
        
        // Screen state tracking to prevent unnecessary re-renders
        private GameState? lastRenderedScreenState = null;
        
        /// <summary>
        /// Gets the last rendered screen state.
        /// Used to prevent unnecessary re-renders when already showing the same screen.
        /// </summary>
        public GameState? LastRenderedScreenState => lastRenderedScreenState;
        
        /// <summary>
        /// Sets the last rendered screen state.
        /// Called by ScreenTransitionProtocol after rendering.
        /// </summary>
        internal void SetLastRenderedScreenState(GameState state)
        {
            lastRenderedScreenState = state;
        }

        public CanvasUICoordinator(GameCanvasControl canvas)
        {
            this.canvas = canvas;
            
            // Use builder to initialize all dependencies
            var builder = new CanvasUICoordinatorBuilder(canvas);
            var buildResult = builder.Build();
            
            // Assign built dependencies
            this.contextManager = buildResult.ContextManager;
            this.layoutManager = buildResult.LayoutManager;
            this.interactionManager = buildResult.InteractionManager;
            this.textManager = buildResult.TextManager;
            this.renderer = buildResult.Renderer;
            this.animationManager = buildResult.AnimationManager;
            this.messageWritingCoordinator = buildResult.MessageWritingCoordinator;
            this.utilityCoordinator = buildResult.UtilityCoordinator;
            this.coloredTextCoordinator = buildResult.ColoredTextCoordinator;
            this.batchOperationCoordinator = buildResult.BatchOperationCoordinator;
            this.displayUpdateCoordinator = buildResult.DisplayUpdateCoordinator;
            this.displayBufferManager = buildResult.DisplayBufferManager;
            
            // Set up DisplayBufferManager with this coordinator (circular reference resolved)
            displayBufferManager.SetCoordinator(this);
            
            // Create context coordinator for context management operations
            this.contextCoordinator = new UIContextCoordinator(contextManager, textManager, stateManager);
            
            // Create character switch handler
            this.characterSwitchHandler = new Coordinators.CharacterSwitchHandler(
                textManager,
                contextManager,
                null, // Will be set via SetStateManager
                (System.Action<Character?>)SetCharacter,
                (System.Action)ClearCurrentEnemy,
                (System.Action)ForceFullLayoutRender);
            
            // Create window manager
            this.windowManager = new CanvasWindowManager();
        }
        
        /// <summary>
        /// Sets the main window reference for accessing UI controls
        /// </summary>
        public void SetMainWindow(MainWindow window)
        {
            windowManager.SetMainWindow(window);
        }
        
        /// <summary>
        /// Gets the main window reference
        /// </summary>
        public MainWindow? GetMainWindow()
        {
            return windowManager.GetMainWindow();
        }
        
        /// <summary>
        /// Gets the animation manager for configuration updates
        /// </summary>
        public ICanvasAnimationManager GetAnimationManager()
        {
            return this.animationManager;
        }
        
        /// <summary>
        /// Gets the stats panel state manager
        /// </summary>
        public Managers.StatsPanelStateManager? GetStatsPanelStateManager()
        {
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                return canvasTextManager.StatsPanelStateManager;
            }
            return null;
        }

        /// <summary>
        /// Focuses the canvas to ensure keyboard input is captured
        /// </summary>
        public void FocusCanvas()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    canvas.Focus();
                }
                catch (Exception)
                {
                    // Ignore focus errors
                }
            }, DispatcherPriority.Normal);
        }
        
        /// <summary>
        /// Sets the game instance reference for accessing handlers
        /// </summary>
        public void SetGame(GameCoordinator gameInstance)
        {
            windowManager.SetGame(gameInstance);
        }
        
        /// <summary>
        /// Gets the game instance reference
        /// </summary>
        public GameCoordinator? GetGame()
        {
            return windowManager.GetGame();
        }

        /// <summary>Canvas routing context (character, enemy, location). Used by combat/lab display sync.</summary>
        public ICanvasContextManager CanvasContext => contextManager;
        
        /// <summary>
        /// Sets the game state manager for the animation system.
        /// This allows the animation manager to subscribe to state change events.
        /// Should be called after Game is initialized.
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager;
            
            // Update context coordinator with state manager
            contextCoordinator.SetStateManager(stateManager);
            
            // Update state manager in display manager and render coordinator
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                // Set state manager on CanvasTextManager (which will update all per-character display managers)
                canvasTextManager.SetStateManager(stateManager);
            }
            
            if (animationManager is CanvasAnimationManager canvasAnimationManager)
            {
                var dungeonRenderer = new Renderers.DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
                System.Action<Character, List<Dungeon>> reRenderCallback = (player, dungeons) => 
                {
                    if (player != null && dungeons != null)
                        RenderDungeonSelection(player, dungeons);
                };
                canvasAnimationManager.SetupAnimationManager(dungeonRenderer, reRenderCallback, stateManager);
                
                // Set up crit line re-render callback to trigger display buffer re-renders
                // Reuse canvasTextManager from outer scope if it exists
                if (textManager is Managers.CanvasTextManager textMgr)
                {
                    System.Action critLineReRenderCallback = () =>
                    {
                        textMgr.ForceRenderForActiveCharacter();
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
            
            // Subscribe to state changes to close settings panel when main menu closes
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
        
        /// <summary>
        /// Refreshes the character panel with the current active character
        /// </summary>
        public void RefreshCharacterPanel()
        {
            characterSwitchHandler.RefreshCharacterPanel();
        }

    }
}
