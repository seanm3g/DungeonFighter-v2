using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Action-card lines for standing class / material / WHILE_EQUIPPED bonuses.
    /// </summary>
    public static class ActionCardExternalBonusCollectorTests
    {
        public static void RunAllTests()
        {
            // Rules under test use fixed numbers; keep the active balance patch out of them.
            using var codeDefaults = RPGGame.Tests.TestConfigScope.CodeDefaults();
            Console.WriteLine("=== ActionCardExternalBonusCollector Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            EnsureTreesLoaded();
            MaterialBuildsLoader.Reload();

            // Card math below is written for flat converts; pin the legacy material knobs whatever patch is active.
            var combat = GameConfiguration.Instance.Combat;
            double oldPct = combat.MaterialConvertPercentPerUnit;
            int oldFive = combat.MaterialFiveStackMintMultiplier;
            int oldMaxUnits = combat.MaterialConvertMaxUnits;
            combat.MaterialConvertPercentPerUnit = 0;
            combat.MaterialFiveStackMintMultiplier = 0;
            combat.MaterialConvertMaxUnits = 0;
            try
            {
                TestUnaffectedActionHasNoLines(ref run, ref passed, ref failed);
                TestConvertScaleLineAndDamagePreview(ref run, ref passed, ref failed);
                TestLoadedIronCullStripTracksCrisis(ref run, ref passed, ref failed);
                TestFirstBronzeAgeLineOnAffectedAction(ref run, ref passed, ref failed);
            }
            finally
            {
                combat.MaterialConvertPercentPerUnit = oldPct;
                combat.MaterialFiveStackMintMultiplier = oldFive;
                combat.MaterialConvertMaxUnits = oldMaxUnits;
            }

            TestBase.PrintSummary("ActionCardExternalBonusCollector Tests", run, passed, failed);
        }

        private static void EnsureTreesLoaded()
        {
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;
        }

        private static Character MakeBareHero(string name)
        {
            var character = TestDataBuilders.Character().WithName(name).WithStats(10, 10, 10, 10).Build();
            return character;
        }

        private static void AddCombo(Character character, Action action)
        {
            character.AddAction(action, 1.0);
            character.Actions.AddToCombo(action);
        }

        private static void TestUnaffectedActionHasNoLines(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestUnaffectedActionHasNoLines));
            var hero = MakeBareHero("Plain");
            var swing = TestDataBuilders.CreateMockAction("STRIKE");
            swing.IsComboAction = true;
            AddCombo(hero, swing);

            var lines = ActionCardExternalBonusCollector.BuildLines(hero, swing, 0);
            TestBase.AssertEqual(0, lines.Count, "no class/material/gear bonus → no card lines",
                ref run, ref passed, ref failed);
        }

        private static void TestConvertScaleLineAndDamagePreview(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestConvertScaleLineAndDamagePreview));
            var hero = MakeBareHero("IronHero");
            hero.Equipment.Head = new HeadItem("Helm", 1, 1) { Material = "Iron" };
            hero.Equipment.Body = new ChestItem("Mail", 1, 1) { Material = "Iron" };
            hero.Effects.AddMaterialKeyword("CRISIS", 4);

            var cull = TestDataBuilders.CreateMockAction("IRON CULL");
            cull.IsComboAction = true;
            cull.DamageMultiplier = 1.0;
            AddCombo(hero, cull);

            var snap = ActionCardExternalBonusCollector.Collect(hero, cull, 0);
            TestBase.AssertEqual(20, snap.ConvertBonus,
                "2-stack Iron convert is +5 per CRISIS (4 → +20)",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(snap.Lines.Any(l => l.Text.IndexOf("CRISIS", StringComparison.OrdinalIgnoreCase) >= 0
                    && l.Text.Contains("+20", StringComparison.Ordinal)),
                "convert card line names CRISIS add",
                ref run, ref passed, ref failed);

            var panels = CombatActionStripBuilder.BuildPanelData(hero);
            TestBase.AssertTrue(panels.Count >= 1, "panel for convert action", ref run, ref passed, ref failed);
            CombatActionStripBuilder.GetStripSwingDisplayValues(
                panels[0], hero, cull, ActionStripDamageLineMode.EffectiveWithComboAmp,
                out int dmgWithConvert, out _, 0);
            hero.Effects.ClearMaterialKeywordBank();
            CombatActionStripBuilder.GetStripSwingDisplayValues(
                panels[0], hero, cull, ActionStripDamageLineMode.EffectiveWithComboAmp,
                out int dmgWithoutConvert, out _, 0);
            TestBase.AssertEqual(dmgWithoutConvert + 20, dmgWithConvert,
                "strip flat damage includes +20 convert add, not a 4× multiplier",
                ref run, ref passed, ref failed);
        }

        /// <summary>
        /// The shipped IRON CULL row (materialScale 5, keywordScale DAMAGE, scaleFormula CRISIS) must
        /// raise the strip's flat damage as the CRISIS bank grows, including percent-of-hit converts.
        /// </summary>
        private static void TestLoadedIronCullStripTracksCrisis(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestLoadedIronCullStripTracksCrisis));
            var combat = GameConfiguration.Instance.Combat;
            double oldPct = combat.MaterialConvertPercentPerUnit;
            var oldTable = combat.MaterialConvertPercentByKeyword;
            combat.MaterialConvertPercentPerUnit = 1.0;
            combat.MaterialConvertPercentByKeyword = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["CRISIS"] = 4.0
            };
            try
            {
                var hero = MakeBareHero("CullTrack");
                var weapon = TestDataBuilders.Weapon().WithName("IronMace").WithWeaponType(WeaponType.Mace).WithBaseDamage(40).Build();
                weapon.Material = "Iron";
                hero.Equipment.Weapon = weapon;
                hero.Equipment.Head = new HeadItem("Helm", 1, 1) { Material = "Iron" };

                var cull = ActionLoader.GetAction("IRON CULL");
                TestBase.AssertTrue(cull != null, "IRON CULL loads from Actions.json", ref run, ref passed, ref failed);
                if (cull == null)
                    return;
                cull.IsComboAction = true;
                AddCombo(hero, cull);

                var panels = CombatActionStripBuilder.BuildPanelData(hero);
                TestBase.AssertTrue(panels.Count >= 1, "strip panel for IRON CULL", ref run, ref passed, ref failed);
                CombatActionStripBuilder.GetStripSwingDisplayValues(
                    panels[0], hero, cull, ActionStripDamageLineMode.EffectiveWithComboAmp,
                    out int atZero, out _, 0);
                hero.Effects.AddMaterialKeyword("CRISIS", 5);
                CombatActionStripBuilder.GetStripSwingDisplayValues(
                    panels[0], hero, cull, ActionStripDamageLineMode.EffectiveWithComboAmp,
                    out int atFive, out _, 0);
                int bonusFive = (int)Math.Round(atZero * 5 * 4.0 / 100.0);
                TestBase.AssertTrue(bonusFive > 0, "5 CRISIS at 4% of the swing is a visible add", ref run, ref passed, ref failed);
                TestBase.AssertEqual(atZero + bonusFive, atFive,
                    "IRON CULL card damage includes 4% of the swing per CRISIS", ref run, ref passed, ref failed);

                hero.Effects.AddMaterialKeyword("CRISIS", 3);
                CombatActionStripBuilder.GetStripSwingDisplayValues(
                    panels[0], hero, cull, ActionStripDamageLineMode.EffectiveWithComboAmp,
                    out int atEight, out _, 0);
                int bonusEight = (int)Math.Round(atZero * 8 * 4.0 / 100.0);
                TestBase.AssertEqual(atZero + bonusEight, atEight,
                    "card damage keeps rising as CRISIS increases", ref run, ref passed, ref failed);
                TestBase.AssertTrue(atEight > atFive, "8 CRISIS pays more than 5", ref run, ref passed, ref failed);
            }
            finally
            {
                combat.MaterialConvertPercentPerUnit = oldPct;
                combat.MaterialConvertPercentByKeyword = oldTable;
            }
        }

        private static void TestFirstBronzeAgeLineOnAffectedAction(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestFirstBronzeAgeLineOnAffectedAction));
            var hero = MakeBareHero("AgeHero");
            hero.Progression.BarbarianPoints = 50;
            hero.Progression.EnsureSkillTreeRootsGranted();
            // Steel Knuckle (WHILE IF harden>=1 AND WEAPON mace -> DAMAGE 3%, 5 ranks): First Steel Age became a tag rule (2026-11-18).
            hero.Progression.LearnedSkillRanks["b-knuckle"] = 5;
            Stores.Add(hero, "harden", 1);

            var weapon = TestDataBuilders.Weapon()
                .WithName("BoneMace")
                .WithWeaponType(WeaponType.Mace)
                .Build();
            weapon.Material = "Bone";
            weapon.Tags.Add("bone");
            hero.Equipment.Weapon = weapon;

            var slam = TestDataBuilders.CreateMockAction("SLAM");
            slam.IsComboAction = true;
            slam.DamageMultiplier = 1.0;
            AddCombo(hero, slam);

            // 3% per rank x the node's power (SkillTrees.json).
            string ageName = GameConfiguration.Instance.SkillTrees.GetNode("b-knuckle")?.Name ?? "Steel Knuckle";
            double agePct = 3.0 * 5 * (GameConfiguration.Instance.SkillTrees.GetNode("b-knuckle")?.Power ?? 1.0);
            var lines = ActionCardExternalBonusCollector.BuildLines(hero, slam, 0);
            TestBase.AssertTrue(lines.Any(l =>
                    l.Text.IndexOf(ageName, StringComparison.OrdinalIgnoreCase) >= 0
                    && l.Text.IndexOf("dmg", StringComparison.OrdinalIgnoreCase) >= 0),
                "First Bronze Age dmg line on affected action card",
                ref run, ref passed, ref failed);

            var panels = CombatActionStripBuilder.BuildPanelData(hero);
            TestBase.AssertTrue(panels.Count >= 1 && Math.Abs(panels[0].DamageModified - (100.0 + agePct)) < 0.01,
                "panel damage % includes First Bronze Age",
                ref run, ref passed, ref failed);

            var tooltip = CombatActionStripBuilder.BuildActionTooltipLines(hero, 0, 80, 20);
            TestBase.AssertTrue(tooltip.Any(l =>
                    l.IndexOf(ageName, StringComparison.OrdinalIgnoreCase) >= 0),
                "action tooltip Stats include First Bronze Age",
                ref run, ref passed, ref failed);
        }

    }
}
