using System;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Materials are shared by every class: any set material counts as a class tag after the Core root, Kinship and
    /// First Age count any set piece, Synthesis feeds every material's mints, convert masteries ride every convert,
    /// and a material still pays its own attribute (Bone STR, Glass TEC) whoever wears it.
    /// </summary>
    public static class MixedMaterialsTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== MixedMaterials Tests ===\n");
            _run = _passed = _failed = 0;
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestAnyMaterialCountsAsClassTag();
            TestKinshipCountsAnySetPiece();
            TestSynthesisFeedsEveryMaterial();
            TestMasteryRidesAnyConvert();
            TestMaterialPaysItsOwnAttribute();
            TestNoClassMaterialBranches();

            TestBase.PrintSummary("MixedMaterials Tests", _run, _passed, _failed);
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

        private static void TestAnyMaterialCountsAsClassTag()
        {
            TestBase.SetCurrentTestName(nameof(TestAnyMaterialCountsAsClassTag));
            var hero = MakeHero("MixTag", WeaponType.Sword, 5);
            hero.Weapon!.Material = "Willow";
            hero.Progression.TryLearnSkillNode("w-cunning", requirePrimaryPath: false);
            hero.Progression.TryLearnSkillNode("w-lineage", requirePrimaryPath: false);
            TestBase.AssertEqual(1, ClassTags.Count(hero, ClassTags.Warrior), "a Willow sword counts as a Warrior tag after Level 1",
                ref _run, ref _passed, ref _failed);
            hero.Weapon!.Material = "Leather";
            TestBase.AssertEqual(0, ClassTags.Count(hero, ClassTags.Warrior), "class-less Leather does not count",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestKinshipCountsAnySetPiece()
        {
            TestBase.SetCurrentTestName(nameof(TestKinshipCountsAnySetPiece));
            var hero = MakeHero("MixKin", WeaponType.Mace, 50);
            hero.Weapon!.Material = "Glass";
            int before = SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR");
            hero.Progression.LearnedSkillRanks["b-kin"] = 5;
            int after = SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR");
            TestBase.AssertEqual((int)Math.Round(1 * 5 * Power("b-kin")), after - before,
                "Tribal Kinship counts a Glass piece for STR", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, SkillEffectRouter.CountSetMaterialPieces(hero), "one set piece worn", ref _run, ref _passed, ref _failed);
        }

        private static void TestSynthesisFeedsEveryMaterial()
        {
            TestBase.SetCurrentTestName(nameof(TestSynthesisFeedsEveryMaterial));
            var hero = MakeHero("MixSynth", WeaponType.Dagger, 50);
            hero.Progression.LearnedSkillRanks["r-synth"] = 5;
            int expected = (int)Math.Floor(hero.GetEffectiveTechnique() / (double)SkillEffectRouter.SynthesisStatPerMint * 5 * Power("r-synth"));
            TestBase.AssertEqual(expected, SkillEffectRouter.SynthesisFeedBonus(hero, "Bone"), "Edge Synthesis feeds Bone mints too",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(expected, SkillEffectRouter.SynthesisFeedBonus(hero, "Glass"), "and Glass mints",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, SkillEffectRouter.SynthesisFeedBonus(hero, "Leather"), "class-less materials have no mints",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestMasteryRidesAnyConvert()
        {
            TestBase.SetCurrentTestName(nameof(TestMasteryRidesAnyConvert));
            var hero = MakeHero("MixWrath", WeaponType.Mace, 80);
            hero.Progression.LearnedSkillRanks["b-wrath"] = 1;
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            hero.Effects.AddMaterialKeyword("CHAIN", 10);
            hero.TakeDamage(hero.MaxHealth / 2);
            int before = hero.CurrentHealth;
            var foe = TestDataBuilders.Enemy().WithName("Dummy").Build();
            CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = new Action { Name = "WILLOW LASH", Type = ActionType.Attack },
                NaturalRollValue = 12
            });
            int expected = Math.Max(1, (int)Math.Round(hero.MaxHealth * 5.0 * Power("b-wrath") / 100.0));
            TestBase.AssertEqual(expected, hero.CurrentHealth - before, "Wrath Unbound heals on a Willow convert",
                ref _run, ref _passed, ref _failed);

            int mid = hero.CurrentHealth;
            hero.Effects.ClearMaterialKeywordBank();
            CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = new Action { Name = "WILLOW LASH", Type = ActionType.Attack },
                NaturalRollValue = 12
            });
            TestBase.AssertEqual(mid, hero.CurrentHealth, "no heal when the convert's bank is empty", ref _run, ref _passed, ref _failed);
        }

        private static void TestMaterialPaysItsOwnAttribute()
        {
            TestBase.SetCurrentTestName(nameof(TestMaterialPaysItsOwnAttribute));
            var wizard = MakeHero("MixWiz", WeaponType.Wand, 5);
            TestBase.AssertEqual("STR", MaterialSetController.GetConvertStatCode(wizard, "RAGE"), "a Wizard's BONE WRATH still pays STR",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("TEC", MaterialSetController.GetConvertStatCode(wizard, "GRAZE"), "and GLASS NICK pays TEC",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestNoClassMaterialBranches()
        {
            TestBase.SetCurrentTestName(nameof(TestNoClassMaterialBranches));
            foreach (var tree in GameConfiguration.Instance.SkillTrees.Trees)
                foreach (var node in tree.Nodes)
                    TestBase.AssertTrue(!ItemMaterialRules.IsSetMaterial(node.Branch),
                        $"{node.Id} branch '{node.Branch}' is not a material name", ref _run, ref _passed, ref _failed);
        }
    }
}
