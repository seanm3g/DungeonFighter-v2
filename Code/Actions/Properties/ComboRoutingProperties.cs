using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Combo routing properties for actions
    /// </summary>
    public class ComboRoutingProperties
    {
        private int _jumpToSlot = 0;
        private int _jumpRelativeSlots = 0;
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

        /// <summary>
        /// Extra combo slots to advance after this action (beyond the normal next slot). 0 = disabled.
        /// Next index = currentSlotIndex + 1 + value (0-based slots; 1-based "position" is index + 1).
        /// </summary>
        public int JumpRelativeSlots
        {
            get => _jumpRelativeSlots;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(JumpRelativeSlots), "JumpRelativeSlots must be >= 0");
                _jumpRelativeSlots = value;
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

        /// <summary>Chain position label (e.g. First, Last). From Actions settings form; used by combo/position logic.</summary>
        public string ChainPosition { get; set; } = "";
        /// <summary>Chain length (e.g. 3). From Actions settings form.</summary>
        public string ChainLength { get; set; } = "";
        /// <summary>Reset flag (e.g. "true"). From Actions settings form.</summary>
        public string Reset { get; set; } = "";
        /// <summary>Modify based on chain position (e.g. "true"). From Actions settings form.</summary>
        public string ModifyBasedOnChainPosition { get; set; } = "";
        /// <summary>Per–combo-slot bonuses when <see cref="ModifyBasedOnChainPosition"/> is enabled.</summary>
        public List<ChainPositionBonusEntry> ChainPositionBonuses { get; set; } = new List<ChainPositionBonusEntry>();
        /// <summary>When true, this action is constrained to the first slot of the combo sequence.</summary>
        public bool IsOpener { get; set; }
        /// <summary>When true, this action is constrained to the last slot of the combo sequence.</summary>
        public bool IsFinisher { get; set; }

        /// <summary>
        /// Validates routing properties for consistency
        /// </summary>
        public void Validate()
        {
            // Only one routing action should be active at a time
            int activeRoutingCount = 0;
            if (JumpToSlot > 0) activeRoutingCount++;
            if (JumpRelativeSlots > 0) activeRoutingCount++;
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

