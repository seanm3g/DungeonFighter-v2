using System;
using Avalonia.Media;
using System.Threading;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Animates a soft undulating glow effect for the stats header
    /// Transitions between yellow and white using a sine wave
    /// </summary>
    public class StatsHeaderGlowAnimator : IDisposable
    {
        private readonly Timer? animationTimer;
        private readonly object lockObject = new object();
        
        // Animation state
        private double animationTime = 0.0;
        private bool isActive = false;
        
        // Color constants
        private static readonly Color YellowColor = Color.FromRgb(0xCF, 0xC0, 0x41); // #cfc041
        private static readonly Color WhiteColor = Color.FromRgb(0xFF, 0xFF, 0xFF); // #ffffff
        
        // Animation parameters
        private const double AnimationFrequency = Math.PI / 1000.0; // ~2 seconds per cycle (1000ms * PI / 1000 = PI per second, so 2 seconds per full cycle)
        private const double BaseIntensity = 0.3;
        private const double IntensityVariation = 0.2;
        private const int GlowRadius = 2;
        private const int UpdateIntervalMs = 42; // ~24fps
        
        // Current animation values (thread-safe access)
        private Color currentGlowColor = YellowColor;
        private double currentGlowIntensity = BaseIntensity;
        
        // Callback for when animation updates
        private System.Action? onUpdateCallback;
        
        public StatsHeaderGlowAnimator()
        {
            animationTimer = new Timer(UpdateAnimation, null, Timeout.Infinite, UpdateIntervalMs);
        }
        
        /// <summary>
        /// Starts the glow animation
        /// </summary>
        public void Start()
        {
            lock (lockObject)
            {
                if (!isActive)
                {
                    isActive = true;
                    animationTimer?.Change(0, UpdateIntervalMs);
                }
            }
        }
        
        /// <summary>
        /// Stops the glow animation
        /// </summary>
        public void Stop()
        {
            lock (lockObject)
            {
                if (isActive)
                {
                    isActive = false;
                    animationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }
        
        /// <summary>
        /// Gets the current glow color (interpolated between yellow and white)
        /// </summary>
        public Color GetCurrentGlowColor()
        {
            lock (lockObject)
            {
                return currentGlowColor;
            }
        }
        
        /// <summary>
        /// Gets the current glow intensity
        /// </summary>
        public double GetCurrentGlowIntensity()
        {
            lock (lockObject)
            {
                return currentGlowIntensity;
            }
        }
        
        /// <summary>
        /// Gets the glow radius
        /// </summary>
        public int GetGlowRadius() => GlowRadius;
        
        /// <summary>
        /// Sets a callback to be invoked when the animation updates
        /// </summary>
        public void SetUpdateCallback(System.Action? callback)
        {
            lock (lockObject)
            {
                onUpdateCallback = callback;
            }
        }
        
        /// <summary>
        /// Updates the animation state (called by timer)
        /// </summary>
        private void UpdateAnimation(object? state)
        {
            lock (lockObject)
            {
                if (!isActive)
                    return;
                
                // Update animation time
                animationTime += UpdateIntervalMs;
                
                // Calculate sine wave value (oscillates between -1 and 1)
                double sinValue = Math.Sin(animationTime * AnimationFrequency);
                
                // Normalize to 0-1 range for interpolation
                double normalizedValue = (sinValue + 1.0) / 2.0;
                
                // Interpolate color between yellow and white
                currentGlowColor = InterpolateColor(YellowColor, WhiteColor, normalizedValue);
                
                // Calculate glow intensity (oscillates between BaseIntensity and BaseIntensity + IntensityVariation)
                currentGlowIntensity = BaseIntensity + IntensityVariation * normalizedValue;
                
                // Invoke update callback if set
                onUpdateCallback?.Invoke();
            }
        }
        
        /// <summary>
        /// Interpolates between two colors
        /// </summary>
        private Color InterpolateColor(Color color1, Color color2, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            
            byte r = (byte)(color1.R + (color2.R - color1.R) * t);
            byte g = (byte)(color1.G + (color2.G - color1.G) * t);
            byte b = (byte)(color1.B + (color2.B - color1.B) * t);
            
            return Color.FromRgb(r, g, b);
        }
        
        public void Dispose()
        {
            Stop();
            animationTimer?.Dispose();
        }
    }
}
