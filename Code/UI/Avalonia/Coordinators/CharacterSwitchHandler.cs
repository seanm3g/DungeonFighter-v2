using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using System;

namespace RPGGame.UI.Avalonia.Coordinators
{
    /// <summary>
    /// Handles character switching events and UI updates.
    /// Extracted from CanvasUICoordinator to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CharacterSwitchHandler
    {
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasContextManager contextManager;
        private GameStateManager? stateManager;
        private readonly System.Action<Character?> setCharacter;
        private readonly System.Action clearCurrentEnemy;
        private readonly System.Action forceFullLayoutRender;

        public CharacterSwitchHandler(
            ICanvasTextManager textManager,
            ICanvasContextManager contextManager,
            GameStateManager? stateManager,
            System.Action<Character?> setCharacter,
            System.Action clearCurrentEnemy,
            System.Action forceFullLayoutRender)
        {
            this.textManager = textManager ?? throw new ArgumentNullException(nameof(textManager));
            this.contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            this.stateManager = stateManager;
            this.setCharacter = setCharacter ?? throw new ArgumentNullException(nameof(setCharacter));
            this.clearCurrentEnemy = clearCurrentEnemy ?? throw new ArgumentNullException(nameof(clearCurrentEnemy));
            this.forceFullLayoutRender = forceFullLayoutRender ?? throw new ArgumentNullException(nameof(forceFullLayoutRender));
        }

        /// <summary>
        /// Handles character switch events - refreshes character panel and updates UI
        /// </summary>
        public void OnCharacterSwitched(object? sender, CharacterSwitchedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Switch to the new character's display manager FIRST
                    // This ensures each character has their own isolated display buffer
                    if (textManager is Managers.CanvasTextManager canvasTextManager)
                    {
                        // Switch to the new character's display manager
                        // This provides complete isolation - each character has their own buffer
                        var newCharacter = e.NewCharacter;
                        canvasTextManager.SwitchToCharacterDisplayManager(newCharacter);
                        
                        // Clear external render callback (prevents old combat callbacks from firing)
                        canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
                        // Reset to standard display mode (prevents combat mode from persisting)
                        canvasTextManager.DisplayManager.SetMode(new StandardDisplayMode());
                        // Note: We don't need to clear the buffer here because we've switched to a different display manager
                        // The new character's display manager will have its own clean buffer
                    }
                    
                    // Clear enemy context to prevent old combat from showing
                    // This ensures the render system doesn't think combat is active for the new character
                    clearCurrentEnemy.Invoke();
                    
                    // Clear dungeon context to prevent old character's enemy info from showing
                    // This ensures the dungeon context doesn't contain enemy info from the previous character
                    contextManager.ClearDungeonContext();
                    
                    // Force a full layout render to ensure clean state
                    // This ensures the enemy is cleared from the right panel and everything is refreshed
                    forceFullLayoutRender.Invoke();
                    
                    RefreshCharacterPanel();
                }
                catch (Exception ex)
                {
                    // Log error but don't crash
                    System.Diagnostics.Debug.WriteLine($"Error refreshing character panel on switch: {ex.Message}");
                }
            }, DispatcherPriority.Normal);
        }
        
        /// <summary>
        /// Sets the state manager (called after construction when state manager is available)
        /// </summary>
        public void SetStateManager(GameStateManager? stateManager)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Refreshes the character panel with the current active character
        /// </summary>
        public void RefreshCharacterPanel()
        {
            if (stateManager != null)
            {
                var activeCharacter = stateManager.GetActiveCharacter();
                if (activeCharacter != null)
                {
                    setCharacter(activeCharacter);
                    // Force a re-render of the character panel if needed
                    // The character panel should auto-update via contextManager
                }
            }
        }
    }
}

