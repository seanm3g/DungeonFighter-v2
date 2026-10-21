using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.GameCore.Helpers;
using RPGGame.Entity.Services;
using RPGGame.World.DungeonMap;

namespace RPGGame
{

    /// <summary>
    /// Orchestrates the main dungeon flow and room iteration
    /// Extracted from DungeonRunnerManager to separate orchestration logic
    /// </summary>
    public class DungeonOrchestrator
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly DungeonDisplayManager displayManager;
        private readonly RoomProcessor roomProcessor;
        private readonly DungeonRewardManager rewardManager;
        private readonly DungeonExitChoiceHandler? exitChoiceHandler;
        private readonly System.Action<int, Item?, List<LevelUpInfo>, List<Item>>? onDungeonCompleted;
        private readonly System.Action? onDungeonExitedEarly;
        private readonly System.Action? onPreWeaponTrainingCompleteToWeaponSelection;
        private readonly System.Action? onPreWeaponTrainingEarlyExitToOffer;

        public DungeonOrchestrator(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            DungeonDisplayManager displayManager,
            RoomProcessor roomProcessor,
            DungeonRewardManager rewardManager,
            DungeonExitChoiceHandler? exitChoiceHandler,
            System.Action<int, Item?, List<LevelUpInfo>, List<Item>>? onDungeonCompleted = null,
            System.Action? onDungeonExitedEarly = null,
            System.Action? onPreWeaponTrainingCompleteToWeaponSelection = null,
            System.Action? onPreWeaponTrainingEarlyExitToOffer = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.displayManager = displayManager ?? throw new ArgumentNullException(nameof(displayManager));
            this.roomProcessor = roomProcessor ?? throw new ArgumentNullException(nameof(roomProcessor));
            this.rewardManager = rewardManager ?? throw new ArgumentNullException(nameof(rewardManager));
            this.exitChoiceHandler = exitChoiceHandler;
            this.onDungeonCompleted = onDungeonCompleted;
            this.onDungeonExitedEarly = onDungeonExitedEarly;
            this.onPreWeaponTrainingCompleteToWeaponSelection = onPreWeaponTrainingCompleteToWeaponSelection;
            this.onPreWeaponTrainingEarlyExitToOffer = onPreWeaponTrainingEarlyExitToOffer;
        }

        /// <summary>
        /// Run the entire dungeon
        /// </summary>
        public async Task RunDungeon()
        {
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null)
            {
                DungeonErrorHandler.HandleMissingComponents(stateManager, customUIManager);
                return;
            }
            
            // Validate dungeon has been generated and has rooms
            if (stateManager.CurrentDungeon.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
            {
                // Try to regenerate the dungeon
                try
                {
                    stateManager.CurrentDungeon.Generate();
                }
                catch (Exception ex)
                {
                    DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, $"Failed to generate dungeon: {ex.Message}");
                    return;
                }
                
                if (stateManager.CurrentDungeon?.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
                {
                    DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, "Dungeon generation failed - no rooms created.");
                    return;
                }
            }

            // Ensure traversal map exists (tests / partial Generate paths)
            if (stateManager.CurrentDungeon.TraversalState == null && stateManager.CurrentDungeon.Rooms.Count > 0)
            {
                bool training = PreWeaponTrainingFlow.IsTrainingGroundDungeonName(stateManager.CurrentDungeon.Name);
                stateManager.CurrentDungeon.EnsureTraversalState(trainingGround: training);
            }
            
            // Set game state to Dungeon
            stateManager.TransitionToState(GameState.Dungeon);
            
            if (stateManager.CurrentPlayer != null)
                stateManager.CurrentPlayer.ClearDungeonRunTempEffects();

            // Reload actions from disk and rebuild character's action pool so updated Actions.json is used
            if (stateManager.CurrentPlayer != null)
            {
                ActionLoader.ReloadActions();
                CharacterSerializer.RebuildCharacterActions(stateManager.CurrentPlayer);
            }
            
            // Capture starting inventory to track items found during the run
            if (stateManager.CurrentPlayer != null)
            {
                rewardManager.CaptureStartingInventory(stateManager.CurrentPlayer);
            }
            
            // Restore display buffer rendering in case it was suppressed (e.g., from dungeon selection screen)
            // This ensures dungeon exploration and combat screens work correctly
            if (customUIManager is CanvasUICoordinator canvasUIRestore)
            {
                if (stateManager.CurrentPlayer != null)
                    canvasUIRestore.SwitchDisplayBufferToCharacter(stateManager.CurrentPlayer);
                canvasUIRestore.RestoreDisplayBufferRendering();
            }
            
            // Start dungeon using unified display manager
            // This prepares the dungeon data but doesn't add content to buffer yet
            // Content (including dungeon header) will be added in ProcessRoom() for the first room
            if (stateManager.CurrentPlayer == null)
            {
                DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, "Cannot start dungeon - no player character available.");
                return;
            }
            displayManager.StartDungeon(stateManager.CurrentDungeon, stateManager.CurrentPlayer);
            stateManager.CurrentPlayer.SessionStats.BeginDungeonRun();

            RPGGame.Combat.Narrative.VictoryLimerickService.Instance.ClearPrefetch();
            RPGGame.Combat.Narrative.VictoryLimerickService.Instance.SetFinaleRoomPrefetchAllowed(false);

            bool tutorialCombatSlowPacing = PreWeaponTrainingFlow.IsPreWeaponTrainingDungeon(
                stateManager,
                stateManager.CurrentDungeon.Name);
            if (tutorialCombatSlowPacing)
                PreWeaponTrainingFlow.SetTutorialCombatSlowPacing(true);
            
            // Process all rooms via automated map traversal
            try
            {
                int totalRooms = stateManager.CurrentDungeon.Rooms.Count;
                bool isFirstRoom = true;
                var traversal = stateManager.CurrentDungeon.TraversalState;

                while (traversal != null && traversal.HasMoreContent)
                {
                    int roomNumber = traversal.NextContentIndex + 1;
                    var room = stateManager.CurrentDungeon.Rooms[traversal.NextContentIndex];

                    // INT d20 picks Smart vs Random for this approach
                    if (stateManager.CurrentPlayer != null)
                    {
                        var resolution = TraversalStrategyResolver.Resolve(stateManager.CurrentPlayer);
                        traversal.CurrentStrategy = resolution.Strategy;
                        // Pathing roll readout (Smart/Random + INT math) intentionally omitted from the combat log for now.
                    }

                    // Auto-walk toward the next content room (pathing / approach outcomes only).
                    // Do not paint per step: the minimap overlay is gone, and full RenderRoomEntry
                    // each step cleared/redrew the side panels (visible right-panel flicker on entry).
                    // RoomProcessor.ProcessRoom paints once on arrival.
                    await DungeonTraversalNavigator.WalkToNextContentAsync(traversal);

                    if (!await roomProcessor.ProcessRoom(room, roomNumber, totalRooms, isFirstRoom))
                    {
                        // Player death: EnemyEncounterHandler already transitions to Death and raises onPlayerDeath.
                        return;
                    }

                    traversal.NextContentIndex++;
                    isFirstRoom = false;
                    
                    // Offer exit choice after every room (except the last room)
                    if (traversal.HasMoreContent)
                    {
                        if (exitChoiceHandler != null)
                        {
                            // Hidden d20 scouting roll on the next room: shapes the report and that room's initiative.
                            RoomScouting.Report? scouting = null;
                            if (stateManager.CurrentPlayer != null)
                            {
                                var nextRoom = stateManager.CurrentDungeon.Rooms[traversal.NextContentIndex];
                                scouting = RoomScouting.Roll(stateManager.CurrentPlayer, nextRoom, stateManager.CurrentDungeon);
                            }
                            bool shouldExit = await exitChoiceHandler.ShowExitChoiceMenu(roomNumber, totalRooms, scouting?.Lines);
                            if (shouldExit && stateManager.CurrentPlayer != null)
                                RoomScouting.Clear(stateManager.CurrentPlayer);
                            if (shouldExit)
                            {
                                // Player chose to leave - exit dungeon without rewards; full heal at hub.
                                int healthRestored = DungeonRunRecovery.RestoreAfterLeavingDungeon(stateManager.CurrentPlayer);
                                
                                if (customUIManager is CanvasUICoordinator canvasUIExit && stateManager.CurrentPlayer != null)
                                {
                                    displayManager.AddCombatEvent("", stateManager.CurrentPlayer);
                                    if (healthRestored > 0)
                                    {
                                        string healingMarkup = ColoredTextRenderer.RenderAsMarkup(
                                            DungeonNarrativeColoredText.FormatHealingMessage(healthRestored));
                                        displayManager.AddCombatEvent(healingMarkup, stateManager.CurrentPlayer, UIMessageType.Environmental);
                                    }
                                    displayManager.AddCombatEvent("You leave the dungeon safely, but receive no rewards.", stateManager.CurrentPlayer, UIMessageType.Environmental);
                                    if (stateManager.CurrentPlayer != null && stateManager.CurrentRoom != null)
                                    {
                                        canvasUIExit.RenderRoomEntry(
                                            stateManager.CurrentRoom, 
                                            stateManager.CurrentPlayer, 
                                            stateManager.CurrentDungeon?.Name);
                                    }
                                    await SimulationPacing.DelayForCombatSpeedAsync(2000);
                                }
                                
                                string? exitingDungeonName = stateManager.CurrentDungeon?.Name;
                                bool returnToTrainingOffer = stateManager.CurrentPlayer != null &&
                                    stateManager.CurrentPlayer.PendingPreWeaponTrainingGround &&
                                    PreWeaponTrainingFlow.IsTrainingGroundDungeonName(exitingDungeonName);

                                RPGGame.Combat.Narrative.VictoryLimerickService.Instance.ClearPrefetch();
                                RPGGame.Combat.Narrative.VictoryLimerickService.Instance.SetFinaleRoomPrefetchAllowed(false);

                                stateManager.SetCurrentDungeon(null!);
                                stateManager.SetCurrentRoom(null!);

                                if (returnToTrainingOffer)
                                {
                                    stateManager.TransitionToState(GameState.TrainingGroundOffer);
                                    onPreWeaponTrainingEarlyExitToOffer?.Invoke();
                                    return;
                                }

                                stateManager.TransitionToState(GameState.GameLoop);
                                
                                // Trigger event to show game loop screen
                                onDungeonExitedEarly?.Invoke();
                                
                                return; // Exit without calling CompleteDungeon()
                            }
                        }
                    }
                }
                
                // Dungeon completed successfully
                var (xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun, skipDungeonCompletionScreen) = await rewardManager.CompleteDungeon();

                // RewardManager already full-heals on a normal clear; ensure cleanup + heal after
                // ClearAllTempEffects / search-buff teardown (and Training Ground skip path).
                DungeonRunRecovery.RestoreAfterLeavingDungeon(stateManager.CurrentPlayer);

                if (skipDungeonCompletionScreen)
                {
                    RPGGame.Combat.Narrative.VictoryLimerickService.Instance.ClearPrefetch();
                    RPGGame.Combat.Narrative.VictoryLimerickService.Instance.SetFinaleRoomPrefetchAllowed(false);
                    stateManager.SetCurrentDungeon(null!);
                    stateManager.SetCurrentRoom(null!);
                    stateManager.TransitionToState(GameState.WeaponSelection);
                    onPreWeaponTrainingCompleteToWeaponSelection?.Invoke();
                    return;
                }
                
                // Transition to completion state
                stateManager.TransitionToState(GameState.DungeonCompletion);
                
                // Trigger event to handle UI display with reward data
                onDungeonCompleted?.Invoke(xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun);
            }
            catch (Exception ex)
            {
                DungeonErrorHandler.HandleException(ex, customUIManager);
                throw;
            }
            finally
            {
                if (tutorialCombatSlowPacing)
                    PreWeaponTrainingFlow.SetTutorialCombatSlowPacing(false);
            }
        }
    }
}

