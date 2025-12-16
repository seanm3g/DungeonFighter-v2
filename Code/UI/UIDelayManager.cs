using System;
using System.Threading;
using System.Threading.Tasks;

namespace RPGGame.UI
{
    /// <summary>
    /// Manages all UI timing and delay logic
    /// Handles message-type-specific delays and progressive menu delays
    /// Uses default values instead of loading from configuration file
    /// </summary>
    public class UIDelayManager
    {
        // Default delay values (in milliseconds)
        private const int DefaultCombatDelay = 100;
        private const int DefaultSystemDelay = 100;
        private const int DefaultMenuDelay = 25;
        private const int DefaultTitleDelay = 400;
        private const int DefaultMainTitleDelay = 400;
        private const int DefaultEnvironmentalDelay = 150;
        private const int DefaultEffectMessageDelay = 50;
        private const int DefaultDamageOverTimeDelay = 50;
        private const int DefaultEncounterDelay = 67;
        private const int DefaultRollInfoDelay = 5;

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
            return messageType switch
            {
                UIMessageType.Combat => DefaultCombatDelay,
                UIMessageType.System => DefaultSystemDelay,
                UIMessageType.Menu => DefaultMenuDelay,
                UIMessageType.Title => DefaultTitleDelay,
                UIMessageType.MainTitle => DefaultMainTitleDelay,
                UIMessageType.Environmental => DefaultEnvironmentalDelay,
                UIMessageType.EffectMessage => DefaultEffectMessageDelay,
                UIMessageType.DamageOverTime => DefaultDamageOverTimeDelay,
                UIMessageType.Encounter => DefaultEncounterDelay,
                UIMessageType.RollInfo => DefaultRollInfoDelay,
                _ => 0
            };
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

            // Store base delay on first menu line
            if (_consecutiveMenuLines == 0)
            {
                _baseMenuDelay = DefaultMenuDelay;
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

