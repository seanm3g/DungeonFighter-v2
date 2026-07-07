using System.Collections.Generic;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionCadenceBonusBankTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;
            Console.WriteLine("=== ActionCadenceBonusBank Tests ===\n");

            var bank = new List<ActionAttackBonusItem>();
            ActionCadenceBonusBank.MergeAdditively(bank, new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 25 }
            }, 2);
            TestBase.AssertTrue(bank.Count == 1 && bank[0].Value == 50,
                "stackTimes multiplies values into one merged entry", ref run, ref passed, ref failed);

            ActionCadenceBonusBank.MergeAdditively(bank, new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 10 },
                new ActionAttackBonusItem { Type = "COMBO", Value = 2 }
            });
            TestBase.AssertTrue(bank.Count == 2 && bank[0].Value == 60 && bank[1].Value == 2,
                "second deposit adds to existing type and new types", ref run, ref passed, ref failed);

            var copy = ActionCadenceBonusBank.Copy(bank);
            TestBase.AssertTrue(copy.Count == bank.Count && copy[0].Value == bank[0].Value && !ReferenceEquals(copy, bank),
                "Copy returns independent list", ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionCadenceBonusBank Tests", run, passed, failed);
        }
    }
}
