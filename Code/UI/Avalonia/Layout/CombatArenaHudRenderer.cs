using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Draws fighter/enemy HP, armor, and d20 roll bars in the center combat arena.
    /// </summary>
    public static class CombatArenaHudRenderer
    {
        public static void Render(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            Character fighter,
            Enemy enemy,
            DisplayBuffer? combatBuffer,
            IReadOnlyList<string>? combatEnemyNamesForLogAlignment)
        {
            if (canvas == null || fighter == null || enemy == null)
                return;

            canvas.SetBattlePresentation(RPGGame.UI.Avalonia.CombatVisuals.BattlePresentation.Capture(fighter, enemy));
            canvas.ConfigureCombatScene(GameConfiguration.Instance.UICustomization.IllustratedCombat,
                enemy.Name, RPGGame.ActionInteractionLab.ActionInteractionLabSession.Current != null,
                RPGGame.Combat.Sequence.CombatVisualPlayback.ActorId(fighter),
                RPGGame.Combat.Sequence.CombatVisualPlayback.ActorId(enemy));

            RenderEnemyHud(canvas, textWriter, enemy, combatBuffer, combatEnemyNamesForLogAlignment, fighter.Name);
            RenderFighterHud(canvas, textWriter, fighter, combatBuffer, combatEnemyNamesForLogAlignment);
        }

        private static void RenderEnemyHud(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            Enemy enemy,
            DisplayBuffer? combatBuffer,
            IReadOnlyList<string>? combatEnemyNamesForLogAlignment,
            string? heroName)
        {
            CombatArenaHudLayout.GetEnemyHud(out int hx, out int hy, out int hw, out int hh);
            canvas.ClearTextInArea(hx, hy, hw, hh);
            canvas.ClearProgressBarsInArea(hx, hy, hw, hh);
            canvas.ClearSegmentedBarsInArea(hx, hy, hw, hh);

            string nameLine = enemy.Name ?? "Enemy";
            CombatArenaHudLayout.GetEnemyBars(out int bx, out int by, out int bw);
            if (nameLine.Length > bw)
                nameLine = nameLine.Substring(0, Math.Max(1, bw - 3)) + "...";
            int nameX = CombatArenaHudLayout.GetEnemyNameX(nameLine.Length);
            var nameSegments = EntityColorHelper.BuildEnemyNamePanelLineSegments(enemy, nameLine);
            textWriter.RenderSegments(nameSegments, nameX, hy);

            int maxArmor = Math.Max(0, enemy.Armor);
            bool hasArmor = maxArmor > 0;
            int barAreaRows = hasArmor
                ? Math.Max(3, D20ThresholdBarRenderer.CombatBarAreaRowCount)
                : D20ThresholdBarRenderer.CombatBarAreaRowCount;

            canvas.ClearProgressBarsInArea(bx, by, bw, barAreaRows);
            canvas.ClearSegmentedBarsInArea(bx, by, bw, barAreaRows);

            if (hasArmor)
            {
                canvas.AddHealthBar(
                    bx, by, bw, maxArmor, maxArmor,
                    AsciiArtAssets.Colors.DarkBlue, AsciiArtAssets.Colors.White,
                    entityId: $"enemy_{enemy.Name}_armor",
                    heightScale: D20ThresholdBarRenderer.CombatArmorHeightScale);
            }

            canvas.AddHealthBar(
                bx, by, bw, enemy.CurrentHealth, enemy.MaxHealth,
                entityId: HealthBarEntityId.ForActor(enemy) ?? $"enemy_{enemy.Name}",
                heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale,
                verticalOffsetScale: hasArmor ? D20ThresholdBarRenderer.CombatArmorHeightScale : 0.0);

            double rollOffset = hasArmor
                ? D20ThresholdBarRenderer.CombatStripVerticalOffsetWithArmor
                : D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor;
            D20ThresholdBarRenderer.RenderBar(
                canvas, bx, by, bw, enemy, ThresholdBarPanel.Enemy,
                D20ThresholdBarRenderer.CombatStripHeightScale, rollOffset);

            CombatArenaHudLayout.GetEnemyLog(out int lx, out int ly, out int lw, out int lh);
            canvas.AddBorder(lx, ly, lw, lh, AsciiArtAssets.Colors.White);
            CombatLocalLogView.RenderLane(
                canvas, textWriter, combatBuffer, CombatLocalLogLane.Enemy,
                lx + 1, ly + 1, Math.Max(1, lw - 2), Math.Max(1, lh - 2),
                combatEnemyNamesForLogAlignment, heroName);
        }

        private static void RenderFighterHud(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            Character fighter,
            DisplayBuffer? combatBuffer,
            IReadOnlyList<string>? combatEnemyNamesForLogAlignment)
        {
            CombatArenaHudLayout.GetFighterHud(out int hx, out int hy, out int hw, out int hh);
            canvas.ClearTextInArea(hx, hy, hw, hh);
            canvas.ClearProgressBarsInArea(hx, hy, hw, hh);
            canvas.ClearSegmentedBarsInArea(hx, hy, hw, hh);
            canvas.ClearBoxesInArea(hx, hy, hw, hh);

            CombatArenaHudLayout.GetFighterBars(out int bx, out int by, out int bw);
            int maxArmor = fighter.GetMaxArmor();
            bool hasArmor = maxArmor > 0;
            int barAreaRows = hasArmor
                ? Math.Max(3, D20ThresholdBarRenderer.CombatBarAreaRowCount)
                : D20ThresholdBarRenderer.CombatBarAreaRowCount;

            if (hasArmor)
            {
                canvas.AddHealthBar(
                    bx, by, bw, maxArmor, maxArmor,
                    AsciiArtAssets.Colors.DarkBlue, AsciiArtAssets.Colors.White,
                    entityId: $"player_{fighter.Name}_armor",
                    heightScale: D20ThresholdBarRenderer.CombatArmorHeightScale);
            }

            canvas.AddHealthBar(
                bx, by, bw, fighter.CurrentHealth, fighter.GetEffectiveMaxHealth(),
                entityId: HealthBarEntityId.ForActor(fighter) ?? $"player_{fighter.Name}",
                heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale,
                verticalOffsetScale: hasArmor ? D20ThresholdBarRenderer.CombatArmorHeightScale : 0.0);

            double rollOffset = hasArmor
                ? D20ThresholdBarRenderer.CombatStripVerticalOffsetWithArmor
                : D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor;
            D20ThresholdBarRenderer.RenderBar(
                canvas, bx, by, bw, fighter, ThresholdBarPanel.Hero,
                D20ThresholdBarRenderer.CombatStripHeightScale, rollOffset);

            int captionY = by + barAreaRows;
            if (captionY < hy + CombatArenaHudLayout.FighterBarsHeight)
            {
                string caption = hasArmor
                    ? $"Health {fighter.CurrentHealth}/{fighter.GetEffectiveMaxHealth()}  Armor {maxArmor}"
                    : $"Health {fighter.CurrentHealth}/{fighter.GetEffectiveMaxHealth()}";
                if (caption.Length > bw)
                    caption = caption.Substring(0, bw);
                canvas.AddText(bx, captionY, caption, AsciiArtAssets.Colors.White);
            }

            CombatArenaHudLayout.GetFighterLog(out int lx, out int ly, out int lw, out int lh);
            canvas.AddBorder(lx, ly, lw, lh, AsciiArtAssets.Colors.White);
            CombatLocalLogView.RenderLane(
                canvas, textWriter, combatBuffer, CombatLocalLogLane.Fighter,
                lx + 1, ly + 1, Math.Max(1, lw - 2), Math.Max(1, lh - 2),
                combatEnemyNamesForLogAlignment, fighter.Name);
        }
    }
}
