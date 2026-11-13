using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Helpers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the pre-weapon Training Ground offer: two framed buttons (enter / skip) and a
    /// "don't show this again" checkbox in the bottom-right corner (<see cref="GameSettings.ShowTrainingGroundOffer"/>).
    /// Hover highlights come from <see cref="FighterRosterHoverState"/>; the mouse handler redraws this screen when it changes.
    /// </summary>
    public class TrainingGroundOfferRenderer
    {
        public const int ButtonWidth = 34;
        public const int ButtonHeight = 6;
        public const int ButtonGap = 6;

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;

        public TrainingGroundOfferRenderer(
            GameCanvasControl canvas,
            List<ClickableElement> clickableElements,
            ICanvasInteractionManager _,
            ICanvasTextManager __)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }

        /// <summary>Checkbox text for the current setting: checked when the offer is turned off.</summary>
        public static string CheckboxLabel(bool showOffer) => (showOffer ? "[ ]" : "[x]") + " Don't show this again";

        public const string SettingsHint = "(You can change this in Settings)";

        public int RenderTrainingGroundOfferContent(int x, int y, int width, int height, Character character)
        {
            // Drawn straight onto the menu cave backdrop (no panels), centered like the path intro screen.
            string title = "────────── Training Ground ──────────";
            string intro = $"{character.Name}: practice unarmed before choosing your weapon.";

            const int blockHeight = 3 + ButtonHeight + 2;
            int cy = y + Math.Max(0, (height / 2) - (blockHeight / 2));
            int startY = cy;

            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length), cy++, title, AsciiArtAssets.Colors.Cyan);
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, intro.Length), cy++, intro, AsciiArtAssets.Colors.White);
            cy += 2;

            int buttonW = Math.Min(ButtonWidth, Math.Max(16, (width - ButtonGap - 4) / 2));
            int rowW = buttonW * 2 + ButtonGap;
            int leftX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, rowW);
            int rightX = leftX + buttonW + ButtonGap;

            DrawButton(leftX, cy, buttonW, "[1] Enter Training Ground", "1",
                new[] { "1 room vs. a Training Dummy", "Learn the ropes unarmed" },
                AsciiArtAssets.Colors.Yellow);
            DrawButton(rightX, cy, buttonW, "[2] Skip Tutorial", "2",
                new[] { "Go straight to", "weapon selection" },
                AsciiArtAssets.Colors.Silver);
            cy += ButtonHeight + 1;

            string prompt = "Click a button or press 1 / 2.";
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, prompt.Length), cy++, prompt, AsciiArtAssets.Colors.Gray);

            DrawDontShowCheckbox(x, y, width, height);
            return cy - startY;
        }

        private void DrawButton(int bx, int by, int bw, string label, string key, string[] extra, Color color)
        {
            bool hovered = FighterRosterHoverState.IsOver(new ClickableElement { X = bx, Y = by, Width = bw, Height = ButtonHeight });
            BoxButton.Draw(canvas, clickableElements, bx, by, bw, ButtonHeight, label, key,
                color: color, borderThickness: 2, extraLines: new[] { "" }.Concat(extra).ToList(), hovered: hovered);
        }

        private void DrawDontShowCheckbox(int x, int y, int width, int height)
        {
            bool showOffer = GameSettings.Instance.ShowTrainingGroundOffer;
            string label = CheckboxLabel(showOffer);
            int boxX = x + width - Math.Max(label.Length, SettingsHint.Length) - 3;
            int boxY = y + height - 4;

            var element = new ClickableElement
            {
                X = boxX,
                Y = boxY,
                Width = label.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = TrainingGroundOfferHandler.ToggleDontShowKey,
                DisplayText = label
            };
            element.IsHovered = FighterRosterHoverState.IsOver(element);
            clickableElements.Add(element);

            var color = element.IsHovered ? BoxButton.HoverColor : showOffer ? AsciiArtAssets.Colors.Silver : AsciiArtAssets.Colors.White;
            canvas.AddText(boxX, boxY, label, color);
            canvas.AddText(boxX, boxY + 1, SettingsHint, AsciiArtAssets.Colors.DarkGray);
        }
    }
}
