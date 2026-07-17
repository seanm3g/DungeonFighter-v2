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
    public static class ActionInteractionLabEnemyPanelTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            RightPanelEnemyLabHover_IdFormat(ref run, ref pass, ref fail);
            EnemyLevelCaption_ShowsHeroDelta(ref run, ref pass, ref fail);
            RightPanelEnemyAdjustment_TryApplyWrongId(ref run, ref pass, ref fail);
            LabSession_ApplyLabEnemyLevelDelta_RebuildsDummy(ref run, ref pass, ref fail);
            CaptureSimulationSnapshot_IncludesEnemyLevel(ref run, ref pass, ref fail);
            TestCharacterFactory_DirectEnemy_ScalesByLevel(ref run, ref pass, ref fail);
            DirectStatEnemy_GetEffectiveStrengthUsesDamage(ref run, ref pass, ref fail);
            DirectStatEnemy_CombatLogSpeedMatchesPanelSeconds(ref run, ref pass, ref fail);
        }



        internal static void RightPanelEnemyLabHover_IdFormat(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual("rphover:", RightPanelEnemyLabHoverState.Prefix, "rphover prefix", ref run, ref passed, ref failed);
            TestBase.AssertEqual("rphover:enemy:level", ActionLabRightPanelEnemyAdjustment.EnemyLevelHoverId, "enemy level hover id", ref run, ref passed, ref failed);
        }


        internal static void EnemyLevelCaption_ShowsHeroDelta(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(EnemyLevelCaption_ShowsHeroDelta));
            var cap = ActionLabRightPanelEnemyAdjustment.FormatEnemyLevelCaptionWithHeroDelta;
            TestBase.AssertEqual("Lvl 9 (-2)", cap(9, 11), "enemy below hero (delta = enemy − hero)", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Lvl 11 (+2)", cap(11, 9), "enemy above hero", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Lvl 5 (0)", cap(5, 5), "same level", ref run, ref passed, ref failed);
        }


        internal static void RightPanelEnemyAdjustment_TryApplyWrongId(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabEnAdj").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "session", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            int lvl0 = lab.LabEnemy.Level;
            TestBase.AssertTrue(!ActionLabRightPanelEnemyAdjustment.TryApply(lab, "not_rphover", +1), "wrong id rejected", ref run, ref passed, ref failed);
            TestBase.AssertEqual(lvl0, lab.LabEnemy.Level, "level unchanged", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void LabSession_ApplyLabEnemyLevelDelta_RebuildsDummy(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabEnLv").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            int hp1 = lab.LabEnemy.MaxHealth;
            lab.ApplyLabEnemyLevelDelta(1);
            TestBase.AssertEqual(2, lab.LabEnemy.Level, "enemy level 2", ref run, ref passed, ref failed);
            TestBase.AssertTrue(lab.LabEnemy.MaxHealth >= hp1, "scaled HP at least L1", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabRightPanelEnemyAdjustment.TryApply(lab, ActionLabRightPanelEnemyAdjustment.EnemyLevelHoverId, -1), "TryApply -1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, lab.LabEnemy.Level, "back to 1", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void CaptureSimulationSnapshot_IncludesEnemyLevel(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabSnapEn").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }
            lab.ApplyLabEnemyLevelDelta(2);
            var snap = lab.CaptureSimulationSnapshot();
            TestBase.AssertEqual(3, snap.EnemyLevel, "snapshot enemy level", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void TestCharacterFactory_DirectEnemy_ScalesByLevel(ref int run, ref int passed, ref int failed)
        {
            var cfg = LabCombatSnapshot.DefaultTestEnemyBattleConfig;
            var e1 = TestCharacterFactory.CreateTestEnemy(cfg, 0, 1);
            var e3 = TestCharacterFactory.CreateTestEnemy(cfg, 0, 3);
            TestBase.AssertEqual(1, e1.Level, "factory L1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(3, e3.Level, "factory L3", ref run, ref passed, ref failed);
            TestBase.AssertTrue(e3.MaxHealth >= e1.MaxHealth, "higher level has >= HP", ref run, ref passed, ref failed);
        }


        internal static void DirectStatEnemy_GetEffectiveStrengthUsesDamage(ref int run, ref int passed, ref int failed)
        {
            var cfg = LabCombatSnapshot.DefaultTestEnemyBattleConfig;
            var e = TestCharacterFactory.CreateTestEnemy(cfg, 0, 1);
            TestBase.AssertTrue(e.UsesDirectCombatStats(), "dummy is direct-stat", ref run, ref passed, ref failed);
            TestBase.AssertTrue(e.GetEffectiveStrength() > 0, "effective strength uses damage", ref run, ref passed, ref failed);
        }


        /// <summary>
        /// Enemy subclasses Character: combat log speed must use <see cref="Enemy.GetTotalAttackSpeed"/> (right panel Spd),
        /// not <see cref="Character.GetTotalAttackSpeed"/> / SpeedCalculator, for direct-stat lab dummies.
        /// </summary>
        internal static void DirectStatEnemy_CombatLogSpeedMatchesPanelSeconds(ref int run, ref int passed, ref int failed)
        {
            _ = GameConfiguration.Instance;
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var cfg = LabCombatSnapshot.DefaultTestEnemyBattleConfig;
            var enemy = TestCharacterFactory.CreateTestEnemy(cfg, 0, 1);
            TestBase.AssertTrue(enemy.UsesDirectCombatStats(), "dummy is direct-stat for speed test", ref run, ref passed, ref failed);
            if (!enemy.UsesDirectCombatStats() || enemy.ActionPool.Count == 0)
                return;

            var strike = enemy.ActionPool[0].action;
            double panelSpd = enemy.GetTotalAttackSpeed();
            double expected = panelSpd * strike.Length;
            double actual = ActionSpeedCalculator.CalculateActualActionSpeed(enemy, strike);
            TestBase.AssertTrue(
                Math.Abs(actual - expected) < 0.02,
                $"log speed ≈ Spd×Len (expect {expected:F3}s, got {actual:F3}s)",
                ref run, ref passed, ref failed);
        }
    }
}
