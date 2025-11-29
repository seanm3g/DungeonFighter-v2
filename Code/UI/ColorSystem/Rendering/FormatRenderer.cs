using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Renders colored text to different output formats (HTML, ANSI, Debug)
    /// </summary>
    public static class FormatRenderer
    {
        /// <summary>
        /// Renders colored text as HTML
        /// </summary>
        public static string RenderAsHtml(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
                
            var html = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                    
                var colorHex = ColorToHex(segment.Color);
                var escapedText = EscapeHtml(segment.Text);
                
                html.Append($"<span style=\"color: {colorHex}\">{escapedText}</span>");
            }
            
            return html.ToString();
        }
        
        /// <summary>
        /// Renders colored text as ANSI escape codes (for console output)
        /// </summary>
        public static string RenderAsAnsi(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
                
            var ansi = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                    
                var ansiCode = ColorToAnsi(segment.Color);
                var escapedText = EscapeAnsi(segment.Text);
                
                ansi.Append($"\x1b[{ansiCode}m{escapedText}\x1b[0m");
            }
            
            return ansi.ToString();
        }
        
        /// <summary>
        /// Renders colored text as a simple format string (for debugging)
        /// </summary>
        public static string RenderAsDebug(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
                
            var debug = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                    
                var colorName = GetColorName(segment.Color);
                debug.Append($"[{colorName}]{segment.Text}[/{colorName}]");
            }
            
            return debug.ToString();
        }
        
        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        
        private static string ColorToAnsi(Color color)
        {
            // Simple ANSI color mapping
            if (color == Colors.Red) return "31";
            if (color == Colors.Green) return "32";
            if (color == Colors.Blue) return "34";
            if (color == Colors.Yellow) return "33";
            if (color == Colors.Cyan) return "36";
            if (color == Colors.Magenta) return "35";
            if (color == Colors.White) return "37";
            if (color == Colors.Black) return "30";
            if (color == Colors.Gray) return "90";
            
            // Default to white
            return "37";
        }
        
        private static string EscapeHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
        
        private static string EscapeAnsi(string text)
        {
            // ANSI doesn't need much escaping, but we'll handle basic cases
            return text.Replace("\x1b", "\\x1b");
        }
        
        private static string GetColorName(Color color)
        {
            // Simple color name mapping for debug output
            if (color == Colors.Red) return "Red";
            if (color == Colors.Green) return "Green";
            if (color == Colors.Blue) return "Blue";
            if (color == Colors.Yellow) return "Yellow";
            if (color == Colors.Cyan) return "Cyan";
            if (color == Colors.Magenta) return "Magenta";
            if (color == Colors.White) return "White";
            if (color == Colors.Black) return "Black";
            if (color == Colors.Gray) return "Gray";
            
            return $"RGB({color.R},{color.G},{color.B})";
        }
    }
}

