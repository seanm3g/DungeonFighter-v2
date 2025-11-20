using System;
using System.Threading;

namespace RPGGame.UI
{
    /// <summary>
    /// Manages all UI timing and delay logic
    /// Handles message-type-specific delays and progressive menu delays
    /// </summary>
    public class UIDelayManager
    {
        private readonly UIConfiguration _uiConfig;

        // Progressive delay system for menu lines
        private int _consecutiveMenuLines = 0;
        private int _baseMenuDelay = 0;

        public UIDelayManager(UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig;
        }

        /// <summary>
        /// Applies appropriate delay based on message type using the beat-based timing system
        /// </summary>
        public void ApplyDelay(UIMessageType messageType)
        {
            int delayMs = _uiConfig.GetEffectiveDelay(messageType);

            if (delayMs > 0)
            {
                Thread.Sleep(delayMs);
            }
        }

        /// <summary>
        /// Applies progressive menu delay - reduces delay by 1ms for each consecutive menu line
        /// After 20 lines, slowly ramps delay down by 1ms each line
        /// </summary>
        public void ApplyProgressiveMenuDelay()
        {
            // Get base menu delay from configuration
            int baseDelay = _uiConfig.BeatTiming.GetMenuDelay();

            // Store base delay on first menu line
            if (_consecutiveMenuLines == 0)
            {
                _baseMenuDelay = baseDelay;
            }

            int progressiveDelay;

            if (_consecutiveMenuLines < 20)
            {
                // First 20 lines: normal progressive reduction (base delay minus 1ms per line)
                progressiveDelay = Math.Max(0, _baseMenuDelay - _consecutiveMenuLines);
            }
            else
            {
                // After 20 lines: slowly ramp down by 1ms each line
                // Start from the delay at line 20, then reduce by 1ms each subsequent line
                int delayAtLine20 = Math.Max(0, _baseMenuDelay - 19); // Delay at line 20 (0-indexed, so line 20 is index 19)
                progressiveDelay = Math.Max(0, delayAtLine20 - (_consecutiveMenuLines - 20));
            }

            // Apply the delay
            if (progressiveDelay > 0)
            {
                Thread.Sleep(progressiveDelay);
            }

            // Increment consecutive menu line counter
            _consecutiveMenuLines++;
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

