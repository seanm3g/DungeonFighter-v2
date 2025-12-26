using System;
using System.Threading;
using RPGGame;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Base class for animation state management
    /// Provides shared brightness mask and undulation functionality
    /// Used by CritAnimationState and DungeonSelectionAnimationState
    /// </summary>
    public abstract class BaseAnimationState
    {
        // Brightness mask state
        protected int _brightnessMaskOffset = 0;
        protected readonly object _brightnessMaskLock = new object();
        protected float _brightnessMaskIntensity;
        protected float _brightnessMaskWaveLength;
        protected bool _brightnessMaskEnabled;
        
        // Undulation state (global phase for all animated text)
        protected double _undulationPhase = 0.0;
        protected double _undulationSpeed;
        protected float _undulationWaveLength = 4.0f; // Default wave length for position-based undulation
        protected readonly object _undulationLock = new object();
        
        /// <summary>
        /// Protected constructor - must be called by derived classes
        /// </summary>
        protected BaseAnimationState()
        {
            LoadConfiguration();
        }
        
        /// <summary>
        /// Loads configuration from UIConfiguration
        /// Must be implemented by derived classes to specify which config to use
        /// </summary>
        protected abstract void LoadConfiguration();
        
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
        /// This is the global undulation that affects all characters equally.
        /// For position-based undulation, use GetUndulationBrightnessAt instead.
        /// </summary>
        public double GetUndulationBrightness()
        {
            return Math.Sin(UndulationPhase) * 0.3;
        }
        
        /// <summary>
        /// Gets the undulation brightness adjustment for a character at a given position
        /// Creates a sine wave effect across text, so different letters have different brightness
        /// </summary>
        /// <param name="position">Character position in the text</param>
        /// <param name="lineOffset">Optional per-line offset to make each line independent</param>
        /// <returns>Brightness adjustment (-0.3 to +0.3)</returns>
        public double GetUndulationBrightnessAt(int position, int lineOffset = 0)
        {
            double phase;
            lock (_undulationLock)
            {
                phase = _undulationPhase;
            }
            
            // Combine the global undulation phase with position-based wave
            // This creates a sine wave that moves across the text
            double positionPhase = (position + lineOffset) * Math.PI / _undulationWaveLength;
            double combinedPhase = phase + positionPhase;
            
            return Math.Sin(combinedPhase) * 0.3;
        }
        
        /// <summary>
        /// Gets the brightness adjustment for a character at a given position
        /// Creates a wave effect across text using sine waves
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
        
        /// <summary>
        /// Reloads configuration from UIConfiguration
        /// Called when settings are changed to apply new values in real-time
        /// </summary>
        public void ReloadConfiguration()
        {
            LoadConfiguration();
        }
    }
}

