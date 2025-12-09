using System;

namespace RPGGame
{
    /// <summary>
    /// Combo routing properties for actions
    /// </summary>
    public class ComboRoutingProperties
    {
        private int _jumpToSlot = 0;
        private int _triggerOnlyInSlot = 0;

        public int JumpToSlot
        {
            get => _jumpToSlot;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(JumpToSlot), "JumpToSlot must be 0 (disabled) or positive");
                _jumpToSlot = value;
            }
        }

        public bool SkipNext { get; set; } = false;
        public bool RepeatPrevious { get; set; } = false;
        public bool LoopToStart { get; set; } = false;
        public bool StopEarly { get; set; } = false;
        public bool DisableSlot { get; set; } = false;
        public bool RandomAction { get; set; } = false;

        public int TriggerOnlyInSlot
        {
            get => _triggerOnlyInSlot;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(TriggerOnlyInSlot), "TriggerOnlyInSlot must be 0 (always) or positive");
                _triggerOnlyInSlot = value;
            }
        }

        /// <summary>
        /// Validates routing properties for consistency
        /// </summary>
        public void Validate()
        {
            // Only one routing action should be active at a time
            int activeRoutingCount = 0;
            if (JumpToSlot > 0) activeRoutingCount++;
            if (SkipNext) activeRoutingCount++;
            if (RepeatPrevious) activeRoutingCount++;
            if (LoopToStart) activeRoutingCount++;
            if (StopEarly) activeRoutingCount++;
            if (RandomAction) activeRoutingCount++;

            if (activeRoutingCount > 1)
                throw new ArgumentException("Only one combo routing action can be active at a time");
        }
    }
}

