using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Beat-based timing configuration system with two core beats
    /// </summary>
    public class BeatTimingConfiguration
    {
        /// <summary>
        /// Combat beat length in milliseconds - used for combat, environmental, effects, and damage over time
        /// </summary>
        public int CombatBeatLengthMs { get; set; } = 1500;
        
        /// <summary>
        /// Menu beat length in milliseconds - used for system messages and titles
        /// </summary>
        public int MenuBeatLengthMs { get; set; } = 20;
        
        /// <summary>
        /// Beat multipliers for different message types
        /// </summary>
        public BeatMultipliers BeatMultipliers { get; set; } = new BeatMultipliers();
        
        /// <summary>
        /// Block-based delays for the new display system
        /// </summary>
        public Dictionary<string, double> BlockDelays { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Legacy multipliers for backward compatibility
        /// </summary>
        public LegacyMultipliers LegacyMultipliers { get; set; } = new LegacyMultipliers();

    }
}
