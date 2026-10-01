using System.Linq;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Combat;
using RPGGame.Combat.Formatting;
using RPGGame.Data;
using RPGGame.Display.Dungeon;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    public static class PackEnemyTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Pack Enemy Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestOverkillWastesDamageAndDropsOneBody(ref run, ref passed, ref failed);
            TestWoundedBodyFinishesBeforeTheNext(ref run, ref passed, ref failed);
            TestHealDoesNotRevive(ref run, ref passed, ref failed);
            TestRemainderStaysOnThePackTotal(ref run, ref passed, ref failed);
            TestOutgoingHitsFollowLivingBodies(ref run, ref passed, ref failed);
            TestPackDiesOnlyAtZeroBodies(ref run, ref passed, ref failed);
            TestNonPackMultiHitStillCreditsOverkill(ref run, ref passed, ref failed);
            TestCombatLogText(ref run, ref passed, ref failed);
            TestAuthoredPackSizesFromData(ref run, ref passed, ref failed);

            TestBase.PrintSummary("Pack Enemy Tests", run, passed, failed);
        }

        private static Enemy CreatePack(string name, int maxHealth, int packSize)
        {
            var enemy = TestDataBuilders.Enemy()
                .WithName(name)
                .WithLevel(1)
                .WithHealth(maxHealth)
                .WithStats(5, 5, 5, 5)
                .Build();
            enemy.InitializePack(packSize);
            return enemy;
        }

        private static void TestOverkillWastesDamageAndDropsOneBody(ref int run, ref int passed, ref int failed)
        {
            var bat = CreatePack("Bat", 60, 3);
            TestBase.AssertEqual(3, bat.LivingBodyCount, "three bodies", ref run, ref passed, ref failed);
            TestBase.AssertEqual(20, bat.FrontBodyRemaining, "even split", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, bat.PackDividerFractions.Length, "two dividers", ref run, ref passed, ref failed);

            bat.TakeDamage(50);
            TestBase.AssertEqual(40, bat.CurrentHealth, "only one body of damage lands", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, bat.LivingBodyCount, "one body down", ref run, ref passed, ref failed);
            TestBase.AssertEqual(30, bat.LastOverkillWasted, "overflow wasted", ref run, ref passed, ref failed);
            TestBase.AssertTrue(bat.IsAlive, "pack still fighting", ref run, ref passed, ref failed);
        }

        private static void TestWoundedBodyFinishesBeforeTheNext(ref int run, ref int passed, ref int failed)
        {
            var bat = CreatePack("Bat", 60, 3);
            bat.TakeDamage(50);
            bat.TakeDamage(10);
            TestBase.AssertEqual(30, bat.CurrentHealth, "second swing wounds the next body", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, bat.LivingBodyCount, "wounded body still up", ref run, ref passed, ref failed);
            TestBase.AssertEqual(10, bat.FrontBodyRemaining, "front body has 10 left", ref run, ref passed, ref failed);

            bat.TakeDamage(15);
            TestBase.AssertEqual(20, bat.CurrentHealth, "killing the wounded body wastes the rest", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, bat.LivingBodyCount, "one body left", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, bat.LastOverkillWasted, "five wasted", ref run, ref passed, ref failed);
        }

        private static void TestHealDoesNotRevive(ref int run, ref int passed, ref int failed)
        {
            var bat = CreatePack("Bat", 60, 3);
            bat.TakeDamage(50);
            bat.Heal(100);
            TestBase.AssertEqual(40, bat.CurrentHealth, "heal does not refill a dead body", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, bat.LivingBodyCount, "still two bodies", ref run, ref passed, ref failed);

            bat.CurrentHealth = bat.MaxHealth;
            TestBase.AssertEqual(60, bat.CurrentHealth, "assigning max HP restores the pack", ref run, ref passed, ref failed);
            TestBase.AssertEqual(3, bat.LivingBodyCount, "restored bodies", ref run, ref passed, ref failed);
        }

        private static void TestRemainderStaysOnThePackTotal(ref int run, ref int passed, ref int failed)
        {
            var pack = CreatePack("Rats", 50, 3);
            TestBase.AssertEqual(50, pack.CurrentHealth, "pack total stays 50", ref run, ref passed, ref failed);
            TestBase.AssertEqual(50, pack.PackMaxHealth, "body maxes sum to 50", ref run, ref passed, ref failed);
            pack.TakeDamage(100);
            TestBase.AssertEqual(33, pack.CurrentHealth, "front body was 17", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, pack.LivingBodyCount, "one of three down", ref run, ref passed, ref failed);
        }

        private static void TestOutgoingHitsFollowLivingBodies(ref int run, ref int passed, ref int failed)
        {
            var bat = CreatePack("Bat", 60, 3);
            TestBase.AssertEqual(3, PackCombat.ResolveOutgoingHitCount(bat, 1), "three bodies swing three times", ref run, ref passed, ref failed);
            bat.TakeDamage(20);
            TestBase.AssertEqual(2, PackCombat.ResolveOutgoingHitCount(bat, 5), "authored hits do not add", ref run, ref passed, ref failed);
            bat.TakeDamage(20);
            TestBase.AssertEqual(1, PackCombat.ResolveOutgoingHitCount(bat, 3), "last body is one attack", ref run, ref passed, ref failed);
        }

        private static void TestPackDiesOnlyAtZeroBodies(ref int run, ref int passed, ref int failed)
        {
            var bat = CreatePack("Bat", 60, 3);
            bat.TakeDamage(20);
            bat.TakeDamage(20);
            TestBase.AssertTrue(bat.IsAlive, "one body left keeps the fight up", ref run, ref passed, ref failed);
            bat.TakeDamage(20);
            TestBase.AssertEqual(0, bat.CurrentHealth, "pack HP is 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, bat.LivingBodyCount, "no bodies left", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!bat.IsAlive, "pack is defeated", ref run, ref passed, ref failed);
        }

        private static void TestNonPackMultiHitStillCreditsOverkill(ref int run, ref int passed, ref int failed)
        {
            var hero = TestDataBuilders.Character()
                .WithName("Hero")
                .WithLevel(1)
                .WithStats(20, 10, 10, 10)
                .Build();
            var enemy = TestDataBuilders.Enemy()
                .WithName("Weak")
                .WithLevel(1)
                .WithHealth(15)
                .WithStats(5, 5, 5, 5)
                .Build();
            var action = new Action
            {
                Name = "Flurry",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 4 }
            };
            int total = MultiHitProcessor.ProcessMultiHit(
                hero, enemy, action, 1.0, 15, 15, 0, 15, null);
            TestBase.AssertEqual(0, enemy.CurrentHealth, "non-pack still dies", ref run, ref passed, ref failed);
            TestBase.AssertTrue(total > 15, $"non-pack multi-hit still credits overkill ({total})", ref run, ref passed, ref failed);
        }

        private static void TestCombatLogText(ref int run, ref int passed, ref int failed)
        {
            var hero = TestDataBuilders.Character().WithName("Hero").WithLevel(1).WithStats(10, 10, 10, 10).Build();
            var bat = CreatePack("Bat", 60, 3);

            string appearance = string.Concat(EnemyInfoBuilder.BuildEncounterAppearanceSegments(bat).Select(s => s.Text));
            TestBase.AssertTrue(appearance.Contains("Bat(3x)", StringComparison.Ordinal), "appearance uses Bat(3x)", ref run, ref passed, ref failed);

            bat.PackHeadlineCount = 3;
            bat.TakeDamage(50);
            string setup = ColoredTextRenderer.RenderAsPlainText(ActionHeadlineFormatter.FormatSetup(hero, bat));
            TestBase.AssertTrue(setup.Contains("Bat(3x)", StringComparison.Ordinal), "killing swing keeps the pre-swing count", ref run, ref passed, ref failed);
            bat.PackHeadlineCount = null;

            var result = new ActionExecutionResult();
            PackCombat.AccountAfterDamage(result, bat, livingBefore: 3, hpBefore: 60, replaceDamage: true);
            TestBase.AssertEqual(20, result.Damage, "logged damage is what landed", ref run, ref passed, ref failed);
            TestBase.AssertEqual(30, result.OverkillWasted, "logged overkill", ref run, ref passed, ref failed);
            TestBase.AssertEqual("One falls. Bat(2x) remain.", result.PackBodyFellText, "body falls line", ref run, ref passed, ref failed);

            var action = new Action { Name = "Strike", Type = ActionType.Attack, DamageMultiplier = 1.0 };
            var (damageText, _) = DamageFormatter.FormatDamageDisplayColored(
                hero, bat, 20, 20, action, 1.0, 1.0, 0, 12, 1, false, false, default, null, result.OverkillWasted);
            string damagePlain = ColoredTextRenderer.RenderAsPlainText(damageText);
            TestBase.AssertTrue(damagePlain.Contains("for 20 damage (30 overkill)", StringComparison.Ordinal),
                "overkill sits on the damage line", ref run, ref passed, ref failed);

            bat.TakeDamage(20);
            int lastHits = PackCombat.ResolveOutgoingHitCount(bat, 3);
            var (lastText, _) = DamageFormatter.FormatDamageDisplayColored(
                bat, hero, 5, 5, action, 1.0, 1.0, 0, 10, lastHits);
            string lastPlain = ColoredTextRenderer.RenderAsPlainText(lastText);
            TestBase.AssertTrue(!lastPlain.Contains(" hits)", StringComparison.Ordinal), "last body has no hit suffix", ref run, ref passed, ref failed);

            bat.TakeDamage(100);
            string defeatedName = string.Concat(EntityColorHelper.BuildEnemyNameDisplaySegments(bat).Select(s => s.Text));
            TestBase.AssertEqual("Bat", defeatedName, "wiped pack drops the count", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Bat has been defeated!", defeatedName + " has been defeated!", "defeat line", ref run, ref passed, ref failed);
        }

        private static void TestAuthoredPackSizesFromData(ref int run, ref int passed, ref int failed)
        {
            EnemyLoader.LoadEnemies();
            var bats = EnemyLoader.CreateEnemy("Bats", 1);
            var rats = EnemyLoader.CreateEnemy("Rats", 1);
            var ants = EnemyLoader.CreateEnemy("Giant Ants", 1);
            TestBase.AssertTrue(bats != null && bats.IsPack && bats.PackSize == 3 && bats.LivingBodyCount == 3,
                "Bats are a 3-body pack", ref run, ref passed, ref failed);
            TestBase.AssertTrue(rats != null && rats.IsPack && rats.PackSize == 7 && rats.LivingBodyCount == 7,
                "Rats are a 7-body pack", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ants != null && ants.IsPack && ants.PackSize == 30 && ants.LivingBodyCount == 30,
                "Giant Ants are a 30-body pack", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ants!.MaxHealth >= 30,
                "Giant Ants have at least 1 HP per body", ref run, ref passed, ref failed);

            TestBase.AssertTrue(bats!.ActionPool.Any(a => a.action.Name.Equals("BAT SWARM", StringComparison.OrdinalIgnoreCase)
                    && a.action.Advanced.MultiHitCount == 3),
                "Bats use BAT SWARM (3 hits)", ref run, ref passed, ref failed);
            TestBase.AssertTrue(rats!.ActionPool.Any(a => a.action.Name.Equals("RAT SWARM", StringComparison.OrdinalIgnoreCase)
                    && a.action.Advanced.MultiHitCount == 7),
                "Rats use RAT SWARM (7 hits)", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ants!.ActionPool.Any(a => a.action.Name.Equals("ANT SWARM", StringComparison.OrdinalIgnoreCase)
                    && a.action.Advanced.MultiHitCount == 30),
                "Giant Ants use ANT SWARM (30 hits)", ref run, ref passed, ref failed);
            var antAction = ActionLoader.GetAction("ANT SWARM");
            TestBase.AssertTrue(antAction != null && antAction.DamageMultiplier <= 0.05,
                "ANT SWARM keeps per-ant damage tiny", ref run, ref passed, ref failed);
        }
    }
}
