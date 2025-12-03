using System;

namespace RPGGame.UI
{
    /// <summary>
    /// Generates a moving brightness mask that creates cloud-like light and dark spots
    /// that drift across text for a dynamic lighting effect
    /// </summary>
    public class BrightnessMask
    {
        private int offset = 0;
        private readonly object offsetLock = new object();
        private readonly float intensity;
        private readonly float waveLength;
        
        /// <summary>
        /// Creates a new brightness mask
        /// </summary>
        /// <param name="intensity">Maximum brightness adjustment (+/- this value in percent, e.g., 5.0 for +/-5%)</param>
        /// <param name="waveLength">Length of the wave pattern (higher = slower, more gradual changes)</param>
        public BrightnessMask(float intensity = 5.0f, float waveLength = 8.0f)
        {
            this.intensity = intensity;
            this.waveLength = waveLength;
        }
        
        /// <summary>
        /// Gets the brightness adjustment for a character at a given position
        /// Creates a smooth wave pattern that moves over time
        /// </summary>
        /// <param name="position">Character position in the text</param>
        /// <param name="lineOffset">Optional per-line offset to make each line independent</param>
        /// <returns>Brightness adjustment percentage (-intensity to +intensity)</returns>
        public float GetBrightnessAt(int position, int lineOffset = 0)
        {
            // Read offset with lock to ensure thread-safety
            int currentOffset;
            lock (offsetLock)
            {
                currentOffset = offset;
            }
            
            // Use sine wave to create smooth brightness variations
            // Combine two waves at different frequencies for a more organic, cloud-like effect
            // lineOffset creates independence between lines
            float wave1 = (float)Math.Sin((position + currentOffset + lineOffset) * Math.PI / waveLength);
            float wave2 = (float)Math.Sin((position + currentOffset * 0.7 + lineOffset * 0.8) * Math.PI / (waveLength * 1.5)) * 0.5f;
            
            float combinedWave = (wave1 + wave2) / 1.5f; // Normalize
            
            return combinedWave * intensity;
        }
        
        /// <summary>
        /// Advances the mask offset to create movement
        /// Call this periodically to animate the mask
        /// </summary>
        public void Advance()
        {
            lock (offsetLock)
            {
                offset++;
            }
        }
        
        /// <summary>
        /// Gets the current offset (thread-safe)
        /// </summary>
        public int Offset
        {
            get
            {
                lock (offsetLock)
                {
                    return offset;
                }
            }
        }
        
        /// <summary>
        /// Resets the offset to zero
        /// </summary>
        public void Reset()
        {
            offset = 0;
        }
    }
}

