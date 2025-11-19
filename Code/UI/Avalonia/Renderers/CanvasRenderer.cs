using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Refactored centralized canvas renderer that coordinates all rendering operations
    /// Uses specialized renderers for different screen types and functionalities
    /// </summary>
    public class CanvasRenderer : ICanvasRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasInteractionManager interactionManager;
        
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
            
            // Initialize core specialized renderers
            this.menuRenderer = new MenuRenderer(canvas, interactionManager.ClickableElements, textManager, interactionManager);
            this.inventoryRenderer = new InventoryRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            this.combatRenderer = new CombatRenderer(canvas, new Renderers.ColoredTextWriter(canvas));
            this.dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            
            // Initialize new specialized renderers
            this.messageRenderer = new MessageDisplayRenderer(canvas);
            this.helpRenderer = new HelpSystemRenderer(canvas);
            this.characterCreationRenderer = new CharacterCreationRenderer(canvas, textManager, interactionManager);
            this.dungeonExplorationRenderer = new DungeonExplorationRenderer(canvas, interactionManager);
        }

        public void RenderDisplayBuffer(CanvasContext context)
        {
            canvas.Clear();
            textManager.RenderDisplayBufferFallback();
            canvas.Refresh();
        }

        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            // Use persistent layout with no character (null) to show blank panels
            RenderWithLayout(null, "MAIN MENU", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderMainMenuContent(contentX, contentY, contentWidth, contentHeight, hasSavedGame, characterName, characterLevel);
            }, new CanvasContext());
        }

        public void RenderInventory(Character character, List<Item> inventory, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderInventory(contentX, contentY, contentWidth, contentHeight, character, inventory);
            }, context);
        }

        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character, inventory, promptMessage, actionType);
            }, context);
        }

        public void RenderSlotSelectionPrompt(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderSlotSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character);
            }, context);
        }

        public void RenderWeaponSelection(List<StartingWeapon> weapons, CanvasContext context)
        {
            RenderWithLayout(null, "WEAPON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderWeaponSelectionContent(contentX, contentY, contentWidth, contentHeight, weapons);
            }, context);
        }

        public void RenderCharacterCreation(Character character, CanvasContext context)
        {
            characterCreationRenderer.RenderCharacterCreation(character, context);
        }

        public void RenderSettings()
        {
            menuRenderer.RenderSettings();
        }

        public void RenderTestingMenu()
        {
            // Use the 3-panel layout like other game screens
            RenderWithLayout(null, "COMPREHENSIVE GAME SYSTEM TESTS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderTestingMenu(contentX, contentY, contentWidth, contentHeight);
            }, new CanvasContext());
        }

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons, CanvasContext context)
        {
            RenderWithLayout(player, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonSelection(contentX, contentY, contentWidth, contentHeight, dungeons);
            }, context);
        }

        public void RenderDungeonStart(Dungeon dungeon, Character player, CanvasContext context)
        {
            RenderWithLayout(player, $"ENTERING DUNGEON: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonStart(contentX, contentY, contentWidth, contentHeight, dungeon);
            }, context);
        }

        public void RenderRoomEntry(Environment room, Character player, string? dungeonName, CanvasContext context)
        {
            RenderWithLayout(player, $"ENTERING ROOM: {room.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderRoomEntry(contentX, contentY, contentWidth, contentHeight, room);
            }, context);
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog, CanvasContext context)
        {
            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                combatRenderer.RenderCombat(contentX, contentY, contentWidth, contentHeight, player, enemy, combatLog);
            }, context, enemy, null, null);
        }

        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName, string? roomName, CanvasContext context)
        {
            RenderWithLayout(player, "PREPARING FOR COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                combatRenderer.RenderEnemyEncounter(contentX, contentY, contentWidth, contentHeight, dungeonLog);
            }, context, enemy, dungeonName, roomName);
        }

        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative, string? dungeonName, string? roomName, CanvasContext context)
        {
            RenderWithLayout(player, "COMBAT RESULT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                combatRenderer.RenderCombatResult(contentX, contentY, contentWidth, contentHeight, playerSurvived, enemy, battleNarrative);
            }, context, enemy, dungeonName, roomName);
        }

        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName, CanvasContext context)
        {
            RenderWithLayout(player, $"ROOM CLEARED: {room.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderRoomCompletion(contentX, contentY, contentWidth, contentHeight, room, player);
            }, context);
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, CanvasContext context)
        {
            RenderWithLayout(player, $"DUNGEON COMPLETED: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonCompletion(contentX, contentY, contentWidth, contentHeight, dungeon, player, xpGained, lootReceived);
            }, context);
        }

        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents, CanvasContext context)
        {
            dungeonExplorationRenderer.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents, context);
        }

        public void RenderGameMenu(Character player, List<Item> inventory, CanvasContext context)
        {
            RenderWithLayout(player, $"WELCOME, {player.Name.ToUpper()}!", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderGameMenu(contentX, contentY, contentWidth, contentHeight);
            }, context);
        }

        // Message display methods - delegated to MessageDisplayRenderer
        public void ShowMessage(string message, Color color = default)
        {
            messageRenderer.ShowMessage(message, color);
        }

        public void ShowError(string error)
        {
            messageRenderer.ShowError(error);
        }

        public void ShowError(string error, string suggestion = "")
        {
            messageRenderer.ShowError(error, suggestion);
        }

        public void ShowSuccess(string message)
        {
            messageRenderer.ShowSuccess(message);
        }

        public void ShowLoadingAnimation(string message = "Loading...")
        {
            messageRenderer.ShowLoadingAnimation(message);
        }

        public void UpdateStatus(string message)
        {
            messageRenderer.UpdateStatus(message);
        }

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

        public void Refresh()
        {
            canvas.Refresh();
        }

        #region Private Helper Methods

        private void RenderWithLayout(Character? character, string title, Action<int, int, int, int> renderContent, CanvasContext context)
        {
            RenderWithLayout(character, title, renderContent, context, null, null, null);
        }

        private void RenderWithLayout(Character? character, string title, Action<int, int, int, int> renderContent, CanvasContext context, Enemy? enemy, string? dungeonName, string? roomName)
        {
            interactionManager.ClearClickableElements();
            
            // Use the persistent layout manager for proper three-panel layout
            var layoutManager = new PersistentLayoutManager(canvas);
            layoutManager.RenderLayout(character, renderContent, title, enemy, dungeonName, roomName);
        }

        #endregion
    }
}
