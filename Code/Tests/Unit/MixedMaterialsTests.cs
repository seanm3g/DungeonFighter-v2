using System;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Any class can wear any material (weapons roll all twelve) and a material pays its own attribute (Bone STR,
    /// Glass TEC) whoever wears it. Class skill trees still key off the class's own three materials: tags and Kinship
    /// count only those.
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

            TestWeaponsRollAnyMaterial();
            TestMaterialPaysItsOwnAttribute();
            TestClassTagsCountOnlyOwnMaterials();
            TestKinshipCountsOnlyOwnMaterials();
            TestClasslessMaterialPaysTopAttribute();

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

        private static void TestWeaponsRollAnyMaterial()
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponsRollAnyMaterial));
            var random = new Random(5);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 400; i++)
                seen.Add(ItemMaterialRules.PickClassMaterial(WeaponType.Wand, random));
            TestBase.AssertEqual(ItemMaterialRules.ClassMaterials.Length, seen.Count, "a Wand can roll every set material",
                ref _run, ref _passed, ref _failed);
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

        private static void TestClasslessMaterialPaysTopAttribute()
        {
            TestBase.SetCurrentTestName(nameof(TestClasslessMaterialPaysTopAttribute));
            var hero = TestDataBuilders.Character().WithName("MixStone").WithStats(8, 14, 10, 6).Build();
            int agi = hero.GetEffectiveAgility();
            int str = hero.GetEffectiveStrength();
            var stone = new Modification { Name = "Stone", PrefixCategory = "MATERIAL", Effect = "PRIMARY", MinValue = 2, MaxValue = 2, RolledValue = 2 };
            var celestial = new Modification { Name = "Celestial", PrefixCategory = "MATERIAL", Effect = "PRIMARY", MinValue = 4, MaxValue = 4, RolledValue = 4 };
            hero.EquipItem(TestDataBuilders.Armor().WithType(ItemType.Head).WithName("StoneHelm").WithModification(stone).Build(), "head");
            hero.EquipItem(TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("CelestialBoots").WithModification(celestial).Build(), "feet");
            TestBase.AssertEqual(agi + 6, hero.GetEffectiveAgility(), "class-less pieces add their value to the top attribute (AGI)",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(str, hero.GetEffectiveStrength(), "and nothing to the other attributes",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestClassTagsCountOnlyOwnMaterials()
        {
            TestBase.SetCurrentTestName(nameof(TestClassTagsCountOnlyOwnMaterials));
            var hero = MakeHero("MixTag", WeaponType.Sword, 5);
            hero.Progression.TryLearnSkillNode("w-cunning", requirePrimaryPath: false);
            hero.Progression.TryLearnSkillNode("w-lineage", requirePrimaryPath: false);
            hero.Weapon!.Material = "Bronze";
            int own = ClassTags.Count(hero, ClassTags.Warrior);
            hero.Weapon!.Material = "Willow";
            int other = ClassTags.Count(hero, ClassTags.Warrior);
            TestBase.AssertTrue(own > other, $"a Bronze sword is a Warrior tag, a Willow one is not ({own} vs {other})",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestKinshipCountsOnlyOwnMaterials()
        {
            TestBase.SetCurrentTestName(nameof(TestKinshipCountsOnlyOwnMaterials));
            var hero = MakeHero("MixKin", WeaponType.Mace, 50);
            hero.Progression.LearnedSkillRanks["b-kin"] = 5;
            hero.Weapon!.Material = "Glass";
            int glass = SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR");
            hero.Weapon!.Material = "Bone";
            int bone = SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR");
            TestBase.AssertEqual((int)Math.Round(1 * 5 * Power("b-kin")), bone - glass,
                "Tribal Kinship counts a Bone piece, not a Glass one", ref _run, ref _passed, ref _failed);
        }
    }
}
