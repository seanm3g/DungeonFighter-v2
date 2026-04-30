using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI;
using RPGGame.Utils;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Manages delays for block display operations
    /// </summary>
    public static class BlockDelayManager
    {
        /// <summary>
        /// Applies delay using centralized delay system
        /// </summary>
        public static void ApplyBlockDelay()
        {
            // Skip delays if combat UI output is disabled (e.g., during statistics runs)
            if (CombatManager.DisableCombatUIOutput) return;

            if (DeveloperModeState.IsCombatLogInstant) return;
            
            if (!UIManager.EnableDelays) return;
            
            // Use centralized delay system for individual messages (fire and forget)
            _ = CombatDelayManager.DelayAfterMessageAsync();
        }
        
        /// <summary>
        /// Calculates delay after batch for action blocks
        /// </summary>
        public static int CalculateActionBlockDelay()
        {
            // Action interaction lab: stepped play and undo replay should not wait on display timers.
            var lab = ActionInteractionLabSession.Current;
            if (lab != null && lab.ZeroDisplayDelays)
                return 0;

            if (DeveloperModeState.IsCombatLogInstant)
                return 0;

            // Skip delays if combat UI output is disabled (e.g., during statistics runs)
            if (CombatManager.DisableCombatUIOutput) return 0;
            
            if (UIManager.EnableDelays)
            {
                return CombatDelayManager.Config.ActionDelayMs;
            }
            return 0;
        }
    }
}

