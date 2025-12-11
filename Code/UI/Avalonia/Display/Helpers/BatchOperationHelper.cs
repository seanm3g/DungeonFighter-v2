using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.UI.ColorSystem;
using RPGGame;

namespace RPGGame.UI.Avalonia.Display.Helpers
{
    /// <summary>
    /// Helper for batch operations with delay scheduling
    /// </summary>
    public static class BatchOperationHelper
    {
        /// <summary>
        /// Schedules a render with optional delay using a timer
        /// </summary>
        public static void ScheduleRenderWithDelay(System.Action renderAction, int delayMs)
        {
            // Skip delays if combat UI output is disabled (e.g., during statistics runs)
            if (delayMs > 0 && !CombatManager.DisableCombatUIOutput)
            {
                Timer? delayTimer = null;
                delayTimer = new Timer(_ =>
                {
                    delayTimer?.Dispose();
                    renderAction();
                }, null, delayMs, Timeout.Infinite);
            }
            else
            {
                renderAction();
            }
        }

        /// <summary>
        /// Schedules a render with optional delay using async/await
        /// </summary>
        public static async Task ScheduleRenderWithDelayAsync(System.Action renderAction, int delayMs)
        {
            // Skip delays if combat UI output is disabled (e.g., during statistics runs)
            if (delayMs > 0 && !CombatManager.DisableCombatUIOutput)
                await Task.Delay(delayMs);
            renderAction();
        }
    }
}

