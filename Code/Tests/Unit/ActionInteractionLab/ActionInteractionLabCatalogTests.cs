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
    public static class ActionInteractionLabCatalogTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            LabCatalogSelectionAddsActionToCombo(ref run, ref pass, ref fail);
            LabRemoveFromComboShrinksSequence(ref run, ref pass, ref fail);
            LabEnemyUsesPlayerComboStripForAmplification(ref run, ref pass, ref fail);
            ActionLabCatalogSync_EnemyNextUsesPlayerStrip(ref run, ref pass, ref fail);
            ActionLabCatalogSync_EnemyNextUsesPlayerStripWhenEnemyHasCombo(ref run, ref pass, ref fail);
            LabSessionSyncCatalogMatchesComputeHelper(ref run, ref pass, ref fail);
            LabCatalogSyncShowsSecondSlotWhenComboStepOne(ref run, ref pass, ref fail);
            LabNudgeComboStepClampsStrip(ref run, ref pass, ref fail);
            LabSequenceEdit_ResetsStripPositionToFirstSlot(ref run, ref pass, ref fail);
            AddSelectedCatalogToStripHelperAddsAction(ref run, ref pass, ref fail);
            AddSelectedCatalogToStrip_AppendsToEnd(ref run, ref pass, ref fail);
            ApplyCatalogScrollOffsetDelta_Clamps(ref run, ref pass, ref fail);
            ApplyEnemyCatalogScrollOffsetDelta_Clamps(ref run, ref pass, ref fail);
        }



        /// <summary>
        /// Mirrors catalog click behavior: resolve name → GetAction → ensure combo-eligible → AddToCombo on lab clone.
        /// </summary>
        internal static void LabCatalogSelectionAddsActionToCombo(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "LabCatalogSelectionAddsActionToCombo skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabComboCatalog").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabCatalogSelectionAddsActionToCombo: session null", ref run, ref passed, ref failed);
                return;
            }

            int before = lab.LabPlayer.GetComboActions().Count;
            string actionName = names[0];
            var action = ActionLoader.GetAction(actionName);
            TestBase.AssertTrue(action != null, "LabCatalogSelectionAddsActionToCombo GetAction", ref run, ref passed, ref failed);
            if (action != null)
            {
                if (!action.IsComboAction)
                    action.IsComboAction = true;
                lab.LabPlayer.AddToCombo(action);
            }

            int after = lab.LabPlayer.GetComboActions().Count;
            TestBase.AssertTrue(after > before, "LabCatalogSelectionAddsActionToCombo increases combo count", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void LabRemoveFromComboShrinksSequence(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabComboRemove").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabRemoveFromComboShrinksSequence: session null", ref run, ref passed, ref failed);
                return;
            }

            var names = ActionLoader.GetAllActionNames();
            var action = ActionLoader.GetAction("JAB");
            if (action == null && names.Count > 0)
                action = ActionLoader.GetAction(names[0]);
            TestBase.AssertTrue(action != null, "LabRemoveFromComboShrinksSequence GetAction", ref run, ref passed, ref failed);
            if (action == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            if (!action.IsComboAction)
                action.IsComboAction = true;
            lab.LabPlayer.AddToCombo(action);
            var combo = lab.LabPlayer.GetComboActions();
            TestBase.AssertTrue(combo.Count > 0, "LabRemoveFromComboShrinksSequence has entry", ref run, ref passed, ref failed);
            int n = combo.Count;
            // Remove the catalog action we added (not necessarily index 0 — that slot may be a required weapon basic).
            lab.LabPlayer.RemoveFromCombo(action);
            TestBase.AssertEqual(n - 1, lab.LabPlayer.GetComboActions().Count, "LabRemoveFromComboShrinksSequence removes one", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Lab enemies have no combo strip; amplification should follow the lab player's configured sequence.
        /// </summary>
        internal static void LabEnemyUsesPlayerComboStripForAmplification(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "LabEnemyUsesPlayerComboStripForAmplification skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabAmpStrip").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabEnemyUsesPlayerComboStripForAmplification: session null", ref run, ref passed, ref failed);
                return;
            }

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names.Count > 1 ? names[1] : names[0]);
            TestBase.AssertTrue(a != null && b != null, "LabEnemyUsesPlayerComboStripForAmplification GetAction", ref run, ref passed, ref failed);
            if (a == null || b == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            lab.LabPlayer.AddToCombo(a);
            lab.LabPlayer.AddToCombo(b);

            // Begin() picks a random catalog enemy; strip must be empty for this amplification scenario.
            foreach (var act in lab.LabEnemy.GetComboActions().ToList())
                lab.LabEnemy.RemoveFromCombo(act);
            TestBase.AssertTrue(lab.LabEnemy.GetComboActions().Count == 0, "Lab enemy has empty combo strip for amp test", ref run, ref passed, ref failed);

            lab.LabEnemy.ComboStep = 1;
            double mult = ActionUtilities.CalculateDamageMultiplier(lab.LabEnemy, a);
            double baseAmp = lab.LabEnemy.GetComboAmplifier();
            double expected = System.Math.Pow(baseAmp, 1);
            TestBase.AssertTrue(System.Math.Abs(mult - expected) < 0.0001,
                $"Lab enemy amp should use player strip length (expect {expected:F4}, got {mult:F4})", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Pure helper: when next is lab enemy with no strip, catalog name comes from hero strip index.
        /// </summary>
        internal static void ActionLabCatalogSync_EnemyNextUsesPlayerStrip(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "ActionLabCatalogSync_EnemyNext skipped (need 2+ actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabSyncPure").Build();
            var enemy = TestCharacterFactory.CreateTestEnemy(
                new BattleConfiguration
                {
                    PlayerDamage = 10,
                    PlayerAttackSpeed = 1.0,
                    PlayerArmor = 0,
                    PlayerHealth = 100,
                    EnemyDamage = 10,
                    EnemyAttackSpeed = 0.65,
                    EnemyArmor = 5,
                    EnemyHealth = 150
                }, 0);

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names[1]);
            if (a == null || b == null)
            {
                TestBase.AssertTrue(false, "ActionLabCatalogSync_EnemyNext GetAction", ref run, ref passed, ref failed);
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            hero.AddToCombo(a);
            hero.AddToCombo(b);
            TestBase.AssertTrue(enemy.GetComboActions().Count == 0, "Test enemy has empty combo strip", ref run, ref passed, ref failed);

            hero.ComboStep = 1;
            string? pick = ActionLabCatalogSync.ComputeSelectedCatalogName(enemy, hero, enemy);
            var heroStrip = hero.GetComboActions();
            TestBase.AssertTrue(heroStrip.Count >= 2, "ActionLabCatalogSync_EnemyNext player strip has 2 actions", ref run, ref passed, ref failed);
            if (heroStrip.Count < 2)
                return;
            TestBase.AssertTrue(
                pick != null && string.Equals(pick, heroStrip[1].Name, StringComparison.OrdinalIgnoreCase),
                "ComputeSelectedCatalogName uses player strip when enemy next and enemy strip empty",
                ref run, ref passed, ref failed);
        }


        /// <summary>
        /// Loader enemies can have a non-empty combo strip; catalog must still follow the hero's combo step, not slot 0 on the enemy.
        /// </summary>
        internal static void ActionLabCatalogSync_EnemyNextUsesPlayerStripWhenEnemyHasCombo(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "ActionLabCatalogSync_EnemyHasCombo skipped (need 2+ actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabSyncEnemyCombo").Build();
            var enemy = TestCharacterFactory.CreateTestEnemy(
                new BattleConfiguration
                {
                    PlayerDamage = 10,
                    PlayerAttackSpeed = 1.0,
                    PlayerArmor = 0,
                    PlayerHealth = 100,
                    EnemyDamage = 10,
                    EnemyAttackSpeed = 0.65,
                    EnemyArmor = 5,
                    EnemyHealth = 150
                }, 0);

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names[1]);
            if (a == null || b == null)
            {
                TestBase.AssertTrue(false, "ActionLabCatalogSync_EnemyHasCombo GetAction", ref run, ref passed, ref failed);
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            hero.AddToCombo(a);
            hero.AddToCombo(b);
            enemy.AddToCombo(a);
            enemy.AddToCombo(b);
            TestBase.AssertTrue(enemy.GetComboActions().Count >= 2, "Test enemy has combo strip", ref run, ref passed, ref failed);

            hero.ComboStep = 1;
            enemy.ComboStep = 0;
            string? pick = ActionLabCatalogSync.ComputeSelectedCatalogName(enemy, hero, enemy);
            var heroStrip = hero.GetComboActions();
            TestBase.AssertTrue(heroStrip.Count >= 2, "ActionLabCatalogSync_EnemyHasCombo player strip has 2 actions", ref run, ref passed, ref failed);
            if (heroStrip.Count < 2)
                return;
            TestBase.AssertTrue(
                pick != null && string.Equals(pick, heroStrip[1].Name, StringComparison.OrdinalIgnoreCase),
                "ComputeSelectedCatalogName ignores enemy strip when enemy has combo; uses hero step",
                ref run, ref passed, ref failed);
        }


        internal static void LabSessionSyncCatalogMatchesComputeHelper(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "LabSessionSyncCatalogMatchesComputeHelper skipped", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabSyncSession").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabSessionSyncCatalog: session null", ref run, ref passed, ref failed);
                return;
            }

            var a = ActionLoader.GetAction(names[0]);
            var b = ActionLoader.GetAction(names[1]);
            if (a == null || b == null)
            {
                ActionInteractionLabSession.EndSession();
                TestBase.AssertTrue(false, "LabSessionSyncCatalog GetAction", ref run, ref passed, ref failed);
                return;
            }

            if (!a.IsComboAction) a.IsComboAction = true;
            if (!b.IsComboAction) b.IsComboAction = true;
            lab.LabPlayer.AddToCombo(a);
            lab.LabPlayer.AddToCombo(b);

            lab.SyncCatalogSelectionToUpcomingActor();
            var expected = ActionLabCatalogSync.ComputeSelectedCatalogName(lab.GetNextActorToAct(), lab.LabPlayer, lab.LabEnemy);
            TestBase.AssertTrue(
                expected != null && string.Equals(lab.SelectedCatalogActionName, expected, StringComparison.OrdinalIgnoreCase),
                "Session SyncCatalogSelectionToUpcomingActor matches ComputeSelectedCatalogName",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Catalog sync follows hero <see cref="Character.ComboStep"/>: at step 1 with a two-action strip, selection is the second action (e.g. SLAM after RAGE).
        /// </summary>
        internal static void LabCatalogSyncShowsSecondSlotWhenComboStepOne(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var rage = ActionLoader.GetAction("RAGE");
            var slam = ActionLoader.GetAction("SLAM");
            if (rage == null || slam == null)
            {
                TestBase.AssertTrue(true, "LabCatalogSyncSecondSlot skipped (no RAGE/SLAM)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabCatalogStep").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabCatalogSyncSecondSlot: session null", ref run, ref passed, ref failed);
                return;
            }

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a);
            if (!rage.IsComboAction) rage.IsComboAction = true;
            if (!slam.IsComboAction) slam.IsComboAction = true;
            lab.LabPlayer.AddToCombo(rage);
            lab.LabPlayer.AddToCombo(slam);
            var combo = lab.LabPlayer.GetComboActions();
            TestBase.AssertTrue(combo.Count >= 2, "Strip should have two actions", ref run, ref passed, ref failed);
            lab.LabPlayer.ComboStep = 1;
            lab.SyncCatalogSelectionToUpcomingActor();

            string expectedSecond = combo[1].Name;
            var nextName = ActionLabCatalogSync.ComputeSelectedCatalogName(lab.GetNextActorToAct(), lab.LabPlayer, lab.LabEnemy);
            TestBase.AssertTrue(
                nextName != null && string.Equals(nextName, expectedSecond, StringComparison.OrdinalIgnoreCase),
                $"Catalog sync at ComboStep 1 should be second strip slot ({expectedSecond}); got '{nextName}'",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void LabNudgeComboStepClampsStrip(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var rage = ActionLoader.GetAction("RAGE");
            var slam = ActionLoader.GetAction("SLAM");
            if (rage == null || slam == null)
            {
                TestBase.AssertTrue(true, "LabNudgeComboStep skipped (no RAGE/SLAM)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabNudge").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabNudgeComboStep: session null", ref run, ref passed, ref failed);
                return;
            }

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a);
            if (!rage.IsComboAction) rage.IsComboAction = true;
            if (!slam.IsComboAction) slam.IsComboAction = true;
            lab.LabPlayer.AddToCombo(rage);
            lab.LabPlayer.AddToCombo(slam);
            int n = lab.LabPlayer.GetComboActions().Count;
            lab.LabPlayer.ComboStep = 0;
            lab.NudgeLabPlayerComboStep(-1);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep % n, "Nudge -1 from first slot stays on first slot", ref run, ref passed, ref failed);
            lab.NudgeLabPlayerComboStep(1);
            TestBase.AssertEqual(1, lab.LabPlayer.ComboStep % n, "Nudge +1 from first slot advances to second", ref run, ref passed, ref failed);
            lab.NudgeLabPlayerComboStep(1);
            TestBase.AssertEqual(1, lab.LabPlayer.ComboStep % n, "Nudge +1 from last slot stays on last slot", ref run, ref passed, ref failed);
            lab.NudgeLabPlayerComboStep(-1);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep % n, "Nudge -1 from last slot returns to first", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void LabSequenceEdit_ResetsStripPositionToFirstSlot(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "LabSequenceEdit_ResetsStripPosition skipped (need 2+ actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabStripReset").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabSequenceEdit_ResetsStripPosition: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.LabPlayer.ActionLabActionSlotBonus = 8;
            lab.IgnoreActionRequirements = true;
            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a, ignoreWeaponRequirement: true);

            lab.SelectedCatalogActionName = names[0];
            lab.AddSelectedCatalogActionToComboStrip();
            lab.SelectedCatalogActionName = names[1];
            lab.AddSelectedCatalogActionToComboStrip();
            int stripCount = lab.LabPlayer.GetComboActions().Count;
            TestBase.AssertTrue(stripCount >= 2, $"LabSequenceEdit: strip has 2+ actions (got {stripCount})", ref run, ref passed, ref failed);
            if (stripCount < 2)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            lab.LabPlayer.ComboStep = 1;
            lab.SelectedCatalogActionName = names[0];
            lab.AddSelectedCatalogActionToComboStrip();
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "Add to sequence resets ComboStep to first slot", ref run, ref passed, ref failed);

            lab.LabPlayer.ComboStep = 1;
            var strip = lab.LabPlayer.GetComboActions();
            Action? removeTarget = strip[strip.Count - 1];
            TestBase.AssertTrue(
                lab.TryRemoveFromLabCombo(removeTarget),
                "TryRemoveFromLabCombo succeeds for removable strip action",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "Remove from sequence resets ComboStep to first slot", ref run, ref passed, ref failed);

            lab.LabPlayer.ComboStep = 1;
            lab.ResetLabStripPositionToFirstSlot();
            TestBase.AssertEqual(0, lab.LabPlayer.ComboStep, "ResetLabStripPositionToFirstSlot zeros ComboStep", ref run, ref passed, ref failed);
            var stripAfter = lab.LabPlayer.GetComboActions();
            if (stripAfter.Count > 0)
            {
                TestBase.AssertTrue(
                    string.Equals(lab.SelectedCatalogActionName, stripAfter[0].Name, StringComparison.OrdinalIgnoreCase),
                    "ResetLabStripPosition syncs catalog to first strip action",
                    ref run, ref passed, ref failed);
            }

            ActionInteractionLabSession.EndSession();
        }


        internal static void AddSelectedCatalogToStripHelperAddsAction(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "AddSelectedCatalogToStrip skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabAddStrip").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "AddSelectedCatalogToStrip: session null", ref run, ref passed, ref failed);
                return;
            }

            string pick = names[names.Count > 1 ? 1 : 0];
            lab.SelectedCatalogActionName = pick;
            int before = lab.LabPlayer.GetComboActions().Count;
            lab.AddSelectedCatalogActionToComboStrip();
            int after = lab.LabPlayer.GetComboActions().Count;
            TestBase.AssertTrue(after > before, "AddSelectedCatalogActionToComboStrip increases combo count", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                lab.LabPlayer.GetComboActions().Any(a => string.Equals(a.Name, pick, StringComparison.OrdinalIgnoreCase)),
                "Strip contains selected catalog action",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// Catalog adds must append to the strip end even when the action's JSON ComboOrder is 0.
        /// </summary>
        internal static void AddSelectedCatalogToStrip_AppendsToEnd(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var names = ActionLoader.GetAllActionNames();
            if (names.Count < 2)
            {
                TestBase.AssertTrue(true, "AddSelectedCatalogToStrip_AppendsToEnd skipped (need 2+ actions)", ref run, ref passed, ref failed);
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var hero = TestDataBuilders.Character().WithName("LabAppendStrip").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "AddSelectedCatalogToStrip_AppendsToEnd: session null", ref run, ref passed, ref failed);
                return;
            }

            string firstName = names[0];
            string secondName = names[1];
            var first = ActionLoader.GetAction(firstName);
            var second = ActionLoader.GetAction(secondName);
            TestBase.AssertTrue(first != null && second != null, "AddSelectedCatalogToStrip_AppendsToEnd GetAction", ref run, ref passed, ref failed);
            if (first == null || second == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a, ignoreWeaponRequirement: true);

            if (!first.IsComboAction)
                first.IsComboAction = true;
            first.ComboOrder = 1;
            lab.LabPlayer.AddToCombo(first);

            lab.SelectedCatalogActionName = secondName;
            if (!second.IsComboAction)
                second.IsComboAction = true;
            second.ComboOrder = 0;
            lab.AddSelectedCatalogActionToComboStrip();

            var strip = lab.LabPlayer.GetComboActions();
            TestBase.AssertEqual(2, strip.Count, "AddSelectedCatalogToStrip_AppendsToEnd strip length", ref run, ref passed, ref failed);
            if (strip.Count < 2)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            TestBase.AssertTrue(
                string.Equals(strip[0].Name, firstName, StringComparison.OrdinalIgnoreCase),
                "AddSelectedCatalogToStrip_AppendsToEnd keeps first action at slot 0",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                string.Equals(strip[1].Name, secondName, StringComparison.OrdinalIgnoreCase),
                "AddSelectedCatalogToStrip_AppendsToEnd places new action at end",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void ApplyCatalogScrollOffsetDelta_Clamps(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabCatalogScroll").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ApplyCatalogScrollOffsetDelta_Clamps: session null", ref run, ref passed, ref failed);
                return;
            }

            var names = ActionLoader.GetAllActionNames();
            names.Sort(StringComparer.OrdinalIgnoreCase);
            if (names.Count == 0)
            {
                ActionInteractionLabSession.EndSession();
                TestBase.AssertTrue(true, "ApplyCatalogScrollOffsetDelta_Clamps skipped (no actions)", ref run, ref passed, ref failed);
                return;
            }

            lab.LastCatalogVisibleRowCount = 4;
            int maxScroll = Math.Max(0, names.Count - 4);
            lab.CatalogScrollOffset = 0;
            ActionLabInputCoordinator.ApplyCatalogScrollOffsetDelta(lab, -5, null);
            TestBase.AssertEqual(0, lab.CatalogScrollOffset, "negative delta clamps at 0", ref run, ref passed, ref failed);

            lab.CatalogScrollOffset = maxScroll;
            ActionLabInputCoordinator.ApplyCatalogScrollOffsetDelta(lab, 5, null);
            TestBase.AssertEqual(maxScroll, lab.CatalogScrollOffset, "positive delta clamps at maxScroll", ref run, ref passed, ref failed);

            lab.CatalogScrollOffset = 1;
            ActionLabInputCoordinator.ApplyCatalogScrollOffsetDelta(lab, -1, null);
            TestBase.AssertEqual(0, lab.CatalogScrollOffset, "single step toward earlier names", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void ApplyEnemyCatalogScrollOffsetDelta_Clamps(ref int run, ref int passed, ref int failed)
        {
            EnemyLoader.LoadEnemies();
            var enemyTypes = EnemyLoader.GetAllEnemyTypes();
            enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
            if (enemyTypes.Count == 0)
            {
                TestBase.AssertTrue(true, "ApplyEnemyCatalogScrollOffsetDelta_Clamps skipped (no Enemies.json)", ref run, ref passed, ref failed);
                return;
            }

            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabEnemyCatalogScroll").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "ApplyEnemyCatalogScrollOffsetDelta_Clamps: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.LastEnemyCatalogVisibleRowCount = 10;
            int visible = 10;
            int maxScroll = Math.Max(0, enemyTypes.Count - visible);
            lab.EnemyCatalogScrollOffset = 0;
            ActionLabInputCoordinator.ApplyEnemyCatalogScrollOffsetDelta(lab, -5, null);
            TestBase.AssertEqual(0, lab.EnemyCatalogScrollOffset, "enemy negative delta clamps at 0", ref run, ref passed, ref failed);

            lab.EnemyCatalogScrollOffset = maxScroll;
            ActionLabInputCoordinator.ApplyEnemyCatalogScrollOffsetDelta(lab, 5, null);
            TestBase.AssertEqual(maxScroll, lab.EnemyCatalogScrollOffset, "enemy positive delta clamps at maxScroll", ref run, ref passed, ref failed);

            lab.EnemyCatalogScrollOffset = 1;
            ActionLabInputCoordinator.ApplyEnemyCatalogScrollOffsetDelta(lab, -1, null);
            TestBase.AssertEqual(0, lab.EnemyCatalogScrollOffset, "enemy single step toward earlier types", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }
    }
}
