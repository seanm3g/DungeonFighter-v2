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
    public static class ActionInteractionLabMiscTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            StoredActionRollMatchesGetActionRoll(ref run, ref pass, ref fail);
            LabStepRoundTrip(ref run, ref pass, ref fail);
            ResolveD20ForNextStepUsesSelectedWhenNotRandom(ref run, ref pass, ref fail);
            SetLabEnemyFromLoaderSwapsEnemy(ref run, ref pass, ref fail);
            MapPageStepInput_MapsUndoAndStep(ref run, ref pass, ref fail);
            CharacterLabSnapshot_RoundTripIncludesGearAndStrip(ref run, ref pass, ref fail);
            LoadCharacterSnapshot_ReplacesLabHeroBaseline(ref run, ref pass, ref fail);
        }



        internal static void StoredActionRollMatchesGetActionRoll(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var character = TestDataBuilders.Character().WithName("LabRoll").Build();
            ActionSelector.ClearStoredRolls();
            ActionSelector.SetStoredActionRoll(character, 17);
            int roll = ActionSelector.GetActionRoll(character);
            TestBase.AssertEqual(17, roll, "GetActionRoll returns SetStoredActionRoll value", ref run, ref passed, ref failed);
            ActionSelector.ClearStoredRolls();
        }


        internal static void LabStepRoundTrip(ref int run, ref int passed, ref int failed)
        {
            var step = new LabStep(12, "Jab");
            TestBase.AssertEqual(12, step.D20, "LabStep D20", ref run, ref passed, ref failed);
            TestBase.AssertTrue(string.Equals(step.ForcedActionName, "Jab", StringComparison.Ordinal), "LabStep name", ref run, ref passed, ref failed);
        }


        internal static void ResolveD20ForNextStepUsesSelectedWhenNotRandom(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabResolveD20").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "Lab for ResolveD20", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            lab.UseRandomD20PerStep = false;
            lab.SelectedD20 = 14;
            TestBase.AssertEqual(14, lab.ResolveD20ForNextStep(), "Fixed mode uses SelectedD20", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void SetLabEnemyFromLoaderSwapsEnemy(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var types = EnemyLoader.GetAllEnemyTypes();
            if (types.Count == 0)
            {
                TestBase.AssertTrue(true, "SetLabEnemyFromLoader skipped (no Enemies.json next to test host)", ref run, ref passed, ref failed);
                return;
            }

            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabEnemySwap").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "SetLabEnemyFromLoader: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = types[types.Count > 1 ? 1 : 0];
            lab.SetLabEnemyFromLoader(pick, 1);
            TestBase.AssertFalse(string.IsNullOrEmpty(lab.LabEnemy.Name), "SetLabEnemyFromLoader sets enemy name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(lab.LabEnemy.MaxHealth > 0, "SetLabEnemyFromLoader sets valid enemy", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void MapPageStepInput_MapsUndoAndStep(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual("lab_step", ActionLabInputCoordinator.MapPageStepInput("pageup"), "Page Up maps to step", ref run, ref passed, ref failed);
            TestBase.AssertEqual("lab_undo", ActionLabInputCoordinator.MapPageStepInput("pagedown"), "Page Down maps to undo", ref run, ref passed, ref failed);
            TestBase.AssertEqual("lab_step", ActionLabInputCoordinator.MapPageStepInput(" PageUp "), "Page Up mapping is case-insensitive", ref run, ref passed, ref failed);
            TestBase.AssertNull(ActionLabInputCoordinator.MapPageStepInput("up"), "arrow up does not map to lab step", ref run, ref passed, ref failed);
        }


        internal static void CharacterLabSnapshot_RoundTripIncludesGearAndStrip(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            string snapName = "UnitTest_LabSnap_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                var hero = TestDataBuilders.Character().WithName("SnapHero").WithLevel(5).Build();
                var names = ActionLoader.GetAllActionNames();
                names.Sort(StringComparer.OrdinalIgnoreCase);
                if (names.Count == 0)
                {
                    TestBase.AssertTrue(false, "Need actions for snapshot strip test", ref run, ref passed, ref failed);
                    return;
                }

                var act = ActionLoader.GetAction(names[0]);
                if (act != null)
                {
                    act.IsComboAction = true;
                    hero.AddToCombo(act);
                }

                CharacterLabSnapshotService.SaveFromCharacter(hero, snapName, overwrite: true);
                var loaded = CharacterLabSnapshotService.Load(snapName);
                TestBase.AssertTrue(loaded != null, "Snapshot loads from disk", ref run, ref passed, ref failed);
                TestBase.AssertTrue(loaded!.ComboStripActionNames.Count > 0, "Snapshot stores combo strip", ref run, ref passed, ref failed);
                var restored = CharacterLabSnapshotService.CreateCharacter(loaded);
                TestBase.AssertEqual(5, restored.Level, "Snapshot restores level", ref run, ref passed, ref failed);
                TestBase.AssertTrue(
                    restored.GetComboActions().Select(a => a.Name).SequenceEqual(loaded.ComboStripActionNames),
                    "Snapshot restores strip names",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                CharacterLabSnapshotService.Delete(snapName);
            }
        }


        internal static void LoadCharacterSnapshot_ReplacesLabHeroBaseline(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            string snapName = "UnitTest_LabLoad_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            CombatManager? cm = null;
            try
            {
                var donor = TestDataBuilders.Character().WithName("Donor").WithLevel(7).Build();
                CharacterLabSnapshotService.SaveFromCharacter(donor, snapName, overwrite: true);
                var data = CharacterLabSnapshotService.Load(snapName)!;

                var starter = TestDataBuilders.Character().WithName("Starter").WithLevel(1).Build();
                cm = new CombatManager();
                ActionInteractionLabSession.Begin(starter, cm, () => { }, null);
                var lab = ActionInteractionLabSession.Current!;
                lab.LoadCharacterSnapshot(data);
                TestBase.AssertEqual(7, lab.LabPlayer.Level, "Load snapshot replaces lab hero level", ref run, ref passed, ref failed);
                TestBase.AssertTrue(
                    string.Equals(lab.LabPlayer.Name, "Donor", StringComparison.Ordinal),
                    "Load snapshot replaces lab hero name",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ActionInteractionLabSession.EndSession();
                CharacterLabSnapshotService.Delete(snapName);
            }
        }
    }
}
