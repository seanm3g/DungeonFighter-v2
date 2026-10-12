using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Hybrid trees: secondary attribute (sum of the parents' stats) as primary attribute, hybrid material tag
    /// merging, hybrid actions, accumulation and hybrid attribute gear requirements.
    /// </summary>
    public static class HybridTreesTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== HybridTrees Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestSecondaryAttributeIsPrimary();
            TestMaterialMergesTags();
            TestBodyBlowScalesWithBody();
            TestAccumulationPerConnect();
            TestBodyRequirementOnGear();

            TestBase.PrintSummary("HybridTrees Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static Character MakeWarbrute(string name)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            hero.Progression.BarbarianPoints = 40;
            hero.Progression.WarriorPoints = 40;
            hero.Progression.EnsureSkillTreeRootsGranted();
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName(name + "Mace").WithWeaponType(WeaponType.Mace).Build();
            return hero;
        }

        private static HybridInfo Warbrute => HybridTrees.ByKey("Warbrute")!;

        private static void TestSecondaryAttributeIsPrimary()
        {
            var hero = MakeWarbrute("BodyHero");
            int body = hero.GetEffectiveStrength() + hero.GetEffectiveAgility();
            TestBase.AssertEqual(body, HybridTrees.SecondaryAttribute(hero, Warbrute), "Body = STR + AGI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.Progression.LearnedSkillRanks["wb-attr"] = 1;
            TestBase.AssertEqual(body, hero.Facade.GetEffectivePrimaryAttributeValue(), "learned Body becomes the primary attribute",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMaterialMergesTags()
        {
            var hero = MakeWarbrute("TagHero");
            var action = TestDataBuilders.CreateMockAction("SWORD_TAGGED");
            action.Tags = new List<string> { "warrior" };
            hero.AddAction(action, 1.0);
            hero.Actions.AddToCombo(action);
            int barbBefore = ClassTags.Count(hero, ClassTags.Barbarian);
            TestBase.AssertEqual(0, ClassTags.Count(hero, "warbrute"), "no Warbrute tags before the material node",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.Progression.LearnedSkillRanks["wb-material"] = 1;
            TestBase.AssertEqual(barbBefore + 1, ClassTags.Count(hero, ClassTags.Barbarian),
                "Warbrute Material: Warrior tags count toward Barbarian bonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ClassTags.Count(hero, "warbrute") >= 1, "Warrior tags count as Warbrute tags",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Hit(Character hero, string actionName)
        {
            CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = TestDataBuilders.Enemy().WithName("Dummy").Build(),
                Action = new Action { Name = actionName, Type = ActionType.Attack },
                NaturalRollValue = 10,
                Damage = 10
            });
        }

        private static void TestBodyBlowScalesWithBody()
        {
            var hero = MakeWarbrute("BlowHero");
            hero.Progression.LearnedSkillRanks["wb-act-attr"] = 3;
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            SkillEffectRouter.Instance.ResetFightState();
            hero.Effects.ConsumedDamageModPercent = 0;
            Hit(hero, "BODY BLOW");
            double power = GameConfiguration.Instance.SkillTrees.GetNode("wb-act-attr")!.Power * 3;
            double expected = SkillEffectRouter.HybridBonusPerTen * Math.Floor(HybridTrees.SecondaryAttribute(hero, Warbrute) / 10.0) * power;
            TestBase.AssertTrue(expected > 0 && hero.Effects.ConsumedDamageModPercent >= expected - 1e-6,
                "BODY BLOW adds +25% x power per 10 Body",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAccumulationPerConnect()
        {
            var hero = MakeWarbrute("SurgeHero");
            hero.Progression.LearnedSkillRanks["wb-acc-tag"] = 3;
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            SkillEffectRouter.Instance.ResetFightState();
            int before = HybridTrees.TagCount(hero, Warbrute);
            for (int i = 0; i < 6; i++)
                Hit(hero, "TEST_SWING");
            TestBase.AssertTrue(HybridTrees.TagCount(hero, Warbrute) > before, "Warbrute Momentum adds tags per connect",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            SkillEffectRouter.Instance.ResetFightState();
            TestBase.AssertEqual(before, HybridTrees.TagCount(hero, Warbrute), "accumulation resets each fight",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBodyRequirementOnGear()
        {
            var hero = MakeWarbrute("GearHero");
            int body = HybridTrees.SecondaryAttribute(hero, Warbrute);
            var item = new Item(ItemType.Head, "Test Helm", 1, 0);
            item.AttributeRequirements = new AttributeRequirements { ["body"] = body };
            TestBase.AssertTrue(item.MeetsRequirements(hero), "Body requirement met at exactly STR + AGI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            item.AttributeRequirements = new AttributeRequirements { ["body"] = body + 1 };
            TestBase.AssertTrue(!item.MeetsRequirements(hero), "Body requirement fails one point short",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.MeetsStatThreshold("Body", body), "stat thresholds understand Body",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
