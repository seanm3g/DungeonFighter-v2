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
        private readonly DeveloperMenuRenderer developerMenuRenderer;
        private readonly BattleStatisticsRenderer battleStatisticsRenderer;
        private readonly VariableEditorRenderer variableEditorRenderer;
        private readonly ActionEditorRenderer actionEditorRenderer;
        private readonly CreateActionFormRenderer createActionFormRenderer;
        private readonly ActionDetailRenderer actionDetailRenderer;
        
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
            this.developerMenuRenderer = new DeveloperMenuRenderer(canvas, clickableElements, textManager);
            this.battleStatisticsRenderer = new BattleStatisticsRenderer(canvas, clickableElements, textManager);
            this.variableEditorRenderer = new VariableEditorRenderer(canvas, clickableElements, textManager);
            this.actionEditorRenderer = new ActionEditorRenderer(canvas, clickableElements, textManager);
            this.createActionFormRenderer = new CreateActionFormRenderer(canvas, clickableElements, textManager);
            this.actionDetailRenderer = new ActionDetailRenderer(canvas, clickableElements, textManager);
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
            // Use the persistent layout system for consistent 3-panel design
            var layoutManager = new PersistentLayoutManager(canvas);
            
            // Get saved character info for display
            var (characterName, characterLevel) = CharacterSaveManager.GetSavedCharacterInfo();
            bool hasSavedCharacter = characterName != null;
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
            // Ensure canvas is refreshed to display the settings menu
            canvas.Refresh();
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

        /// <summary>
        /// Renders the developer menu content
        /// </summary>
        public void RenderDeveloperMenuContent(int x, int y, int width, int height)
        {
            currentLineCount = developerMenuRenderer.RenderDeveloperMenuContent(x, y, width, height);
        }

        /// <summary>
        /// Renders the battle statistics menu content
        /// </summary>
        public void RenderBattleStatisticsMenuContent(int x, int y, int width, int height, BattleStatisticsRunner.StatisticsResult? results, bool isRunning)
        {
            currentLineCount = battleStatisticsRenderer.RenderBattleStatisticsMenuContent(x, y, width, height, results, isRunning);
        }

        /// <summary>
        /// Renders battle statistics results
        /// </summary>
        public void RenderBattleStatisticsResultsContent(int x, int y, int width, int height, BattleStatisticsRunner.StatisticsResult results)
        {
            currentLineCount = battleStatisticsRenderer.RenderBattleStatisticsResults(x, y, width, height, results);
        }

        public void RenderWeaponTestResultsContent(int x, int y, int width, int height, List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            currentLineCount = battleStatisticsRenderer.RenderWeaponTestResults(x, y, width, height, results);
        }

        public void RenderComprehensiveWeaponEnemyResultsContent(int x, int y, int width, int height, BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            currentLineCount = battleStatisticsRenderer.RenderComprehensiveWeaponEnemyResults(x, y, width, height, results);
        }

        /// <summary>
        /// Renders the variable editor content
        /// </summary>
        public void RenderVariableEditorContent(int x, int y, int width, int height)
        {
            currentLineCount = variableEditorRenderer.RenderVariableEditorContent(x, y, width, height);
        }

        /// <summary>
        /// Renders the action editor content
        /// </summary>
        public void RenderActionEditorContent(int x, int y, int width, int height)
        {
            currentLineCount = actionEditorRenderer.RenderActionEditorContent(x, y, width, height);
        }

        /// <summary>
        /// Renders the action list content
        /// </summary>
        public void RenderActionListContent(int x, int y, int width, int height, List<ActionData> actions, int page)
        {
            currentLineCount = actionEditorRenderer.RenderActionListContent(x, y, width, height, actions, page);
        }

        /// <summary>
        /// Renders the create action form content
        /// </summary>
        public void RenderCreateActionFormContent(int x, int y, int width, int height, ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null)
        {
            currentLineCount = createActionFormRenderer.RenderCreateActionFormContent(x, y, width, height, actionData, currentStep, formSteps, currentInput);
        }

        /// <summary>
        /// Renders the action detail content
        /// </summary>
        public void RenderActionDetailContent(int x, int y, int width, int height, ActionData action)
        {
            currentLineCount = actionDetailRenderer.RenderActionDetailContent(x, y, width, height, action);
        }
    }
}

