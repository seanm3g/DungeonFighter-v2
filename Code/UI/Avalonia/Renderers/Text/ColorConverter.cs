using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Text
{

    /// <summary>
    /// Handles color conversion utilities for canvas rendering.
    /// </summary>
    public static class ColorConverter
    {
        /// <summary>
        /// Converts Avalonia color to canvas color format
        /// 
        /// This method does NOT store or hardcode color values. It relies entirely on the
        /// color configuration system (ColorPalette.json, ColorCodes.json) which are loaded
        /// dynamically. Colors passed to this method should already be resolved through:
        /// - ColorPalette.GetColor() - loads from ColorPalette.json
        /// - ColorCodeLoader.GetColor() - loads from ColorCodes.json
        /// - ColorPatterns.GetColorForPattern() - uses pattern-based mapping
        /// 
        /// The only processing done here is ensuring visibility on black background.
        /// The canvas supports any RGB color value directly, so no mapping is needed.
        /// </summary>
        public static Color ConvertToCanvasColor(Color color)
        {
            // Ensure color is visible on black background, then return as-is
            // All color values come from the JSON configuration system, not hardcoded here
            // The canvas can render any RGB color value directly
            return ColorValidator.EnsureVisible(color);
        }
    }
}

