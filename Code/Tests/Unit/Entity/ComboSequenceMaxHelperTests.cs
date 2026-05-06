using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    public static class ComboSequenceMaxHelperTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboSequenceMaxHelper Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestEffectiveMaxBasePlusEquippedExtraSlots();
            TestTrimRemovesNonRequiredWhenOverCap();

            TestBase.PrintSummary("ComboSequenceMaxHelper Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestEffectiveMaxBasePlusEquippedExtraSlots()
        {
            Console.WriteLine("--- Testing GetEffectiveMax base + equipped extra slots ---");
            var cfg = GameConfiguration.Instance;
            var backupLoot = cfg.LootSystem;
            try
            {
                cfg.LootSystem = new LootSystemConfig
                {
                    ComboSequenceBaseMax = 2,
                    ComboSequenceAbsoluteMax = 6
                };

                var c = TestDataBuilders.Character().WithName("CapTest").Build();
                TestBase.AssertEqual(2, ComboSequenceMaxHelper.GetEffectiveMax(c),
                    "no gear", ref _testsRun, ref _testsPassed, ref _testsFailed);

                c.Equipment.Feet = new FeetItem("Boots", 1, 1) { ExtraActionSlots = 2 };
                TestBase.AssertEqual(4, ComboSequenceMaxHelper.GetEffectiveMax(c),
                    "base 2 + 2 feet", ref _testsRun, ref _testsPassed, ref _testsFailed);

                c.Equipment.Body = new ChestItem("Vest", 1, 1) { ExtraActionSlots = 1 };
                TestBase.AssertEqual(5, ComboSequenceMaxHelper.GetEffectiveMax(c),
                    "feet + chest catalog slots", ref _testsRun, ref _testsPassed, ref _testsFailed);

                c.Equipment.Feet = new FeetItem("Boots", 1, 1) { ExtraActionSlots = 10 };
                c.Equipment.Body = null;
                TestBase.AssertEqual(6, ComboSequenceMaxHelper.GetEffectiveMax(c),
                    "absolute cap", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.LootSystem = backupLoot;
            }
        }

        private static void TestTrimRemovesNonRequiredWhenOverCap()
        {
            Console.WriteLine("\n--- Testing TrimComboSequenceToMax ---");
            var cfg = GameConfiguration.Instance;
            var backupLoot = cfg.LootSystem;
            try
            {
                cfg.LootSystem = new LootSystemConfig { ComboSequenceBaseMax = 2, ComboSequenceAbsoluteMax = 8 };

                var c = TestDataBuilders.Character().WithName("TrimTest").Build();
                var a1 = TestDataBuilders.CreateMockAction("A1");
                a1.IsComboAction = true;
                a1.ComboOrder = 1;
                c.AddAction(a1, 1.0);
                var a2 = TestDataBuilders.CreateMockAction("A2");
                a2.IsComboAction = true;
                a2.ComboOrder = 2;
                c.AddAction(a2, 1.0);
                var a3 = TestDataBuilders.CreateMockAction("A3");
                a3.IsComboAction = true;
                a3.ComboOrder = 3;
                c.AddAction(a3, 1.0);
                c.Actions.AddToCombo(a1, null);
                c.Actions.AddToCombo(a2, null);
                c.Actions.AddToCombo(a3, null);
                TestBase.AssertEqual(3, c.GetComboActions().Count, "setup 3", ref _testsRun, ref _testsPassed, ref _testsFailed);

                ComboSequenceMaxHelper.TrimComboSequenceToMax(c, 2);
                TestBase.AssertEqual(2, c.GetComboActions().Count, "trimmed to 2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.LootSystem = backupLoot;
            }
        }
    }
}
