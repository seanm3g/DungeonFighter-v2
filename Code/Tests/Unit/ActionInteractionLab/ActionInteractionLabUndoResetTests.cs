using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Combat.Formatting;
using RPGGame.Entity.Services;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.ActionInteractionLab
{
    public static class ActionInteractionLabUndoResetTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            UndoReplayPreservesComboStripEdits(ref run, ref pass, ref fail);
            UndoReplayPreservesLabStatEdits(ref run, ref pass, ref fail);
            UndoLastStep_RemovesOneStepFromMultiStepHistory(ref run, ref pass, ref fail);
            UndoLastStep_BumpsInputEpochSoQueuedStepsCanBeDropped(ref run, ref pass, ref fail);
            LabCombatLogSnapshot_CloneFromDeepCopiesLines(ref run, ref pass, ref fail);
            ResetLabEncounterZerosBothSteps(ref run, ref pass, ref fail);
            ResetLabEncounterAsync_ClearsHistoryHpEffectsKeepsStripEnemy(ref run, ref pass, ref fail);
            ResetLabEncounterAsync_RestoresCombatLogEnemyAlignment(ref run, ref pass, ref fail);
        }



        /// <summary>
        /// Undo replays from initial clone JSON; combo strip edits must be reapplied so Back only removes the last combat step.
        /// </summary>
        internal static void UndoReplayPreservesComboStripEdits(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "UndoReplayPreservesComboStripEdits skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabUndoStrip").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UndoReplayPreservesComboStripEdits: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            lab.AddSelectedCatalogActionToComboStrip();
            int comboCountBeforeStep = lab.LabPlayer.GetComboActions().Count;
            var stripNamesBefore = lab.LabPlayer.GetComboActions().Select(a => a.Name).ToList();

            lab.StepAsync(lab.ResolveD20ForNextStep(), lab.SelectedCatalogActionName).GetAwaiter().GetResult();
            lab.UndoLastStepAsync().GetAwaiter().GetResult();

            int comboCountAfterUndo = lab.LabPlayer.GetComboActions().Count;
            var stripNamesAfter = lab.LabPlayer.GetComboActions().Select(a => a.Name).ToList();

            TestBase.AssertEqual(comboCountBeforeStep, comboCountAfterUndo, "Undo preserves combo strip count", ref run, ref passed, ref failed);
            TestBase.AssertEqual(stripNamesBefore.Count, stripNamesAfter.Count, "Undo preserves strip name list length", ref run, ref passed, ref failed);
            for (int i = 0; i < stripNamesBefore.Count; i++)
            {
                TestBase.AssertTrue(
                    string.Equals(stripNamesBefore[i], stripNamesAfter[i], StringComparison.OrdinalIgnoreCase),
                    $"Undo preserves strip order [{i}]",
                    ref run, ref passed, ref failed);
            }

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Undo replay must not restore hero from stale <c>_initialPlayerJson</c> for left-panel lab stat edits.
        /// </summary>
        internal static void UndoReplayPreservesLabStatEdits(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "UndoReplayPreservesLabStatEdits skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabUndoStats").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UndoReplayPreservesLabStatEdits: session null", ref run, ref passed, ref failed);
                return;
            }

            string p = ActionLabLeftPanelStatAdjustment.StatHoverPrefix;
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, p + "str", +4), "str +4 for undo stat test", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, p + "armor", +3), "armor +3 for undo stat test", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, p + "actionslots", +1), "slots +1 for undo stat test", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabLeftPanelStatAdjustment.TryApply(lab.LabPlayer, ActionLabLeftPanelStatAdjustment.HeroLevelHoverId, +2), "level +2 for undo stat test", ref run, ref passed, ref failed);

            int expectStr = lab.LabPlayer.Stats.Strength;
            int expectArmorBonus = lab.LabPlayer.ActionLabArmorBonus;
            int expectSlotBonus = lab.LabPlayer.ActionLabActionSlotBonus;
            int expectLevel = lab.LabPlayer.Level;

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            lab.AddSelectedCatalogActionToComboStrip();

            lab.StepAsync(lab.ResolveD20ForNextStep(), lab.SelectedCatalogActionName).GetAwaiter().GetResult();
            lab.UndoLastStepAsync().GetAwaiter().GetResult();

            TestBase.AssertEqual(expectStr, lab.LabPlayer.Stats.Strength, "Undo preserves lab STR", ref run, ref passed, ref failed);
            TestBase.AssertEqual(expectArmorBonus, lab.LabPlayer.ActionLabArmorBonus, "Undo preserves ActionLabArmorBonus", ref run, ref passed, ref failed);
            TestBase.AssertEqual(expectSlotBonus, lab.LabPlayer.ActionLabActionSlotBonus, "Undo preserves ActionLabActionSlotBonus", ref run, ref passed, ref failed);
            TestBase.AssertEqual(expectLevel, lab.LabPlayer.Level, "Undo preserves lab level", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Back removes only the last interactive step; remaining history length and fighters stay consistent after muted replay.
        /// </summary>
        internal static void UndoLastStep_RemovesOneStepFromMultiStepHistory(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "UndoLastStep_RemovesOneStep skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabUndoMulti").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UndoLastStep_RemovesOneStep: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            lab.AddSelectedCatalogActionToComboStrip();
            lab.SelectedD20 = 16;
            lab.UseRandomD20PerStep = false;

            for (int i = 0; i < 3; i++)
                lab.StepAsync(lab.ResolveD20ForNextStep(), lab.SelectedCatalogActionName).GetAwaiter().GetResult();

            int historyAfterSteps = lab.History.Count;
            TestBase.AssertTrue(historyAfterSteps >= 1, "Undo multi: at least one step recorded", ref run, ref passed, ref failed);

            lab.UndoLastStepAsync().GetAwaiter().GetResult();
            TestBase.AssertEqual(historyAfterSteps - 1, lab.History.Count, "Undo removes exactly one history step", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!lab.IsReplayingHistory, "Undo leaves IsReplayingHistory false", ref run, ref passed, ref failed);
            TestBase.AssertTrue(lab.CanStepForward, "Undo leaves CanStepForward true when both alive", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Undo bumps <see cref="ActionInteractionLabSession.InputEpoch"/> so Steps that waited behind the gate can be dropped.
        /// </summary>
        internal static void UndoLastStep_BumpsInputEpochSoQueuedStepsCanBeDropped(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "UndoLastStep_BumpsInputEpoch skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabUndoEpoch").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UndoLastStep_BumpsInputEpoch: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            lab.AddSelectedCatalogActionToComboStrip();
            lab.StepAsync(16, lab.SelectedCatalogActionName).GetAwaiter().GetResult();

            int epochBeforeUndo = lab.InputEpoch;
            lab.UndoLastStepAsync().GetAwaiter().GetResult();
            TestBase.AssertTrue(lab.InputEpoch > epochBeforeUndo, "Undo bumps InputEpoch", ref run, ref passed, ref failed);

            int epochBeforeReset = lab.InputEpoch;
            lab.StepAsync(16, lab.SelectedCatalogActionName).GetAwaiter().GetResult();
            lab.ResetLabEncounterAsync().GetAwaiter().GetResult();
            TestBase.AssertTrue(lab.InputEpoch > epochBeforeReset, "Reset bumps InputEpoch", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.History.Count, "Reset clears history after epoch bump", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void LabCombatLogSnapshot_CloneFromDeepCopiesLines(ref int run, ref int passed, ref int failed)
        {
            var originalSeg = new ColoredText("hit", Avalonia.Media.Colors.Red);
            var source = new List<(List<ColoredText>, UIMessageType)>
            {
                (new List<ColoredText> { originalSeg }, UIMessageType.System)
            };
            var snap = LabCombatLogSnapshot.CloneFrom(source);
            TestBase.AssertEqual(1, snap.Lines.Count, "CloneFrom keeps line count", ref run, ref passed, ref failed);
            TestBase.AssertEqual("hit", snap.Lines[0].Segments[0].Text, "CloneFrom copies text", ref run, ref passed, ref failed);
            originalSeg.Text = "mutated";
            TestBase.AssertEqual("hit", snap.Lines[0].Segments[0].Text, "CloneFrom deep-copies ColoredText", ref run, ref passed, ref failed);
        }


        internal static void ResetLabEncounterZerosBothSteps(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabResetCombo").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ResetLabEncounterZerosBothSteps: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.LabPlayer.ComboStep = 4;
            lab.LabEnemy.ComboStep = 2;
            lab.ResetLabEncounterAsync().GetAwaiter().GetResult();
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "ResetLabEncounter zeros LabPlayer.ComboStep", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabEnemy.ComboStep, "ResetLabEncounter zeros LabEnemy.ComboStep", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// <see cref="ActionInteractionLabSession.ResetLabEncounterAsync"/> clears undo history and DoT, refills HP,
        /// and leaves the combo strip and current enemy unchanged.
        /// </summary>
        internal static void ResetLabEncounterAsync_ClearsHistoryHpEffectsKeepsStripEnemy(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "ResetLabEncounterAsync_ClearsHistory skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabEncounterReset").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ResetLabEncounterAsync_ClearsHistory: session null", ref run, ref passed, ref failed);
                return;
            }

            string enemyName = lab.LabEnemy.Name;
            int enemyLevel = lab.LabEnemy.Level;

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a, ignoreWeaponRequirement: true);
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            lab.LabPlayer.AddToCombo(jab);
            int stripCount = lab.LabPlayer.GetComboActions().Count;
            lab.SelectedCatalogActionName = "JAB";

            lab.LabPlayer.CurrentHealth = 3;
            lab.LabEnemy.CurrentHealth = 7;
            lab.LabPlayer.PoisonPercentOfMaxHealth = 4;
            lab.LabPlayer.ComboStep = 3;

            lab.StepAsync(18, "JAB").GetAwaiter().GetResult();
            TestBase.AssertTrue(lab.LabTotalActionTicks > 0, "ResetLabEncounter test: history recorded a step", ref run, ref passed, ref failed);

            lab.ResetLabEncounterAsync().GetAwaiter().GetResult();

            TestBase.AssertEqual(0, lab.History.Count, "ResetLabEncounter clears step history", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabTotalActionTicks, "ResetLabEncounter zeros LabTotalActionTicks", ref run, ref passed, ref failed);
            TestBase.AssertEqual(lab.LabPlayer.GetEffectiveMaxHealth(), lab.LabPlayer.CurrentHealth, "ResetLabEncounter refills hero HP", ref run, ref passed, ref failed);
            TestBase.AssertEqual(lab.LabEnemy.GetEffectiveMaxHealth(), lab.LabEnemy.CurrentHealth, "ResetLabEncounter refills enemy HP", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0.0, lab.LabPlayer.PoisonPercentOfMaxHealth, "ResetLabEncounter clears poison %", ref run, ref passed, ref failed);
            TestBase.AssertEqual(stripCount, lab.LabPlayer.GetComboActions().Count, "ResetLabEncounter keeps combo strip size", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "ResetLabEncounter zeros hero combo step", ref run, ref passed, ref failed);
            TestBase.AssertTrue(string.Equals(enemyName, lab.LabEnemy.Name, StringComparison.Ordinal),
                "ResetLabEncounter keeps same enemy name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(enemyLevel, lab.LabEnemy.Level, "ResetLabEncounter keeps enemy level", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Center-panel clear during lab reset wipes sticky enemy alignment names; reset must re-register the lab enemy.
        /// </summary>
        internal static void ResetLabEncounterAsync_RestoresCombatLogEnemyAlignment(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var hero = TestDataBuilders.Character().WithName("LabAlignReset").Build();
            var ctx = new CanvasContextManager();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(
                hero,
                combatManager,
                () => { },
                ctx,
                prepareLabHistoryReplay: () => ctx.ClearCombatLogEnemyAlignmentSticky());

            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ResetLabEncounterAsync_RestoresCombatLogEnemyAlignment: session null", ref run, ref passed, ref failed);
                return;
            }

            string enemyName = lab.LabEnemy.Name;
            TestBase.AssertEqual(enemyName, ctx.GetCombatLogEnemyAlignmentName(), "Begin registers lab enemy for log alignment", ref run, ref passed, ref failed);

            ctx.ClearCombatLogEnemyAlignmentSticky();
            TestBase.AssertNull(ctx.GetCombatLogEnemyAlignmentName(), "sticky cleared before reset", ref run, ref passed, ref failed);

            lab.ResetLabEncounterAsync().GetAwaiter().GetResult();

            TestBase.AssertEqual(enemyName, ctx.GetCombatLogEnemyAlignmentName(),
                "ResetLabEncounter restores combat log enemy alignment after center panel clear",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }
    }
}
