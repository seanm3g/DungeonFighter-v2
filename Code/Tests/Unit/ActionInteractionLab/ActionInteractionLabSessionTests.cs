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
    public static class ActionInteractionLabSessionTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            LabSessionBeginsWithDefaultD20Selection16(ref run, ref pass, ref fail);
            LabSessionBegin_UsesCurrentTuningBaseHealth(ref run, ref pass, ref fail);
            LabSessionBegin_PicksDefaultCatalogEnemyWhenAvailable(ref run, ref pass, ref fail);
            LabPlayerIsActiveForDisplayWhenInLabState(ref run, ref pass, ref fail);
            CanvasContextRestoredAfterEndSession(ref run, ref pass, ref fail);
            LabBeginDoesNotResetGlobalGameTime(ref run, ref pass, ref fail);
            RefreshGameDataAsync_ReloadsAndPreservesComboStrip(ref run, ref pass, ref fail);
        }



        internal static void LabSessionBeginsWithDefaultD20Selection16(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabDefaultD20").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "Lab session exists for default d20", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            TestBase.AssertFalse(lab.UseRandomD20PerStep, "Lab starts with fixed d20 mode (not random)", ref run, ref passed, ref failed);
            TestBase.AssertEqual(16, lab.SelectedD20, "Lab default SelectedD20 is 16", ref run, ref passed, ref failed);
            TestBase.AssertEqual(16, lab.ResolveD20ForNextStep(), "ResolveD20ForNextStep uses default SelectedD20", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void LabSessionBegin_UsesCurrentTuningBaseHealth(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var cfg = GameConfiguration.Instance;
            int savedBase = cfg.Character.PlayerBaseHealth;
            try
            {
                cfg.Character.PlayerBaseHealth = 150;
                var hero = TestDataBuilders.Character().WithName("LabTuningHp").WithLevel(1).Build();
                hero.MaxHealth = 60;
                hero.CurrentHealth = 60;

                var combatManager = new CombatManager();
                ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
                var lab = ActionInteractionLabSession.Current;
                TestBase.AssertTrue(lab != null, "LabSessionBegin_UsesCurrentTuningBaseHealth: session exists", ref run, ref passed, ref failed);
                if (lab == null)
                {
                    ActionInteractionLabSession.EndSession();
                    return;
                }

                TestBase.AssertEqual(150, lab.LabPlayer.MaxHealth, "lab clone uses tuning base health, not stale save max", ref run, ref passed, ref failed);
                TestBase.AssertEqual(150, lab.LabPlayer.GetEffectiveMaxHealth(), "lab clone effective max matches tuning", ref run, ref passed, ref failed);
                ActionInteractionLabSession.EndSession();
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = savedBase;
                ActionInteractionLabSession.EndSession();
            }
        }


        internal static void LabSessionBegin_PicksDefaultCatalogEnemyWhenAvailable(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var types = EnemyLoader.GetAllEnemyTypes();
            if (types.Count == 0)
            {
                TestBase.AssertTrue(true, "LabSessionBegin_PicksDefaultCatalogEnemy skipped (no Enemies.json)", ref run, ref passed, ref failed);
                return;
            }

            const string defaultEnemy = "Sandstorm Flanker";
            if (!types.Any(t => string.Equals(t, defaultEnemy, StringComparison.OrdinalIgnoreCase)))
            {
                TestBase.AssertTrue(true, "LabSessionBegin_PicksDefaultCatalogEnemy skipped (Sandstorm Flanker missing)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabDefaultFoe").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "LabSessionBegin_PicksDefaultCatalogEnemy: session null", ref run, ref passed, ref failed);
                return;
            }

            var snap = lab.CaptureSimulationSnapshot();
            TestBase.AssertTrue(
                string.Equals(defaultEnemy, snap.SessionEnemyLoaderType, StringComparison.OrdinalIgnoreCase),
                "Begin sets Sandstorm Flanker as default loader enemy",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, snap.EnemyLevel, "Begin uses level 1 loader enemy", ref run, ref passed, ref failed);

            types.Sort(StringComparer.OrdinalIgnoreCase);
            int idx = types.FindIndex(t => string.Equals(t, snap.SessionEnemyLoaderType, StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(idx >= 0, "Selected type in sorted list", ref run, ref passed, ref failed);
            int visible = lab.LastEnemyCatalogVisibleRowCount > 0
                ? lab.LastEnemyCatalogVisibleRowCount
                : ActionInteractionLabSession.EnemyCatalogVisibleRowCount;
            int maxScroll = Math.Max(0, types.Count - visible);
            TestBase.AssertTrue(lab.EnemyCatalogScrollOffset >= 0 && lab.EnemyCatalogScrollOffset <= maxScroll, "Enemy scroll offset clamped", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                lab.EnemyCatalogScrollOffset <= idx && idx < lab.EnemyCatalogScrollOffset + visible,
                "Selected enemy row is in visible scroll window",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void LabPlayerIsActiveForDisplayWhenInLabState(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabDisplayActive").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var ctx = new CanvasContextManager();
            ctx.SetDungeonName("Dungeon X");
            ctx.SetRoomName("Room Y");
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, ctx);
            var lab = ActionInteractionLabSession.Current;
            TestBase.AssertTrue(lab != null, "Lab session exists", ref run, ref passed, ref failed);
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                return;
            }

            bool labPlayerActive = DisplayStateCoordinator.IsCharacterActive(lab.LabPlayer, stateManager);
            TestBase.AssertTrue(labPlayerActive, "DisplayStateCoordinator treats LabPlayer as active in lab state", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }


        internal static void CanvasContextRestoredAfterEndSession(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var stateManager = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("LabCtxRestore").Build();
            stateManager.AddCharacter(hero);
            stateManager.TransitionToState(GameState.ActionInteractionLab);
            var ctx = new CanvasContextManager();
            ctx.SetCurrentCharacter(hero);
            ctx.SetDungeonName("BeforeLab");
            ctx.SetRoomName("BeforeRoom");

            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, ctx);
            TestBase.AssertTrue(ActionInteractionLabSession.Current != null, "Lab with context started", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();

            TestBase.AssertTrue(ReferenceEquals(ctx.GetCurrentCharacter(), hero), "Context character restored after EndSession", ref run, ref passed, ref failed);
            TestBase.AssertEqual("BeforeLab", ctx.GetDungeonName(), "Dungeon name restored", ref run, ref passed, ref failed);
            TestBase.AssertEqual("BeforeRoom", ctx.GetRoomName(), "Room name restored", ref run, ref passed, ref failed);
        }


        internal static void LabBeginDoesNotResetGlobalGameTime(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Lab Begin does not reset global GameTicker ---");
            ActionLoader.LoadActions();
            GameTicker.Instance.AdvanceGameTime(9.0);
            double before = GameTicker.Instance.GameTime;
            var hero = TestDataBuilders.Character().WithName("LabTicker").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            TestBase.AssertTrue(Math.Abs(GameTicker.Instance.GameTime - before) < 0.0001,
                "Global GameTime unchanged after lab bootstrap", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
            TestBase.AssertTrue(Math.Abs(GameTicker.Instance.GameTime - before) < 0.0001,
                "Global GameTime unchanged after lab EndSession", ref run, ref passed, ref failed);
        }


        /// <summary>
        /// <see cref="ActionInteractionLabSession.RefreshGameDataAsync"/> reloads disk data, clears step history,
        /// and preserves the combo strip order.
        /// </summary>
        internal static void RefreshGameDataAsync_ReloadsAndPreservesComboStrip(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "RefreshGameDataAsync_Reloads skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("LabRefreshData").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "RefreshGameDataAsync_Reloads: session null", ref run, ref passed, ref failed);
                return;
            }

            lab.SelectedCatalogActionName = "JAB";
            lab.AddSelectedCatalogActionToComboStrip();
            int stripCount = lab.LabPlayer.GetComboActions().Count;
            var stripNamesBefore = lab.LabPlayer.GetComboActions().Select(a => a.Name).ToList();
            string firstStripName = stripNamesBefore[0];

            lab.StepAsync(18, "JAB").GetAwaiter().GetResult();
            TestBase.AssertTrue(lab.LabTotalActionTicks > 0, "RefreshGameData test: history recorded a step", ref run, ref passed, ref failed);

            lab.RefreshGameDataAsync().GetAwaiter().GetResult();

            TestBase.AssertEqual(0, lab.History.Count, "RefreshGameData clears step history", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, lab.LabTotalActionTicks, "RefreshGameData zeros LabTotalActionTicks", ref run, ref passed, ref failed);
            TestBase.AssertEqual(stripCount, lab.LabPlayer.GetComboActions().Count, "RefreshGameData keeps combo strip size", ref run, ref passed, ref failed);
            TestBase.AssertTrue(string.Equals(firstStripName, lab.LabPlayer.GetComboActions()[0].Name, StringComparison.OrdinalIgnoreCase),
                "RefreshGameData keeps first combo slot name", ref run, ref passed, ref failed);
            var stripNamesAfter = lab.LabPlayer.GetComboActions().Select(a => a.Name).ToList();
            for (int i = 0; i < stripNamesBefore.Count; i++)
            {
                TestBase.AssertTrue(
                    string.Equals(stripNamesBefore[i], stripNamesAfter[i], StringComparison.OrdinalIgnoreCase),
                    $"RefreshGameData keeps combo slot {i + 1} ({stripNamesBefore[i]})",
                    ref run, ref passed, ref failed);
            }
            TestBase.AssertTrue(ActionLoader.GetAllActionNames().Count > 0, "RefreshGameData reloads action catalog", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }
    }
}
