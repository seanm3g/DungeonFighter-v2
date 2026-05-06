using System;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the pre-weapon Training Ground offer (enter or skip).
    /// </summary>
    public class TrainingGroundOfferRenderer
    {
        private readonly GameCanvasControl canvas;

        public TrainingGroundOfferRenderer(
            GameCanvasControl canvas,
            ICanvasInteractionManager _,
            ICanvasTextManager __)
        {
            this.canvas = canvas;
        }

        public int RenderTrainingGroundOfferContent(int x, int y, int width, int height, Character character)
        {
            int boxWidth = Math.Min(70, width - 8);
            int boxHeight = 16;
            int boxX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, boxWidth);
            int boxY = y + Math.Max(2, (height / 2) - (boxHeight / 2));

            int cx = boxX + 2;
            int cy = boxY + 2;

            canvas.AddText(cx, cy++, "────────── Training Ground ──────────", AsciiArtAssets.Colors.Cyan);
            canvas.AddText(cx, cy++, $"{character.Name}: practice unarmed before choosing your weapon.", AsciiArtAssets.Colors.White);
            cy++;
            canvas.AddText(cx, cy++, "1. Enter Training Ground (1 room, Training Dummy)", AsciiArtAssets.Colors.Yellow);
            canvas.AddText(cx, cy++, "2. Skip tutorial — go to weapon selection", AsciiArtAssets.Colors.White);
            cy++;
            canvas.AddText(cx, cy++, "Press 1 or 2.", AsciiArtAssets.Colors.Gray);

            return cy - y;
        }
    }
}
