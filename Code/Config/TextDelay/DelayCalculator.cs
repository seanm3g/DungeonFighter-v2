using RPGGame.UI;

namespace RPGGame.Config.TextDelay
{
    /// <summary>
    /// Handles delay calculation and retrieval for text delay configuration
    /// Extracted from TextDelayConfiguration to separate calculation logic
    /// </summary>
    public static class DelayCalculator
    {
        /// <summary>
        /// Gets the delay for a specific message type
        /// </summary>
        public static int GetMessageTypeDelay(UIMessageType messageType, TextDelayLoader.TextDelayConfigData configData)
        {
            return configData.MessageTypeDelays.TryGetValue(messageType, out var delay) ? delay : 0;
        }

        /// <summary>
        /// Gets the action delay (delay after complete action)
        /// </summary>
        public static int GetActionDelayMs(TextDelayLoader.TextDelayConfigData configData)
        {
            return configData.ActionDelayMs;
        }

        /// <summary>
        /// Gets the message delay (delay between messages within action)
        /// </summary>
        public static int GetMessageDelayMs(TextDelayLoader.TextDelayConfigData configData)
        {
            return configData.MessageDelayMs;
        }

        /// <summary>
        /// Gets whether GUI delays are enabled
        /// </summary>
        public static bool GetEnableGuiDelays(TextDelayLoader.TextDelayConfigData configData)
        {
            return configData.EnableGuiDelays;
        }

        /// <summary>
        /// Gets whether console delays are enabled
        /// </summary>
        public static bool GetEnableConsoleDelays(TextDelayLoader.TextDelayConfigData configData)
        {
            return configData.EnableConsoleDelays;
        }
    }
}

