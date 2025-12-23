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
        /// Applies delay after a complete action is processed and displayed
        /// For GUI, delays are handled by the rendering system, so we skip blocking delays here
        /// </summary>
        public static async Task DelayAfterActionAsync()
        {
            if (!ShouldApplyDelay()) return;
            
            // For GUI, skip blocking delays - timing is handled by the rendering system
            if (UIManager.GetCustomUIManager() != null)
            {
                // No blocking delay for GUI - rendering system handles timing
                return;
            }
            else
            {
                // Use async delay for console (non-blocking)
                await Task.Delay(Config.ActionDelayMs);
            }
        }
        
        /// <summary>
        /// Applies delay after individual messages within an action
        /// For GUI, delays are handled by the rendering system, so we skip blocking delays here
        /// </summary>
        public static async Task DelayAfterMessageAsync()
        {
            if (!ShouldApplyDelay()) return;
            
            // For GUI, skip blocking delays - timing is handled by the rendering system
            if (UIManager.GetCustomUIManager() != null)
            {
                // No blocking delay for GUI - rendering system handles timing
                return;
            }
            else
            {
                // Use async delay for console (non-blocking)
                await Task.Delay(Config.MessageDelayMs);
            }
        }
        
        /// <summary>
        /// Determines if delays should be applied based on UI type and configuration
        /// </summary>
        private static bool ShouldApplyDelay()
        {
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
