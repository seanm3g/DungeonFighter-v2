using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Level-100 class-tree effects: tier gate, Kinship, Synthesis feed, Titan, Perfect Aim, Archmage, Arcane Weave,
    /// convert masteries and Wand Mastery. Expected values read node power from SkillTrees.json.
    /// </summary>
    public static class ClassTreeEffectsTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ClassTreeEffects Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestTierGate();
            TestKinship();
            TestSynthesisFeed();
            TestTitanRaisesStrengthCap();
            TestPerfectAimAndArchmage();
            TestArcaneWeave();
            TestBoneWrathHeals();
            TestWandMasteryTagsStrip();

            TestBase.PrintSummary("ClassTreeEffects Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static double Power(string id) => GameConfiguration.Instance.SkillTrees.GetNode(id)?.Power ?? 1.0;

        private static Character MakeHero(string name, WeaponType weaponType, int points)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            switch (weaponType)
            {
                case WeaponType.Mace: hero.Progression.BarbarianPoints = points; break;
                case WeaponType.Sword: hero.Progression.WarriorPoints = points; break;
                case WeaponType.Dagger: hero.Progression.RoguePoints = points; break;
                case WeaponType.Wand: hero.Progression.WizardPoints = points; break;
            }
            hero.Progression.EnsureSkillTreeRootsGranted();
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName(name + "Weapon").WithWeaponType(weaponType).Build();
            return hero;
        }

        private static void TestTierGate()
        {
            var node = GameConfiguration.Instance.SkillTrees.GetNode("b-titan")!;
            var tree = SkillTreePrerequisites.FindOwnerTree(node)!;
            var low = new CharacterProgression { BarbarianPoints = SkillTreesConfig.TierPointGate(node.Tier) - 1 };
            var high = new CharacterProgression { BarbarianPoints = SkillTreesConfig.TierPointGate(node.Tier) };
            TestBase.AssertTrue(!SkillTreePrerequisites.IsTierOpen(low, tree, node), "capstone tier closed below its point gate",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(SkillTreePrerequisites.IsTierOpen(high, tree, node), "capstone tier opens at its point gate",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestKinship()
        {
            var hero = MakeHero("Kin", WeaponType.Mace, 50);
            hero.Weapon!.Material = "Bone";
            int before = SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR");
            hero.Progression.LearnedSkillRanks["b-kin"] = 5;
            int after = SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR");
            TestBase.AssertEqual((int)Math.Round(1 * 5 * Power("b-kin")), after - before,
                "Tribal Kinship: STR = Barbarian pieces x rank x power",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSynthesisFeed()
        {
            var hero = MakeHero("Synth", WeaponType.Dagger, 50);
            TestBase.AssertEqual(0, SkillEffectRouter.SynthesisFeedBonus(hero, "Glass"), "no feed without Edge Synthesis",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.Progression.LearnedSkillRanks["r-synth"] = 5;
            int tec = hero.GetEffectiveTechnique();
            int expected = (int)Math.Floor(tec / (double)SkillEffectRouter.SynthesisStatPerMint * 5 * Power("r-synth"));
            TestBase.AssertEqual(expected, SkillEffectRouter.SynthesisFeedBonus(hero, "Glass"),
                "Edge Synthesis feeds Glass mints from TEC",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, SkillEffectRouter.SynthesisFeedBonus(hero, "Bone"), "Edge Synthesis does not feed Barbarian materials",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTitanRaisesStrengthCap()
        {
            var hero = MakeHero("Titan", WeaponType.Mace, 80);
            TestBase.AssertEqual(0, SkillEffectRouter.StrengthCapBonus(hero), "no cap bonus without Titan",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.Progression.LearnedSkillRanks["b-titan"] = 1;
            TestBase.AssertEqual((int)Math.Round(50 * Power("b-titan")), SkillEffectRouter.StrengthCapBonus(hero),
                "Titan raises the STR damage cap by 50 x power",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPerfectAimAndArchmage()
        {
            var rogue = MakeHero("Aim", WeaponType.Dagger, 80);
            rogue.Progression.LearnedSkillRanks["r-aim"] = 1;
            int expected = (int)Math.Floor(rogue.GetEffectiveTechnique() / 25.0 * Power("r-aim"));
            TestBase.AssertEqual(expected, SkillEffectRouter.PerfectAimCritSteps(rogue), "Perfect Aim crit steps from TEC",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var wizard = MakeHero("Arch", WeaponType.Wand, 80);
            TestBase.AssertEqual(1.0, SkillEffectRouter.ArchmageDurationMultiplier(wizard), "no Archmage, no duration change",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            wizard.Progression.LearnedSkillRanks["z-archmage"] = 1;
            TestBase.AssertEqual(1.0 + Power("z-archmage"), SkillEffectRouter.ArchmageDurationMultiplier(wizard),
                "Archmage multiplies INT's duration bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Publish(Character hero, Action action, Actor? target)
        {
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            SkillEffectRouter.Instance.ResetFightState();
            CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = target,
                Action = action,
                NaturalRollValue = 10,
                Damage = 10
            });
        }

        private static void TestArcaneWeave()
        {
            var hero = MakeHero("Weave", WeaponType.Wand, 50);
            hero.Progression.LearnedSkillRanks["z-weave"] = 5;
            hero.Effects.ConsumedSkillAmpPercent = 0;
            Publish(hero, new Action { Name = "TEST_SPELL", Type = ActionType.Attack }, TestDataBuilders.Enemy().WithName("Dummy").Build());
            double expected = Math.Floor(hero.GetEffectiveIntelligence() / 10.0) * 2.0 * 5 * Power("z-weave");
            TestBase.AssertTrue(Math.Abs(hero.Effects.ConsumedSkillAmpPercent - expected) < 1e-6 || expected == 0,
                "Arcane Weave: AMP from INT on Wand actions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBoneWrathHeals()
        {
            var hero = MakeHero("Wrath", WeaponType.Mace, 80);
            hero.Progression.LearnedSkillRanks["b-wrath"] = 3;
            hero.Effects.AddMaterialKeyword("RAGE", 10);
            hero.TakeDamage(hero.MaxHealth / 2);
            int before = hero.CurrentHealth;
            Publish(hero, new Action { Name = "BONE WRATH", Type = ActionType.Attack }, TestDataBuilders.Enemy().WithName("Dummy").Build());
            TestBase.AssertTrue(hero.CurrentHealth > before, "Wrath Unbound: BONE WRATH heals per RAGE",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWandMasteryTagsStrip()
        {
            var hero = MakeHero("WandMaster", WeaponType.Wand, 80);
            var plain = TestDataBuilders.CreateMockAction("PLAIN");
            plain.Tags = new List<string>();
            hero.AddAction(plain, 1.0);
            hero.Actions.AddToCombo(plain);
            int before = ClassTags.Count(hero, ClassTags.Wizard);
            hero.Progression.LearnedSkillRanks["z-mastery"] = 1;
            TestBase.AssertEqual(before + 1, ClassTags.Count(hero, ClassTags.Wizard), "Wand Mastery: untagged strip action counts as Wizard",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
