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
    public class DungeonSelectionAnimationState
    {
        private static DungeonSelectionAnimationState? _instance;
        private static readonly object _lock = new object();
        
        // Brightness mask state
        private int _brightnessMaskOffset = 0;
        private readonly object _brightnessMaskLock = new object();
        
        // Undulation state (global phase for all dungeon names)
        private double _undulationPhase = 0.0;
        private double _undulationSpeed; // Loaded from config
        private readonly object _undulationLock = new object();
        
        // Brightness mask configuration (loaded from config)
        private float _brightnessMaskIntensity;
        private float _brightnessMaskWaveLength;
        private bool _brightnessMaskEnabled;
        
        private DungeonSelectionAnimationState()
        {
            // Load configuration from UIConfiguration
            var uiConfig = UIConfiguration.LoadFromFile();
            var animConfig = uiConfig.DungeonSelectionAnimation;
            
            // Set undulation speed from config
            _undulationSpeed = animConfig.UndulationSpeed;
            
            // Set brightness mask configuration from config
            _brightnessMaskEnabled = animConfig.BrightnessMask.Enabled;
            _brightnessMaskIntensity = animConfig.BrightnessMask.Intensity;
            _brightnessMaskWaveLength = animConfig.BrightnessMask.WaveLength;
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
        /// Advances the brightness mask offset (thread-safe)
        /// </summary>
        public void AdvanceBrightnessMask()
        {
            lock (_brightnessMaskLock)
            {
                _brightnessMaskOffset++;
            }
        }
        
        /// <summary>
        /// Gets the current brightness mask offset (thread-safe)
        /// </summary>
        public int BrightnessMaskOffset
        {
            get
            {
                lock (_brightnessMaskLock)
                {
                    return _brightnessMaskOffset;
                }
            }
        }
        
        /// <summary>
        /// Advances the undulation phase (thread-safe)
        /// </summary>
        public void AdvanceUndulation()
        {
            lock (_undulationLock)
            {
                _undulationPhase += _undulationSpeed;
                if (_undulationPhase > Math.PI * 2)
                {
                    _undulationPhase -= Math.PI * 2;
                }
            }
        }
        
        /// <summary>
        /// Gets the current undulation phase (thread-safe)
        /// </summary>
        public double UndulationPhase
        {
            get
            {
                lock (_undulationLock)
                {
                    return _undulationPhase;
                }
            }
        }
        
        /// <summary>
        /// Gets the undulation brightness adjustment (-0.3 to +0.3)
        /// </summary>
        public double GetUndulationBrightness()
        {
            return Math.Sin(UndulationPhase) * 0.3;
        }
        
        /// <summary>
        /// Gets the brightness adjustment for a character at a given position
        /// </summary>
        /// <param name="position">Character position in the text</param>
        /// <param name="lineOffset">Optional per-line offset to make each line independent</param>
        /// <returns>Brightness adjustment percentage (-intensity to +intensity)</returns>
        public float GetBrightnessAt(int position, int lineOffset = 0)
        {
            // If brightness mask is disabled, return no adjustment
            if (!_brightnessMaskEnabled)
                return 0.0f;
            
            int currentOffset;
            lock (_brightnessMaskLock)
            {
                currentOffset = _brightnessMaskOffset;
            }
            
            // Use sine wave to create smooth brightness variations
            // Combine two waves at different frequencies for a more organic, cloud-like effect
            float wave1 = (float)Math.Sin((position + currentOffset + lineOffset) * Math.PI / _brightnessMaskWaveLength);
            float wave2 = (float)Math.Sin((position + currentOffset * 0.7f + lineOffset * 0.8f) * Math.PI / (_brightnessMaskWaveLength * 1.5f)) * 0.5f;
            
            float combinedWave = (wave1 + wave2) / 1.5f; // Normalize
            
            return combinedWave * _brightnessMaskIntensity;
        }
        
        /// <summary>
        /// Resets all animation state
        /// </summary>
        public void Reset()
        {
            lock (_brightnessMaskLock)
            {
                _brightnessMaskOffset = 0;
            }
            lock (_undulationLock)
            {
                _undulationPhase = 0.0;
            }
        }
    }
}

