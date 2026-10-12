using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Class tags (gear + strip, materials after the Core root), the cadence / market rules built on them, and the
    /// formerly empty skill nodes: natural-roll passives, Concussive Crit, Precision multihit, Dagger crit-miss salvage.
    /// </summary>
    public static class ClassTagsTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ClassTags Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestMaterialCountsAfterRoot();
            TestStripActionTagsCount();
            TestCadenceStepsPerTenTags();
            TestMarketChanceInLootContext();
            TestIronHideRollArmor();
            TestConcussiveCritStuns();
            TestPrecisionAddsMultihit();

            TestBase.PrintSummary("ClassTags Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static Character MakeHero(string name, WeaponType weaponType, int classPoints)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            switch (weaponType)
            {
                case WeaponType.Mace: hero.Progression.BarbarianPoints = classPoints; break;
                case WeaponType.Sword: hero.Progression.WarriorPoints = classPoints; break;
                case WeaponType.Dagger: hero.Progression.RoguePoints = classPoints; break;
                case WeaponType.Wand: hero.Progression.WizardPoints = classPoints; break;
            }
            hero.Progression.EnsureSkillTreeRootsGranted();
            var weapon = TestDataBuilders.Weapon().WithName(name + "Weapon").WithWeaponType(weaponType).Build();
            hero.Equipment.Weapon = weapon;
            return hero;
        }

        private static void AddStrip(Character hero, string name, params string[] tags)
        {
            var action = TestDataBuilders.CreateMockAction(name);
            action.Tags = new List<string>(tags);
            hero.AddAction(action, 1.0);
            hero.Actions.AddToCombo(action);
        }

        private static void TestMaterialCountsAfterRoot()
        {
            var noPoints = MakeHero("NoRoot", WeaponType.Mace, 0);
            noPoints.Weapon!.Material = "Bone";
            TestBase.AssertEqual(0, ClassTags.Count(noPoints, ClassTags.Barbarian),
                "Bone does not count as Barbarian before the Core root",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rooted = MakeHero("Rooted", WeaponType.Mace, 5);
            rooted.Weapon!.Material = "Bone";
            TestBase.AssertEqual(1, ClassTags.Count(rooted, ClassTags.Barbarian),
                "Bone counts as Barbarian once Tribe Metal (root) is learned",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            rooted.Weapon!.Tags.Add("barbarian");
            TestBase.AssertEqual(2, ClassTags.Count(rooted, ClassTags.Barbarian),
                "a Barbarian-tagged Bone weapon counts twice (tag + material)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStripActionTagsCount()
        {
            var hero = MakeHero("Strip", WeaponType.Dagger, 5);
            AddStrip(hero, "TAG_A", "rogue");
            AddStrip(hero, "TAG_B", "rogue", "dagger");
            AddStrip(hero, "TAG_C", "warrior");
            TestBase.AssertEqual(2, ClassTags.Count(hero, ClassTags.Rogue), "strip actions with the Rogue tag count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCadenceStepsPerTenTags()
        {
            var hero = MakeHero("Cadence", WeaponType.Mace, 50);
            double cadencePower = GameConfiguration.Instance.SkillTrees.GetNode("b-combo")?.Power ?? 1.0;
            int tagsForOneStep = (int)Math.Ceiling(2.0 / cadencePower);
            for (int i = 0; i < tagsForOneStep; i++)
                AddStrip(hero, "BARB_" + i, "barbarian");
            TestBase.AssertEqual(0, ClassCadence.ComboSteps(hero), "no cadence without Heavy Cadence",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.Progression.LearnedSkillRanks["b-combo"] = 1;
            double power = GameConfiguration.Instance.SkillTrees.GetNode("b-combo")?.Power ?? 1.0;
            int expected = (int)Math.Floor(ClassTags.Count(hero, ClassTags.Barbarian) / 2.0 * power);
            TestBase.AssertTrue(expected >= 1, "enough Barbarian tags reach one cadence step",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(expected, ClassCadence.ComboSteps(hero), "Heavy Cadence steps = tags / 2 x power",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, ClassCadence.ComboSteps(TestDataBuilders.Enemy().WithName("Foe").Build()), "enemies get no cadence",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMarketChanceInLootContext()
        {
            var hero = MakeHero("Market", WeaponType.Sword, 20);
            TestBase.AssertEqual(0, LootContext.Create(hero).ClassMaterialChance.Count, "no market rule, no material bias",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.Progression.LearnedSkillRanks["w-loot"] = 1;
            double power = GameConfiguration.Instance.SkillTrees.GetNode("w-loot")?.Power ?? 1.0;
            var ctx = LootContext.Create(hero);
            TestBase.AssertTrue(ctx.ClassMaterialChance.TryGetValue(ClassTags.Warrior, out double chance)
                    && Math.Abs(chance - Math.Min(1.0, LootContext.MarketChancePerPower * power)) < 1e-9,
                "Muster Hall biases drops toward Warrior materials",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Publish(Character hero, CombatEventType type, Actor? target, int naturalRoll, bool critical, Action? action = null)
        {
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            CombatEventBus.Instance.Publish(new CombatEvent(type, hero)
            {
                Target = target,
                Action = action ?? new Action { Name = "TEST_SWING", Type = ActionType.Attack },
                NaturalRollValue = naturalRoll,
                IsCritical = critical,
                Damage = 10
            });
        }

        private static void TestIronHideRollArmor()
        {
            var hero = MakeHero("IronHide", WeaponType.Mace, 50);
            hero.Progression.LearnedSkillRanks["b-lvl2"] = 1;
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            SkillEffectRouter.Instance.ResetFightState();
            Publish(hero, CombatEventType.ActionHit, null, 12, false);
            double power = GameConfiguration.Instance.SkillTrees.GetNode("b-lvl2")?.Power ?? 1.0;
            TestBase.AssertEqual((int)Math.Round(12 * power), SkillEffectRouter.Instance.GetSkillArmorBonus(hero),
                "Iron Hide Roll: armor = natural roll x power until the hero acts again",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestConcussiveCritStuns()
        {
            var hero = MakeHero("Concuss", WeaponType.Mace, 50);
            hero.Progression.LearnedSkillRanks["b-stun"] = 1;
            var foe = TestDataBuilders.Enemy().WithName("Dummy").Build();
            for (int i = 0; i < 60 && !foe.IsStunned; i++)
                Publish(hero, CombatEventType.ActionCritical, foe, 20, true);
            TestBase.AssertTrue(foe.IsStunned && foe.StunTurnsRemaining >= 1, "Mace crits with Concussive Crit stun (35% chance each)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPrecisionAddsMultihit()
        {
            var hero = MakeHero("Precise", WeaponType.Dagger, 50);
            hero.Progression.LearnedSkillRanks["r-precision"] = 1;
            hero.Effects.ConsumedMultiHitMod = 0;
            var rogueAction = new Action { Name = "TEST_STAB", Type = ActionType.Attack, Tags = new List<string> { "rogue" } };
            Publish(hero, CombatEventType.ActionHit, TestDataBuilders.Enemy().WithName("Dummy").Build(), 12, false, rogueAction);
            TestBase.AssertTrue(hero.Effects.ConsumedMultiHitMod >= 1, "Precision: Rogue-tagged action gains multihit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
