using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// Framed, clickable button on the character grid: a border box with its label centered inside. Clicking sends
    /// <c>key</c> as input (<see cref="ElementType.MenuOption"/>); the frame and label turn yellow on hover.
    /// Used by the hub-style screens (fighter roster, inventory side buttons, action sequence editor, travel popup).
    /// </summary>
    public static class BoxButton
    {
        public static Color DefaultColor => ColorPalette.Gold.GetColor();
        public static Color HoverColor => ColorPalette.Yellow.GetColor();

        /// <summary>Draws the button and registers its click target; returns the element (hover state already resolved).</summary>
        public static ClickableElement Draw(
            GameCanvasControl canvas,
            List<ClickableElement> clickables,
            int x,
            int y,
            int width,
            int height,
            string label,
            string key,
            Color? color = null,
            int borderThickness = 1,
            IReadOnlyList<string>? extraLines = null,
            bool? hovered = null)
        {
            width = Math.Max(3, width);
            height = Math.Max(1, height);
            var element = new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Type = ElementType.MenuOption,
                Value = key,
                DisplayText = label
            };
            clickables.Add(element);
            // Screens redrawn on hover pass the pointer state in, since a fresh element starts un-hovered.
            if (hovered.HasValue)
                element.IsHovered = hovered.Value;
            Color drawColor = element.IsHovered ? HoverColor : color ?? DefaultColor;

            if (height >= 3)
                canvas.AddBorder(x, y, width, height, drawColor, element.IsHovered ? borderThickness + 1 : borderThickness);

            int lineCount = 1 + (extraLines?.Count ?? 0);
            int innerW = Math.Max(1, width - 2);
            int textY = y + Math.Max(0, (height - lineCount) / 2);
            DrawCentered(canvas, x, innerW, textY, label, drawColor);
            if (extraLines != null)
            {
                var dim = element.IsHovered ? drawColor : AsciiArtAssets.Colors.Gray;
                for (int i = 0; i < extraLines.Count; i++)
                {
                    int lineY = textY + 1 + i;
                    if (lineY >= y + height - (height >= 3 ? 1 : 0))
                        break;
                    DrawCentered(canvas, x, innerW, lineY, extraLines[i], dim);
                }
            }
            return element;
        }

        private static void DrawCentered(GameCanvasControl canvas, int x, int innerW, int y, string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (text.Length > innerW)
                text = innerW > 1 ? text.Substring(0, innerW - 1) + "." : text.Substring(0, innerW);
            int textX = x + 1 + Math.Max(0, (innerW - text.Length) / 2);
            canvas.AddText(textX, y, text, color);
        }
    }
}
