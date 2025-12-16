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
            
            if (!UIManager.EnableDelays) return;
            
            // Use centralized delay system for individual messages (fire and forget)
            _ = CombatDelayManager.DelayAfterMessageAsync();
        }
        
        /// <summary>
        /// Calculates delay after batch for action blocks
        /// </summary>
        public static int CalculateActionBlockDelay()
        {
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

