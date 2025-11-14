using System.Collections.Generic;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Interface for rendering colored text to different output formats
    /// </summary>
    public interface IColoredTextRenderer
    {
        /// <summary>
        /// Renders colored text segments to the target output
        /// </summary>
        void Render(IEnumerable<ColoredText> segments, int x, int y);
        
        /// <summary>
        /// Renders colored text segments with wrapping
        /// </summary>
        void RenderWrapped(IEnumerable<ColoredText> segments, int x, int y, int maxWidth);
        
        /// <summary>
        /// Renders colored text segments centered within a width
        /// </summary>
        void RenderCentered(IEnumerable<ColoredText> segments, int x, int y, int width);
        
        /// <summary>
        /// Renders colored text segments with padding
        /// </summary>
        void RenderPadded(IEnumerable<ColoredText> segments, int x, int y, int minWidth, char paddingChar = ' ');
        
        /// <summary>
        /// Gets the display length of colored text (ignoring color markup)
        /// </summary>
        int GetDisplayLength(IEnumerable<ColoredText> segments);
        
        /// <summary>
        /// Truncates colored text to a maximum length
        /// </summary>
        List<ColoredText> Truncate(IEnumerable<ColoredText> segments, int maxLength);
        
        /// <summary>
        /// Pads colored text to a minimum length
        /// </summary>
        List<ColoredText> PadRight(IEnumerable<ColoredText> segments, int minLength, char paddingChar = ' ');
        
        /// <summary>
        /// Centers colored text within a given width
        /// </summary>
        List<ColoredText> Center(IEnumerable<ColoredText> segments, int width, char paddingChar = ' ');
    }
}
