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
    }
}

