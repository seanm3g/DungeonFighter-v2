using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Conditional trigger properties for actions
    /// </summary>
    public class ConditionalTriggerProperties
    {
        public List<string> TriggerConditions { get; set; } = new List<string>(); // List of condition types
        public int ExactRollTriggerValue { get; set; } = 0; // Exact roll value to trigger (0 = disabled)
        /// <summary>When &gt; 0 with ONROOMSCLEARED, only fire when rooms-cleared count equals this; 0 = every clear.</summary>
        public int RoomsClearedTriggerValue { get; set; } = 0;
        public string? RequiredTag { get; set; } = null; // Required tag for trigger

        /// <summary>
        /// Spreadsheet TRIGGERS band triples (WHEN / SCOPE / mechanic pointers). Status gate still uses <see cref="TriggerConditions"/>.
        /// </summary>
        public List<ActionTriggerBundle> Bundles { get; set; } = new List<ActionTriggerBundle>();
    }
}

