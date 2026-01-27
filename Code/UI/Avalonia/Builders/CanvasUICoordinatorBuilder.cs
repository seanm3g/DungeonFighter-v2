using RPGGame;
using RPGGame.UI.Avalonia.Coordinators;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using System;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Builder for creating CanvasUICoordinator instances.
    /// Extracts complex initialization logic from CanvasUICoordinator constructor.
    /// </summary>
    public class CanvasUICoordinatorBuilder
    {
        private readonly GameCanvasControl canvas;

        public CanvasUICoordinatorBuilder(GameCanvasControl canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        /// <summary>
        /// Builds a CanvasUICoordinator with all dependencies initialized.
        /// </summary>
        public CanvasUICoordinatorBuildResult Build()
        {
            // Initialize specialized managers
            var contextManager = new CanvasContextManager();
            var layoutManager = new CanvasLayoutManager();
            var interactionManager = new CanvasInteractionManager();
            var textWriter = new Renderers.ColoredTextWriter(canvas);
            var textManager = new CanvasTextManager(canvas, textWriter, contextManager, stateManager: null, interactionManager: interactionManager);
            var renderer = new CanvasRenderer(canvas, textManager, interactionManager, contextManager);

            // Initialize specialized coordinators
            var messageWritingCoordinator = new MessageWritingCoordinator(textManager, renderer, contextManager);
            var animationManager = new CanvasAnimationManager(canvas, null, null);
            var utilityCoordinator = new UtilityCoordinator(canvas, renderer, textManager, contextManager);
            var coloredTextCoordinator = new ColoredTextCoordinator(textManager, messageWritingCoordinator);
            var batchOperationCoordinator = new BatchOperationCoordinator(textManager, messageWritingCoordinator);

            // Create DisplayUpdateCoordinator with display manager if available
            Display.CenterPanelDisplayManager? displayManager = null;
            Managers.CanvasTextManager? canvasTextManager = textManager as Managers.CanvasTextManager;
            if (canvasTextManager != null)
            {
                displayManager = canvasTextManager.DisplayManager;
            }
            var displayUpdateCoordinator = new Display.DisplayUpdateCoordinator(canvas, textManager, displayManager);

            // Create DisplayBufferManager for automatic display buffer state management
            // Note: Coordinator will be set after the coordinator is created
            var displayBufferManager = new Display.DisplayBufferManager(null); // Will be set after coordinator creation

            // Set up animation manager with proper dependencies
            var dungeonRenderer = new DungeonRenderer(canvas, textWriter, interactionManager.ClickableElements);
            // Note: reRenderCallback will be set in CanvasUICoordinator.SetStateManager() after coordinator is created
            animationManager.SetupAnimationManager(dungeonRenderer, null, null); // State manager and callback will be set later

            // Set up crit line re-render callback to trigger display buffer re-renders
            if (animationManager is CanvasAnimationManager canvasAnimationManager && canvasTextManager != null)
            {
                System.Action critLineReRenderCallback = () => canvasTextManager.DisplayManager.ForceRender();
                canvasAnimationManager.SetCritLineReRenderCallback(critLineReRenderCallback);
            }

            return new CanvasUICoordinatorBuildResult
            {
                ContextManager = contextManager,
                LayoutManager = layoutManager,
                InteractionManager = interactionManager,
                TextManager = textManager,
                Renderer = renderer,
                AnimationManager = animationManager,
                MessageWritingCoordinator = messageWritingCoordinator,
                UtilityCoordinator = utilityCoordinator,
                ColoredTextCoordinator = coloredTextCoordinator,
                BatchOperationCoordinator = batchOperationCoordinator,
                DisplayUpdateCoordinator = displayUpdateCoordinator,
                DisplayBufferManager = displayBufferManager,
                CanvasTextManager = canvasTextManager
            };
        }
    }

    /// <summary>
    /// Result of building a CanvasUICoordinator, containing all initialized dependencies.
    /// </summary>
    public class CanvasUICoordinatorBuildResult
    {
        public ICanvasContextManager ContextManager { get; set; } = null!;
        public CanvasLayoutManager LayoutManager { get; set; } = null!;
        public ICanvasInteractionManager InteractionManager { get; set; } = null!;
        public ICanvasTextManager TextManager { get; set; } = null!;
        public CanvasRenderer Renderer { get; set; } = null!;
        public ICanvasAnimationManager AnimationManager { get; set; } = null!;
        public MessageWritingCoordinator MessageWritingCoordinator { get; set; } = null!;
        public UtilityCoordinator UtilityCoordinator { get; set; } = null!;
        public ColoredTextCoordinator ColoredTextCoordinator { get; set; } = null!;
        public BatchOperationCoordinator BatchOperationCoordinator { get; set; } = null!;
        public Display.DisplayUpdateCoordinator DisplayUpdateCoordinator { get; set; } = null!;
        public Display.DisplayBufferManager DisplayBufferManager { get; set; } = null!;
        public Managers.CanvasTextManager? CanvasTextManager { get; set; }
    }
}

