using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Environments pass (Documentation/ENVIRONMENTS_2026-11-22.md): 12 environments (region theme × dungeon theme),
    /// 3 enemies each with 1-2 actions, one hazard and one boon per environment; everything else is retired (loads,
    /// never spawns or rolls).
    /// </summary>
    public static class EnvironmentsTests
    {
        private static int _run, _passed, _failed;

        private static void Check(bool ok, string name) => TestBase.AssertTrue(ok, name, ref _run, ref _passed, ref _failed);

        public static void RunAllTests()
        {
            Console.WriteLine("=== Environments Tests ===\n");
            _run = _passed = _failed = 0;

            TestShape();
            TestRetiredStillLoads();
            TestBoon();
            TestSpawnsOnlyKept();
            TestLoreRenames();

            TestBase.PrintSummary("Environments Tests", _run, _passed, _failed);
        }

        private static List<DungeonData> RegionDungeons() =>
            JsonLoader.LoadJsonList<DungeonData>("Dungeons.json").Where(d => !d.retired).ToList();

        private static void TestShape()
        {
            var dungeons = RegionDungeons();
            Check(dungeons.Count == 12, $"12 environments offered (got {dungeons.Count})");
            var themes = new TravelRegionCatalog().GetAllRegions().SelectMany(r => r.LinkedDungeonThemes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Check(themes.Count == 12 && themes.All(t => dungeons.Any(d => string.Equals(d.theme, t, StringComparison.OrdinalIgnoreCase))),
                "every region dungeon theme has its environment");

            var kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in dungeons)
            {
                Check(d.possibleEnemies.Count == 3, $"{d.name}: 3 enemies");
                foreach (var name in d.possibleEnemies)
                {
                    var data = EnemyLoader.GetEnemyData(name);
                    Check(data != null && !data.Retired && data.Actions.Count is >= 1 and <= 2, $"{name}: kept, 1-2 actions");
                    kept.Add(name);
                }
                var rooms = RoomLoader.GetRoomsByTheme(d.theme).Select(RoomLoader.GetRoomData).Where(r => r != null).ToList();
                Check(rooms.Count > 0, $"{d.name}: has rooms");
                foreach (var room in rooms)
                {
                    Check(room!.Enemies.Select(e => e.Name).OrderBy(n => n).SequenceEqual(d.possibleEnemies.OrderBy(n => n)),
                        $"{room.GetLocationKey()}: lists its environment's 3 enemies");
                    var acts = room.Actions.Select(a => EnvironmentalActionLoader.TryCreateByName(a.Name)).ToList();
                    Check(acts.Count == 2 && acts.All(a => a != null) && acts.Count(a => a!.IsRoomBoon) == 1,
                        $"{room.GetLocationKey()}: one hazard and one boon");
                }
            }
            Check(kept.Count == 36, $"36 kept enemies (got {kept.Count})");
            var families = kept.Select(n => EnemyLoader.GetEnemyData(n)!).Select(e => EnemyFamilies.For(e.Archetype, e.Tags)?.Id).Where(id => id != null).Distinct().Count();
            Check(families == 6, $"all six families appear (got {families})");

            var envActions = new EnvironmentalActionLoader().LoadAllActions();
            Check(envActions.Count(a => !a.Retired) == 24, "24 live room actions (12 hazards, 12 boons)");
            Check(envActions.Where(a => !a.Retired).Count(a => !string.IsNullOrWhiteSpace(a.Grants)) == 12, "12 boons");
            Check(RoomLoader.GetAllRoomData().Where(r => !r.Retired).All(r => r.Actions.All(a =>
                    envActions.Any(e => !e.Retired && string.Equals(e.Name, a.Name, StringComparison.OrdinalIgnoreCase)))),
                "no live room lists a retired room action");
            Check(RoomLoader.GetAllRoomData().Where(r => string.Equals(r.Region, "Legacy", StringComparison.OrdinalIgnoreCase)).All(r => r.Retired),
                "Legacy rooms are retired");
        }

        private static void TestRetiredStillLoads()
        {
            Check(EnemyLoader.CreateEnemy("Goblin", 3) != null, "a retired enemy (Goblin) still loads by name");
            Check(EnemyLoader.GetEnemyData("Territorial Hound")?.Retired == true, "Territorial Hound is retired");
            Check(EnvironmentalActionLoader.TryCreateByName("Musk Haze") != null, "a retired hazard still loads by name");
            Check(RoomLoader.GetRoomsByTheme("Forest").Count == 0, "Legacy Forest rooms are never picked");
        }

        private static void TestBoon()
        {
            var boon = EnvironmentalActionLoader.TryCreateByName("Warm Hollow");
            Check(boon != null && boon.IsRoomBoon && boon.RoomBoon == "harden", "Warm Hollow grants Guard");
            if (boon == null)
                return;
            var room = new Environment("Boon Room", "", true, "Den");
            int heroGuard = 0;
            int foeGuard = 0;
            for (int i = 0; i < 20; i++)
            {
                var hero = new Character("Boon Hero", 5);
                var foe = new Enemy("Boon Foe", 5, 100, 1, 1, 1, 1);
                EnvironmentalActionExecutor.ExecuteEnvironmentalAction(room, new List<Actor> { hero, foe }, boon, room);
                heroGuard += Stores.Get(hero, "harden");
                foeGuard += Stores.Get(foe, "harden");
            }
            Check(heroGuard > 0, "a boon gives the hero Guard");
            Check(foeGuard == 0, "a boon never lands on the enemy");

            room.AddAction(boon, 1.0);
            room.AddAction(EnvironmentalActionLoader.TryCreateByName("Ember Rain")!, 1.0);
            RoomAffixRuntime.ApplyAffix(room, new RoomAffixDef { Name = "Test", HazardStatus = "bleed", HazardDamage = 2.0 }, "Den");
            Check(!boon.CausesBleed && Math.Abs(boon.DamageMultiplier) < 1e-9, "room affixes change hazards, not the boon");

            var brine = EnvironmentalActionLoader.TryCreateByName("Corroding Brine")!;
            var target = new Character("Brine Hero", 5);
            for (int i = 0; i < 20 && Stores.Get(target, "expose") == 0; i++)
                EnvironmentalActionExecutor.ExecuteEnvironmentalAction(room, new List<Actor> { target }, brine, room);
            Check(Stores.Get(target, "expose") > 0, "Corroding Brine (Acid) now lands Expose");
        }

        private static void TestLoreRenames()
        {
            var wolf = EnemyLoader.CreateEnemy("Alpha Wolf", 5);
            Check(wolf != null && wolf.Name == "Mother's Wolf", "a pre-rename enemy name loads the renamed enemy");
            Check(EnemyLoader.GetEnemyData("Mother's Wolf")?.FormerNames?.Contains("Alpha Wolf") == true, "the renamed row keeps its former name");
            Check(ActionLoader.GetAction("VENOM COAT")?.Name == "RED CORD", "VENOM COAT loads as RED CORD");
            Check(RPGGame.UI.Avalonia.DungeonThemeColors.GetThemeForDungeonName("The Den") == "Den"
                && RPGGame.UI.Avalonia.DungeonThemeColors.GetThemeForDungeonName("Denwatch") == "Den", "old and new dungeon names map to the theme");
            var forest = new TravelRegionCatalog().GetAllRegions().FirstOrDefault(r => r.Id == "forest");
            Check(forest?.DisplayName == "Widow's Blush", "region display name renamed, id kept");
            Check(SkillTreesConfig.TryLoadFromGameDataFile()?.GetNode("b-venom")?.Name == "Hammerdown", "Venom Mace node is Hammerdown (id kept)");
            var en = EnemyLoader.GetAllEnemyData();
            Check(en.Where(e => !e.Retired).All(e => e.Actions.All(a => !a.Contains("Venom") && !a.Contains("Toxic"))),
                "no kept enemy action is named for poison");
        }

        private static void TestSpawnsOnlyKept()
        {
            int spawned = 0;
            bool anyRetired = false;
            var rng = new Random(11);
            foreach (var d in RegionDungeons())
            {
                foreach (var roomName in RoomLoader.GetRoomsByTheme(d.theme))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var room = RoomLoader.CreateRoom(roomName, d.theme, true, new Random(rng.Next()));
                        room.GenerateEnemies(6, d.possibleEnemies);
                        foreach (var e in room.GetEnemies())
                        {
                            spawned++;
                            if (EnemyLoader.GetEnemyData(e.BaseName ?? e.Name)?.Retired == true)
                                anyRetired = true;
                        }
                    }
                }
            }
            Check(spawned > 0 && !anyRetired, $"no retired enemy spawns, wanderers included ({spawned} spawned)");
        }
    }
}
