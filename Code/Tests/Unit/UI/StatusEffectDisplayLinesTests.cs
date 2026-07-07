using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    public static class StatusEffectDisplayLinesTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== StatusEffectDisplayLines Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var clean = TestDataBuilders.Character().WithName("CleanHero").WithStats(10, 10, 10, 0).Build();
            var noEffects = StatusEffectDisplayLines.Build(clean, clean);
            TestBase.AssertEqual(0, noEffects.Count, "no effects yields empty list (INT 0 avoids persistent roll bonus line)", ref run, ref passed, ref failed);

            var tempRoll = TestDataBuilders.Character().WithName("TempRoll").WithStats(10, 10, 10, 0).Build();
            tempRoll.Effects.SetTempRollBonus(2, 3);
            var tempRollLines = StatusEffectDisplayLines.Build(tempRoll, tempRoll);
            TestBase.AssertTrue(tempRollLines.Any(l => l.Contains("Accuracy +2") && l.Contains("3 turns")), "temp roll bonus merges into Accuracy +N with turn count", ref run, ref passed, ref failed);

            var turnAcc = TestDataBuilders.Character().WithName("TurnAcc").WithStats(10, 10, 10, 0).Build();
            turnAcc.Effects.AddActionAttackBonuses(new ActionAttackBonuses
            {
                BonusGroups = new System.Collections.Generic.List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "TURN",
                        Count = 3,
                        Bonuses = new System.Collections.Generic.List<ActionAttackBonusItem>
                        {
                            new ActionAttackBonusItem { Type = "ACCURACY", Value = 3 }
                        }
                    }
                }
            });
            var turnAccLines = StatusEffectDisplayLines.Build(turnAcc, turnAcc);
            TestBase.AssertTrue(turnAccLines.Any(l => l.Contains("Accuracy +3") && l.Contains("3 turns")),
                "TURN cadence accuracy shows remaining turn count", ref run, ref passed, ref failed);

            var turnHit = TestDataBuilders.Character().WithName("TurnHit").WithStats(10, 10, 10, 0).Build();
            turnHit.Effects.AddActionAttackBonuses(new ActionAttackBonuses
            {
                BonusGroups = new System.Collections.Generic.List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "TURN",
                        Count = 2,
                        Bonuses = new System.Collections.Generic.List<ActionAttackBonusItem>
                        {
                            new ActionAttackBonusItem { Type = "HIT", Value = 1 }
                        }
                    }
                }
            });
            var turnHitLines = StatusEffectDisplayLines.Build(turnHit, turnHit);
            TestBase.AssertTrue(turnHitLines.Any(l => l.Contains("+1 HIT") && l.Contains("2 turns")),
                "TURN HIT bonus shows as status line", ref run, ref passed, ref failed);

            var stackA = TestDataBuilders.Character().WithName("StackA").WithStats(10, 10, 10, 0).Build();
            stackA.Effects.TurnBonuses.Add(new ActionAttackBonusGroup
            {
                CadenceType = "TURN",
                Count = 3,
                Bonuses = new System.Collections.Generic.List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = "COMBO", Value = 1 }
                }
            });
            stackA.Effects.TurnBonuses.Add(new ActionAttackBonusGroup
            {
                CadenceType = "TURN",
                Count = 2,
                Bonuses = new System.Collections.Generic.List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = "HIT", Value = 2 }
                }
            });
            var stackLines = StatusEffectDisplayLines.Build(stackA, stackA);
            TestBase.AssertTrue(stackLines.Count(l => l.Contains("TURN x")) >= 2,
                "two concurrent TURN groups both visible for stacking", ref run, ref passed, ref failed);

            var bleed = TestDataBuilders.Character().WithName("BleedHero").Build();
            bleed.BleedIntensity = 3;
            var bleedLines = StatusEffectDisplayLines.Build(bleed, bleed);
            TestBase.AssertTrue(bleedLines.Any(l => l.Contains("Bleed 3")), "bleed intensity line", ref run, ref passed, ref failed);

            var poison = TestDataBuilders.Character().WithName("PoisonHero").Build();
            poison.PoisonPercentOfMaxHealth = 2;
            var poisonLines = StatusEffectDisplayLines.Build(poison, poison);
            TestBase.AssertTrue(poisonLines.Any(l => l.Contains("Poison") && l.Contains("2") && l.Contains("max HP")), "poison % line", ref run, ref passed, ref failed);

            var enemy = new Enemy(name: "Goblin", level: 1, maxHealth: 20, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0);
            enemy.BleedIntensity = 4;
            var enemyOnly = StatusEffectDisplayLines.Build(enemy, null);
            TestBase.AssertTrue(enemyOnly.Any(l => l.Contains("Bleed 4")), "enemy without Character extras", ref run, ref passed, ref failed);

            TestBase.AssertTrue(StatusEffectDisplayLines.GetNonLivingEnemyImmunityLine(enemy) == null,
                "living enemy (default isLiving): no template immunity line", ref run, ref passed, ref failed);
            var skeleton = new Enemy(name: "Skeleton", level: 1, maxHealth: 20, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0,
                primaryAttribute: PrimaryAttribute.Strength, isLiving: false);
            var skLine = StatusEffectDisplayLines.GetNonLivingEnemyImmunityLine(skeleton);
            TestBase.AssertTrue(skLine != null && skLine.Contains("Immune") && skLine.Contains("Bleed") && skLine.Contains("Poison"),
                "non-living enemy: immunity line lists bleed and poison", ref run, ref passed, ref failed);

            TestBase.PrintSummary("StatusEffectDisplayLines Tests", run, passed, failed);
        }
    }
}
