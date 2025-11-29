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
            if (!UIManager.EnableDelays) return;
            
            // Use centralized delay system for individual messages
            CombatDelayManager.DelayAfterMessage();
        }
        
        /// <summary>
        /// Calculates delay after batch for action blocks
        /// </summary>
        public static int CalculateActionBlockDelay()
        {
            if (UIManager.EnableDelays)
            {
                return CombatDelayManager.Config.ActionDelayMs;
            }
            return 0;
        }
    }
}

