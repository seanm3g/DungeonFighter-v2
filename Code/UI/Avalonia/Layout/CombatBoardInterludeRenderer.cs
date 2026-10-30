using System;
using RPGGame;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Center column for <see cref="CombatBoardLayout.IsInterlude"/>: between fights the board keeps its shape, so the
    /// dungeon log (room text, next-room choice, completion / death summary) floats over the backdrop in the board's
    /// log rect, with the hero health bar and name below it and the hero strip along the bottom.
    /// </summary>
    public static class CombatBoardInterludeRenderer
    {
        /// <summary>Clears the column above the hero strip before the log repaints (leftover fight board, old text).</summary>
        public static void ClearColumn(GameCanvasControl canvas)
        {
            int x = LayoutConstants.CENTER_PANEL_X;
            int w = LayoutConstants.CENTER_PANEL_WIDTH;
            int h = LayoutConstants.ACTION_INFO_Y;
            canvas.ClearTextInArea(x, 0, w, h);
            canvas.ClearBoxesInArea(x, 0, w, h);
            canvas.ClearProgressBarsInArea(x, 0, w, h);
            canvas.ClearSegmentedBarsInArea(x, 0, w, h);
            canvas.ClearLinesInArea(x, 0, w, h);
        }

        /// <summary>The log's label and the hero health bar; call after the log has painted. No frame or fill: the
        /// log floats over the room backdrop.</summary>
        public static void RenderChrome(GameCanvasControl canvas, Character? character, string label)
        {
            var (x, y, w, h) = CombatBoardLayout.InterludeBoxRect();
            if (!string.IsNullOrEmpty(label))
            {
                string text = $" {label} ";
                if (text.Length > w - 4)
                    text = text.Substring(0, Math.Max(0, w - 4));
                if (text.Length > 0)
                    canvas.AddText(x + 2, y, text, AsciiArtAssets.Colors.Cyan);
            }

            if (character == null || CombatBoardLayout.IsBetweenFights)
                return;
            int barY = CombatBoardLayout.HeroBarY;
            if (barY <= y + h)
                return;
            int hp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve($"player_{character.Name}", character.CurrentHealth);
            int max = character.GetEffectiveMaxHealth();
            canvas.AddHealthBar(x, barY, w, hp, max,
                entityId: $"player_{character.Name}",
                heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale);
            canvas.AddText(x + w + 2, barY, $"{hp}/{max}", AsciiArtAssets.Colors.White);
            string name = character.Name ?? "";
            canvas.AddText(x + Math.Max(0, (w - name.Length) / 2), barY + 1, name, AsciiArtAssets.Colors.Gold);
        }

        /// <summary>Box label: the screen title when it says something (ROOM CLEARED, YOU DIED…), else none — the log
        /// already announces the dungeon.</summary>
        public static string ResolveLabel(string title)
        {
            bool generic = string.IsNullOrWhiteSpace(title) || title == "DUNGEON FIGHTERS" || title == "COMBAT" || title == "COMBAT RESULT";
            return generic ? "" : title;
        }
    }
}
