using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>Tests for <see cref="ChainPositionBonusApplier"/> and mapper wiring.</summary>
    public static class ChainPositionBonusTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;
            Console.WriteLine("\n=== ChainPositionBonus Tests ===\n");
            TestRollBonusDelta(ref run, ref passed, ref failed);
            TestEmptyPositionBasisUsesOneBasedSlot(ref run, ref passed, ref failed);
            TestLegacyRollModifiesParamRoll(ref run, ref passed, ref failed);
            TestActionAttackBonusItemRollTypeNormalizesToAccuracy(ref run, ref passed, ref failed);
            TestDisplayNameForModifiesParam(ref run, ref passed, ref failed);
            TestDamageMultiplierAdjust(ref run, ref passed, ref failed);
            TestMultiHitDelta(ref run, ref passed, ref failed);
            TestMapperCopiesList(ref run, ref passed, ref failed);
            TestBase.PrintSummary("ChainPositionBonus Tests", run, passed, failed);
        }

        private static void TestRollBonusDelta(ref int run, ref int passed, ref int failed)
        {
            run++;
            var hero = TestDataBuilders.Character().WithName("ChainPosHero").Build();
            var a1 = new Action { Name = "A1", IsComboAction = true };
            var a2 = new Action { Name = "A2", IsComboAction = true };
            var combo = new List<Action> { a1, a2 };
            var action = new Action
            {
                Name = "Strike",
                IsComboAction = true,
                ComboRouting = new ComboRoutingProperties
                {
                    ModifyBasedOnChainPosition = "true",
                    ChainPositionBonuses = new List<ChainPositionBonusEntry>
                    {
                        new ChainPositionBonusEntry { ModifiesParam = "Accuracy", Value = 2, ValueKind = "#", PositionBasis = "ComboSlotIndex0" }
                    }
                }
            };
            int delta = ChainPositionBonusApplier.GetChainAccuracyDelta(hero, action, combo, 1);
            TestBase.AssertEqual(2, delta, "Accuracy 2 * slot index 1", ref run, ref passed, ref failed);
        }

        private static void TestEmptyPositionBasisUsesOneBasedSlot(ref int run, ref int passed, ref int failed)
        {
            run++;
            var hero = TestDataBuilders.Character().WithName("EmptyBasis").Build();
            var combo = new List<Action>
            {
                new Action { Name = "A1", IsComboAction = true },
                new Action { Name = "A2", IsComboAction = true }
            };
            var action = new Action
            {
                Name = "Slotted",
                IsComboAction = true,
                ComboRouting = new ComboRoutingProperties
                {
                    ModifyBasedOnChainPosition = "true",
                    ChainPositionBonuses = new List<ChainPositionBonusEntry>
                    {
                        new ChainPositionBonusEntry { ModifiesParam = "Accuracy", Value = 3, ValueKind = "#", PositionBasis = "" }
                    }
                }
            };
            int step0 = ChainPositionBonusApplier.GetChainAccuracyDelta(hero, action, combo, 0);
            TestBase.AssertEqual(3, step0, "empty basis: (0%2)+1 = 1 → 3*1", ref run, ref passed, ref failed);
            int step1 = ChainPositionBonusApplier.GetChainAccuracyDelta(hero, action, combo, 1);
            TestBase.AssertEqual(6, step1, "empty basis: (1%2)+1 = 2 → 3*2", ref run, ref passed, ref failed);

            var actionIdx0 = new Action
            {
                Name = "ZeroBasis",
                IsComboAction = true,
                ComboRouting = new ComboRoutingProperties
                {
                    ModifyBasedOnChainPosition = "true",
                    ChainPositionBonuses = new List<ChainPositionBonusEntry>
                    {
                        new ChainPositionBonusEntry { ModifiesParam = "Accuracy", Value = 3, ValueKind = "#", PositionBasis = "ComboSlotIndex0" }
                    }
                }
            };
            run++;
            int z0 = ChainPositionBonusApplier.GetChainAccuracyDelta(hero, actionIdx0, combo, 0);
            TestBase.AssertEqual(0, z0, "ComboSlotIndex0 step 0 uses factor 0", ref run, ref passed, ref failed);
        }

        private static void TestLegacyRollModifiesParamRoll(ref int run, ref int passed, ref int failed)
        {
            var hero = TestDataBuilders.Character().WithName("ChainRollAlias").Build();
            var a1 = new Action { Name = "A1", IsComboAction = true };
            var a2 = new Action { Name = "A2", IsComboAction = true };
            var combo = new List<Action> { a1, a2 };
            var action = new Action
            {
                Name = "Strike",
                IsComboAction = true,
                ComboRouting = new ComboRoutingProperties
                {
                    ModifyBasedOnChainPosition = "true",
                    ChainPositionBonuses = new List<ChainPositionBonusEntry>
                    {
                        new ChainPositionBonusEntry { ModifiesParam = "ROLL", Value = 2, ValueKind = "#", PositionBasis = "ComboSlotIndex0" }
                    }
                }
            };
            int delta = ChainPositionBonusApplier.GetChainAccuracyDelta(hero, action, combo, 1);
            TestBase.AssertEqual(2, delta, "ROLL alias same as Accuracy for chain position", ref run, ref passed, ref failed);
        }

        private static void TestActionAttackBonusItemRollTypeNormalizesToAccuracy(ref int run, ref int passed, ref int failed)
        {
            var item = new ActionAttackBonusItem { Type = "ROLL", Value = 4 };
            TestBase.AssertEqual("ACCURACY", item.Type, "bonus Type ROLL normalizes to ACCURACY", ref run, ref passed, ref failed);
            TestBase.AssertEqual("ACCURACY", ActionAttackBonusItem.NormalizeBonusType("roll"), "NormalizeBonusType is case-insensitive", ref run, ref passed, ref failed);
        }

        private static void TestDisplayNameForModifiesParam(ref int run, ref int passed, ref int failed)
        {
            run++;
            TestBase.AssertEqual("Accuracy", ChainPositionBonusApplier.GetDisplayNameForModifiesParam("RollBonus"), "legacy RollBonus → Accuracy label", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Accuracy", ChainPositionBonusApplier.GetDisplayNameForModifiesParam("ROLL"), "ROLL → Accuracy label", ref run, ref passed, ref failed);
            TestBase.AssertEqual("EnemyAccuracy", ChainPositionBonusApplier.GetDisplayNameForModifiesParam("EnemyRollBonus"), "legacy EnemyRollBonus → EnemyAccuracy label", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Damage", ChainPositionBonusApplier.GetDisplayNameForModifiesParam("Damage"), "unchanged", ref run, ref passed, ref failed);
        }

        private static void TestDamageMultiplierAdjust(ref int run, ref int passed, ref int failed)
        {
            run++;
            var hero = TestDataBuilders.Character().WithName("ChainDmgHero").Build();
            var combo = new List<Action>
            {
                new Action { Name = "A1", IsComboAction = true },
                new Action { Name = "A2", IsComboAction = true }
            };
            var action = new Action
            {
                Name = "Finisher",
                IsComboAction = true,
                ComboRouting = new ComboRoutingProperties
                {
                    ModifyBasedOnChainPosition = "true",
                    ChainPositionBonuses = new List<ChainPositionBonusEntry>
                    {
                        new ChainPositionBonusEntry { ModifiesParam = "Damage", Value = 0.5, ValueKind = "#", PositionBasis = "ComboSlotIndex0" }
                    }
                }
            };
            hero.ComboStep = 1;
            double adjusted = ChainPositionBonusApplier.AdjustComboDamageMultiplier(2.0, hero, action, combo, hero.ComboStep);
            TestBase.AssertTrue(System.Math.Abs(adjusted - 2.5) < 1e-9, $"expected 2.5 combo mult (2 + 0.5*1), got {adjusted}", ref run, ref passed, ref failed);
        }

        private static void TestMultiHitDelta(ref int run, ref int passed, ref int failed)
        {
            run++;
            var hero = TestDataBuilders.Character().WithName("ChainMultiHero").Build();
            var combo = new List<Action>
            {
                new Action { Name = "A1", IsComboAction = true },
                new Action { Name = "A2", IsComboAction = true },
                new Action { Name = "A3", IsComboAction = true }
            };
            var action = new Action
            {
                Name = "Flurry",
                IsComboAction = true,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 2 },
                ComboRouting = new ComboRoutingProperties
                {
                    ModifyBasedOnChainPosition = "1",
                    ChainPositionBonuses = new List<ChainPositionBonusEntry>
                    {
                        new ChainPositionBonusEntry { ModifiesParam = "MultiHit", Value = 1, ValueKind = "#", PositionBasis = "ComboSlotIndex0" }
                    }
                }
            };
            hero.ComboStep = 2;
            int d = ChainPositionBonusApplier.GetMultiHitDelta(hero, action, combo, hero.ComboStep);
            TestBase.AssertEqual(2, d, "1 * (2 % 3) = 2 extra hits", ref run, ref passed, ref failed);
        }

        private static void TestMapperCopiesList(ref int run, ref int passed, ref int failed)
        {
            run++;
            var data = new ActionData
            {
                Name = "Mapped",
                Type = "Attack",
                TargetType = "SingleTarget",
                ModifyBasedOnChainPosition = "true",
                ChainPositionBonuses = new List<ChainPositionBonusEntry>
                {
                    new ChainPositionBonusEntry { ModifiesParam = "Accuracy", Value = 5, ValueKind = "#" }
                }
            };
            data.NormalizeChainPositionBonuses();
            var action = ActionDataToActionMapper.CreateAction(data);
            TestBase.AssertTrue(action.ComboRouting.ChainPositionBonuses.Count == 1, "mapper copies chain list", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Accuracy", action.ComboRouting.ChainPositionBonuses[0].ModifiesParam, "param", ref run, ref passed, ref failed);
        }
    }
}
