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
            // Drawn straight onto the menu cave backdrop (no panels), centered like the path intro screen.
            string title = "────────── Training Ground ──────────";
            string intro = $"{character.Name}: practice unarmed before choosing your weapon.";
            string enter = "1. Enter Training Ground (1 room, Training Dummy)";
            string skip = "2. Skip tutorial — go to weapon selection";
            string prompt = "Press 1 or 2.";

            // Options share one left edge so the numbers line up.
            int optionsWidth = Math.Max(enter.Length, skip.Length);
            int optionsX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, optionsWidth);

            const int blockHeight = 7;
            int cy = y + Math.Max(0, (height / 2) - (blockHeight / 2));
            int startY = cy;

            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length), cy++, title, AsciiArtAssets.Colors.Cyan);
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, intro.Length), cy++, intro, AsciiArtAssets.Colors.White);
            cy++;
            canvas.AddText(optionsX, cy++, enter, AsciiArtAssets.Colors.Yellow);
            canvas.AddText(optionsX, cy++, skip, AsciiArtAssets.Colors.White);
            cy++;
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, prompt.Length), cy++, prompt, AsciiArtAssets.Colors.Gray);

            return cy - startY;
        }
    }
}
