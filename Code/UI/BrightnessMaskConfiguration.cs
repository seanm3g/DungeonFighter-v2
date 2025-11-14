namespace RPGGame
{
    /// <summary>
    /// Configuration for the moving brightness mask effect
    /// </summary>
    public class BrightnessMaskConfig
    {
        /// <summary>
        /// Enable or disable the brightness mask effect
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Maximum brightness adjustment (+/- this value in percent)
        /// Must be set in UIConfiguration.json
        /// </summary>
        public float Intensity { get; set; }
        
        /// <summary>
        /// Length of the wave pattern (higher = slower, more gradual changes)
        /// Must be set in UIConfiguration.json
        /// </summary>
        public float WaveLength { get; set; }
        
        /// <summary>
        /// How often the brightness mask moves (in milliseconds)
        /// Must be set in UIConfiguration.json
        /// </summary>
        public int UpdateIntervalMs { get; set; }
    }
}
