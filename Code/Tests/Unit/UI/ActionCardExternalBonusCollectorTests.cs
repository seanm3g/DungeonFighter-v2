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
            Console.WriteLine("=== ActionCardExternalBonusCollector Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            EnsureTreesLoaded();
            MaterialBuildsLoader.Reload();

            TestUnaffectedActionHasNoLines(ref run, ref passed, ref failed);
            TestConvertScaleLineAndDamagePreview(ref run, ref passed, ref failed);
            TestFirstBronzeAgeLineOnAffectedAction(ref run, ref passed, ref failed);
            TestWhileEquippedTagAmpOnlyOnMatchingAction(ref run, ref passed, ref failed);

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

        private static void TestFirstBronzeAgeLineOnAffectedAction(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestFirstBronzeAgeLineOnAffectedAction));
            var hero = MakeBareHero("AgeHero");
            hero.Progression.BarbarianPoints = 50;
            hero.Progression.EnsureSkillTreeRootsGranted();
            hero.Progression.LearnedSkillRanks["b-age1"] = 1;

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

            var lines = ActionCardExternalBonusCollector.BuildLines(hero, slam, 0);
            TestBase.AssertTrue(lines.Any(l =>
                    l.Text.IndexOf("First Bronze Age", StringComparison.OrdinalIgnoreCase) >= 0
                    && l.Text.Contains("+8%", StringComparison.Ordinal)
                    && l.Text.IndexOf("dmg", StringComparison.OrdinalIgnoreCase) >= 0),
                "First Bronze Age +8% dmg on affected action card",
                ref run, ref passed, ref failed);

            var panels = CombatActionStripBuilder.BuildPanelData(hero);
            TestBase.AssertTrue(panels.Count >= 1 && Math.Abs(panels[0].DamageModified - 108.0) < 0.01,
                "panel damage % includes First Bronze Age",
                ref run, ref passed, ref failed);

            var tooltip = CombatActionStripBuilder.BuildActionTooltipLines(hero, 0, 80, 20);
            TestBase.AssertTrue(tooltip.Any(l =>
                    l.IndexOf("First Bronze Age", StringComparison.OrdinalIgnoreCase) >= 0),
                "action tooltip Stats include First Bronze Age",
                ref run, ref passed, ref failed);
        }

        private static void TestWhileEquippedTagAmpOnlyOnMatchingAction(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestWhileEquippedTagAmpOnlyOnMatchingAction));
            var hero = MakeBareHero("TagAmp");
            var weapon = TestDataBuilders.Weapon().WithName("ChorusMace").WithWeaponType(WeaponType.Mace).Build();
            weapon.EquipEffects = new List<ActionTriggerBundle>
            {
                new ActionTriggerBundle
                {
                    When = "WHILE_EQUIPPED",
                    Count = "1",
                    Mechanics = "hero_action_damage",
                    Value = 12,
                    IdentityName = "BludgeonChorus",
                    Filters = new List<string> { "IFACTIONHASTAG:bludgeon" }
                }
            };
            hero.Equipment.Weapon = weapon;

            var bludgeon = TestDataBuilders.CreateMockAction("BLUDGEON");
            bludgeon.IsComboAction = true;
            bludgeon.Tags = new List<string> { "bludgeon" };
            var strike = TestDataBuilders.CreateMockAction("STRIKE");
            strike.IsComboAction = true;
            strike.Tags = new List<string> { "weapon" };

            var matched = ActionCardExternalBonusCollector.BuildLines(hero, bludgeon, 0);
            var unmatched = ActionCardExternalBonusCollector.BuildLines(hero, strike, 0);

            TestBase.AssertTrue(matched.Any(l =>
                    l.Text.IndexOf("BludgeonChorus", StringComparison.OrdinalIgnoreCase) >= 0
                    && l.Text.Contains("+12%", StringComparison.Ordinal)),
                "WHILE_EQUIPPED tag amp listed on matching action",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, unmatched.Count,
                "WHILE_EQUIPPED tag amp omitted on untagged action",
                ref run, ref passed, ref failed);
        }
    }
}
