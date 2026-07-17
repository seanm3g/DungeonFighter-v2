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
    public static class ActionInteractionLabComboTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            EnemyComboSelectionUsesComboStepIndex(ref run, ref pass, ref fail);
            LabEnemyTurnUsesEnemyPoolNotForcedCatalog(ref run, ref pass, ref fail);
            LabTotalActionTicks_StepUndoSimAndFightReset(ref run, ref pass, ref fail);
            WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll(ref run, ref pass, ref fail);
            StepBlockedAfterCombatantDeath(ref run, ref pass, ref fail);
        }



        /// <summary>
        /// With effective INT ≥ threshold, enemies use <see cref="Character.ComboStep"/> for combo picks (same ordered rule as heroes).
        /// </summary>
        internal static void EnemyComboSelectionUsesComboStepIndex(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            ActionSelector.ClearStoredRolls();

            var enemy = new Enemy(
                name: "ComboStepEnemy",
                level: 1,
                maxHealth: 80,
                damage: 8,
                armor: 0,
                attackSpeed: 1.0,
                primaryAttribute: PrimaryAttribute.Strength,
                isLiving: true,
                archetype: EnemyArchetype.Berserker,
                useDirectStats: true);
            enemy.Intelligence = GameConstants.ComboSequenceIntelligenceThreshold;

            var a1 = new Action("EC_ONE", comboOrder: 0, damageMultiplier: 1.0, length: 1.0, isComboAction: true);
            var a2 = new Action("EC_TWO", comboOrder: 1, damageMultiplier: 1.0, length: 1.0, isComboAction: true);
            enemy.AddToCombo(a1);
            enemy.AddToCombo(a2);

            Dice.SetTestRoll(20);
            ActionSelector.SetStoredActionRoll(enemy, 20);
            enemy.ComboStep = 0;
            var pick0 = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(pick0 != null && string.Equals(pick0.Name, "EC_ONE", StringComparison.Ordinal),
                "Enemy ComboStep 0 selects first combo action", ref run, ref passed, ref failed);

            enemy.ComboStep = 1;
            Dice.SetTestRoll(20);
            ActionSelector.SetStoredActionRoll(enemy, 20);
            var pick1 = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(pick1 != null && string.Equals(pick1.Name, "EC_TWO", StringComparison.Ordinal),
                "Enemy ComboStep 1 selects second combo action", ref run, ref passed, ref failed);

            Dice.ClearTestRoll();
            ActionSelector.ClearStoredRolls();
        }


        /// <summary>
        /// Lab steps pass a catalog action for the hero; enemy turns must still resolve from the enemy's <see cref="Actor.ActionPool"/>, not that name.
        /// </summary>
        internal static void LabEnemyTurnUsesEnemyPoolNotForcedCatalog(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var drunk = ActionLoader.GetAction("DRUNKEN BRAWLER");
            if (drunk == null)
            {
                TestBase.AssertTrue(true, "LabEnemyTurnUsesEnemyPool skipped (no DRUNKEN BRAWLER)", ref run, ref passed, ref failed);
                return;
            }

            var types = EnemyLoader.GetAllEnemyTypes();
            if (types == null || !types.Any(t => string.Equals(t, "Goblin", StringComparison.OrdinalIgnoreCase)))
            {
                TestBase.AssertTrue(true, "LabEnemyTurnUsesEnemyPool skipped (no Goblin in Enemies.json)", ref run, ref passed, ref failed);
                return;
            }

            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabEnemyPool").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabEnemyTurnUsesEnemyPool: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.SetLabEnemyFromLoader("Goblin", 1);
            if (!drunk.IsComboAction)
                drunk.IsComboAction = true;
            lab.LabPlayer.AddToCombo(drunk);

            // Speed order can schedule multiple player turns before the Goblin; step until the enemy actually acts.
            Action? used = null;
            for (int i = 0; i < 40; i++)
            {
                var r = lab.StepAsync(10, "DRUNKEN BRAWLER").GetAwaiter().GetResult();
                if (r != CombatSingleTurnResult.Advanced)
                    break;
                used = ActionExecutor.GetLastUsedAction(lab.LabEnemy);
                if (used != null)
                    break;
            }

            bool poolOk = used != null
                && !string.Equals(used.Name, "DRUNKEN BRAWLER", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(used.Name, "JAB", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(used.Name, "TAUNT", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(poolOk,
                $"Lab enemy turn should use Goblin pool (JAB/TAUNT), not catalog DRUNKEN BRAWLER; got '{used?.Name ?? "null"}'",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// <see cref="ActionInteractionLabSession.LabTotalActionTicks"/> tracks interactive steps (history length),
        /// adds encounter turn totals after batch sim, drops on undo, and resets when the lab clears fight history.
        /// </summary>
        internal static void LabTotalActionTicks_StepUndoSimAndFightReset(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "LabTotalActionTicks skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabActionTicks").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabTotalActionTicks: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertEqual(0, lab.LabTotalActionTicks, "LabTotalActionTicks starts at 0", ref run, ref passed, ref failed);

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a);
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            lab.LabPlayer.AddToCombo(jab);
            lab.SelectedCatalogActionName = "JAB";

            var stepResult = lab.StepAsync(18, "JAB").GetAwaiter().GetResult();
            TestBase.AssertTrue(
                stepResult == CombatSingleTurnResult.Advanced
                || stepResult == CombatSingleTurnResult.EnemyDefeated
                || stepResult == CombatSingleTurnResult.PlayerDefeated,
                "LabTotalActionTicks step returns a recorded outcome",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, lab.LabTotalActionTicks, "LabTotalActionTicks after one step", ref run, ref passed, ref failed);

            var report = new ActionLabEncounterSimulationReport();
            report.Encounters.Add(new EncounterMetrics { Turns = 3 });
            report.Encounters.Add(new EncounterMetrics { Turns = 5 });
            lab.RecordEncounterSimulationTurns(report);
            TestBase.AssertEqual(9, lab.LabTotalActionTicks, "LabTotalActionTicks includes simulated encounter turns", ref run, ref passed, ref failed);

            lab.UndoLastStepAsync().GetAwaiter().GetResult();
            TestBase.AssertEqual(8, lab.LabTotalActionTicks, "LabTotalActionTicks after undo (sim total kept)", ref run, ref passed, ref failed);

            EnemyLoader.LoadEnemies();
            var types = EnemyLoader.GetAllEnemyTypes();
            if (types == null || types.Count == 0)
            {
                TestBase.AssertTrue(true, "LabTotalActionTicks fight reset skipped (no loader enemies)", ref run, ref passed, ref failed);
            }
            else
            {
                types.Sort(StringComparer.OrdinalIgnoreCase);
                lab.SetLabEnemyFromLoader(types[0], 1);
                TestBase.AssertEqual(0, lab.LabTotalActionTicks, "LabTotalActionTicks cleared when fight history resets", ref run, ref passed, ref failed);
            }

            ActionInteractionLabSession.EndSession();
        }


        /// <summary>
        /// <see cref="ActionSelector.WouldNaturalRollSelectComboAction"/> must agree with
        /// <see cref="ActionSelector.SelectActionBasedOnRoll"/> for every d20 on a typical lab hero.
        /// </summary>
        internal static void WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "WouldNaturalRollSelectComboAction_MatchesSelectActionBasedOnRoll skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabComboPred").Build();
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            hero.AddAction(jab, 1.0);
            hero.Actions.AddToCombo(jab);

            for (int r = 1; r <= 20; r++)
            {
                Dice.SetTestRoll(r);
                ActionSelector.ClearStoredRolls();
                var sel = ActionSelector.SelectActionBasedOnRoll(hero);
                bool expectCombo = sel != null && sel.IsComboAction;
                bool pred = ActionSelector.WouldNaturalRollSelectComboAction(hero, r);
                TestBase.AssertEqual(expectCombo, pred, $"WouldNaturalRollSelectComboAction roll {r}", ref run, ref passed, ref failed);
            }

            Dice.ClearTestRoll();
            ActionSelector.ClearStoredRolls();
        }


        /// <summary>
        /// After a fighter dies in the lab log, forward stepping is disabled until undo or reset restores the encounter.
        /// </summary>
        internal static void StepBlockedAfterCombatantDeath(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabStepDeath").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "StepBlockedAfterCombatantDeath: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertTrue(lab.CanStepForward, "Can step at fight start", ref run, ref passed, ref failed);

            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "StepBlockedAfterCombatantDeath skipped (no JAB)", ref run, ref passed, ref failed);
                ActionInteractionLabSession.EndSession();
                return;
            }

            foreach (var a in lab.LabPlayer.GetComboActions().ToList())
                lab.LabPlayer.RemoveFromCombo(a);
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            lab.LabPlayer.AddToCombo(jab);
            lab.SelectedCatalogActionName = "JAB";

            lab.StepAsync(18, "JAB").GetAwaiter().GetResult();
            TestBase.AssertEqual(1, lab.History.Count, "First step recorded", ref run, ref passed, ref failed);

            lab.LabEnemy.CurrentHealth = 0;
            TestBase.AssertFalse(lab.CanStepForward, "Cannot step after enemy death", ref run, ref passed, ref failed);

            lab.StepAsync(lab.ResolveD20ForNextStep(), lab.SelectedCatalogActionName).GetAwaiter().GetResult();
            TestBase.AssertEqual(1, lab.History.Count, "Blocked step does not add history", ref run, ref passed, ref failed);

            lab.UndoLastStepAsync().GetAwaiter().GetResult();
            TestBase.AssertTrue(lab.CanStepForward, "Undo restores stepping after death", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.History.Count, "Undo clears death step", ref run, ref passed, ref failed);

            lab.StepAsync(18, "JAB").GetAwaiter().GetResult();
            lab.LabEnemy.CurrentHealth = 0;
            lab.ResetLabEncounterAsync().GetAwaiter().GetResult();
            TestBase.AssertTrue(lab.CanStepForward, "Reset restores stepping after death", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.History.Count, "Reset clears history", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }
    }
}
