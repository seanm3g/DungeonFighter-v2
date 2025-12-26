using System;
using System.Threading;
using RPGGame;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Centralized animation state for dungeon selection screen
    /// Thread-safe singleton that holds animation state shared across all renderers
    /// Loads configuration from UIConfiguration.json
    /// </summary>
    public class DungeonSelectionAnimationState : BaseAnimationState
    {
        private static DungeonSelectionAnimationState? _instance;
        private static readonly object _lock = new object();
        
        private DungeonSelectionAnimationState() : base()
        {
            // Base class constructor calls LoadConfiguration()
        }
        
        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static DungeonSelectionAnimationState Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DungeonSelectionAnimationState();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Loads configuration from UIConfiguration
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

