using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Menu;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles rendering of all menu screens (main menu, settings, game menu, weapon selection)
    /// Refactored to use extracted screen-specific renderers and layout calculator
    /// </summary>
    public class MenuRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasInteractionManager interactionManager;
        private int currentLineCount;
        
        // Screen-specific renderers
        private readonly MainMenuRenderer mainMenuRenderer;
        private readonly SettingsMenuRenderer settingsMenuRenderer;
        private readonly WeaponSelectionRenderer weaponSelectionRenderer;
        private readonly GameMenuRenderer gameMenuRenderer;
        private readonly TestingMenuRenderer testingMenuRenderer;
        
        public MenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager, ICanvasInteractionManager interactionManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.interactionManager = interactionManager;
            this.currentLineCount = 0;
            
            // Initialize screen-specific renderers
            this.mainMenuRenderer = new MainMenuRenderer(canvas, clickableElements, textManager);
            this.settingsMenuRenderer = new SettingsMenuRenderer(canvas, clickableElements, textManager);
            this.weaponSelectionRenderer = new WeaponSelectionRenderer(canvas, clickableElements, interactionManager, textManager);
            this.gameMenuRenderer = new GameMenuRenderer(canvas, clickableElements, textManager);
            this.testingMenuRenderer = new TestingMenuRenderer(canvas, clickableElements, textManager);
        }
        
        // IScreenRenderer implementation
        public void Render()
        {
            // This is a placeholder - specific render methods are called directly
            // Future refactor could use a state machine pattern here
        }
        
        public void Clear()
        {
            clickableElements.Clear();
            currentLineCount = 0;
        }
        
        public int GetLineCount()
        {
            return currentLineCount;
        }
        
        // IInteractiveRenderer implementation
        public List<ClickableElement> GetClickableElements()
        {
            return clickableElements;
        }
        
        public void UpdateHoverState(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                element.IsHovered = element.Contains(x, y);
            }
        }
        
        public bool HandleClick(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                if (element.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Renders the main menu content within the center panel (top-left justified)
        /// </summary>
        public void RenderMainMenuContent(int x, int y, int width, int height, bool hasSavedGame, string? characterName, int characterLevel)
        {
            currentLineCount = mainMenuRenderer.RenderMainMenuContent(x, y, width, height, hasSavedGame, characterName, characterLevel);
        }
        
        /// <summary>
        /// Renders the main menu with saved game info if available (legacy method)
        /// </summary>
        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            currentLineCount = mainMenuRenderer.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        }
        
        /// <summary>
        /// Renders the settings screen using the 3-panel layout
        /// </summary>
        public void RenderSettings()
        {
            RPGGame.Utils.ScrollDebugLogger.Log("MenuRenderer: RenderSettings called");
            // Use the persistent layout system for consistent 3-panel design
            var layoutManager = new PersistentLayoutManager(canvas);
            
            // Get saved character info for display
            var (characterName, characterLevel) = CharacterSaveManager.GetSavedCharacterInfo();
            bool hasSavedCharacter = characterName != null;
            
            ScrollDebugLogger.Log($"MenuRenderer: About to call RenderLayout with title='SETTINGS'");
            // Render the layout with settings content
            layoutManager.RenderLayout(
                character: null, // No character in settings
                renderCenterContent: (x, y, width, height) => RenderSettingsContent(x, y, width, height, hasSavedCharacter, characterName, characterLevel),
                title: "SETTINGS",
                enemy: null,
                dungeonName: null,
                roomName: null,
                clearCanvas: true // Always clear canvas when showing settings
            );
            
            ScrollDebugLogger.Log("MenuRenderer: RenderLayout completed, refreshing canvas");
            // Ensure canvas is refreshed to display the settings menu
            canvas.Refresh();
            ScrollDebugLogger.Log("MenuRenderer: Canvas refreshed");
        }
        
        /// <summary>
        /// Renders the simplified settings content with only functional options
        /// </summary>
        private void RenderSettingsContent(int x, int y, int width, int height, bool hasSavedCharacter, string? characterName, int characterLevel)
        {
            currentLineCount = settingsMenuRenderer.RenderSettingsContent(x, y, width, height, hasSavedCharacter, characterName, characterLevel);
        }
        
        
        /// <summary>
        /// Renders the testing menu screen using the 3-panel layout
        /// </summary>
        public void RenderTestingMenu(int x, int y, int width, int height)
        {
            currentLineCount = testingMenuRenderer.RenderTestingMenu(x, y, width, height);
        }
        
        /// <summary>
        /// Renders the in-game menu (after character creation)
        /// </summary>
        public void RenderGameMenu(int x, int y, int width, int height)
        {
            currentLineCount = gameMenuRenderer.RenderGameMenu(x, y, width, height);
        }

        /// <summary>
        /// Renders the weapon selection content with centered layout
        /// </summary>
        public void RenderWeaponSelectionContent(int x, int y, int width, int height, List<StartingWeapon> weapons)
        {
            currentLineCount = weaponSelectionRenderer.RenderWeaponSelectionContent(x, y, width, height, weapons);
        }
    }
}

