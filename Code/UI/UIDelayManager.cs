using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Config;

namespace RPGGame.UI
{
    /// <summary>
    /// Manages all UI timing and delay logic
    /// Handles message-type-specific delays and progressive menu delays
    /// Loads delay values from TextDelayConfig.json
    /// </summary>
    public class UIDelayManager
    {
        // Progressive delay system for menu lines
        private int _consecutiveMenuLines = 0;
        private int _baseMenuDelay = 0;

        public UIDelayManager()
        {
        }

        /// <summary>
        /// Applies appropriate delay based on message type using default values
        /// </summary>
        public async Task ApplyDelayAsync(UIMessageType messageType)
        {
            // Check if delays are enabled globally
            if (!UIManager.EnableDelays)
            {
                return;
            }

            int delayMs = GetDelayForMessageType(messageType);

            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
        }
        
        /// <summary>
        /// Synchronous version for backwards compatibility (fire-and-forget)
        /// </summary>
        public void ApplyDelay(UIMessageType messageType)
        {
            if (!UIManager.EnableDelays)
            {
                return;
            }

            // Fire and forget - don't block the calling thread
            _ = ApplyDelayAsync(messageType);
        }

        /// <summary>
        /// Gets the delay for a specific message type
        /// </summary>
        private int GetDelayForMessageType(UIMessageType messageType)
        {
            return TextDelayConfiguration.GetMessageTypeDelay(messageType);
        }

        /// <summary>
        /// Applies progressive menu delay - reduces delay by 1ms for each consecutive menu line
        /// After 20 lines, slowly ramps delay down by 1ms each line
        /// </summary>
        public async Task ApplyProgressiveMenuDelayAsync()
        {
            // Check if delays are enabled globally
            if (!UIManager.EnableDelays)
            {
                return;
            }

            // Get progressive menu delay configuration
            var progressiveConfig = TextDelayConfiguration.GetProgressiveMenuDelays();

            // Store base delay on first menu line
            if (_consecutiveMenuLines == 0)
            {
                _baseMenuDelay = progressiveConfig.BaseMenuDelay;
            }

            int progressiveDelay;

            if (_consecutiveMenuLines < progressiveConfig.ProgressiveThreshold)
            {
                // First N lines: normal progressive reduction (base delay minus reduction rate per line)
                progressiveDelay = Math.Max(0, _baseMenuDelay - (_consecutiveMenuLines * progressiveConfig.ProgressiveReductionRate));
            }
            else
            {
                // After threshold: slowly ramp down by reduction rate each line
                // Start from the delay at threshold, then reduce by reduction rate each subsequent line
                int delayAtThreshold = Math.Max(0, _baseMenuDelay - ((progressiveConfig.ProgressiveThreshold - 1) * progressiveConfig.ProgressiveReductionRate));
                progressiveDelay = Math.Max(0, delayAtThreshold - ((_consecutiveMenuLines - progressiveConfig.ProgressiveThreshold) * progressiveConfig.ProgressiveReductionRate));
            }

            // Apply the delay
            if (progressiveDelay > 0)
            {
                await Task.Delay(progressiveDelay);
            }

            // Increment consecutive menu line counter
            _consecutiveMenuLines++;
        }
        
        /// <summary>
        /// Synchronous version for backwards compatibility (fire-and-forget)
        /// </summary>
        public void ApplyProgressiveMenuDelay()
        {
            if (!UIManager.EnableDelays)
            {
                return;
            }

            // Fire and forget - don't block the calling thread
            _ = ApplyProgressiveMenuDelayAsync();
        }

        /// <summary>
        /// Resets the progressive menu delay counter (call when menu section is complete)
        /// </summary>
        public void ResetMenuDelayCounter()
        {
            _consecutiveMenuLines = 0;
            _baseMenuDelay = 0;
        }

        /// <summary>
        /// Gets the current consecutive menu line count (for testing/debugging)
        /// </summary>
        public int GetConsecutiveMenuLineCount() => _consecutiveMenuLines;

        /// <summary>
        /// Gets the current base menu delay (for testing/debugging)
        /// </summary>
        public int GetBaseMenuDelay() => _baseMenuDelay;
    }
}

