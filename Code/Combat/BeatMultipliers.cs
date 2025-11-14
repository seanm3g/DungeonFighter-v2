namespace RPGGame
{
    /// <summary>
    /// Beat multipliers for different message types
    /// </summary>
    public class BeatMultipliers
    {
        /// <summary>
        /// Combat actions - 1 beat (standard timing)
        /// </summary>
        public double Combat { get; set; } = 1.0;
        
        /// <summary>
        /// System messages - 1 beat (same as combat)
        /// </summary>
        public double System { get; set; } = 1.0;
        
        /// <summary>
        /// Environmental actions - 1.5 beats (slightly longer for dramatic effect)
        /// </summary>
        public double Environmental { get; set; } = 1.5;
        
        /// <summary>
        /// Status effect messages (stun, poison, bleed, etc.) - 0.5 beats (quick, snappy)
        /// </summary>
        public double EffectMessage { get; set; } = 0.5;
        
        /// <summary>
        /// Damage over time - 0.5 beats (quick, like stun)
        /// </summary>
        public double DamageOverTime { get; set; } = 0.5;
        
        /// <summary>
        /// Title screens - 10 beats (dramatic, long pause)
        /// </summary>
        public double Title { get; set; } = 10.0;
        
        /// <summary>
        /// Main title screens - 20 beats (extra dramatic pause for main game title)
        /// </summary>
        public double MainTitle { get; set; } = 20.0;
        
        /// <summary>
        /// Encounter messages - 0.67 beats (dramatic pause after encountering enemies)
        /// </summary>
        public double Encounter { get; set; } = 0.67;
        
        /// <summary>
        /// Roll information messages - 0.10 beats (quick display of roll details)
        /// </summary>
        public double RollInfo { get; set; } = 0.10;
    }
}
