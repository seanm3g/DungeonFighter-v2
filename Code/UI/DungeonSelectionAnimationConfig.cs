namespace RPGGame
{
    /// <summary>
    /// Configuration for dungeon selection screen animations
    /// All parameters can be adjusted in UIConfiguration.json
    /// </summary>
    public class DungeonSelectionAnimationConfig
    {
        /// <summary>
        /// Speed of the undulation effect (how fast the brightness pulses)
        /// Lower values = slower, more subtle pulsing
        /// Higher values = faster, more noticeable pulsing
        /// Default: 0.05
        /// </summary>
        public double UndulationSpeed { get; set; } = 0.05;
        
        /// <summary>
        /// How often the undulation animation updates (in milliseconds)
        /// Lower values = smoother animation but more CPU usage
        /// Higher values = choppier animation but less CPU usage
        /// Default: 50
        /// </summary>
        public int UndulationIntervalMs { get; set; } = 50;
        
        /// <summary>
        /// Configuration for the brightness mask (wave effect across text)
        /// </summary>
        public DungeonBrightnessMaskConfig BrightnessMask { get; set; } = new DungeonBrightnessMaskConfig();
    }
    
    /// <summary>
    /// Configuration for the brightness mask effect on dungeon selection screen
    /// </summary>
    public class DungeonBrightnessMaskConfig
    {
        /// <summary>
        /// Enable or disable the brightness mask effect
        /// Default: true
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Maximum brightness adjustment (+/- this value in percent)
        /// Lower values = more subtle effect
        /// Higher values = more dramatic effect
        /// Default: 50.0
        /// </summary>
        public float Intensity { get; set; } = 50.0f;
        
        /// <summary>
        /// Length of the wave pattern (higher = slower, more gradual changes)
        /// Lower values = tighter waves, more flickering
        /// Higher values = wider waves, smoother effect
        /// Default: 3.0
        /// </summary>
        public float WaveLength { get; set; } = 3.0f;
        
        /// <summary>
        /// How often the brightness mask moves (in milliseconds)
        /// Lower values = faster movement
        /// Higher values = slower movement
        /// Default: 1000
        /// </summary>
        public int UpdateIntervalMs { get; set; } = 1000;
    }
}

