using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Renderers.Layout;
using RPGGame.UI.Avalonia.Renderers.Validators;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Refactored centralized canvas renderer that coordinates all rendering operations
    /// Uses specialized renderers for different screen types and functionalities
    /// </summary>
    public partial class CanvasRenderer : ICanvasRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasInteractionManager interactionManager;
        private readonly ICanvasContextManager contextManager;
        private readonly PersistentLayoutRenderCoordinator layoutCoordinator;
        
        // Validators and helpers
        private readonly CombatRenderingValidator combatValidator;
        private readonly MenuScreenRenderingHelper menuScreenHelper;
        
        // Core specialized renderers
        private readonly MenuRenderer menuRenderer;
        private readonly InventoryRenderer inventoryRenderer;
        private readonly CombatRenderer combatRenderer;
        private readonly DungeonRenderer dungeonRenderer;
        
        // New specialized renderers
        private readonly MessageDisplayRenderer messageRenderer;
        private readonly HelpSystemRenderer helpRenderer;
        private readonly CharacterCreationRenderer characterCreationRenderer;
        private readonly DungeonExplorationRenderer dungeonExplorationRenderer;

        public CanvasRenderer(GameCanvasControl canvas, ICanvasTextManager textManager, ICanvasInteractionManager interactionManager, ICanvasContextManager contextManager)
        {
            this.canvas = canvas;
            this.textManager = textManager;
            this.interactionManager = interactionManager;
            this.contextManager = contextManager;
            this.layoutCoordinator = new PersistentLayoutRenderCoordinator(canvas, interactionManager, textManager);
            
            // Initialize validators and helpers
            this.combatValidator = new CombatRenderingValidator(contextManager);
            this.menuScreenHelper = new MenuScreenRenderingHelper(canvas, layoutCoordinator);
            
            // Initialize core specialized renderers
            this.menuRenderer = new MenuRenderer(canvas, interactionManager.ClickableElements, textManager, interactionManager);
            this.inventoryRenderer = new InventoryRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            this.combatRenderer = new CombatRenderer(canvas, new Renderers.ColoredTextWriter(canvas));
            this.dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            
            // Initialize new specialized renderers
            // MessageDisplayRenderer and HelpSystemRenderer need clearing actions - use canvas.Clear() directly
            // This is acceptable as these renderers are used for full-screen operations that need immediate clearing
            System.Action clearCanvasAction = () => canvas.Clear();
            this.messageRenderer = new MessageDisplayRenderer(canvas, clearCanvasAction);
            this.helpRenderer = new HelpSystemRenderer(canvas, clearCanvasAction);
            this.characterCreationRenderer = new CharacterCreationRenderer(canvas, textManager, interactionManager);
            this.dungeonExplorationRenderer = new DungeonExplorationRenderer(canvas, interactionManager);
        }

        public void RenderDisplayBuffer(CanvasContext context)
        {
            // Rendering is now handled automatically by the unified display system
            // This method is kept for backward compatibility but does nothing
            // The display manager handles all rendering automatically
        }

        // Message display methods - delegated to MessageDisplayRenderer
        public void ShowMessage(string message, Color color = default) => messageRenderer.ShowMessage(message, color);
        public void ShowError(string error) => messageRenderer.ShowError(error);
        public void ShowError(string error, string suggestion = "") => messageRenderer.ShowError(error, suggestion);
        public void ShowSuccess(string message) => messageRenderer.ShowSuccess(message);
        public void ShowLoadingAnimation(string message = "Loading...") => messageRenderer.ShowLoadingAnimation(message);
        public void UpdateStatus(string message) => messageRenderer.UpdateStatus(message);
        public void ShowInvalidKeyMessage(string message) => messageRenderer.ShowInvalidKeyMessage(message);
        public void ShowLoadingStatus(string message = "Loading data...") => messageRenderer.ShowLoadingStatus(message);
        public void ClearLoadingStatus() => messageRenderer.ClearLoadingStatus();

        // Help system methods - delegated to HelpSystemRenderer
        public void ToggleHelp()
        {
            bool showHelp = helpRenderer.ToggleHelp();
            if (showHelp)
            {
                helpRenderer.RenderHelp();
            }
            else
            {
                RenderMainMenu(false, null, 0);
            }
        }

        public void RenderHelp()
        {
            helpRenderer.RenderHelp();
        }

        /// <summary>
        /// Redraws only the action-info strip (and optional tooltip overlay). Strip clears its own text/box region first.
        /// Used for GameLoop hover when the pointer moves between strip panels; hover-out (-1) uses full chrome refresh to erase the tooltip.
        /// </summary>
        public void RefreshActionInfoStripOnly(Character? player, bool drawHoverDetailOverlay = true)
        {
            dungeonRenderer.RenderActionInfoStrip(player, drawHoverDetailOverlay);
            canvas.Refresh();
        }

        public void Refresh()
        {
            canvas.Refresh();
        }

        #region Private Helper Methods

        /// <summary>
        /// Points the active center-panel display buffer at <paramref name="player"/> so reads/writes match
        /// <see cref="CanvasTextManager.GetDisplayManagerForCharacter"/> (dungeon narrative routing).
        /// </summary>
        private void EnsureDisplayManagerForPlayer(Character? player)
        {
            if (player == null || textManager is not CanvasTextManager ctm)
                return;
            ctm.SwitchToCharacterDisplayManager(player);
        }

        private void RenderWithLayout(Character? character, string title, Action<int, int, int, int> renderContent, CanvasContext context, Enemy? enemy, string? dungeonName, string? roomName, bool clearCanvas = true, bool usePersistentChrome = true, bool inventoryComboRightPanel = false)
        {
            layoutCoordinator.RenderWithLayout(character, title, renderContent, context, enemy, dungeonName, roomName, clearCanvas, usePersistentChrome, inventoryComboRightPanel);
        }

        #endregion
    }
}
