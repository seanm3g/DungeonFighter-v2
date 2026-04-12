using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class CharacterProgressionClassReferenceTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            ClassPresentationDefaults(ref run, ref passed, ref failed);
            BuildSummaryContainsBaseClassesAndThresholds(ref run, ref passed, ref failed);
            HybridTitleSameTierBandIsPrimaryOnly(ref run, ref passed, ref failed);
            HybridTitleDifferentTierBandsCombines(ref run, ref passed, ref failed);
            RenamedWandDisplayStillWizardPrimary(ref run, ref passed, ref failed);
            DuoHybridRuleOverridesJoiner(ref run, ref passed, ref failed);

            TestBase.PrintSummary("CharacterProgressionClassReferenceTests", run, passed, failed);
        }

        private static void ClassPresentationDefaults(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig().EnsureNormalized();
            var t = c.TierThresholds;
            TestBase.AssertTrue(t.Length == ClassPresentationConfig.TierSlotCount, "four class point thresholds", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, t[0], "tier-1 threshold default is 2", ref run, ref passed, ref failed);
            TestBase.AssertEqual(20, t[1], "tier-2 threshold default is 20", ref run, ref passed, ref failed);
            TestBase.AssertEqual(60, t[2], "tier-3 threshold default is 60", ref run, ref passed, ref failed);
            TestBase.AssertEqual(120, t[3], "tier-4 threshold default is 120", ref run, ref passed, ref failed);
        }

        private static void BuildSummaryContainsBaseClassesAndThresholds(ref int run, ref int passed, ref int failed)
        {
            var defaults = new ClassPresentationConfig().EnsureNormalized();
            string s = CharacterProgression.BuildClassSystemSettingsSummary(defaults);
            TestBase.AssertTrue(s.Contains("Barbarian"), "summary names Barbarian", ref run, ref passed, ref failed);
            TestBase.AssertTrue(s.Contains("Warrior"), "summary names Warrior", ref run, ref passed, ref failed);
            TestBase.AssertTrue(s.Contains("Rogue"), "summary names Rogue", ref run, ref passed, ref failed);
            TestBase.AssertTrue(s.Contains("Wizard"), "summary names Wizard", ref run, ref passed, ref failed);
            TestBase.AssertTrue(s.Contains("weapon-path hybrid", StringComparison.OrdinalIgnoreCase), "summary explains weapon hybrids", ref run, ref passed, ref failed);
            TestBase.AssertTrue(s.Contains("attributes", StringComparison.OrdinalIgnoreCase), "summary mentions attribute display", ref run, ref passed, ref failed);
            TestBase.AssertTrue(s.Contains("2, 20, 60, 120"), "summary lists four tier thresholds", ref run, ref passed, ref failed);
        }

        private static void HybridTitleSameTierBandIsPrimaryOnly(ref int run, ref int passed, ref int failed)
        {
            var backup = SnapshotPresentation(GameConfiguration.Instance.ClassPresentation);
            try
            {
                GameConfiguration.Instance.ClassPresentation = new ClassPresentationConfig().EnsureNormalized();
                var p = new CharacterProgression { WizardPoints = 5, WarriorPoints = 3 };
                string title = p.GetWeaponPointsClassTitle();
                TestBase.AssertTrue(!title.Contains("Warrior"), "same tier band: secondary not appended", ref run, ref passed, ref failed);
                TestBase.AssertTrue(title.Contains("Wizard"), "primary wand path in title", ref run, ref passed, ref failed);
            }
            finally
            {
                GameConfiguration.Instance.ClassPresentation = backup;
            }
        }

        private static void HybridTitleDifferentTierBandsCombines(ref int run, ref int passed, ref int failed)
        {
            var backup = SnapshotPresentation(GameConfiguration.Instance.ClassPresentation);
            try
            {
                GameConfiguration.Instance.ClassPresentation = new ClassPresentationConfig().EnsureNormalized();
                var p = new CharacterProgression { WizardPoints = 25, WarriorPoints = 3 };
                string title = p.GetWeaponPointsClassTitle();
                TestBase.AssertTrue(title.Contains("-"), "different bands: default hybrid joiner", ref run, ref passed, ref failed);
                TestBase.AssertTrue(title.Contains("Wizard") && title.Contains("Warrior"), "hybrid names both paths", ref run, ref passed, ref failed);
            }
            finally
            {
                GameConfiguration.Instance.ClassPresentation = backup;
            }
        }

        private static void RenamedWandDisplayStillWizardPrimary(ref int run, ref int passed, ref int failed)
        {
            var backup = SnapshotPresentation(GameConfiguration.Instance.ClassPresentation);
            try
            {
                var pres = new ClassPresentationConfig().EnsureNormalized();
                pres.WandClassDisplayName = "Archmage";
                GameConfiguration.Instance.ClassPresentation = pres;
                var p = new CharacterProgression { WizardPoints = 10, WarriorPoints = 0 };
                string title = p.GetWeaponPointsClassTitle();
                TestBase.AssertTrue(title.Contains("Archmage"), "display uses configured wand name", ref run, ref passed, ref failed);
                TestBase.AssertTrue(!title.Contains("Wizard"), "legacy Wizard label not in title", ref run, ref passed, ref failed);
                TestBase.AssertTrue(p.IsWizardClass(WeaponType.Wand), "IsWizardClass uses path not display string", ref run, ref passed, ref failed);
                TestBase.AssertTrue(!p.IsWizardClass(WeaponType.Sword), "non-wand path rejected", ref run, ref passed, ref failed);
            }
            finally
            {
                GameConfiguration.Instance.ClassPresentation = backup;
            }
        }

        private static void DuoHybridRuleOverridesJoiner(ref int run, ref int passed, ref int failed)
        {
            var backup = SnapshotPresentation(GameConfiguration.Instance.ClassPresentation);
            try
            {
                var pres = new ClassPresentationConfig().EnsureNormalized();
                pres.HybridDuoTierRules = new System.Collections.Generic.List<HybridDuoTierRule>
                {
                    new HybridDuoTierRule
                    {
                        PrimaryPath = "Wand",
                        SecondaryPath = "Sword",
                        PrimaryTierBand = 2,
                        SecondaryTierBand = 1,
                        Titles = new[] { "SpellbladeCustom" }
                    }
                };
                GameConfiguration.Instance.ClassPresentation = pres;
                var p = new CharacterProgression { WizardPoints = 25, WarriorPoints = 3 };
                string title = p.GetWeaponPointsClassTitle();
                TestBase.AssertEqual("SpellbladeCustom", title, "duo tier rule replaces composed hybrid", ref run, ref passed, ref failed);
            }
            finally
            {
                GameConfiguration.Instance.ClassPresentation = backup;
            }
        }

        private static ClassPresentationConfig SnapshotPresentation(ClassPresentationConfig c)
        {
            c = c.EnsureNormalized();
            return new ClassPresentationConfig
            {
                DefaultNoPointsClassName = c.DefaultNoPointsClassName,
                PreTierLabel = c.PreTierLabel,
                HybridJoiner = c.HybridJoiner,
                MaceClassDisplayName = c.MaceClassDisplayName,
                SwordClassDisplayName = c.SwordClassDisplayName,
                DaggerClassDisplayName = c.DaggerClassDisplayName,
                WandClassDisplayName = c.WandClassDisplayName,
                TierThresholds = (int[])c.TierThresholds.Clone(),
                TierNames = (string[])c.TierNames.Clone(),
                MaceTierNames = c.MaceTierNames == null ? null : (string[])c.MaceTierNames.Clone(),
                SwordTierNames = c.SwordTierNames == null ? null : (string[])c.SwordTierNames.Clone(),
                DaggerTierNames = c.DaggerTierNames == null ? null : (string[])c.DaggerTierNames.Clone(),
                WandTierNames = c.WandTierNames == null ? null : (string[])c.WandTierNames.Clone(),
                HybridDuoTierRules = c.HybridDuoTierRules.Select(r => new HybridDuoTierRule
                {
                    PrimaryPath = r.PrimaryPath,
                    SecondaryPath = r.SecondaryPath,
                    PrimaryTierBand = r.PrimaryTierBand,
                    SecondaryTierBand = r.SecondaryTierBand,
                    Titles = r.Titles == null ? System.Array.Empty<string>() : (string[])r.Titles.Clone()
                }).ToList(),
                HybridTrioRules = c.HybridTrioRules.Select(r => new HybridPathComboRule
                {
                    Paths = r.Paths == null ? System.Array.Empty<string>() : (string[])r.Paths.Clone(),
                    Titles = r.Titles == null ? System.Array.Empty<string>() : (string[])r.Titles.Clone()
                }).ToList(),
                QuadHybridTitles = c.QuadHybridTitles == null ? null : (string[])c.QuadHybridTitles.Clone(),
                MeaningfulAttributeMinimum = c.MeaningfulAttributeMinimum,
                AttributeSoloTrioTierPrefixes = (string[])c.AttributeSoloTrioTierPrefixes.Clone(),
                AttributeQuadTierNames = (string[])c.AttributeQuadTierNames.Clone(),
                AttributeModifierMace = c.AttributeModifierMace,
                AttributeModifierSword = c.AttributeModifierSword,
                AttributeModifierDagger = c.AttributeModifierDagger,
                AttributeModifierWand = c.AttributeModifierWand,
                AttributeDuoMaceSword = c.AttributeDuoMaceSword,
                AttributeDuoMaceDagger = c.AttributeDuoMaceDagger,
                AttributeDuoMaceWand = c.AttributeDuoMaceWand,
                AttributeDuoSwordDagger = c.AttributeDuoSwordDagger,
                AttributeDuoSwordWand = c.AttributeDuoSwordWand,
                AttributeDuoDaggerWand = c.AttributeDuoDaggerWand,
                AttributeTrioMaceSwordDagger = c.AttributeTrioMaceSwordDagger,
                AttributeTrioMaceSwordWand = c.AttributeTrioMaceSwordWand,
                AttributeTrioMaceDaggerWand = c.AttributeTrioMaceDaggerWand,
                AttributeTrioSwordDaggerWand = c.AttributeTrioSwordDaggerWand
            };
        }
    }
}
