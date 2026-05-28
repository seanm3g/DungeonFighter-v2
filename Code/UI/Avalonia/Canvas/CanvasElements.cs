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

        /// <summary>Device pixels for the rectangle stroke (default 1).</summary>
        public int BorderThicknessPixels { get; set; } = 1;

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

        /// <summary>Bar height as a multiple of one character row (default 1.0).</summary>
        public double HeightScale { get; set; } = 1.0;

        /// <summary>Vertical offset from <see cref="Y"/> as a multiple of one character row.</summary>
        public double VerticalOffsetScale { get; set; } = 0.0;
        
        // Damage delta tracking for health bars
        public int? PreviousHealth { get; set; }
        public int MaxHealth { get; set; }
        public System.DateTime? DamageDeltaStartTime { get; set; }

        /// <summary>When non-null and valid, drawn inside the delta region instead of a flat yellow overlay.</summary>
        public System.Collections.Generic.IReadOnlyList<HealthBarDamageDeltaSegment>? DamageDeltaSegments { get; set; }
    }

    public class CanvasBarSegment
    {
        public int FaceCount { get; set; }
        public Color Color { get; set; }
    }

    /// <summary>Multi-segment bar (e.g. d20 threshold outcomes) with internal divider lines.</summary>
    public class CanvasSegmentedBar
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int TotalFaces { get; set; } = 20;
        public Color BorderColor { get; set; } = Colors.White;
        public Color DividerColor { get; set; } = Colors.Black;
        public System.Collections.Generic.List<CanvasBarSegment> Segments { get; set; } = new();
        /// <summary>Bar height as a multiple of one character row (default 1.0).</summary>
        public double HeightScale { get; set; } = 1.0;
        /// <summary>Vertical offset from <see cref="Y"/> as a multiple of one character row.</summary>
        public double VerticalOffsetScale { get; set; } = 0.0;
        /// <summary>Optional per-segment color override (segment index, base color) for pulse feedback.</summary>
        public System.Func<int, Color, Color>? SegmentHighlight { get; set; }
    }
}

