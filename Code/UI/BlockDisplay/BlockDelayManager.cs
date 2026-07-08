using System.Threading.Tasks;
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
        /// Applies delay using centralized delay system and waits for completion.
        /// Prefer this over fire-and-forget on any path that must not race the combat loop.
        /// </summary>
        public static async Task ApplyBlockDelayAsync()
        {
            // Skip delays if combat UI output is disabled (e.g., during statistics runs)
            if (CombatManager.DisableCombatUIOutput) return;

            if (DeveloperModeState.IsCombatLogInstant) return;
            
            if (!UIManager.EnableDelays) return;
            
            await CombatDelayManager.DelayAfterMessageAsync();
        }

        /// <summary>
        /// Applies delay using centralized delay system (sync callers dump without waiting).
        /// Combat display should use <see cref="ApplyBlockDelayAsync"/>.
        /// </summary>
        public static void ApplyBlockDelay()
        {
            // Intentionally no-op for sync callers: awaiting DelayAfterMessageAsync via fire-and-forget
            // raced the combat loop. Async display paths already wait via the batch coordinator.
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
                return DeveloperModeState.ScaleDelayMs(CombatDelayManager.Config.ActionDelayMs);
            }
            return 0;
        }
    }
}
