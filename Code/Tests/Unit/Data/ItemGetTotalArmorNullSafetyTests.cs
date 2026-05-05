using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>Regression: lab / JSON may yield null <see cref="Modification.Effect"/> or null collections.</summary>
    public static class ItemGetTotalArmorNullSafetyTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== ItemGetTotalArmor null-safety Tests ===\n");
            _run = _pass = _fail = 0;

            GetTotalArmor_DoesNotThrow_WhenModificationEffectNull();
            GetTotalArmor_DoesNotThrow_WhenStatBonusesNull();
            GetTotalArmor_Skips_NullStatBonusEntry();

            TestBase.PrintSummary("ItemGetTotalArmor null-safety Tests", _run, _pass, _fail);
        }

        private static void GetTotalArmor_DoesNotThrow_WhenModificationEffectNull()
        {
            TestBase.SetCurrentTestName(nameof(GetTotalArmor_DoesNotThrow_WhenModificationEffectNull));
            var h = new HeadItem("helm", 1, 4)
            {
                Modifications = new List<Modification>
                {
                    new Modification { Effect = null!, RolledValue = 99 },
                    new Modification { Effect = "armorBonus", RolledValue = 2 }
                }
            };

            int total = 0;
            try
            {
                total = h.GetTotalArmor();
            }
            catch (System.NullReferenceException)
            {
                TestBase.AssertTrue(false, "GetTotalArmor should not throw on null Effect", ref _run, ref _pass, ref _fail);
                return;
            }

            TestBase.AssertTrue(total >= 4, "returns at least base armor", ref _run, ref _pass, ref _fail);
        }

        private static void GetTotalArmor_DoesNotThrow_WhenStatBonusesNull()
        {
            TestBase.SetCurrentTestName(nameof(GetTotalArmor_DoesNotThrow_WhenStatBonusesNull));
            var c = new ChestItem("vest", 2, 6) { StatBonuses = null! };
            try
            {
                int t = c.GetTotalArmor();
                TestBase.AssertTrue(t >= 6, "base armor preserved", ref _run, ref _pass, ref _fail);
            }
            catch (System.NullReferenceException)
            {
                TestBase.AssertTrue(false, "GetTotalArmor should tolerate null StatBonuses", ref _run, ref _pass, ref _fail);
            }
        }

        private static void GetTotalArmor_Skips_NullStatBonusEntry()
        {
            TestBase.SetCurrentTestName(nameof(GetTotalArmor_Skips_NullStatBonusEntry));
            var f = new FeetItem("boots", 1, 2)
            {
                StatBonuses = new List<StatBonus> { null!, new StatBonus { StatType = "Armor", Value = 3 } }
            };
            try
            {
                int t = f.GetTotalArmor();
                TestBase.AssertTrue(t >= 5, "includes +3 armor from non-null bonus", ref _run, ref _pass, ref _fail);
            }
            catch (System.NullReferenceException)
            {
                TestBase.AssertTrue(false, "GetTotalArmor should skip null list entries", ref _run, ref _pass, ref _fail);
            }
        }
    }
}
