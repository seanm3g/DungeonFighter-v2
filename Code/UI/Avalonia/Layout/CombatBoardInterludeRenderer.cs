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
        /// <summary>Clears the column above the hero strip before the log repaints (leftover fight board, old text);
        /// between fights the log owns the strip band too, so that goes as well.</summary>
        public static void ClearColumn(GameCanvasControl canvas)
        {
            int x = LayoutConstants.CENTER_PANEL_X;
            int w = LayoutConstants.CENTER_PANEL_WIDTH;
            int h = CombatBoardLayout.IsBetweenFights
                ? LayoutConstants.ACTION_INFO_Y + LayoutConstants.ACTION_INFO_HEIGHT
                : LayoutConstants.ACTION_INFO_Y;
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

            if (CombatBoardLayout.IsBetweenFights && DungeonExitChoicePrompt.IsOpen)
                RenderExitChoiceButtons(canvas, x, y, w, h);

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

        /// <summary>
        /// Room cleared, stay or leave: two large buttons along the bottom of the log box (CONTINUE sends 1, LEAVE 2).
        /// </summary>
        private static void RenderExitChoiceButtons(GameCanvasControl canvas, int x, int y, int w, int h)
        {
            var targets = DungeonExitChoicePrompt.ClickTargets;
            DungeonExitChoicePrompt.RemoveTargets();
            const int buttonH = 5;
            int gap = Math.Max(4, w / 10);
            int buttonW = Math.Clamp((w - gap) / 2 - 2, 12, 34);
            int totalW = buttonW * 2 + gap;
            int bx = x + Math.Max(0, (w - totalW) / 2);
            int by = y + h - buttonH - 1;
            DrawExitButton(canvas, targets, bx, by, buttonW, buttonH, "CONTINUE", DungeonExitChoicePrompt.ContinueKey, RPGGame.UI.ColorSystem.ColorPaletteExtensions.GetColor(RPGGame.UI.ColorSystem.ColorPalette.Success));
            DrawExitButton(canvas, targets, bx + buttonW + gap, by, buttonW, buttonH, "LEAVE", DungeonExitChoicePrompt.LeaveKey, RPGGame.UI.ColorSystem.ColorPaletteExtensions.GetColor(RPGGame.UI.ColorSystem.ColorPalette.Warning));
        }

        private static void DrawExitButton(GameCanvasControl canvas, System.Collections.Generic.List<ClickableElement>? targets,
            int bx, int by, int bw, int bh, string label, string key, global::Avalonia.Media.Color accent)
        {
            var fill = global::Avalonia.Media.Color.FromRgb(236, 236, 236);
            // Above the backdrop: otherwise the room art paints over the fill and the buttons sink into the scene.
            canvas.AddBox(bx, by, bw, bh, accent, fill, borderThicknessPixels: 3, aboveBackdrop: true);
            canvas.AddText(bx + Math.Max(1, (bw - label.Length) / 2), by + bh / 2, label, global::Avalonia.Media.Color.FromRgb(20, 20, 20));
            targets?.Add(new ClickableElement
            {
                X = bx,
                Y = by,
                Width = bw,
                Height = bh,
                Type = ElementType.MenuOption,
                Value = key,
                DisplayText = DungeonExitChoicePrompt.ButtonTag + label
            });
        }

        /// <summary>Box label: the screen title when it says something (ROOM CLEARED, YOU DIED…), else none — the log
        /// already announces the dungeon.</summary>
        public static string ResolveLabel(string title)
        {
            bool generic = string.IsNullOrWhiteSpace(title) || title == "DUNGEON FIGHTERS" || title == "COMBAT" || title == "COMBAT RESULT"
                || title.StartsWith("DUNGEON COMPLETED", StringComparison.Ordinal);
            return generic ? "" : title;
        }
    }
}
