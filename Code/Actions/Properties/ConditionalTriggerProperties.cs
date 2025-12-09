using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Conditional trigger properties for actions
    /// </summary>
    public class ConditionalTriggerProperties
    {
        public List<string> TriggerConditions { get; set; } = new List<string>(); // List of condition types
        public int ExactRollTriggerValue { get; set; } = 0; // Exact roll value to trigger (0 = disabled)
        public string? RequiredTag { get; set; } = null; // Required tag for trigger
    }
}

