using System;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Tests for compact victory-limerick battle packets.</summary>
    public static class BattleLimerickPacketBuilderTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleLimerickPacketBuilder Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestBuildFromFunMoments();
            TestEventFallbackWhenFewMoments();
            TestDigitStrippingInHighlights();
            TestPromptBlockShape();

            TestBase.PrintSummary("BattleLimerickPacketBuilder Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBuildFromFunMoments()
        {
            Console.WriteLine("--- Build from fun moments ---");
            var hero = new Character("Taran", 5);
            var enemy = new Enemy("Crypt Wight", 5, 80, 10, 10, 10, 10);
            var tracker = new FunMomentTracker();
            tracker.InitializeCombat(hero.Name, enemy.Name, hero.GetEffectiveMaxHealth(), enemy.GetEffectiveMaxHealth());

            // Consecutive crits produce CriticalSequence fun moments.
            for (int i = 0; i < 3; i++)
            {
                tracker.RecordEvent(new BattleEvent
                {
                    Actor = hero.Name,
                    Target = enemy.Name,
                    Action = "SLAM",
                    IsSuccess = true,
                    IsCritical = true,
                    Damage = 25,
                    ActorHealthAfter = hero.CurrentHealth,
                    TargetHealthAfter = Math.Max(0, 80 - (i + 1) * 25)
                }, hero.CurrentHealth, Math.Max(0, 80 - (i + 1) * 25));
            }

            var packet = BattleLimerickPacketBuilder.Build(
                hero, enemy, "Bone Crypt", heroWon: true, tracker, battleNarrative: null);

            TestBase.AssertEqual("Taran", packet.HeroName, "hero name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Crypt Wight", packet.EnemyName, "enemy name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Bone Crypt", packet.Location, "location", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.HeroWon, "hero won", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Highlights.Count > 0, "fun moments produce highlights",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Highlights.Count <= BattleLimerickPacketBuilder.MaxHighlights,
                "highlights capped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEventFallbackWhenFewMoments()
        {
            Console.WriteLine("--- Event fallback when few moments ---");
            var hero = new Character("Taran", 5);
            var enemy = new Enemy("Crypt Wight", 5, 80, 10, 10, 10, 10);
            var narrative = new BattleNarrative(hero.Name, enemy.Name, "Bone Crypt", 100, 80);
            narrative.AddEvent(new BattleEvent
            {
                Actor = hero.Name,
                Target = enemy.Name,
                Action = "STRIKE",
                IsSuccess = true,
                IsCritical = true,
                Damage = 12
            });
            narrative.AddEvent(new BattleEvent
            {
                Actor = hero.Name,
                Target = enemy.Name,
                Action = "SLASH",
                IsSuccess = true,
                IsCombo = true,
                Damage = 18
            });

            var packet = BattleLimerickPacketBuilder.Build(
                hero, enemy, "Bone Crypt", heroWon: true, funMoments: null, narrative);

            TestBase.AssertTrue(packet.Highlights.Count >= 2, "narrative events fill highlights",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Highlights.Any(h => h.Contains("critical", StringComparison.OrdinalIgnoreCase)),
                "critical event described",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDigitStrippingInHighlights()
        {
            Console.WriteLine("--- Digit stripping in highlights ---");
            var hero = new Character("Taran", 5);
            var enemy = new Enemy("Crypt Wight", 5, 80, 10, 10, 10, 10);
            var tracker = new FunMomentTracker();
            tracker.InitializeCombat(hero.Name, enemy.Name, 100, 80);

            // BigDamageSpike needs ≥3 prior damage samples; description includes raw damage digits.
            int enemyHp = 80;
            for (int i = 0; i < 3; i++)
            {
                enemyHp -= 8;
                tracker.RecordEvent(new BattleEvent
                {
                    Actor = hero.Name,
                    Target = enemy.Name,
                    Action = "SLAM",
                    IsSuccess = true,
                    Damage = 8,
                    ActorHealthAfter = 100,
                    TargetHealthAfter = enemyHp
                }, 100, enemyHp);
            }

            enemyHp -= 40;
            tracker.RecordEvent(new BattleEvent
            {
                Actor = hero.Name,
                Target = enemy.Name,
                Action = "SLAM",
                IsSuccess = true,
                Damage = 40,
                ActorHealthAfter = 100,
                TargetHealthAfter = enemyHp
            }, 100, enemyHp);

            var packet = BattleLimerickPacketBuilder.Build(
                hero, enemy, "Bone Crypt", heroWon: true, tracker, null);

            TestBase.AssertTrue(packet.Highlights.Count > 0, "spike produces highlights",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            foreach (var h in packet.Highlights)
            {
                TestBase.AssertFalse(h.Any(char.IsDigit),
                    $"highlight should have no digits: '{h}'",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestPromptBlockShape()
        {
            Console.WriteLine("--- Prompt block shape ---");
            var packet = new BattleLimerickPacket
            {
                HeroName = "Taran",
                EnemyName = "Wight",
                Location = "Crypt",
                HeroWon = true,
                HeroHpLeft = 80,
                HeroMaxHp = 100,
                Highlights = new[] { "a critical slam" }
            };

            string block = packet.ToPromptBlock();
            TestBase.AssertTrue(block.Contains("Hero name (inspiration only", StringComparison.Ordinal),
                "inspiration-only hero line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(block.Contains("Taran", StringComparison.Ordinal), "hero name present for inspiration",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(block.Contains("Suggested epithet", StringComparison.Ordinal), "suggested epithet line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(block.Contains("Enemy: Wight", StringComparison.Ordinal), "enemy line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(block.Contains("hero victory", StringComparison.Ordinal), "outcome",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(block.Contains("barely scratched", StringComparison.Ordinal), "condition band",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(block.Contains("- a critical slam", StringComparison.Ordinal), "highlight bullet",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Highlight scrub: hero name replaced with epithet in prompt bullets.
            var withName = new BattleLimerickPacket
            {
                HeroName = "Taran",
                EnemyName = "Wight",
                Location = "Crypt",
                HeroWon = true,
                HeroHpLeft = 80,
                HeroMaxHp = 100,
                Highlights = new[] { "Taran landed a critical slam" }
            };
            string scrubbedBlock = withName.ToPromptBlock();
            TestBase.AssertFalse(
                scrubbedBlock.Contains("- Taran landed", StringComparison.OrdinalIgnoreCase),
                "highlight bullet must not start with literal hero name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
