using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>Environment-tagged actions must not enter hero or enemy action pools; enemy-tagged actions must not enter the hero pool.</summary>
    public static class CharacterEnvironmentActionPoolTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Character Environment Action Pool Tests ===\n");
            int run = 0, pass = 0, fail = 0;

            CharacterRejectsEnvironmentTaggedAddAction(ref run, ref pass, ref fail);
            CharacterRejectsEnvironmentTaggedAddActionAllowDuplicates(ref run, ref pass, ref fail);
            EnemyInheritsCharacterEnvironmentRejection(ref run, ref pass, ref fail);
            CharacterRejectsEnemyTaggedAddAction(ref run, ref pass, ref fail);
            CharacterRejectsEnemyTaggedAddActionAllowDuplicates(ref run, ref pass, ref fail);
            EnemyAcceptsEnemyTaggedActions(ref run, ref pass, ref fail);

            TestBase.PrintSummary("Character Environment Action Pool Tests", run, pass, fail);
        }

        private static void CharacterRejectsEnvironmentTaggedAddAction(ref int run, ref int pass, ref int fail)
        {
            run++;
            var c = new Character("PoolTestHero");
            var env = new Action("RoomHazard", ActionType.Debuff, TargetType.AreaOfEffect, 0, "x")
            {
                Tags = new System.Collections.Generic.List<string> { "environment" }
            };
            var ok = new Action("Strike", ActionType.Attack, TargetType.SingleTarget, 0, "hit")
            {
                Tags = new System.Collections.Generic.List<string> { "weapon", "sword" }
            };
            c.AddAction(env, 1.0);
            c.AddAction(ok, 1.0);
            if (!c.ActionPool.Any(a => a.action.Name == "RoomHazard") && c.ActionPool.Any(a => a.action.Name == "Strike"))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL CharacterRejectsEnvironmentTaggedAddAction");
            }
        }

        private static void CharacterRejectsEnvironmentTaggedAddActionAllowDuplicates(ref int run, ref int pass, ref int fail)
        {
            run++;
            var c = new Character("DupTestHero");
            var env = new Action("HazardDup", ActionType.Debuff, TargetType.AreaOfEffect, 0, "x")
            {
                Tags = new System.Collections.Generic.List<string> { "ENVIRONMENT" }
            };
            c.AddActionAllowDuplicates(env, 1.0);
            if (c.ActionPool.Count == 0)
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL CharacterRejectsEnvironmentTaggedAddActionAllowDuplicates");
            }
        }

        private static void EnemyInheritsCharacterEnvironmentRejection(ref int run, ref int pass, ref int fail)
        {
            run++;
            var e = new Enemy("PoolTestEnemy", level: 1, maxHealth: 10, strength: 5, agility: 5, technique: 5, intelligence: 5, armor: 0);
            e.ActionPool.Clear();
            var env = new Action("EnemyEnvLeak", ActionType.Debuff, TargetType.AreaOfEffect, 0, "x")
            {
                Tags = new System.Collections.Generic.List<string> { "environment" }
            };
            e.AddAction(env, 1.0);
            if (!e.ActionPool.Any(a => a.action.Name == "EnemyEnvLeak"))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL EnemyInheritsCharacterEnvironmentRejection");
            }
        }

        private static void CharacterRejectsEnemyTaggedAddAction(ref int run, ref int pass, ref int fail)
        {
            run++;
            var c = new Character("EnemyTagHero");
            var enemyOnly = new Action("BossClaw", ActionType.Attack, TargetType.SingleTarget, 0, "hit")
            {
                Tags = new System.Collections.Generic.List<string> { "enemy" }
            };
            var ok = new Action("Strike", ActionType.Attack, TargetType.SingleTarget, 0, "hit")
            {
                Tags = new System.Collections.Generic.List<string> { "weapon", "sword" }
            };
            c.AddAction(enemyOnly, 1.0);
            c.AddAction(ok, 1.0);
            if (!c.ActionPool.Any(a => a.action.Name == "BossClaw") && c.ActionPool.Any(a => a.action.Name == "Strike"))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL CharacterRejectsEnemyTaggedAddAction");
            }
        }

        private static void CharacterRejectsEnemyTaggedAddActionAllowDuplicates(ref int run, ref int pass, ref int fail)
        {
            run++;
            var c = new Character("EnemyTagDupHero");
            var enemyOnly = new Action("BossClawDup", ActionType.Attack, TargetType.SingleTarget, 0, "hit")
            {
                Tags = new System.Collections.Generic.List<string> { "ENEMY" }
            };
            c.AddActionAllowDuplicates(enemyOnly, 1.0);
            if (c.ActionPool.Count == 0)
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL CharacterRejectsEnemyTaggedAddActionAllowDuplicates");
            }
        }

        private static void EnemyAcceptsEnemyTaggedActions(ref int run, ref int pass, ref int fail)
        {
            run++;
            var e = new Enemy("EnemyTagMob", level: 1, maxHealth: 10, strength: 5, agility: 5, technique: 5, intelligence: 5, armor: 0);
            e.ActionPool.Clear();
            var enemyOnly = new Action("BossClawOnEnemy", ActionType.Attack, TargetType.SingleTarget, 0, "hit")
            {
                Tags = new System.Collections.Generic.List<string> { "enemy" }
            };
            e.AddAction(enemyOnly, 1.0);
            if (e.ActionPool.Any(a => a.action.Name == "BossClawOnEnemy"))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL EnemyAcceptsEnemyTaggedActions");
            }
        }
    }
}
