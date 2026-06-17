using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Editors;
using RPGGame.Handlers.Inventory;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Renderers.Inventory;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Screen rendering methods that forward to <see cref="CanvasRenderer"/> with context.
    /// </summary>
    public partial class CanvasUICoordinator
    {
        private CanvasContext GetContext() => contextManager.GetCurrentContext();

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            animationManager.StartDungeonSelectionAnimation(player, dungeons);
            var currentRegion = new TravelRegionCatalog().GetRegionForCharacter(player);
            string? regionLabel = string.IsNullOrWhiteSpace(currentRegion.DisplayName) ? null : currentRegion.DisplayName.Trim();
            renderer.RenderDungeonSelection(player, dungeons, GetContext(), dungeonSelectionCustomLevelEntryBuffer, regionLabel);
        }

        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            renderer.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        }

        public void RenderWeaponSelection(List<StartingWeapon> weapons)
        {
            renderer.RenderWeaponSelection(weapons, GetContext());
        }

        public void RenderTrainingGroundOffer(Character player)
        {
            renderer.RenderTrainingGroundOffer(player, GetContext());
        }

        public void RenderPreWeaponPathIntro(Character player)
        {
            renderer.RenderPreWeaponPathIntro(player, GetContext());
        }

        public void RenderCharacterCreation(Character character)
        {
            renderer.RenderCharacterCreation(character, GetContext());
        }

        public void RenderCharacterSelection(List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses)
        {
            renderer.RenderCharacterSelection(characters, activeCharacterName, characterStatuses, GetContext());
        }

        public void RenderLoadCharacterSelection(List<(string characterId, string characterName, int level)> savedCharacters)
        {
            renderer.RenderLoadCharacterSelection(savedCharacters, GetContext());
        }

        public void RenderSettings()
        {
            renderer.RenderSettings();
        }

        public void RenderDeveloperMenu()
        {
            renderer.RenderDeveloperMenu();
        }

        public void RenderVariableEditor(EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null)
        {
            renderer.RenderVariableEditor(selectedVariable, isEditing, currentInput, message);
        }

        public void RenderTuningParametersMenu(string? selectedCategory = null, EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null)
        {
            renderer.RenderTuningParametersMenu(selectedCategory, selectedVariable, isEditing, currentInput, message);
        }

        public void RenderActionEditor()
        {
            renderer.RenderActionEditor();
        }

        public void RenderActionList(List<ActionData> actions, int page)
        {
            renderer.RenderActionList(actions, page);
        }

        public void RenderCreateActionForm(ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false)
        {
            renderer.RenderCreateActionForm(actionData, currentStep, formSteps, currentInput, isEditMode);
        }

        public void RenderActionDetails(ActionData action)
        {
            renderer.RenderActionDetails(action);
        }

        public void RenderDeleteActionConfirmation(ActionData action, string? errorMessage = null)
        {
            renderer.RenderDeleteActionConfirmation(action, errorMessage);
        }

        public void RenderBattleStatisticsMenu(BattleStatisticsRunner.StatisticsResult? results, bool isRunning)
        {
            renderer.RenderBattleStatisticsMenu(results, isRunning);
        }

        public void RenderBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results)
        {
            renderer.RenderBattleStatisticsResults(results);
        }

        public void RenderWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            renderer.RenderWeaponTestResults(results);
        }

        public void RenderComprehensiveWeaponEnemyResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            renderer.RenderComprehensiveWeaponEnemyResults(results);
        }

        public void AdjustInventoryItemScroll(int itemDelta)
        {
            inventoryItemScrollOffset = Math.Max(0, inventoryItemScrollOffset + itemDelta);
        }

        public void ResetInventoryItemScroll()
        {
            inventoryItemScrollOffset = 0;
        }

        public void RenderInventory(
            Character character,
            List<Item> inventory,
            string? pendingMutatingInventoryMenuAction = null,
            InventoryItemSortMode sortMode = InventoryItemSortMode.InventoryOrder,
            bool hideRequirementBlockedItems = false,
            string? inventoryEquipSlotFilter = null)
        {
            if (lastRenderedScreenState != GameState.Inventory || !ReferenceEquals(lastInventoryScrollCharacter, character))
            {
                inventoryItemScrollOffset = 0;
                lastInventoryScrollCharacter = character;
            }

            inventoryItemScrollOffset = InventoryItemScrollLayout.ClampFirstVisibleIndex(inventoryItemScrollOffset, inventory.Count);
            renderer.RenderInventory(character, inventory, GetContext(), pendingMutatingInventoryMenuAction, inventoryItemScrollOffset, sortMode, hideRequirementBlockedItems, inventoryEquipSlotFilter);
        }

        public void RenderItemSelectionPrompt(
            Character character,
            List<Item> inventory,
            string promptMessage,
            string actionType,
            InventoryItemSortMode sortMode = InventoryItemSortMode.InventoryOrder,
            bool hideRequirementBlockedItems = false,
            string? inventoryEquipSlotFilter = null)
        {
            renderer.RenderItemSelectionPrompt(
                character,
                inventory,
                promptMessage,
                actionType,
                GetContext(),
                sortMode,
                hideRequirementBlockedItems,
                inventoryEquipSlotFilter);
        }

        public void RenderSlotSelectionPrompt(Character character)
        {
            renderer.RenderSlotSelectionPrompt(character, GetContext());
        }

        public void RenderRaritySelectionPrompt(Character character, List<IGrouping<string, Item>> rarityGroups)
        {
            renderer.RenderRaritySelectionPrompt(character, rarityGroups, GetContext());
        }

        public void RenderTradeUpPreview(Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity)
        {
            renderer.RenderTradeUpPreview(character, itemsToTrade, resultingItem, currentRarity, nextRarity, GetContext());
        }

        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot, int newItemInventoryIndex = -1)
        {
            renderer.RenderItemComparison(character, newItem, currentItem, slot, GetContext(), newItemInventoryIndex);
        }

        public void RenderComboManagement(Character character)
        {
            renderer.RenderComboManagement(character, GetContext());
        }

        public void RenderComboReorderPrompt(Character character, string currentSequence)
        {
            renderer.RenderComboReorderPrompt(character, currentSequence, GetContext());
        }

        public void RenderComboActionSelection(Character character, string actionType)
        {
            renderer.RenderComboActionSelection(character, actionType, GetContext());
        }

        public void RenderGameMenu(Character player, List<Item> inventory)
        {
            renderer.RenderGameMenu(player, inventory, GetContext());
        }

        public void RenderRegionTravel(Character player, IReadOnlyList<TravelRegion> destinations, TravelRouteResult? routeResult)
        {
            renderer.RenderRegionTravel(player, destinations, routeResult, GetContext());
        }

        /// <inheritdoc cref="CanvasRenderer.RefreshActionInfoStripOnly(Character?, bool)"/>
        public void RefreshActionInfoStripOnly(Character player, bool drawHoverDetailOverlay = true)
        {
            renderer.RefreshActionInfoStripOnly(player, drawHoverDetailOverlay);
        }

        public void RenderCharacterInfoScreen(Character player)
        {
            renderer.RenderCharacterInfoScreen(player, GetContext());
        }

        public void RenderDungeonStart(Dungeon dungeon, Character player)
        {
            renderer.RenderDungeonStart(dungeon, player, GetContext());
        }

        public void RenderRoomEntry(Environment room, Character player, string? dungeonName, int? startFromBufferIndex = null)
        {
            renderer.RenderRoomEntry(room, player, dungeonName, GetContext(), startFromBufferIndex);
        }

        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName, string? roomName)
        {
            renderer.RenderEnemyEncounter(enemy, player, dungeonLog, dungeonName, roomName, GetContext());
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog)
        {
            renderer.RenderCombat(player, enemy, combatLog, GetContext());
            if (stateManager?.CurrentState == GameState.ActionInteractionLab && ActionInteractionLabSession.Current != null)
                ActionLabControlsWindow.RefreshIfOpen();
        }

        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative, string? dungeonName, string? roomName)
        {
            renderer.RenderCombatResult(playerSurvived, player, enemy, battleNarrative, dungeonName, roomName, GetContext());
        }

        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName)
        {
            renderer.RenderRoomCompletion(room, player, dungeonName, GetContext());
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun)
        {
            _dungeonCompletionRenderDungeon = dungeon;
            _dungeonCompletionRenderPlayer = player;
            _dungeonCompletionRenderXpGained = xpGained;
            _dungeonCompletionRenderLoot = lootReceived;
            _dungeonCompletionRenderLevelUps = levelUpInfos ?? new List<LevelUpInfo>();
            _dungeonCompletionRenderItemsFound = itemsFoundDuringRun ?? new List<Item>();
            renderer.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived, _dungeonCompletionRenderLevelUps, _dungeonCompletionRenderItemsFound, GetContext());
        }

        public void RenderDeathScreen(Character player, string defeatSummary)
        {
            _deathRenderPlayer = player;
            _deathRenderSummary = defeatSummary;
            renderer.RenderDeathScreen(player, defeatSummary, GetContext());
        }

        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents)
        {
            renderer.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents, GetContext());
        }
    }
}
