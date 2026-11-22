using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>Action roles (D5.1) and class hits applying the class status (Rebuild step 3.1).</summary>
    public static class ActionRolesTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionRoles Tests ===\n");
            _run = _passed = _failed = 0;

            var all = ActionLoader.GetAllActionData();
            var missing = all.Where(a => !ActionRoles.IsRole(a.Role)).Select(a => a.Name).ToList();
            TestBase.AssertTrue(all.Count > 100 && missing.Count == 0,
                $"every hero action has a role ({all.Count} actions; missing: {string.Join(", ", missing.Take(8))})", ref _run, ref _passed, ref _failed);
            foreach (string role in ActionRoles.All)
                TestBase.AssertTrue(all.Any(a => ActionRoles.Normalize(a.Role) == role), $"some action is a {role}", ref _run, ref _passed, ref _failed);

            foreach (var (name, status) in new[] { ("SLAM", "stagger"), ("STRIKE", "harden"), ("STAB", "bleed"), ("MAGIC MISSILE", "burn") })
            {
                var action = ActionLoader.GetAction(name);
                var rule = action == null ? null : RPGGame.Combat.Rules.ActionRules.FromVerb(action);
                TestBase.AssertTrue(rule != null && rule.Store == status && ActionRoles.Of(action) == ActionRoles.Payoff,
                    $"{name} is a payoff that applies its class status ({status})", ref _run, ref _passed, ref _failed);
            }

            var stab = ActionLoader.GetAction("STAB");
            TestBase.AssertEqual("Payoff · applies 1 Bleed", ActionRoles.CardLine(stab, ""), "STAB's card line", ref _run, ref _passed, ref _failed);
            var strike = ActionLoader.GetAction("STRIKE");
            TestBase.AssertEqual("Payoff · gains 1 Guard", ActionRoles.CardLine(strike, ""), "STRIKE's card line", ref _run, ref _passed, ref _failed);
            var windUp = ActionLoader.GetAction("WIND UP");
            TestBase.AssertTrue(ActionRoles.CardLine(windUp, "Next action: +25% damage.").StartsWith("Setup · ", StringComparison.Ordinal),
                "WIND UP reads as a setup", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(ActionRoles.Sequence, ActionRoles.Infer("", "STRIP JUMP", null, ""), "strip cards infer as sequence", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(ActionRoles.Payoff, ActionRoles.Infer("CASH FULLEST 75", "X", null, "Next action"), "cash-ins infer as payoff", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("ActionRoles Tests", _run, _passed, _failed);
        }
    }
}
