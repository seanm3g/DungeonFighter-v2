using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat;
using RPGGame.Combat.Narrative;
using RPGGame.Combat.Sequence;
using RPGGame.Combat.Turn;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Simplified combat turn handler using generic processors to eliminate duplication
    /// Replaces the original CombatTurnHandler with cleaner, more maintainable code
    /// </summary>
    public class CombatTurnHandlerSimplified
    {
        private readonly CombatStateManager stateManager;

        public CombatTurnHandlerSimplified(CombatStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Processes a single player turn using the new ActionSelector system
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ProcessPlayerTurnAsync(
            Character player,
            Enemy currentEnemy,
            Environment room,
            Action? forcedAction = null,
            TrainingGroundTutorialScript? tutorialScript = null)
        {
            // Check if player is stunned
            if (player.StunTurnsRemaining > 0)
            {
                await StunProcessor.ProcessStunnedEntityAsync(player, stateManager);
            }
            else
            {
                // Use the new ActionSelector system for action selection and execution
                int comboStepBefore = player.ComboStep;
                ArmFreeComboActions(player, tutorialScript);
                await ProcessPlayerActionAsync(player, currentEnemy, room, forcedAction, tutorialScript);
                await RunFreeComboActionsAsync(player, player, currentEnemy, comboStepBefore, tutorialScript,
                    () => ProcessPlayerActionAsync(player, currentEnemy, room, forcedAction: null, tutorialScript: null, freeAction: true));
            }

            StatusEffectProcessor.ProcessBleedAfterActorResolvedTurn(player);
            
            // Process health regeneration for player after they act
            ProcessPlayerHealthRegeneration(player);

            // Bleed/DoT may finish the foe after the swing display — still prefetch before combat teardown delays.
            TryPrefetchVictoryLimerick(player, currentEnemy, room);
            
            // Return false if enemy is dead (combat should end)
            return currentEnemy.IsAlive;
        }

        /// <summary>
        /// Processes a single enemy turn using the new ActionSelector system
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ProcessEnemyTurnAsync(
            Character player,
            Enemy currentEnemy,
            Environment room,
            Action? forcedAction = null,
            TrainingGroundTutorialScript? tutorialScript = null)
        {
            // Check if enemy is stunned
            if (currentEnemy.StunTurnsRemaining > 0)
            {
                await StunProcessor.ProcessStunnedEntityAsync(currentEnemy, stateManager);
            }
            else
            {
                int comboStepBefore = currentEnemy.ComboStep;
                ArmFreeComboActions(currentEnemy, tutorialScript);
                await ProcessEnemyActionAsync(player, currentEnemy, room, forcedAction, tutorialScript);
                await RunFreeComboActionsAsync(currentEnemy, player, currentEnemy, comboStepBefore, tutorialScript,
                    () => ProcessEnemyActionAsync(player, currentEnemy, room, forcedAction: null, tutorialScript: null, freeAction: true));
            }

            StatusEffectProcessor.ProcessBleedAfterActorResolvedTurn(currentEnemy);

            // Return false if player is dead (combat should end)
            if (!player.IsAlive)
            {
                return false;
            }

            // Process poison and burn damage after enemy's turn (bleed is per-actor on their own turn)
            { long profT = RPGGame.Diagnostics.ToolProfiler.Start(); stateManager.ProcessDamageOverTimeEffects(player, currentEnemy); RPGGame.Diagnostics.ToolProfiler.Stop("Turn.DoT", profT); }

            return true;
        }

        /// <summary>Safety cap on free swings in one turn; the strip-pass rule normally stops the chain sooner.</summary>
        private const int MaxFreeComboActionsPerTurn = 20;

        /// <summary>
        /// A swing that succeeds on its combo roll advances the strip and earns a free action: the next slot rolls right away.
        /// The whole chain costs avg swing × (1 + ComboChainGrowth × (actions − 1)). The chain keeps going while each roll succeeds and stops on a failed roll, at the end of
        /// the strip (the step wraps to slot 0), on a stun, or when either side falls. Training Ground scripts keep one swing per turn.
        /// </summary>
        private static async System.Threading.Tasks.Task RunFreeComboActionsAsync(
            Character actor,
            Character player,
            Enemy currentEnemy,
            int comboStepBefore,
            TrainingGroundTutorialScript? tutorialScript,
            Func<System.Threading.Tasks.Task> freeSwing)
        {
            if (!FreeComboActionsAllowed(tutorialScript))
                return;
            int stripLength = actor.GetComboActions().Count;
            for (int free = 0; free < stripLength - 1 && free < MaxFreeComboActionsPerTurn; free++)
            {
                if (!EarnedFreeComboAction(actor, comboStepBefore, stripLength))
                    return;
                if (!player.IsAlive || !currentEnemy.IsAlive || actor.StunTurnsRemaining > 0)
                    return;
                if (!CombatManager.DisableCombatUIOutput)
                    BlockDisplayManager.DisplaySystemBlock(CombatFlowColoredText.FormatFreeComboActionColored(actor.Name));
                comboStepBefore = actor.ComboStep;
                CombatTurnQueue.BeginFreeAction(actor);
                await freeSwing();
            }
        }

        /// <summary>Combo growth g: chain costs avg × (1 + g × (actions − 1)) (see ComboSystemConfig.ComboChainGrowth).</summary>
        private static double ComboChainGrowth
            => GameConfiguration.Instance.ComboSystem?.ComboChainGrowth ?? 0.25;

        private static bool FreeComboActionsAllowed(TrainingGroundTutorialScript? tutorialScript)
            => tutorialScript == null && (GameConfiguration.Instance.ComboSystem?.FreeActionOnComboHit ?? true);

        /// <summary>Lets the combat board's turn queue list a free combo action once the turn's swing earns one.</summary>
        private static void ArmFreeComboActions(Character actor, TrainingGroundTutorialScript? tutorialScript)
        {
            if (FreeComboActionsAllowed(tutorialScript))
                CombatTurnQueue.ArmFreeActions(actor);
        }

        /// <summary>True when the last swing moved the strip forward without wrapping back to its first slot.</summary>
        private static bool EarnedFreeComboAction(Character actor, int comboStepBefore, int stripLength)
            => stripLength > 1 && actor.ComboStep > comboStepBefore && actor.ComboStep % stripLength != 0;

        /// <summary>Runs one enemy swing; a <paramref name="freeAction"/> swing only adds what raises the chain's cost.</summary>
        private async System.Threading.Tasks.Task ProcessEnemyActionAsync(
            Character player,
            Enemy currentEnemy,
            Environment room,
            Action? forcedAction,
            TrainingGroundTutorialScript? tutorialScript,
            bool freeAction = false)
        {
            {
                var tutorialApplied = ApplyTrainingGroundTutorialEvent(currentEnemy, tutorialScript, player);
                try
                {
                    // Use the new ActionExecutor system for consistent action handling with ColoredText
                    var ((actionText, rollInfo), statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                        currentEnemy, player, forcedAction, room,
                        stateManager.GetLastPlayerAction(),
                        stateManager.GetCurrentBattleNarrative());
                
                    bool textDisplayed = actionText != null && actionText.Count > 0;
                
                // Get the action that was actually used for turn counting
                    var usedAction = ActionExecutor.GetLastUsedAction(currentEnemy);
                    string actionName = usedAction?.Name ?? "UNKNOWN ACTION";
                
                // Record the action for turn counting
                    bool newTurn = stateManager.RecordAction(currentEnemy.Name, actionName);
                    if (newTurn && !CombatManager.DisableCombatUIOutput)
                    {
                        // Turn separator line removed for cleaner combat logs
                    }
                
                // Consume per-swing BattleNarrative so FIFO flavor cannot leak onto later turns.
                // Overview combat log shows action blocks only (no flavor); F7 is the prose path.
                    var battleNarrative = stateManager.GetCurrentBattleNarrative();
                    if (textDisplayed && actionText != null && rollInfo != null)
                    {
                        { long profN = RPGGame.Diagnostics.ToolProfiler.Start(); battleNarrative?.GetTriggeredNarrativesIfSignificant(); RPGGame.Diagnostics.ToolProfiler.Stop("Turn.Narrative", profN); }
                        { long profD = RPGGame.Diagnostics.ToolProfiler.Start(); await TextDisplayIntegration.DisplayCombatActionAsync(actionText, rollInfo, statusEffects, null, player); RPGGame.Diagnostics.ToolProfiler.Stop("Turn.Display", profD); }
                    }
                
                    // Update enemy's action timing in the action speed system
                    // CRITICAL: Always update action speed system, even if no action was selected
                    // This prevents infinite loops when ActionSelector returns null. Free combo actions extend the chain's cost.
                    var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                    if (actionSpeedSystem != null && freeAction)
                    {
                        if (usedAction != null)
                            actionSpeedSystem.ExecuteChainedAction(currentEnemy, usedAction,
                                ActionExecutor.GetLastCriticalMissStatus(currentEnemy), ComboChainGrowth);
                    }
                    else if (actionSpeedSystem != null)
                    {
                        if (usedAction != null)
                        {
                            bool isCriticalMiss = ActionExecutor.GetLastCriticalMissStatus(currentEnemy);
                            actionSpeedSystem.ExecuteAction(currentEnemy, usedAction, isCriticalMiss: isCriticalMiss);
                        }
                        else
                        {
                            // No action was selected (empty ActionPool or stunned) - still advance turn to prevent infinite loop
                            DebugLogger.WriteCombatDebug("CombatTurnHandlerSimplified", $"Enemy {currentEnemy.Name} had no action selected - advancing turn to prevent infinite loop");
                            actionSpeedSystem.AdvanceEntityTurn(currentEnemy, 1.0);
                        }
                    }
                }
                finally
                {
                    if (tutorialApplied)
                    {
                        Dice.ClearAsyncForcedD20Rolls();
                        Dice.ClearAsyncForcedLuckD20Rolls();
                        TutorialCoach.DisarmSwing();
                    }
                }
            }
        }

        /// <summary>Sequence beats the board shows a room's quiet roll (card slide plus a read) before moving on.</summary>
        private const int QuietRoomBoardBeats = 3;

        /// <summary>
        /// Processes a single environment turn and awaits combat log display completion.
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ProcessEnvironmentTurnAsync(Character player, Enemy currentEnemy, Environment room)
        {
            if (room.ShouldEnvironmentAct())
            {
                var envAction = room.SelectAction();
                if (envAction != null)
                {
                    await RunEnvironmentActionAsync(player, currentEnemy, room, envAction);

                    // Update environment's action timing in the action speed system
                    var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                    if (actionSpeedSystem != null)
                    {
                        actionSpeedSystem.ExecuteAction(room, envAction);
                    }

                    // Return false if player is dead (combat should end)
                    if (!player.IsAlive)
                    {
                        return false;
                    }
                }
            }
            else
            {
                // On-screen fights: the quiet roll plays the same d20 sequence as a swing (the die tumbles to the room's
                // face, the quiet branch lights, its card slides into NOW). Without the HUD, hold the quiet card a moment.
                // A room that won't act this fight rolls nothing and just passes.
                bool rolled = room.LastStrikeRoll > 0;
                if (rolled && CombatTurnQueue.IsBound && CombatSequencePresenter.ShouldPlay())
                {
                    CombatSequencePresenter.SetPending(CombatSequenceBuilder.FromEnvironmentalQuiet(room));
                    await CombatSequencePresenter.PlayPendingAsync();
                }
                else if (rolled && CombatTurnQueue.IsBound)
                {
                    CombatTurnQueue.SetActingQuiet();
                    UI.Avalonia.Layout.CombatBoardAnimation.RequestFrame();
                    for (int i = 0; i < QuietRoomBoardBeats; i++)
                        await CombatDelayManager.DelayAfterSequenceHudBeatAsync();
                }

                // Environment doesn't want to act, but we still need to advance its turn
                // to prevent it from getting stuck in the action speed system
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    // Advance the environment's turn by a small amount to prevent infinite loops
                    actionSpeedSystem.AdvanceEntityTurn(room, 1.0);
                }
            }
            
            return true;
        }

        /// <summary>
        /// When the swing that just finished rolled the room's <see cref="Environment.TriggerRoll"/>, the room takes a
        /// bonus environmental action now. It doesn't use the room's turn, its action limit, or its speed timing.
        /// </summary>
        /// <returns>True when a bonus action ran.</returns>
        public async System.Threading.Tasks.Task<bool> ProcessRoomRollTriggerAsync(Character player, Enemy currentEnemy, Environment room)
        {
            if (!room.TryConsumeRollTrigger(out var rolledBy) || rolledBy == null)
                return false;
            if (!player.IsAlive || !currentEnemy.IsAlive)
                return false;
            var envAction = room.SelectAction();
            if (envAction == null)
                return false;

            if (!CombatManager.DisableCombatUIOutput)
                BlockDisplayManager.DisplaySystemBlock(
                    CombatFlowColoredText.FormatRoomRollTriggerColored(rolledBy.Name, room.Name, room.TriggerRoll));
            await RunEnvironmentActionAsync(player, currentEnemy, room, envAction);
            return true;
        }

        /// <summary>Runs <paramref name="envAction"/> on everyone in the room, records it, and awaits its combat log block.</summary>
        private async System.Threading.Tasks.Task RunEnvironmentActionAsync(Character player, Enemy currentEnemy, Environment room, Action envAction)
        {
            CombatTurnQueue.SetActingAction(envAction);
            // Create list of all characters in the room (player and current enemy)
            var allTargets = new List<Actor> { player, currentEnemy };

            // Use structured environmental action execution
            var ((actionText, rollInfo), statusEffects) = EnvironmentalActionHandler.ExecuteEnvironmentalActionStructured(room, allTargets, envAction, room);
            bool textDisplayed = actionText != null && actionText.Count > 0;

            // Add environmental action to narrative system (convert to string for backward compatibility)
            var battleNarrative = stateManager.GetCurrentBattleNarrative();
            if (battleNarrative != null && textDisplayed && actionText != null)
            {
                string resultString = UI.ColorSystem.ColoredTextRenderer.RenderAsPlainText(actionText);
                if (rollInfo != null && rollInfo.Count > 0)
                {
                    resultString += "\n" + UI.ColorSystem.ColoredTextRenderer.RenderAsPlainText(rollInfo);
                }
                foreach (var effect in statusEffects)
                {
                    if (effect != null && effect.Count > 0)
                    {
                        resultString += "\n" + UI.ColorSystem.ColoredTextRenderer.RenderAsPlainText(effect);
                    }
                }
                battleNarrative.AddEnvironmentalAction(resultString);
            }

            // Record the environmental action for turn counting
            stateManager.RecordAction(room.Name, envAction.Name);

            // Display environmental action result using unified action block system (await so next turn waits)
            if (textDisplayed && actionText != null)
            {
                // Use empty list if rollInfo is null (DisplayActionBlock expects non-nullable rollInfo)
                var rollInfoToDisplay = rollInfo ?? new List<UI.ColorSystem.ColoredText>();
                await BlockDisplayManager.DisplayActionBlockAsync(actionText, rollInfoToDisplay, statusEffects, null, null, null, TextSpacingSystem.BlockType.EnvironmentalAction);
            }
        }

        /// <summary>
        /// Synchronous wrapper kept for callers that cannot await. Prefer <see cref="ProcessEnvironmentTurnAsync"/>.
        /// </summary>
        public bool ProcessEnvironmentTurn(Character player, Enemy currentEnemy, Environment room)
        {
            return ProcessEnvironmentTurnAsync(player, currentEnemy, room).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Processes a player action using the new ActionSelector system
        /// </summary>
        private async System.Threading.Tasks.Task ProcessPlayerActionAsync(
            Character player,
            Enemy currentEnemy,
            Environment room,
            Action? forcedAction = null,
            TrainingGroundTutorialScript? tutorialScript = null,
            bool freeAction = false)
        {
            var tutorialApplied = ApplyTrainingGroundTutorialEvent(player, tutorialScript, player);
            try
            {
                // Use the new ColoredText system to execute the action
                var ((actionText, rollInfo), statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                    player, currentEnemy, forcedAction, room,
                    stateManager.GetLastPlayerAction(),
                    stateManager.GetCurrentBattleNarrative());

                // Outcome is known before setup/HUD/punchline display — overlap OpenAI with remaining reveal.
                TryPrefetchVictoryLimerick(player, currentEnemy, room);
            
                bool textDisplayed = actionText != null && actionText.Count > 0;
            
            // Get the action that was actually used for turn counting
                var usedAction = ActionExecutor.GetLastUsedAction(player);
                string actionName = usedAction?.Name ?? "UNKNOWN ACTION";
            
            // Record the action for turn counting
                bool newTurn = stateManager.RecordAction(player.Name, actionName);
                if (newTurn && !CombatManager.DisableCombatUIOutput)
                {
                    // Turn separator line removed for cleaner combat logs
                }
            
            // Consume per-swing BattleNarrative so FIFO flavor cannot leak onto later turns.
            // Overview combat log shows action blocks only (no flavor); F7 is the prose path.
                var battleNarrative = stateManager.GetCurrentBattleNarrative();
                if (textDisplayed && actionText != null && rollInfo != null)
                {
                    { long profN = RPGGame.Diagnostics.ToolProfiler.Start(); battleNarrative?.GetTriggeredNarrativesIfSignificant(); RPGGame.Diagnostics.ToolProfiler.Stop("Turn.Narrative", profN); }
                    { long profD = RPGGame.Diagnostics.ToolProfiler.Start(); await TextDisplayIntegration.DisplayCombatActionAsync(actionText, rollInfo, statusEffects, null, player); RPGGame.Diagnostics.ToolProfiler.Stop("Turn.Display", profD); }
                }
                
                // End turn for statistics tracking
                player.EndTurn();
            
            // Update last player action for DEJA VU functionality
                if (usedAction != null)
                {
                    stateManager.UpdateLastPlayerAction(usedAction);
                }
            
            // Update player's action timing in the action speed system
            // CRITICAL: Always update action speed system, even if no action was selected
            // This prevents infinite loops when ActionSelector returns null. Free combo actions extend the chain's cost.
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null && freeAction)
                {
                    if (usedAction != null)
                        actionSpeedSystem.ExecuteChainedAction(player, usedAction,
                            ActionExecutor.GetLastCriticalMissStatus(player), ComboChainGrowth);
                }
                else if (actionSpeedSystem != null)
                {
                    if (usedAction != null)
                    {
                        bool isCriticalMiss = ActionExecutor.GetLastCriticalMissStatus(player);
                        actionSpeedSystem.ExecuteAction(player, usedAction, isCriticalMiss: isCriticalMiss);
                    }
                    else
                    {
                        // No action was selected (empty ActionPool or stunned) - still advance turn to prevent infinite loop
                        DebugLogger.WriteCombatDebug("CombatTurnHandlerSimplified", $"Player {player.Name} had no action selected - advancing turn to prevent infinite loop");
                        actionSpeedSystem.AdvanceEntityTurn(player, 1.0);
                    }
                }
            }
            finally
            {
                if (tutorialApplied)
                {
                    Dice.ClearAsyncForcedD20Rolls();
                    Dice.ClearAsyncForcedLuckD20Rolls();
                    TutorialCoach.DisarmSwing();
                }
            }
        }

        /// <summary>
        /// Queues the next Training Ground forced d20 (and Lucky's second die) for <paramref name="actor"/>, and arms the
        /// beat's coach popups. Tutorial copy is not written under the mechanical action block (overview is action-blocks only).
        /// </summary>
        private static bool ApplyTrainingGroundTutorialEvent(
            Actor actor,
            TrainingGroundTutorialScript? tutorialScript,
            Character hero)
        {
            if (tutorialScript == null)
                return false;

            if (!tutorialScript.TryConsumeForActor(actor, out var tutorialEvent) || tutorialEvent == null)
                return false;

            Dice.QueueAsyncForcedD20Rolls(tutorialEvent.Roll);
            if (tutorialEvent.LuckRoll is int luckRoll)
                Dice.QueueAsyncForcedLuckD20Rolls(luckRoll);
            TutorialCoach.ArmSwing(tutorialEvent.Notes, TutorialCoach.BuildTokens(hero, tutorialEvent, heroRolls: ReferenceEquals(actor, hero)));
            return true;
        }

        /// <summary>
        /// When the foe is already dead and the hero still stands on the dungeon's final room,
        /// start the victory limerick (OpenAI) so it overlaps remaining action display / room teardown.
        /// Delivery happens later on the dungeon completion screen. Safe to call twice.
        /// </summary>
        private void TryPrefetchVictoryLimerick(Character player, Enemy currentEnemy, Environment room)
        {
            if (player == null || currentEnemy == null)
                return;
            if (!player.IsAlive || currentEnemy.IsAlive)
                return;
            if (CombatUiMuteScope.IsMuted || CombatManager.DisableCombatUIOutput)
                return;
            if (!VictoryLimerickService.Instance.IsFinaleRoomPrefetchAllowed)
                return;
            if (VictoryLimerickService.Instance.HasPrefetch)
                return;

            try
            {
                var funTracker = stateManager.GetFunMomentTracker();
                funTracker?.FinalizeCombat(playerWon: true, player.CurrentHealth, player.GetEffectiveMaxHealth());

                string location = room?.Name ?? "the dungeon";
                var packet = BattleLimerickPacketBuilder.Build(
                    player,
                    currentEnemy,
                    location,
                    heroWon: true,
                    funTracker,
                    stateManager.GetCurrentBattleNarrative());

                VictoryLimerickService.Instance.BeginPrefetch(packet);
            }
            catch (Exception ex)
            {
                DebugLogger.WriteCombatDebug("CombatTurnHandlerSimplified",
                    $"Victory limerick prefetch failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes health regeneration for the player
        /// </summary>
        private void ProcessPlayerHealthRegeneration(Character player)
        {
            int playerHealthRegen = player.GetEquipmentHealthRegenBonus();
            if (playerHealthRegen > 0 && player.CurrentHealth < player.GetEffectiveMaxHealth())
            {
                int oldHealth = player.CurrentHealth;
                // Use negative damage to heal (TakeDamage with negative value heals)
                player.TakeDamage(-playerHealthRegen);
                // Cap at max health
                if (player.CurrentHealth > player.GetEffectiveMaxHealth())
                {
                    player.TakeDamage(player.CurrentHealth - player.GetEffectiveMaxHealth());
                }
                int actualRegen = player.CurrentHealth - oldHealth;
                if (actualRegen > 0 && !CombatManager.DisableCombatUIOutput)
                {
                    // Use new ColoredText system for health regeneration
                    var coloredText = CombatFlowColoredText.FormatHealthRegenerationColored(
                        player.Name, actualRegen, player.CurrentHealth, player.GetEffectiveMaxHealth());
                    BlockDisplayManager.DisplaySystemBlock(coloredText);
                    UIManager.WriteLine(""); // Add blank line after regeneration message
                }
            }
        }
    }
}

