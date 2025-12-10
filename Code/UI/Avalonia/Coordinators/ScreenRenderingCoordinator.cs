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
        /// Renders item comparison screen
        /// </summary>
        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot)
        {
            renderer.RenderItemComparison(character, newItem, currentItem, slot, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders combo management menu
        /// </summary>
        public void RenderComboManagement(Character character)
        {
            renderer.RenderComboManagement(character, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders combo action selection prompt
        /// </summary>
        public void RenderComboActionSelection(Character character, string actionType)
        {
            renderer.RenderComboActionSelection(character, actionType, contextManager.GetCurrentContext());
        }
        
        /// <summary>
        /// Renders combo reorder prompt
        /// </summary>
        public void RenderComboReorderPrompt(Character character, string currentSequence = "")
        {
            renderer.RenderComboReorderPrompt(character, currentSequence, contextManager.GetCurrentContext());
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
        /// Renders developer menu screen
        /// </summary>
        public void RenderDeveloperMenu()
        {
            renderer.RenderDeveloperMenu();
        }

        /// <summary>
        /// Renders variable editor screen
        /// </summary>
        public void RenderVariableEditor()
        {
            renderer.RenderVariableEditor();
        }

        /// <summary>
        /// Renders action editor screen
        /// </summary>
        public void RenderActionEditor()
        {
            renderer.RenderActionEditor();
        }

        /// <summary>
        /// Renders action list screen
        /// </summary>
        public void RenderActionList(List<ActionData> actions, int page)
        {
            renderer.RenderActionList(actions, page);
        }

        /// <summary>
        /// Renders create action form screen
        /// </summary>
        public void RenderCreateActionForm(ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null)
        {
            renderer.RenderCreateActionForm(actionData, currentStep, formSteps, currentInput);
        }

        /// <summary>
        /// Renders action details screen
        /// </summary>
        public void RenderActionDetails(ActionData action)
        {
            renderer.RenderActionDetails(action);
        }
        
        /// <summary>
        /// Renders dungeon selection screen
        /// </summary>
        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            // Parameters are validated by caller (CanvasUICoordinator), so they're non-null here
            if (animationManager != null)
            {
                animationManager.StartDungeonSelectionAnimation(player!, dungeons!);
            }
            renderer.RenderDungeonSelection(player!, dungeons!, contextManager.GetCurrentContext());
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
        public void RenderRoomEntry(Environment room, Character player, string? dungeonName = null, int? startFromBufferIndex = null)
        {
            renderer.RenderRoomEntry(room, player, dungeonName, contextManager.GetCurrentContext(), startFromBufferIndex);
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
        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos)
        {
            renderer.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), contextManager.GetCurrentContext());
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
            // Ensure character is set in context manager for persistent display
            contextManager.SetCurrentCharacter(player);
            renderer.RenderGameMenu(player, inventory, contextManager.GetCurrentContext());
        }
    }
}

