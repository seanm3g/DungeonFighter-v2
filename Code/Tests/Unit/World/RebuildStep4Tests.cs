using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>Rebuild step 4: enemy families (D8.1), matchups (D0.8), one visible modifier and prefix rules (D8.2, D8.9),
    /// region requirements (D1.4) and non-hostile encounters (D8.10).</summary>
    public static class RebuildStep4Tests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Rebuild Step 4 Tests ===\n");
            _run = _passed = _failed = 0;

            // Families: six, and every enemy in Enemies.json maps to one.
            TestBase.AssertEqual(6, EnemyFamilies.All.Count, "six enemy families", ref _run, ref _passed, ref _failed);
            var unmapped = EnemyLoader.GetAllEnemyData().Where(e => EnemyFamilies.For(e) == null).Select(e => $"{e.Name} ({e.Archetype})").ToList();
            TestBase.AssertTrue(unmapped.Count == 0, $"every enemy has a family (unmapped: {string.Join(", ", unmapped.Take(6))})", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("beast", EnemyFamilies.For(EnemyLoader.GetEnemyData("Wolf"))?.Id, "a Wolf (Berserker, no hands) is a Beast", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(EnemyFamilies.All.All(f => f.PlaysLike.Length > 0 && f.PunishedBy.Length > 0 && f.Fernis.Length > 0),
                "every family has plays-like, punished-by and a Fernis line", ref _run, ref _passed, ref _failed);

            // Matchup: a dungeon of Brutes is good for a Warrior (Guard) and bad for a Rogue.
            var brutes = EnemyLoader.GetAllEnemyData().Where(e => EnemyFamilies.For(e)?.Id == "brute").Select(e => e.Name).Take(3).ToList();
            var dungeon = new Dungeon("Test Hall", 1, 5, "Forest", brutes);
            var forWarrior = DungeonMatchup.Evaluate(dungeon, "warrior");
            var forRogue = DungeonMatchup.Evaluate(dungeon, "rogue");
            TestBase.AssertTrue(forWarrior.Mark == DungeonMatchup.Good && forRogue.Mark == DungeonMatchup.Bad,
                $"matchup marks follow the families (warrior {forWarrior.Mark}, rogue {forRogue.Mark})", ref _run, ref _passed, ref _failed);
            var lines = forWarrior.Lines();
            TestBase.AssertTrue(lines.Count >= 2 && lines[0].StartsWith(DungeonMatchup.Good) && lines.Any(l => l.StartsWith("Fernis:")),
                $"matchup lines: mark + families, weakness, Fernis ({string.Join(" | ", lines)})", ref _run, ref _passed, ref _failed);

            // One visible modifier: a prefix replaces the region family affix.
            var enemy = EnemyLoader.CreateEnemy("Wolf", 8);
            if (enemy != null)
            {
                EnemyAffixRuntime.ApplyHome(enemy, EnemyLoader.GetEnemyData("Wolf"), null);
                int before = enemy.Affixes.Count;
                var giant = EnemyPrefixLoader.Find("Giant");
                enemy.ApplyPrefix(giant!.Name, giant.Tags);
                EnemyAffixRuntime.ApplyPrefixEffects(enemy, giant);
                TestBase.AssertTrue(enemy.Affixes.All(a => a.Name == "Giant") && enemy.Affixes.Count == 1,
                    $"a prefixed enemy keeps only its prefix rule (before {before}, after {enemy.Affixes.Count})", ref _run, ref _passed, ref _failed);
            }
            var prefixes = EnemyPrefixLoader.Get().Prefixes.Where(p => !p.BossOnly).ToList();
            var bad = prefixes.Where(p => p.Effects.Select(e => e.Lever).Distinct().Count() > 1).Select(p => p.Name).ToList();
            TestBase.AssertTrue(bad.Count == 0, $"each prefix has one rule (several levers: {string.Join(", ", bad)})", ref _run, ref _passed, ref _failed);
            var core = new HashSet<string> { "stagger", "harden", "bleed", "burn", "focus", "rattle", "expose" };
            var offCore = prefixes.SelectMany(p => p.Effects.Where(e => !string.IsNullOrWhiteSpace(e.Status) && !core.Contains(e.Status!)).Select(e => $"{p.Name}:{e.Status}")).ToList();
            TestBase.AssertTrue(offCore.Count == 0, $"prefix statuses are core statuses (off: {string.Join(", ", offCore)})", ref _run, ref _passed, ref _failed);

            // Region requirements.
            var hero = TestDataBuilders.CreateTestCharacter("Traveller", 3);
            var region = new TravelRegion { Id = "x", DisplayName = "X", Requirements = new List<string> { "level 5", "STR 999" } };
            bool ok = RegionRequirements.Meets(region, hero, out string missing);
            TestBase.AssertTrue(!ok && missing == "level 5, STR 999", $"unmet requirements are named ({missing})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(RegionRequirements.Meets(new TravelRegion(), hero, out _), "a region with no requirements is open", ref _run, ref _passed, ref _failed);
            var water = new TravelRegionCatalog().GetById("water");
            TestBase.AssertTrue(water != null && water.Requirements.Contains("level 5"), "Regions.json loads requirements", ref _run, ref _passed, ref _failed);

            // Encounters: each kind plays and writes lines; the shrine heals.
            var rng = new Random(11);
            foreach (string kind in RoomEncounters.Kinds)
            {
                var h = TestDataBuilders.CreateTestCharacter("Visitor", 5);
                h.CurrentHealth = Math.Max(1, h.GetEffectiveMaxHealth() / 2);
                int hpBefore = h.CurrentHealth;
                var played = RoomEncounters.Play(kind, h, null, rng);
                TestBase.AssertTrue(played.Count >= 2, $"{kind} encounter writes lines", ref _run, ref _passed, ref _failed);
                if (kind == "shrine")
                    TestBase.AssertTrue(h.CurrentHealth > hpBefore, "the shrine heals", ref _run, ref _passed, ref _failed);
            }
            int hits = Enumerable.Range(0, 2000).Count(i => RoomEncounters.TryEncounter(TestDataBuilders.CreateTestCharacter("C", 1), null, new Random(i)).Count > 0);
            TestBase.AssertTrue(hits > 400 && hits < 600, $"about 1 in 4 empty rooms has an encounter ({hits}/2000)", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("Rebuild Step 4 Tests", _run, _passed, _failed);
        }
    }
}
