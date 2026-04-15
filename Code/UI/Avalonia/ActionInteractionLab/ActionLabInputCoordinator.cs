using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.ActionInteractionLab;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Handles clicks for Action Lab control tokens (<c>lab_*</c>) from the main canvas or the tools pop-out window.
    /// </summary>
    public static class ActionLabInputCoordinator
    {
        /// <summary>
        /// Moves <see cref="ActionInteractionLabSession.CatalogScrollOffset"/> by <paramref name="delta"/> rows
        /// (negative = toward earlier names, same as ▲ more). Optionally refreshes combat UI when <paramref name="canvasUI"/> is non-null.
        /// </summary>
        public static void ApplyCatalogScrollOffsetDelta(ActionInteractionLabSession session, int delta, CanvasUICoordinator? canvasUI)
        {
            if (delta == 0) return;
            var names = ActionLoader.GetAllActionNames();
            names.Sort(StringComparer.OrdinalIgnoreCase);
            int visible = session.LastCatalogVisibleRowCount > 0
                ? session.LastCatalogVisibleRowCount
                : ActionInteractionLabSession.LabCatalogVisibleNameRows;
            int maxScroll = Math.Max(0, names.Count - visible);
            if (delta < 0)
                session.CatalogScrollOffset = Math.Max(0, session.CatalogScrollOffset + delta);
            else
                session.CatalogScrollOffset = Math.Min(maxScroll, session.CatalogScrollOffset + delta);

            if (canvasUI != null)
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
        }

        public static async Task HandleLabControlAsync(string value, CanvasUICoordinator? canvasUI, GameCoordinator? game)
        {
            if (canvasUI == null || game == null) return;
            var session = ActionInteractionLabSession.Current;
            if (session == null) return;

            void RefreshLabCombat()
            {
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
            }

            if (value == "lab_exit")
            {
                game.ExitActionInteractionLab();
                try { canvasUI.GetMainWindow()?.Activate(); } catch { /* ignore */ }
                return;
            }

            if (value == "lab_d20_random")
            {
                session.UseRandomD20PerStep = true;
                RefreshLabCombat();
                return;
            }

            if (value.StartsWith("lab_d20:", StringComparison.Ordinal))
            {
                if (int.TryParse(value.AsSpan("lab_d20:".Length), out int d) && d >= 1 && d <= 20)
                {
                    session.UseRandomD20PerStep = false;
                    session.SelectedD20 = d;
                }
                RefreshLabCombat();
                return;
            }

            if (value.StartsWith("lab_act:", StringComparison.Ordinal))
            {
                if (int.TryParse(value.AsSpan("lab_act:".Length), out int idx))
                {
                    var names = ActionLoader.GetAllActionNames();
                    names.Sort(StringComparer.OrdinalIgnoreCase);
                    if (idx >= 0 && idx < names.Count)
                    {
                        session.SelectedCatalogActionName = names[idx];
                        session.AddSelectedCatalogActionToComboStrip();
                    }
                }
                return;
            }

            if (value == "lab_combo_prev")
            {
                session.NudgeLabPlayerComboStep(-1);
                return;
            }

            if (value == "lab_combo_next")
            {
                session.NudgeLabPlayerComboStep(1);
                return;
            }

            if (value == "lab_scrl:up")
            {
                ApplyCatalogScrollOffsetDelta(session, -1, canvasUI);
                return;
            }

            if (value == "lab_scrl:down")
            {
                ApplyCatalogScrollOffsetDelta(session, +1, canvasUI);
                return;
            }

            if (value == "lab_enemy_up")
            {
                session.EnemyCatalogScrollOffset = Math.Max(0, session.EnemyCatalogScrollOffset - 1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_enemy_down")
            {
                var enemyTypes = EnemyLoader.GetAllEnemyTypes();
                enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
                const int enemyVisible = 2;
                int maxScroll = Math.Max(0, enemyTypes.Count - enemyVisible);
                session.EnemyCatalogScrollOffset = Math.Min(maxScroll, session.EnemyCatalogScrollOffset + 1);
                RefreshLabCombat();
                return;
            }

            if (value.StartsWith("lab_enemy:", StringComparison.Ordinal))
            {
                if (int.TryParse(value.AsSpan("lab_enemy:".Length), out int idx))
                {
                    var enemyTypes = EnemyLoader.GetAllEnemyTypes();
                    enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
                    if (idx >= 0 && idx < enemyTypes.Count)
                        session.SetLabEnemyFromLoader(enemyTypes[idx], 1);
                }
                return;
            }

            if (value == "lab_reset_combo")
            {
                session.ResetLabCombo();
                return;
            }

            if (value == "lab_step")
            {
                await session.StepAsync(session.ResolveD20ForNextStep(), session.SelectedCatalogActionName).ConfigureAwait(true);
                return;
            }

            if (value == "lab_undo")
            {
                await session.UndoLastStepAsync().ConfigureAwait(true);
            }
        }
    }
}
