using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Coordinators
{
    /// <summary>
    /// Specialized coordinator for handling screen rendering operations
    /// </summary>
    public class ScreenRenderingCoordinator
    {
        private readonly CanvasRenderer renderer;
        private readonly ICanvasContextManager contextManager;
        private readonly ICanvasAnimationManager animationManager;
        private readonly ICanvasTextManager textManager;
        
        public ScreenRenderingCoordinator(CanvasRenderer renderer, ICanvasContextManager contextManager, ICanvasAnimationManager animationManager, ICanvasTextManager textManager)
        {
            this.renderer = renderer;
            this.contextManager = contextManager;
            this.animationManager = animationManager;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the main menu
        /// </summary>
        public void RenderMainMenu()
        {
            RenderMainMenu(false, null, 0);
        }
        
        /// <summary>
        /// Renders the main menu with character info
        /// </summary>
        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            renderer.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        }
        
        /// <summary>
        /// Renders the inventory screen
        /// </summary>
        public void RenderInventory(Character character, List<Item> inventory)
        {
            renderer.RenderInventory(character, inventory, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders item selection prompt
        /// </summary>
        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            renderer.RenderItemSelectionPrompt(character, inventory, promptMessage, actionType, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders slot selection prompt
        /// </summary>
        public void RenderSlotSelectionPrompt(Character character)
        {
            renderer.RenderSlotSelectionPrompt(character, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders combat screen
        /// </summary>
        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog)
        {
            renderer.RenderCombat(player, enemy, combatLog, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders weapon selection screen
        /// </summary>
        public void RenderWeaponSelection(List<StartingWeapon> weapons)
        {
            renderer.RenderWeaponSelection(weapons, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders character creation screen
        /// </summary>
        public void RenderCharacterCreation(Character character)
        {
            renderer.RenderCharacterCreation(character, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders settings screen
        /// </summary>
        public void RenderSettings()
        {
            renderer.RenderSettings();
        }
        
        /// <summary>
        /// Renders testing menu screen
        /// </summary>
        public void RenderTestingMenu()
        {
            renderer.RenderTestingMenu();
        }
        
        /// <summary>
        /// Renders dungeon selection screen
        /// </summary>
        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            animationManager.StartDungeonSelectionAnimation(player, dungeons);
            renderer.RenderDungeonSelection(player, dungeons, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Stops dungeon selection animation
        /// </summary>
        public void StopDungeonSelectionAnimation()
        {
            animationManager.StopDungeonSelectionAnimation();
        }
        
        /// <summary>
        /// Renders dungeon start screen
        /// </summary>
        public void RenderDungeonStart(Dungeon dungeon, Character player)
        {
            renderer.RenderDungeonStart(dungeon, player, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders room entry screen
        /// </summary>
        public void RenderRoomEntry(Environment room, Character player, string? dungeonName = null)
        {
            renderer.RenderRoomEntry(room, player, dungeonName, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders enemy encounter screen
        /// </summary>
        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName = null, string? roomName = null)
        {
            renderer.RenderEnemyEncounter(enemy, player, dungeonLog, dungeonName, roomName, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders combat result screen
        /// </summary>
        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative = null, string? dungeonName = null, string? roomName = null)
        {
            renderer.RenderCombatResult(playerSurvived, player, enemy, battleNarrative, dungeonName, roomName, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders room completion screen
        /// </summary>
        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName = null)
        {
            renderer.RenderRoomCompletion(room, player, dungeonName, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders dungeon completion screen
        /// </summary>
        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived)
        {
            renderer.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders death screen with statistics
        /// </summary>
        public void RenderDeathScreen(Character player, string defeatSummary)
        {
            renderer.RenderDeathScreen(player, defeatSummary, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders dungeon exploration screen
        /// </summary>
        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents)
        {
            renderer.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders game menu screen
        /// </summary>
        public void RenderGameMenu(Character player, List<Item> inventory)
        {
            renderer.RenderGameMenu(player, inventory, contextManager.GetCurrentContext());
        }
    }
}

