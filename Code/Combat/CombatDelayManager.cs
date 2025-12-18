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
        /// Synchronous version for backwards compatibility
        /// </summary>
        [Obsolete("Use DelayAfterActionAsync instead")]
        public static void DelayAfterAction()
        {
            DelayAfterActionAsync().GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Synchronous version for backwards compatibility
        /// </summary>
        [Obsolete("Use DelayAfterMessageAsync instead")]
        public static void DelayAfterMessage()
        {
            DelayAfterMessageAsync().GetAwaiter().GetResult();
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
        
        /// <summary>
        /// Updates delay configuration
        /// Note: Configuration is now loaded from TextDelayConfig.json
        /// This method is kept for backwards compatibility but values should be updated in the JSON file
        /// </summary>
        /// <param name="actionDelayMs">Delay between complete actions (deprecated - update TextDelayConfig.json instead)</param>
        /// <param name="messageDelayMs">Delay between individual messages (deprecated - update TextDelayConfig.json instead)</param>
        /// <param name="enableGuiDelays">Whether to enable delays for GUI (deprecated - update TextDelayConfig.json instead)</param>
        /// <param name="enableConsoleDelays">Whether to enable delays for console (deprecated - update TextDelayConfig.json instead)</param>
        [Obsolete("Update TextDelayConfig.json instead. This method is kept for backwards compatibility only.")]
        public static void UpdateConfig(int actionDelayMs = -1, int messageDelayMs = -1, bool? enableGuiDelays = null, bool? enableConsoleDelays = null)
        {
            // Configuration is now loaded from TextDelayConfig.json
            // This method is kept for backwards compatibility but does nothing
            // To update delays, edit GameData/TextDelayConfig.json
        }
    }
}
