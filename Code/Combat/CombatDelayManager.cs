using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Config;

namespace RPGGame
{
    /// <summary>
    /// Centralized delay management for combat actions
    /// Provides a single point of control for all combat timing
    /// Now loads configuration from TextDelayConfig.json via TextDelayConfiguration
    /// </summary>
    public static class CombatDelayManager
    {
        /// <summary>
        /// Configuration for combat delays
        /// Now loads from TextDelayConfiguration
        /// </summary>
        public static class Config
        {
            /// <summary>
            /// Delay between complete actions (in milliseconds)
            /// </summary>
            public static int ActionDelayMs => TextDelayConfiguration.GetActionDelayMs();
            
            /// <summary>
            /// Delay between individual messages within an action (in milliseconds)
            /// </summary>
            public static int MessageDelayMs => TextDelayConfiguration.GetMessageDelayMs();
            
            /// <summary>
            /// Whether delays are enabled for GUI
            /// </summary>
            public static bool EnableGuiDelays => TextDelayConfiguration.GetEnableGuiDelays();
            
            /// <summary>
            /// Whether delays are enabled for console
            /// </summary>
            public static bool EnableConsoleDelays => TextDelayConfiguration.GetEnableConsoleDelays();
        }
        
        /// <summary>
        /// Applies delay after a complete action is processed and displayed.
        /// Console: blocks here with ActionDelayMs.
        /// GUI: no-op — end-of-action pacing is applied by the batch display path via delayAfterBatchMs
        /// so the combat loop does not double-wait after DisplayActionBlockAsync returns.
        /// </summary>
        public static async Task DelayAfterActionAsync()
        {
            if (!ShouldApplyDelay()) return;

            // GUI action gaps are owned by BatchOperationCoordinator's delayAfterBatchMs.
            if (UIManager.GetCustomUIManager() != null)
                return;

            int delayMs = DeveloperModeState.ScaleDelayMs(Config.ActionDelayMs);
            if (delayMs > 0)
                await Task.Delay(delayMs);
        }
        
        /// <summary>
        /// Applies delay between individual messages within an action block.
        /// Applies for both GUI and console when the matching Enable*Delays flag is on,
        /// so Avalonia combat log gets real line-by-line reveal (not an instant buffer dump).
        /// </summary>
        public static async Task DelayAfterMessageAsync()
        {
            if (!ShouldApplyDelay()) return;

            int delayMs = DeveloperModeState.ScaleDelayMs(Config.MessageDelayMs);
            if (delayMs > 0)
                await Task.Delay(delayMs);
        }
        
        /// <summary>
        /// Determines if delays should be applied based on UI type and configuration
        /// </summary>
        private static bool ShouldApplyDelay()
        {
            if (DeveloperModeState.IsCombatLogInstant)
                return false;

            // Skip all delays if combat UI output is disabled (e.g., during statistics runs)
            if (CombatManager.DisableCombatUIOutput)
            {
                return false;
            }
            
            // Check if we have a custom UI manager (GUI)
            if (UIManager.GetCustomUIManager() != null)
            {
                return Config.EnableGuiDelays;
            }
            else
            {
                return Config.EnableConsoleDelays;
            }
        }
        
    }
}
