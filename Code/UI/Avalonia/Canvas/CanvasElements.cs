using Avalonia.Media;

namespace RPGGame.UI.Avalonia.Canvas
{
    /// <summary>
    /// Helper classes for canvas elements
    /// </summary>
    
    public class CanvasText
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Content { get; set; } = "";
        public Color Color { get; set; } = Colors.White;

        /// <summary>
        /// When true, drawn after normal text so opaque panels (e.g. hover tooltips) stay under this text only, not under body copy.
        /// </summary>
        public bool IsOverlay { get; set; }
        
        // Glow effect properties
        public bool HasGlow { get; set; } = false;
        public Color GlowColor { get; set; } = Colors.White;
        public double GlowIntensity { get; set; } = 0.5;
        public int GlowRadius { get; set; } = 3;
    }

    public class CanvasBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Color BorderColor { get; set; } = Colors.White;
        public Color BackgroundColor { get; set; } = Colors.Transparent;

        /// <summary>
        /// Expands an opaque <see cref="BackgroundColor"/> fill by this many device pixels on each side (subpixel fringes).
        /// </summary>
        public int OpaqueBackgroundBleedDevicePixels { get; set; }

        /// <summary>
        /// When true, drawn after normal text and non-overlay boxes so the fill is not covered by center-panel narrative.
        /// </summary>
        public bool IsOverlay { get; set; }
    }

    /// <summary>
    /// One colored strip inside the health bar damage-delta overlay (DoT chunk or fallback single color).
    /// </summary>
    public class HealthBarDamageDeltaSegment
    {
        public int Amount { get; set; }
        public Color Color { get; set; }
    }

    public class CanvasProgressBar
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public double Progress { get; set; }
        public Color ForegroundColor { get; set; } = Colors.Green;
        public Color BackgroundColor { get; set; } = Colors.DarkGreen;
        public Color BorderColor { get; set; } = Colors.White;
        
        // Damage delta tracking for health bars
        public int? PreviousHealth { get; set; }
        public int MaxHealth { get; set; }
        public System.DateTime? DamageDeltaStartTime { get; set; }

        /// <summary>When non-null and valid, drawn inside the delta region instead of a flat yellow overlay.</summary>
        public System.Collections.Generic.IReadOnlyList<HealthBarDamageDeltaSegment>? DamageDeltaSegments { get; set; }
    }
}

