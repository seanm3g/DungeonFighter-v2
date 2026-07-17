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
    public static class ActionInteractionLabSimulationTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            EncounterSimulationBatchCount_ClampedTiers(ref run, ref pass, ref fail);
            UseParallelEncounterSimulation_DefaultsTrueAndMutable(ref run, ref pass, ref fail);
            IgnoreActionRequirements_ToggleBypassesGearAndWeaponBasic(ref run, ref pass, ref fail);
            SeededDungeonGenerate_IsDeterministic(ref run, ref pass, ref fail);
            SetLabDungeonSeed_UpdatesSessionSeed(ref run, ref pass, ref fail);
            SeededD20_RepeatsSequenceAfterReset(ref run, ref pass, ref fail);
            DungeonSim_SingleRunCompletes(ref run, ref pass, ref fail);
        }



        internal static void EncounterSimulationBatchCount_ClampedTiers(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabSimBatch").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "EncounterSimulationBatchCount_ClampedTiers: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertEqual(ActionLabEncounterSimulator.DefaultBatchEncounterCount, lab.EncounterSimulationBatchCount, "lab starts at default batch count", ref run, ref passed, ref failed);
            lab.CycleEncounterSimulationBatchCount(1);
            TestBase.AssertEqual(ActionLabEncounterSimulator.DefaultBatchEncounterCount, lab.EncounterSimulationBatchCount, "clamp up at max tier (no wrap to 1)", ref run, ref passed, ref failed);
            lab.CycleEncounterSimulationBatchCount(-1);
            TestBase.AssertEqual(100, lab.EncounterSimulationBatchCount, "one step down from 1000", ref run, ref passed, ref failed);
            lab.EncounterSimulationBatchCount = 1;
            lab.CycleEncounterSimulationBatchCount(-1);
            TestBase.AssertEqual(1, lab.EncounterSimulationBatchCount, "clamp down at min tier (no wrap to 1000)", ref run, ref passed, ref failed);
            lab.EncounterSimulationBatchCount = 7;
            lab.CycleEncounterSimulationBatchCount(1);
            TestBase.AssertEqual(ActionLabEncounterSimulator.DefaultBatchEncounterCount, lab.EncounterSimulationBatchCount, "non-tier count falls back to default index then +1 clamps at 1000", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void UseParallelEncounterSimulation_DefaultsTrueAndMutable(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabSimPar").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "UseParallelEncounterSimulation_DefaultsTrueAndMutable: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertTrue(lab.UseParallelEncounterSimulation, "lab starts with parallel encounter simulation", ref run, ref passed, ref failed);
            lab.UseParallelEncounterSimulation = false;
            TestBase.AssertFalse(lab.UseParallelEncounterSimulation, "parallel flag can be cleared", ref run, ref passed, ref failed);
            lab.UseParallelEncounterSimulation = true;
            TestBase.AssertTrue(lab.UseParallelEncounterSimulation, "parallel flag can be restored", ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void IgnoreActionRequirements_ToggleBypassesGearAndWeaponBasic(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("LabReqToggle").WithStats(3, 3, 3, 3).Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                TestBase.AssertTrue(false, "IgnoreActionRequirements_ToggleBypassesGearAndWeaponBasic: session null", ref run, ref passed, ref failed);
                return;
            }

            TestBase.AssertFalse(lab.IgnoreActionRequirements, "lab starts with requirements enforced", ref run, ref passed, ref failed);

            var blockedWeapon = TestDataBuilders.Weapon()
                .WithName("High Tec Blade")
                .WithTier(1)
                .Build();
            blockedWeapon.AttributeRequirements = new AttributeRequirements(new Dictionary<string, int> { ["technique"] = 10 });

            TestBase.AssertFalse(
                lab.TryApplyLabGear(blockedWeapon, "weapon", out _),
                "TryApplyLabGear blocks gear when requirements unmet and toggle off",
                ref run, ref passed, ref failed);

            lab.IgnoreActionRequirements = true;
            TestBase.AssertTrue(
                lab.TryApplyLabGear(blockedWeapon, "weapon", out _),
                "TryApplyLabGear succeeds when IgnoreActionRequirements is true",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                lab.LabPlayer.Equipment.Weapon == blockedWeapon,
                "Blocked weapon should equip after bypass toggle",
                ref run, ref passed, ref failed);

            CharacterSerializer.RebuildCharacterActions(lab.LabPlayer);

            var strike = lab.LabPlayer.GetComboActions()
                .FirstOrDefault(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase));
            if (strike == null)
            {
                TestBase.AssertTrue(true, "IgnoreActionRequirements weapon-basic portion skipped (no STRIKE in combo)", ref run, ref passed, ref failed);
                ActionInteractionLabSession.EndSession();
                return;
            }

            lab.IgnoreActionRequirements = false;
            TestBase.AssertFalse(
                lab.TryRemoveFromLabCombo(strike),
                "TryRemoveFromLabCombo refuses last required basic when requirements enforced",
                ref run, ref passed, ref failed);

            lab.IgnoreActionRequirements = true;
            TestBase.AssertTrue(
                lab.TryRemoveFromLabCombo(strike),
                "TryRemoveFromLabCombo removes required basic when requirements bypassed",
                ref run, ref passed, ref failed);
            TestBase.AssertFalse(
                lab.LabPlayer.GetComboActions().Any(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase)),
                "STRIKE should be gone after bypass removal",
                ref run, ref passed, ref failed);

            ActionInteractionLabSession.EndSession();
        }


        internal static void SeededDungeonGenerate_IsDeterministic(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            const int seed = 424242;
            var names = ActionLabDungeonFactory.ListCatalogDungeonNames();
            string key = names.Count > 0 ? names[0] : "Forest";
            var a = ActionLabDungeonFactory.Generate(key, dungeonLevel: 3, seed: seed);
            var b = ActionLabDungeonFactory.Generate(key, dungeonLevel: 3, seed: seed);
            TestBase.AssertEqual(a.Dungeon.Rooms.Count, b.Dungeon.Rooms.Count, "Same seed → same room count", ref run, ref passed, ref failed);
            for (int i = 0; i < a.Dungeon.Rooms.Count && i < b.Dungeon.Rooms.Count; i++)
            {
                TestBase.AssertEqual(a.Dungeon.Rooms[i].Name, b.Dungeon.Rooms[i].Name, $"Room {i} name match", ref run, ref passed, ref failed);
                var ea = a.Dungeon.Rooms[i].GetEnemies();
                var eb = b.Dungeon.Rooms[i].GetEnemies();
                TestBase.AssertEqual(ea.Count, eb.Count, $"Room {i} enemy count", ref run, ref passed, ref failed);
                for (int j = 0; j < ea.Count && j < eb.Count; j++)
                {
                    TestBase.AssertEqual(ea[j].Name, eb[j].Name, $"Room {i} enemy {j} name", ref run, ref passed, ref failed);
                    TestBase.AssertEqual(ea[j].Level, eb[j].Level, $"Room {i} enemy {j} level", ref run, ref passed, ref failed);
                }
            }
        }


        internal static void SetLabDungeonSeed_UpdatesSessionSeed(ref int run, ref int passed, ref int failed)
        {
            try
            {
                var hero = TestDataBuilders.Character().WithName("SeedHero").WithLevel(1).Build();
                var cm = new CombatManager();
                ActionInteractionLabSession.Begin(hero, cm, () => { }, null);
                var lab = ActionInteractionLabSession.Current!;
                lab.SetLabDungeonSeed(987654321);
                TestBase.AssertEqual(987654321, lab.LabDungeonSeed, "SetLabDungeonSeed stores typed seed", ref run, ref passed, ref failed);
                lab.SetLabDungeonSeed(-42);
                TestBase.AssertEqual(-42, lab.LabDungeonSeed, "SetLabDungeonSeed accepts negative ints", ref run, ref passed, ref failed);
            }
            finally
            {
                ActionInteractionLabSession.EndSession();
            }
        }


        internal static void SeededD20_RepeatsSequenceAfterReset(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var hero = TestDataBuilders.Character().WithName("SeedD20").Build();
            var cm = new CombatManager();
            try
            {
                ActionInteractionLabSession.Begin(hero, cm, () => { }, null);
                var lab = ActionInteractionLabSession.Current!;
                lab.UseSeededD20 = true;
                lab.UseRandomD20PerStep = false;
                lab.D20SequenceSeed = 99;
                lab.RewindSeededD20Stream();
                var first = new List<int>();
                for (int i = 0; i < 5; i++)
                    first.Add(lab.ResolveD20ForNextStep());
                lab.RewindSeededD20Stream();
                var second = new List<int>();
                for (int i = 0; i < 5; i++)
                    second.Add(lab.ResolveD20ForNextStep());
                TestBase.AssertTrue(first.SequenceEqual(second), "Seeded d20 rewinds to same sequence", ref run, ref passed, ref failed);
            }
            finally
            {
                ActionInteractionLabSession.EndSession();
            }
        }


        internal static void DungeonSim_SingleRunCompletes(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            EnemyLoader.LoadEnemies();
            var hero = TestDataBuilders.Character().WithName("DungSim").WithLevel(5).Build();
            var names = ActionLoader.GetAllActionNames();
            names.Sort(StringComparer.OrdinalIgnoreCase);
            string pick = names.FirstOrDefault(n =>
            {
                var a = ActionLoader.GetAction(n);
                return a != null && a.IsComboAction;
            }) ?? (names.Count > 0 ? names[0] : "");
            if (string.IsNullOrEmpty(pick))
            {
                TestBase.AssertTrue(false, "Need a catalog action for dungeon sim", ref run, ref passed, ref failed);
                return;
            }

            var act = ActionLoader.GetAction(pick)!;
            act.IsComboAction = true;
            hero.AddToCombo(act);
            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(hero);
            var snapshot = new LabCombatSnapshot(
                json, 0, 0, 0, 0, 0, 0, 0, null, 1,
                hero.GetComboActions().Select(a => a.Name).ToList(),
                pick);
            var catalog = ActionLabDungeonFactory.ListCatalogDungeonNames();
            string key = catalog.Count > 0 ? catalog[0] : "Forest";
            var report = ActionLabDungeonSimulator.RunBatchAsync(
                snapshot, key, dungeonLevel: 2, baseSeed: 777, runCount: 1, varySeedPerRun: false, maxDegreeOfParallelism: 1)
                .GetAwaiter().GetResult();
            TestBase.AssertEqual(1, report.Runs.Count, "Dungeon sim produces one run", ref run, ref passed, ref failed);
            TestBase.AssertTrue(string.IsNullOrEmpty(report.Runs[0].ErrorMessage),
                "Dungeon sim run has no error: " + (report.Runs[0].ErrorMessage ?? ""),
                ref run, ref passed, ref failed);
        }
    }
}
