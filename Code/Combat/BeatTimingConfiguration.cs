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
        
        /// <summary>
        /// Gets the beat multiplier for a specific message type
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <returns>Beat multiplier (e.g., 1.0 = 1 beat, 0.5 = half beat, 2.0 = 2 beats)</returns>
        public double GetBeatMultiplier(UIMessageType messageType)
        {
            return messageType switch
            {
                UIMessageType.Combat => BeatMultipliers.Combat,
                UIMessageType.Menu => 0, // Menu uses MenuSpeedMs instead
                UIMessageType.System => BeatMultipliers.System,
                UIMessageType.Title => BeatMultipliers.Title,
                UIMessageType.MainTitle => BeatMultipliers.MainTitle,
                UIMessageType.Environmental => BeatMultipliers.Environmental,
                UIMessageType.EffectMessage => BeatMultipliers.EffectMessage,
                UIMessageType.DamageOverTime => BeatMultipliers.DamageOverTime,
                UIMessageType.RollInfo => BeatMultipliers.RollInfo, // Use JSON configuration
                UIMessageType.Encounter => BeatMultipliers.Encounter,
                _ => 0
            };
        }
        
        /// <summary>
        /// Gets the appropriate base beat length for a message type
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <returns>Base beat length in milliseconds</returns>
        public int GetBaseBeatLength(UIMessageType messageType)
        {
            return messageType switch
            {
                UIMessageType.Combat => CombatBeatLengthMs,
                UIMessageType.Environmental => CombatBeatLengthMs,
                UIMessageType.EffectMessage => CombatBeatLengthMs,
                UIMessageType.DamageOverTime => CombatBeatLengthMs,
                UIMessageType.RollInfo => CombatBeatLengthMs,
                UIMessageType.Encounter => CombatBeatLengthMs,
                UIMessageType.System => MenuBeatLengthMs,
                UIMessageType.Title => MenuBeatLengthMs,
                UIMessageType.MainTitle => MenuBeatLengthMs,
                UIMessageType.Menu => MenuBeatLengthMs,
                _ => CombatBeatLengthMs
            };
        }
        
        /// <summary>
        /// Gets the effective menu delay (independent of beat system)
        /// </summary>
        /// <returns>Menu delay in milliseconds</returns>
        public int GetMenuDelay()
        {
            return MenuBeatLengthMs;
        }
    }
}
