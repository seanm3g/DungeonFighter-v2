using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Entity.Actions.ComboRouting
{
    /// <summary>
    /// Handles combo flow control (jump, skip, repeat, loop, stop, disable, random)
    /// </summary>
    public class ComboRouter
    {
        /// <summary>
        /// Routing action types
        /// </summary>
        public enum RoutingAction
        {
            None,
            JumpToSlot,
            SkipNext,
            RepeatPrevious,
            LoopToStart,
            StopEarly,
            DisableSlot,
            RandomAction
        }

        /// <summary>
        /// Routes combo execution based on action properties
        /// </summary>
        public static RoutingResult RouteCombo(Character character, Action currentAction, int currentSlotIndex, List<Action> comboSequence)
        {
            var result = new RoutingResult { ContinueCombo = true, NextSlotIndex = currentSlotIndex + 1 };

            // Check for jump to slot
            if (currentAction.ComboRouting.JumpToSlot > 0)
            {
                result.NextSlotIndex = Math.Min(currentAction.ComboRouting.JumpToSlot - 1, comboSequence.Count - 1);
                result.RoutingAction = RoutingAction.JumpToSlot;
                return result;
            }

            // Check for skip next
            if (currentAction.ComboRouting.SkipNext)
            {
                result.NextSlotIndex = currentSlotIndex + 2;
                result.RoutingAction = RoutingAction.SkipNext;
                return result;
            }

            // Check for repeat previous
            if (currentAction.ComboRouting.RepeatPrevious && currentSlotIndex > 0)
            {
                result.NextSlotIndex = currentSlotIndex - 1;
                result.RoutingAction = RoutingAction.RepeatPrevious;
                return result;
            }

            // Check for loop to start
            if (currentAction.ComboRouting.LoopToStart)
            {
                result.NextSlotIndex = 0;
                result.RoutingAction = RoutingAction.LoopToStart;
                return result;
            }

            // Check for stop early
            if (currentAction.ComboRouting.StopEarly)
            {
                result.ContinueCombo = false;
                result.RoutingAction = RoutingAction.StopEarly;
                return result;
            }

            // Check for random action
            if (currentAction.ComboRouting.RandomAction)
            {
                var random = new Random();
                result.NextSlotIndex = random.Next(0, comboSequence.Count);
                result.RoutingAction = RoutingAction.RandomAction;
                return result;
            }

            return result;
        }
    }

    /// <summary>
    /// Result of combo routing
    /// </summary>
    public class RoutingResult
    {
        public bool ContinueCombo { get; set; } = true;
        public int NextSlotIndex { get; set; } = 0;
        public ComboRouter.RoutingAction RoutingAction { get; set; } = ComboRouter.RoutingAction.None;
    }
}

