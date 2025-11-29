using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.UI.ColorSystem;

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
            if (delayMs > 0)
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
            if (delayMs > 0)
                await Task.Delay(delayMs);
            renderAction();
        }
    }
}

