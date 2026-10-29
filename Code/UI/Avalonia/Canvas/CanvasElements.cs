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
        /// Sub-cell horizontal shift in cells (e.g. 0.5 centers odd-length text in an even-width box).
        /// </summary>
        public double NudgeCellsX { get; set; }

        /// <summary>
        /// When true, drawn after normal text so opaque panels (e.g. hover tooltips) stay under this text only, not under body copy.
        /// </summary>
        public bool IsOverlay { get; set; }

        /// <summary>
        /// Full-screen scene text (title screen): skips center-column zoom and clip so it stays on the cell grid.
        /// </summary>
        public bool IgnoresCenterColumn { get; set; }
        
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

        /// <summary>
        /// Non-overlay box drawn after the room backdrop (still under text), so board cards stay solid over the scene.
        /// </summary>
        public bool AboveBackdrop { get; set; }
    }

    /// <summary>
    /// A bitmap region (e.g. one item icon on an atlas sheet) drawn inside a grid-cell rectangle.
    /// By default the region stays square and centered. <see cref="FillFrame"/> paints it across the whole cell.
    /// Drawn after non-overlay boxes and before text, so hover tips (overlay boxes) still cover it.
    /// </summary>
    public class CanvasImage
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public global::Avalonia.Media.IImage Source { get; set; } = null!;
        /// <summary>Pixel rectangle on <see cref="Source"/>.</summary>
        public global::Avalonia.Rect SourceRect { get; set; }
        /// <summary>Device pixels kept clear inside the cell rectangle on every side.</summary>
        public double PaddingPixels { get; set; } = 2;
        /// <summary>When true, the bitmap fills the cell. When false, it stays square and centered inside the cell.</summary>
        public bool FillFrame { get; set; }
        /// <summary>The bitmap was rendered at the cell rectangle's device size, so it is drawn without filtering.</summary>
        public bool PixelAligned { get; set; }
        /// <summary>Item art: redrawn as glyphs on its cells while the room plate uses the Ascii style (needs a <see cref="global::Avalonia.Media.Imaging.Bitmap"/> source).</summary>
        public bool FollowsAsciiStyle { get; set; }
        /// <summary>Drawn right after the bitmap, with the origin at the cell rectangle's top-left (DIPs).</summary>
        public System.Action<global::Avalonia.Media.DrawingContext>? Overlay { get; set; }
    }

    /// <summary>
    /// A full picture (the combat room's environment plate) scaled to cover a grid-cell rectangle, cropped to it and
    /// styled by <see cref="Look"/>. Drawn over non-overlay box fills, under everything else.
    /// </summary>
    public class CanvasBackdrop
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public SkiaSharp.SKImage Source { get; set; } = null!;
        /// <summary>Brightness, blend mode etc. (Settings → Appearance → Room Backdrop).</summary>
        public RPGGame.UI.Avalonia.Effects.RoomBackdropConfig Look { get; set; } = new();
        /// <summary>Device pixels kept clear inside the rectangle so the panel border stays visible.</summary>
        public double InsetPixels { get; set; } = 2;
        /// <summary>Whole-plate opacity on top of <see cref="Look"/> (the location crossfade, <see cref="LocationCrossfade"/>).</summary>
        public double Opacity { get; set; } = 1;
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

        /// <summary>Vertical black dividers as fractions of bar width (0–1), used by pack-enemy body boundaries.</summary>
        public System.Collections.Generic.IReadOnlyList<double>? DividerFractions { get; set; }
        /// <summary>Health bar (from AddHealthBar); the combat turn scrim never dims these.</summary>
        public bool IsHealthBar { get; set; }
        /// <summary>Drawn mirrored: the fill is anchored at the right edge and drains toward the left.</summary>
        public bool FillFromRight { get; set; }
    }

    public class CanvasBarSegment
    {
        public int FaceCount { get; set; }
        public Color Color { get; set; }
    }

    /// <summary>Straight connector line between two points in grid-cell units (fractional cells allowed).</summary>
    public class CanvasLine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public Color Color { get; set; } = Colors.White;
        public double ThicknessPixels { get; set; } = 1.5;
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
        /// <summary>When set, returns the d20 face (1–20) to mark with a pixel caret under the bar.</summary>
        public System.Func<int?>? RollMarkerRoll { get; set; }
        /// <summary>Draw the roll caret under the bar pointing up (top bar of a pair) instead of above it pointing down.</summary>
        public bool RollMarkerBelow { get; set; }
    }
}

