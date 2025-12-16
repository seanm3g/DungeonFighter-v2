using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Centralized delay management for combat actions
    /// Provides a single point of control for all combat timing
    /// </summary>
    public static class CombatDelayManager
    {
        private static bool _configLoaded = false;
        
        /// <summary>
        /// Configuration for combat delays
        /// </summary>
        public static class Config
        {
            /// <summary>
            /// Delay between complete actions (in milliseconds)
            /// </summary>
            public static int ActionDelayMs = 1000;
            
            /// <summary>
            /// Delay between individual messages within an action (in milliseconds)
            /// </summary>
            public static int MessageDelayMs = 200;
            
            /// <summary>
            /// Whether delays are enabled for GUI
            /// </summary>
            public static bool EnableGuiDelays = true;
            
            /// <summary>
            /// Whether delays are enabled for console
            /// </summary>
            public static bool EnableConsoleDelays = true;
        }
        
        /// <summary>
        /// Loads delay configuration from JSON file
        /// </summary>
        private static void LoadConfig()
        {
            if (_configLoaded) return;
            
            try
            {
                string configPath = "GameData/CombatDelayConfig.json";
                if (System.IO.File.Exists(configPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    
                    if (config.TryGetProperty("ActionDelayMs", out var actionDelay))
                        Config.ActionDelayMs = actionDelay.GetInt32();
                    
                    if (config.TryGetProperty("MessageDelayMs", out var messageDelay))
                        Config.MessageDelayMs = messageDelay.GetInt32();
                    
                    if (config.TryGetProperty("EnableGuiDelays", out var enableGui))
                        Config.EnableGuiDelays = enableGui.GetBoolean();
                    
                    if (config.TryGetProperty("EnableConsoleDelays", out var enableConsole))
                        Config.EnableConsoleDelays = enableConsole.GetBoolean();
                }
            }
            catch (Exception ex)
            {
                // If config loading fails, use default values
                UIManager.WriteLine($"Warning: Could not load combat delay config: {ex.Message}", UIMessageType.System);
            }
            
            _configLoaded = true;
        }
        
        /// <summary>
        /// Applies delay after a complete action is processed and displayed
        /// For GUI, delays are handled by the rendering system, so we skip blocking delays here
        /// </summary>
        public static async Task DelayAfterActionAsync()
        {
            LoadConfig();
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
            LoadConfig();
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
        /// </summary>
        /// <param name="actionDelayMs">Delay between complete actions</param>
        /// <param name="messageDelayMs">Delay between individual messages</param>
        /// <param name="enableGuiDelays">Whether to enable delays for GUI</param>
        /// <param name="enableConsoleDelays">Whether to enable delays for console</param>
        public static void UpdateConfig(int actionDelayMs = -1, int messageDelayMs = -1, bool? enableGuiDelays = null, bool? enableConsoleDelays = null)
        {
            if (actionDelayMs >= 0) Config.ActionDelayMs = actionDelayMs;
            if (messageDelayMs >= 0) Config.MessageDelayMs = messageDelayMs;
            if (enableGuiDelays.HasValue) Config.EnableGuiDelays = enableGuiDelays.Value;
            if (enableConsoleDelays.HasValue) Config.EnableConsoleDelays = enableConsoleDelays.Value;
        }
    }
}
