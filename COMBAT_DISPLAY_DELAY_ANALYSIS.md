# Combat Display Delay Analysis

## Current Flow

1. **Combat Loop** (`CombatManager.RunCombat()`)
   - Loops through combat actions
   - Calls `CombatTurnHandlerSimplified.ProcessPlayerTurn()` / `ProcessEnemyTurn()`
   - Calls `CombatDelayManager.DelayAfterAction()` (which returns immediately for GUI)

2. **Action Processing** (`CombatTurnHandlerSimplified`)
   - Executes action via `ActionExecutor`
   - Calls `TextDisplayIntegration.DisplayCombatAction()`

3. **Display Queueing** (`BlockDisplayManager.DisplayActionBlock()`)
   - Collects all messages (action, roll info, status effects, narratives)
   - Calls `WriteColoredSegmentsBatch()` with `delayAfterBatchMs`

4. **Message Batching** (`CenterPanelDisplayManager.AddMessageBatch()`)
   - Adds all messages to buffer
   - Schedules a render with a delay timer (non-blocking)
   - Returns immediately

## The Problem

The combat loop doesn't wait for the display delay to complete. It immediately processes the next action, causing multiple action blocks to be queued at once. Even though there's a delay timer, the next action is already queued before the previous one finishes displaying.

## Solution

We need to make the combat loop wait for each action's display to complete before processing the next action. This requires:

1. Making `AddMessageBatch` return a `Task` that completes when the delay/display is done
2. Making the combat loop `await` this task before processing the next action
3. Ensuring proper async/await flow throughout the display chain

