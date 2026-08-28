using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Equipped material counts, 2/3/5 mint, Gold↔Mithril feed, convert grant and damage scale.</summary>
    public static class MaterialSetControllerTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Material Set Controller Tests ===\n");
            _run = _passed = _failed = 0;

            MaterialBuildsLoader.Reload();

            TestMintAmountThresholds();
            TestCountAndConvertGrantAtTwoStack();
            TestTwoStackMintsOne();
            TestThreeStackAddsFeedCount();
            TestFiveStackMultipliesFeed();
            TestGoldCrossFeedsMithril();
            TestClasslessMaterialHasNoBuild();
            TestConvertDamageScaleFromBankAndStacks();
            TestDsDtDuOverrides();
            TestMintWritesFeedCombatLine();
            TestFormatWhenLabel();
            TestKeywordBankSurvivesCombatInitAndClearsOnDungeonEnd();

            TestBase.PrintSummary("Material Set Controller Tests", _run, _passed, _failed);
        }

        private static Character EquipPieces(params (Item item, string material)[] pieces)
        {
            var hero = new Character("Hero", 1);
            foreach (var (item, material) in pieces)
            {
                item.Material = material;
                switch (item.Type)
                {
                    case ItemType.Head: hero.Equipment.Head = item; break;
                    case ItemType.Chest: hero.Equipment.Body = item; break;
                    case ItemType.Legs: hero.Equipment.Legs = item; break;
                    case ItemType.Feet: hero.Equipment.Feet = item; break;
                    case ItemType.Weapon: hero.Equipment.Weapon = item; break;
                }
            }
            return hero;
        }

        private static Character EquipIron(int count)
        {
            var slots = new Item[]
            {
                new HeadItem("Helm", 1, 1),
                new ChestItem("Mail", 1, 1),
                new LegsItem("Greaves", 1, 1),
                new FeetItem("Boots", 1, 1),
                new WeaponItem("Mace", 1, 5, 1.0, WeaponType.Mace)
            };
            var pairs = new (Item, string)[count];
            for (int i = 0; i < count; i++)
                pairs[i] = (slots[i], "Iron");
            return EquipPieces(pairs);
        }

        private static void TestMintAmountThresholds()
        {
            TestBase.SetCurrentTestName(nameof(TestMintAmountThresholds));
            TestBase.AssertEqual(0, MaterialBuildData.ComputeMintAmount(1, 4), "below 2 → 0", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, MaterialBuildData.ComputeMintAmount(2, 0), "2-stack +1", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3, MaterialBuildData.ComputeMintAmount(3, 3), "3-stack +feed", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(25, MaterialBuildData.ComputeMintAmount(5, 5), "5-stack ×feed", ref _run, ref _passed, ref _failed);
        }

        private static void TestCountAndConvertGrantAtTwoStack()
        {
            TestBase.SetCurrentTestName(nameof(TestCountAndConvertGrantAtTwoStack));
            var one = EquipIron(1);
            TestBase.AssertEqual(1, MaterialSetController.CountEquipped(one, "Iron"), "1 iron", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!MaterialSetController.GetGrantedConvertActionNames(one).Any(n =>
                    n.Equals("IRON CULL", StringComparison.OrdinalIgnoreCase)),
                "1 piece does not grant convert", ref _run, ref _passed, ref _failed);

            var two = EquipIron(2);
            TestBase.AssertEqual(2, MaterialSetController.CountEquipped(two, "Iron"), "2 iron", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(MaterialSetController.GetGrantedConvertActionNames(two)
                    .Any(n => n.Equals("IRON CULL", StringComparison.OrdinalIgnoreCase)),
                "2 pieces grant IRON CULL", ref _run, ref _passed, ref _failed);
        }

        private static void TestTwoStackMintsOne()
        {
            TestBase.SetCurrentTestName(nameof(TestTwoStackMintsOne));
            var hero = EquipIron(2);
            var evt = new CombatEvent(CombatEventType.EnemyHealthThreshold, hero) { Target = hero, HealthPercentage = 0.2 };
            MaterialSetController.TryMintFromEvent(hero, evt);
            TestBase.AssertEqual(1, hero.Effects.GetMaterialKeyword("CRISIS"), "2-stack mints +1 CRISIS", ref _run, ref _passed, ref _failed);
        }

        private static void TestThreeStackAddsFeedCount()
        {
            TestBase.SetCurrentTestName(nameof(TestThreeStackAddsFeedCount));
            var hero = EquipIron(3);
            var evt = new CombatEvent(CombatEventType.EnemyHealthThreshold, hero) { Target = hero };
            MaterialSetController.TryMintFromEvent(hero, evt);
            TestBase.AssertEqual(3, hero.Effects.GetMaterialKeyword("CRISIS"), "3-stack mints +Iron count", ref _run, ref _passed, ref _failed);
        }

        private static void TestFiveStackMultipliesFeed()
        {
            TestBase.SetCurrentTestName(nameof(TestFiveStackMultipliesFeed));
            var hero = EquipIron(5);
            var evt = new CombatEvent(CombatEventType.EnemyHealthThreshold, hero) { Target = hero };
            MaterialSetController.TryMintFromEvent(hero, evt);
            TestBase.AssertEqual(25, hero.Effects.GetMaterialKeyword("CRISIS"), "5-stack mints ×Iron count", ref _run, ref _passed, ref _failed);
        }

        private static void TestGoldCrossFeedsMithril()
        {
            TestBase.SetCurrentTestName(nameof(TestGoldCrossFeedsMithril));
            var hero = EquipPieces(
                (new HeadItem("G1", 1, 1), "Gold"),
                (new ChestItem("G2", 1, 1), "Gold"),
                (new LegsItem("G3", 1, 1), "Gold"),
                (new FeetItem("M1", 1, 1), "Mithril"),
                (new WeaponItem("M2", 1, 5, 1.0, WeaponType.Sword), "Mithril"));
            var swing = new Action { Name = "Anchor", Type = ActionType.Attack, CausesSlow = true };
            var hit = new CombatEvent(CombatEventType.ActionHit, hero) { Action = swing };
            MaterialSetController.TryMintFromEvent(hero, hit, swing);
            TestBase.AssertEqual(2, hero.Effects.GetMaterialKeyword("DRAG"), "Gold 3+ feeds Mithril count", ref _run, ref _passed, ref _failed);
        }

        private static void TestClasslessMaterialHasNoBuild()
        {
            TestBase.SetCurrentTestName(nameof(TestClasslessMaterialHasNoBuild));
            var hero = EquipPieces(
                (new HeadItem("Cap", 1, 1), "Leather"),
                (new ChestItem("Vest", 1, 1), "Leather"));
            TestBase.AssertEqual(0, MaterialSetController.GetGrantedConvertActionNames(hero).Count,
                "Leather has no convert", ref _run, ref _passed, ref _failed);
            var lines = MaterialSetController.FormatSetStatusLines(hero, hero.Equipment.Head).ToList();
            TestBase.AssertTrue(lines.Count > 0 && lines[0].Contains("no material build", StringComparison.OrdinalIgnoreCase),
                "class-less tooltip", ref _run, ref _passed, ref _failed);
        }

        private static void TestConvertDamageScaleFromBankAndStacks()
        {
            TestBase.SetCurrentTestName(nameof(TestConvertDamageScaleFromBankAndStacks));
            var two = EquipIron(2);
            two.Effects.AddMaterialKeyword("CRISIS", 4);
            var cull = new Action { Name = "IRON CULL", DamageMultiplier = 1.0 };
            TestBase.AssertEqual(4.0, MaterialSetController.GetConvertDamageMultiplier(two, cull),
                "2-stack uses bank", ref _run, ref _passed, ref _failed);

            var three = EquipIron(3);
            three.Effects.AddMaterialKeyword("CRISIS", 4);
            TestBase.AssertEqual(7.0, MaterialSetController.GetConvertDamageMultiplier(three, cull),
                "3-stack bank+feed", ref _run, ref _passed, ref _failed);

            var five = EquipIron(5);
            five.Effects.AddMaterialKeyword("CRISIS", 4);
            TestBase.AssertEqual(45.0, MaterialSetController.GetConvertDamageMultiplier(five, cull),
                "5-stack (bank+feed)×feed", ref _run, ref _passed, ref _failed);
        }

        private static void TestDsDtDuOverrides()
        {
            TestBase.SetCurrentTestName(nameof(TestDsDtDuOverrides));
            var hero = EquipIron(3);
            hero.Effects.AddMaterialKeyword("CRISIS", 4);
            var keywordOnly = new Action
            {
                Name = "CUSTOM",
                MaterialScale = "Iron",
                KeywordScale = "CRISIS",
                ScaleFormula = "keyword"
            };
            TestBase.AssertEqual(4.0, MaterialSetController.GetConvertDamageMultiplier(hero, keywordOnly),
                "formula keyword uses bank", ref _run, ref _passed, ref _failed);

            var materialOnly = new Action
            {
                Name = "CUSTOM2",
                MaterialScale = "Iron",
                KeywordScale = "CRISIS",
                ScaleFormula = "material"
            };
            TestBase.AssertEqual(3.0, MaterialSetController.GetConvertDamageMultiplier(hero, materialOnly),
                "formula material uses equipped count", ref _run, ref _passed, ref _failed);
        }

        private static void TestMintWritesFeedCombatLine()
        {
            TestBase.SetCurrentTestName(nameof(TestMintWritesFeedCombatLine));
            var hero = EquipIron(2);
            var messages = new List<string>();
            var evt = new CombatEvent(CombatEventType.EnemyHealthThreshold, hero) { Target = hero, HealthPercentage = 0.2 };
            MaterialSetController.TryMintFromEvent(hero, evt, action: null, messages);
            TestBase.AssertEqual(1, messages.Count, "one feed combat line", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(messages.Count > 0 && MaterialSetController.IsFeedCombatLine(messages[0]),
                "line is tagged as a feed combat line", ref _run, ref _passed, ref _failed);
            string plain = messages.Count == 0
                ? ""
                : ColoredTextRenderer.RenderAsPlainText(ColoredTextParser.Parse(messages[0]));
            TestBase.AssertTrue(plain.IndexOf("CRISIS", StringComparison.OrdinalIgnoreCase) >= 0,
                "line names CRISIS", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(plain.IndexOf("feeds", StringComparison.OrdinalIgnoreCase) >= 0,
                "line says feeds", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(plain.IndexOf("+1", StringComparison.Ordinal) >= 0,
                "line shows +1", ref _run, ref _passed, ref _failed);
        }

        private static void TestFormatWhenLabel()
        {
            TestBase.SetCurrentTestName(nameof(TestFormatWhenLabel));
            TestBase.AssertEqual("ON HIT", MaterialBuildData.FormatWhenLabel("ON_HIT = GRAZE"),
                "underscores to spaces", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("ON ENEMY HEALTH THRESHOLD",
                MaterialBuildData.FormatWhenLabel("ON_ENEMY_HEALTH_THRESHOLD = CRISIS"),
                "long WHEN", ref _run, ref _passed, ref _failed);
        }

        private static void TestKeywordBankSurvivesCombatInitAndClearsOnDungeonEnd()
        {
            TestBase.SetCurrentTestName(nameof(TestKeywordBankSurvivesCombatInitAndClearsOnDungeonEnd));
            var hero = EquipIron(2);
            var foe = TestDataBuilders.Enemy().WithName("Foe").Build();
            hero.Effects.AddMaterialKeyword("CRISIS", 4);
            hero.Effects.MaterialConsecutiveConnects = 3;

            MaterialSetController.ResetFightConnects(hero);
            TestBase.AssertEqual(4, hero.Effects.GetMaterialKeyword("CRISIS"),
                "fight-connect reset keeps keyword currency", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, hero.Effects.MaterialConsecutiveConnects,
                "fight-connect reset zeros consecutive hits", ref _run, ref _passed, ref _failed);

            var manager = new CombatStateManager();
            manager.InitializeCombatEntities(hero, foe);
            TestBase.AssertEqual(4, hero.Effects.GetMaterialKeyword("CRISIS"),
                "combat init keeps dungeon keyword currency", ref _run, ref _passed, ref _failed);

            hero.ClearEncounterTempEffects();
            TestBase.AssertEqual(4, hero.Effects.GetMaterialKeyword("CRISIS"),
                "encounter clear keeps dungeon keyword currency", ref _run, ref _passed, ref _failed);

            hero.ClearDungeonRunTempEffects();
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("CRISIS"),
                "dungeon-run clear resets keyword currency", ref _run, ref _passed, ref _failed);
        }
    }
}
