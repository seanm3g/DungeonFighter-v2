using System;
using System.Threading;
using RPGGame;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Centralized animation state for critical hit lines
    /// Thread-safe singleton that holds animation state for crit line animations
    /// Uses the same animation style as dungeon selection screen
    /// </summary>
    public class CritAnimationState : BaseAnimationState
    {
        private static CritAnimationState? _instance;
        private static readonly object _lock = new object();
        
        private CritAnimationState() : base()
        {
            // Base class constructor calls LoadConfiguration()
        }
        
        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static CritAnimationState Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CritAnimationState();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Loads configuration from UIConfiguration (reuse dungeon selection animation config)
        /// </summary>
        protected override void LoadConfiguration()
        {
            var uiConfig = UIConfiguration.LoadFromFile();
            var animConfig = uiConfig.DungeonSelectionAnimation;
            
            // Set undulation speed from config
            _undulationSpeed = animConfig.UndulationSpeed;
            
            // Set brightness mask configuration from config
            _brightnessMaskEnabled = animConfig.BrightnessMask.Enabled;
            _brightnessMaskIntensity = animConfig.BrightnessMask.Intensity;
            _brightnessMaskWaveLength = animConfig.BrightnessMask.WaveLength;
        }
    }
}

