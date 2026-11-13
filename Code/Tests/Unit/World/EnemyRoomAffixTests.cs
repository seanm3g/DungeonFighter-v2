using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Region / dungeon enemy affixes, signature kits, the Demon, room terrain and rolled room affixes, and the 90/10
    /// wander rolls (GameData/EnemyAffixes.json, GameData/RoomAffixes.json).
    /// </summary>
    public static class EnemyRoomAffixTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyRoomAffix Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            AffixDataLoader.SetForTests(null, null);

            TestRegionRank();
            TestFamilyRanks();
            TestHomeAffixesByRarity();
            TestKitsMatchSignatures();
            TestKitRunsSignatureTemplate();
            TestOnHitAndDamageLevers();
            TestDemon();
            TestRoomAffixes();
            TestTerrainTags();
            TestWanderingRoomLeavesHomeTheme();
            TestGeneratedDungeonsEndWithDemon();
            TestPrefixEffects();
            TestHudRows();
            TestFeatureSwitches();

            Console.WriteLine($"\nEnemyRoomAffix: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed.\n");
        }

        private static void Check(bool condition, string message) =>
            TestBase.AssertTrue(condition, message, ref _testsRun, ref _testsPassed, ref _testsFailed);

        private static void TestRegionRank()
        {
            Check(EnemyAffixRuntime.RegionRank(new TravelRegion { Id = "fire-3" }, 1) == 3, "fire-3 is rank 3");
            Check(EnemyAffixRuntime.RegionRank(new TravelRegion { Id = "forest" }, 50) == 1, "first region of a theme is rank 1");
            Check(EnemyAffixRuntime.RegionRank(null, 45) == 3, "no region: rank from the level band (L45 = 3)");
        }

        private static void TestFamilyRanks()
        {
            var file = AffixDataLoader.Enemy;
            Check(file.RegionFamilies.Count == 4, "4 region families");
            Check(file.Dungeons.Count == 12, "12 dungeon identities");
            var rooted = file.FamilyFor("Forest")!.AtRank(3);
            Check(rooted.Name == "Rooted III" && Math.Abs(rooted.Value - 0.15) < 1e-9, "Rooted III = +15% HP");
            var kindled = file.FamilyFor("Fire")!.AtRank(5);
            Check(Math.Abs(kindled.Chance - 0.5) < 1e-9 && kindled.Status == "burn", "Kindled V = 50% Burn on hit");
            Check(file.Dungeons.Select(d => d.Material).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 12, "each dungeon has its own material");
        }

        private static void TestHomeAffixesByRarity()
        {
            var rareData = EnemyLoader.GetEnemyData("Bone Spreader")!;
            var rare = EnemyLoader.CreateEnemy("Bone Spreader", 10)!;
            int hpBefore = rare.MaxHealth;
            EnemyAffixRuntime.ApplyHome(rare, rareData, new TravelRegion { Id = "forest-2", Theme = "Forest" });
            Check(rare.Affixes.Any(a => a.Name == "Rooted II") && rare.Affixes.Any(a => a.Name == "Bloodscent"),
                "a Rare Den enemy carries its region family and its dungeon affix");
            Check(rare.MaxHealth == (int)Math.Round(hpBefore * 1.10), "Rooted II adds 10% max HP");
            Check(rare.Weapon == null || rare.Weapon.Material == "Bone", "Den enemies' weapons are Bone");

            var plainData = EnemyLoader.GetEnemyData("Alpha Wolf")!;
            var plain = EnemyLoader.CreateEnemy("Alpha Wolf", 10)!;
            EnemyAffixRuntime.ApplyHome(plain, plainData, new TravelRegion { Id = "fire-2", Theme = "Fire" });
            Check(plain.Affixes.Count == 1 && plain.Affixes[0].Name == "Rooted II",
                "an uncommon wanderer keeps its home region family at the current rank");

            var lair = EnemyLoader.CreateEnemy("Alpha Wolf", 10)!;
            EnemyAffixRuntime.ApplyHome(lair, plainData, null, forceDungeonAffix: true);
            Check(lair.Affixes.Any(a => a.Name == "Bloodscent"), "a Lair room gives any enemy its dungeon affix");
        }

        private static void TestKitsMatchSignatures()
        {
            var file = AffixDataLoader.Enemy;
            var regional = EnemyLoader.GetAllEnemyData().Where(e => file.DungeonFor(e.Biome) != null).ToList();
            Check(regional.Count == 108, $"108 regional enemies (got {regional.Count})");
            bool allInside = regional.All(e => e.Kit != null && e.Kit.Count == e.Actions.Count
                && e.Kit.All(k => file.DungeonFor(e.Biome)!.Signature.Contains(k)));
            Check(allInside, "every regional enemy's kit slots come from its dungeon's signature");
        }

        private static void TestKitRunsSignatureTemplate()
        {
            var action = EnemyActionCatalog.TryCreate("Flavor Bite", "ENEMY CRUSH");
            Check(action != null && action.Name == "Flavor Bite", "a kit slot keeps the flavor name");
            var crush = EnemyActionCatalog.TryCreate("ENEMY CRUSH");
            Check(action != null && crush != null && Math.Abs(action.DamageMultiplier - crush.DamageMultiplier) < 1e-9,
                "a kit slot runs its template's mechanics");
        }

        private static void TestOnHitAndDamageLevers()
        {
            var enemy = new Enemy("Lever Test", 5, 100, 1, 1, 1, 1);
            enemy.Affixes.Add(new EnemyAffixDef { Name = "Sure", Lever = "onhit", Status = "burn", Chance = 1.0 });
            enemy.Affixes.Add(new EnemyAffixDef { Name = "Zealot", Lever = "damage", Value = 0.25, BelowHealth = 0.5 });
            enemy.Affixes.Add(new EnemyAffixDef { Name = "Swift", Lever = "speed", Value = 0.25 });
            Check(EnemyAffixRuntime.RollOnHitStatuses(enemy, new Random(1)).Select(x => x.status).SequenceEqual(new[] { "burn" }), "a 100% on-hit affix always applies");
            var venom = new Enemy("Venom Test", 5, 100, 1, 1, 1, 1);
            venom.Affixes.Add(new EnemyAffixDef { Name = "Venomous", Lever = "onhit", Status = "poison", Chance = 1.0, Value = 0.25 });
            var bitten = new Character("Bitten", 5);
            foreach (var (status, amount) in EnemyAffixRuntime.RollOnHitStatuses(venom, new Random(1)))
                CombatEffectsSimplified.ApplyNamedStatus(bitten, status, new List<string>(), amount);
            Check(Math.Abs(bitten.PoisonPercentOfMaxHealth - 0.25) < 1e-9, "an affix's poison adds its own amount (0.25% per tick)");
            // Hazard poison: the generic 1% per application stacked faster than any enemy (Curse of the Tomb, 2026-11-12).
            var curse = EnvironmentalActionLoader.TryCreateByName("Curse of the Tomb");
            Check(curse != null && curse.CausesPoison && Math.Abs(curse.PoisonPercentToAdd - 0.25) < 1e-9, "a poison hazard reads its poisonPercent (0.25)");
            var overgrown = new RPGGame.Action { Name = "Hazard" };
            RoomAffixRuntime.SetCauses(overgrown, "poison");
            Check(overgrown.CausesPoison && Math.Abs(overgrown.PoisonPercentToAdd - 0.25) < 1e-9, "terrain / room-affix poison adds 0.25% like enemy poison");
            Check(Math.Abs(EnemyAffixRuntime.DamageMultiplier(enemy) - 1.0) < 1e-9, "Zealot is off above half HP");
            enemy.CurrentHealth = enemy.MaxHealth / 2 - 1;
            Check(Math.Abs(EnemyAffixRuntime.DamageMultiplier(enemy) - 1.25) < 1e-9, "Zealot adds 25% below half HP");
            Check(Math.Abs(EnemyAffixRuntime.AttackTimeMultiplier(enemy) - 0.8) < 1e-9, "+25% speed = 0.8x swing time");
        }

        private static void TestDemon()
        {
            var boss = EnemyLoader.CreateEnemy("Vigil Alpha", 12)!;
            boss.MarkAsFinalBoss();
            EnemyAffixRuntime.MakeDemon(boss, "Den", new TravelRegion { Id = "forest-3", Theme = "Forest" });
            var names = boss.ActionPool.Select(e => e.action.Name).ToList();
            Check(names.Take(2).SequenceEqual(new[] { "Infernal Rend", "Infernal Frenzy" }), "the Demon opens with two signature actions");
            Check(names.Count <= AffixDataLoader.Enemy.Demon.MaxActions, "the Demon's pool is capped");
            Check(boss.Affixes.Any(a => a.Name == "Rooted IV") && boss.Affixes.Any(a => a.Name == "Bloodscent"),
                "the Demon carries its region family one rank up and its dungeon affix");
            Check(boss.Name.EndsWith("Demon of the Den"), "the Demon is titled after its dungeon");
            Check(boss.PendingPhaseAction?.Name == "Infernal Crush", "the third signature waits for the second phase");

            boss.CurrentHealth = (int)(boss.MaxHealth * 0.4);
            Check(EnemyAffixRuntime.CheckPhase(boss), "below half HP the second phase starts");
            Check(boss.ActionPool.Any(e => e.action.Name == "Infernal Crush"), "the second phase adds the third signature action");
        }

        private static void TestRoomAffixes()
        {
            var room = new Environment("Test Room", "", true, "Den");
            room.ActionPool.Clear();
            var hazard = new Action { Name = "Test Hazard", DamageMultiplier = 1.0 };
            room.AddAction(hazard, 1.0);
            room.SetTags(new[] { "dormant" });

            RoomAffixRuntime.ApplyAffix(room, AffixDataLoader.Room.Affixes.First(a => a.Name == "Volatile"), "Den");
            Check(Math.Abs(hazard.DamageMultiplier - 1.5) < 1e-9, "Volatile: hazards hit 50% harder");
            Check(room.Tags.Contains("cycling") && !room.Tags.Contains("dormant"), "Volatile: dormant becomes cycling");

            RoomAffixRuntime.ApplyAffix(room, AffixDataLoader.Room.Affixes.First(a => a.Name == "Infused"), "Lava Cave");
            Check(hazard.CausesBurn, "Infused: a Fire dungeon's hazards Burn");

            RoomAffixRuntime.ApplyAffix(room, AffixDataLoader.Room.Affixes.First(a => a.Name == "Sheltered"), "Den");
            var hero = new Character("Hero", 1);
            var foe = new Enemy("Foe", 1, 50, 1, 1, 1, 1);
            var targets = RoomAffixRuntime.FilterHazardTargets(room, hero, foe);
            Check(targets.Count == 1 && ReferenceEquals(targets[0], foe), "Sheltered: hazards strike only enemies");

            var crowded = AffixDataLoader.Room.Affixes.First(a => a.Name == "Crowded");
            Check(RoomAffixRuntime.AdjustEnemyCount(2, crowded) == 3, "Crowded: one more enemy");
            Check(RoomAffixRuntime.AdjustEnemyCount(3, crowded) == 3, "Crowded: capped at the room maximum");

            var garrison = new Environment("Garrison", "", true, "Den") { RoomAffix = AffixDataLoader.Room.Affixes.First(a => a.Name == "Garrisoned") };
            Check(EnemyAffixRuntime.StartStatuses(foe, garrison).Any(s => s.status == "fortify"), "Garrisoned: enemies start Fortified");
        }

        private static void TestTerrainTags()
        {
            string? scorchedName = RoomLoader.GetRoomsByTheme("Ashen Land")
                .FirstOrDefault(n => RoomLoader.GetRoomData(n)?.Tags?.Contains("scorched") == true);
            Check(scorchedName != null, "an Ashen Land room is scorched");
            if (scorchedName == null)
                return;
            var room = RoomLoader.CreateRoom(scorchedName, "Ashen Land", true, new Random(3));
            Check(room.ActionPool.Count > 0 && room.ActionPool.All(e => e.action.CausesBurn), "scorched: every hazard Burns");
        }

        private static void TestGeneratedDungeonsEndWithDemon()
        {
            int dungeons = 0, demons = 0, regionAffixed = 0, enemies = 0;
            foreach (var identity in AffixDataLoader.Enemy.Dungeons)
            {
                for (int seed = 1; seed <= 4; seed++)
                {
                    var dungeon = new Dungeon($"Test {identity.Theme}", 12, 12, identity.Theme, spawnRegionId: "water", generationSeed: seed);
                    dungeon.Generate();
                    dungeons++;
                    var last = dungeon.Rooms.LastOrDefault();
                    var finalEnemy = last?.GetEnemies().LastOrDefault();
                    if (finalEnemy != null && finalEnemy.IsBoss && finalEnemy.Prefix == "Demon" && finalEnemy.Name.EndsWith(identity.Demon))
                        demons++;
                    foreach (var e in dungeon.Rooms.SelectMany(r => r.GetEnemies()))
                    {
                        enemies++;
                        if (e.RegionAffixName != null)
                            regionAffixed++;
                    }
                }
            }
            Check(demons == dungeons, $"every generated dungeon ends with its Demon ({demons}/{dungeons})");
            Check(enemies > 0 && regionAffixed == enemies, $"every spawned enemy carries a region affix ({regionAffixed}/{enemies})");
        }

        private static Enemy WithPrefix(string enemyName, string prefixName, int level = 40)
        {
            var enemy = EnemyLoader.CreateEnemy(enemyName, level)!;
            EnemyAffixRuntime.ApplyHome(enemy, EnemyLoader.GetEnemyData(enemyName), null);
            var prefix = EnemyPrefixLoader.Find(prefixName)!;
            enemy.ApplyPrefix(prefix.Name, prefix.Tags);
            EnemyAffixRuntime.ApplyPrefixEffects(enemy, prefix);
            return enemy;
        }

        private static void TestPrefixEffects()
        {
            Check(EnemyPrefixLoader.Get().Prefixes.Where(p => !p.BossOnly).All(p => p.Effects.Count > 0),
                "every rollable prefix has effects");

            var undead = WithPrefix("Alpha Wolf", "Undead");
            Check(RPGGame.Combat.StatusAffinity.IsImmune(undead, "poison"), "Undead: immune to Poison (prefix tags feed affinities)");

            var mace = new Character("MaceHero", 10) { Weapon = new WeaponItem("Mace", 1, 10, 0.05, WeaponType.Mace) };
            var sword = new Character("SwordHero", 10) { Weapon = new WeaponItem("Sword", 1, 10, 0.05, WeaponType.Sword) };
            var wand = new Character("WandHero", 10) { Weapon = new WeaponItem("Wand", 1, 10, 0.05, WeaponType.Wand) };
            var skeleton = WithPrefix("Alpha Wolf", "Skeleton");
            Check(Math.Abs(EnemyAffixRuntime.DamageTakenMultiplier(skeleton, mace) - 1.3) < 1e-9, "Skeleton: maces deal 30% more");
            Check(Math.Abs(EnemyAffixRuntime.DamageTakenMultiplier(skeleton, sword) - 0.75) < 1e-9, "Skeleton: swords deal 25% less");
            var ghost = WithPrefix("Alpha Wolf", "Ghost");
            Check(Math.Abs(EnemyAffixRuntime.DamageTakenMultiplier(ghost, wand) - 1.0) < 1e-9
                  && EnemyAffixRuntime.DamageTakenMultiplier(ghost, sword) < 1.0, "Ghost: only wands hit it fully");

            var tinyBase = EnemyLoader.CreateEnemy("Alpha Wolf", 40)!;
            EnemyAffixRuntime.ApplyHome(tinyBase, EnemyLoader.GetEnemyData("Alpha Wolf"), null);
            var tiny = WithPrefix("Alpha Wolf", "Tiny");
            Check(tiny.MaxHealth < tinyBase.MaxHealth && EnemyAffixRuntime.Evasion(tiny) == 1, "Tiny: less HP and 1 harder to hit");

            var giant = WithPrefix("Alpha Wolf", "Giant");
            Check(EnemyAffixRuntime.AttackTimeMultiplier(giant) > 1.0 && EnemyAffixRuntime.DamageMultiplier(giant) > 1.0,
                "Giant: slower swings, harder hits");

            var feral = WithPrefix("Alpha Wolf", "Feral");
            Check(Math.Abs(EnemyAffixRuntime.AttackTimeMultiplier(feral) - 1.0) < 1e-9, "Feral: normal speed above half HP");
            feral.CurrentHealth = feral.MaxHealth / 3;
            Check(EnemyAffixRuntime.AttackTimeMultiplier(feral) < 1.0, "Feral: faster below half HP");

            var plain = EnemyLoader.CreateEnemy("Alpha Wolf", 40)!;
            var ancient = WithPrefix("Alpha Wolf", "Ancient");
            Check(ancient.ActionPool.Count == plain.ActionPool.Count + 1, "Ancient: one more signature action");

            var armored = WithPrefix("Alpha Wolf", "Armored");
            Check(EnemyAffixRuntime.StartStatuses(armored, null).Any(s => s.status == "fortify" && s.stacks == 3), "Armored: starts with 3 Fortify");
        }

        private static void TestHudRows()
        {
            var enemy = EnemyLoader.CreateEnemy("Bone Spreader", 10)!;
            EnemyAffixRuntime.ApplyHome(enemy, EnemyLoader.GetEnemyData("Bone Spreader"), new TravelRegion { Id = "forest", Theme = "Forest" });
            var lines = EnemyAffixRuntime.DescribeAffixes(enemy);
            Check(lines.Any(l => l.StartsWith("Rooted I:")) && lines.Any(l => l.StartsWith("Bloodscent:")), "enemy HUD lists region and dungeon affixes");

            var room = new Environment("Hud Room", "", true, "Ashen Land");
            room.SetTags(new[] { "scorched" });
            room.RoomAffix = AffixDataLoader.Room.Affixes.First(a => a.Name == "Volatile");
            room.IsWanderer = true;
            var rows = RPGGame.Combat.RoomStatusEffects.AffixRowNames(room);
            Check(rows.SequenceEqual(new[] { "Scorched", "Volatile", "Wandering room" }), "room HUD rows: terrain, affix, wander mark");
            Check(RPGGame.Combat.RoomStatusEffects.NameFromLine("Volatile") == "Volatile", "room affix rows resolve for hover");
        }

        private static void TestFeatureSwitches()
        {
            try
            {
                AffixFeatures.SetOff("region,dungeon");
                var rare = EnemyLoader.CreateEnemy("Bone Spreader", 10)!;
                EnemyAffixRuntime.ApplyHome(rare, EnemyLoader.GetEnemyData("Bone Spreader"), new TravelRegion { Id = "forest", Theme = "Forest" });
                Check(rare.Affixes.Count == 0, "--features-off region,dungeon: no home affixes");

                AffixFeatures.SetOff("prefix");
                var undead = WithPrefix("Alpha Wolf", "Undead");
                Check(!RPGGame.Combat.StatusAffinity.IsImmune(undead, "poison") && undead.Affixes.All(a => a.Name != "Undead"),
                    "--features-off prefix: prefixes only rename");

                AffixFeatures.SetOff("all");
                Check(!AffixFeatures.Region && !AffixFeatures.Wander && !AffixFeatures.Demon && !AffixFeatures.Kits, "--features-off all");
                bool threw = false;
                try { AffixFeatures.SetOff("nonsense"); } catch (ArgumentException) { threw = true; }
                Check(threw, "--features-off rejects unknown names");
            }
            finally
            {
                AffixFeatures.SetOff(null);
            }
        }

        private static void TestWanderingRoomLeavesHomeTheme()
        {
            var wander = AffixDataLoader.Enemy.Wander;
            double saved = wander.RoomChance;
            try
            {
                wander.RoomChance = 1.0;
                var home = new HashSet<string>(RoomLoader.GetRoomsByTheme("Den"), StringComparer.OrdinalIgnoreCase);
                var rng = new Random(7);
                bool allAway = Enumerable.Range(0, 30).All(_ => RoomGenerator.TryPickWanderingRoom("Den", rng) is string n && !home.Contains(n));
                Check(allAway, "a wandering room comes from another dungeon theme");
                wander.RoomChance = 0.0;
                Check(RoomGenerator.TryPickWanderingRoom("Den", rng) == null, "no wander at 0% chance");
            }
            finally
            {
                wander.RoomChance = saved;
            }
        }
    }
}
