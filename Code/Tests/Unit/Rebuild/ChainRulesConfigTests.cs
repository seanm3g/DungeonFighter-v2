using System;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>
    /// Rebuild 1.6 (D2.1, D2.8–D2.12): the chain rules are already built; this pins them to the decisions.
    /// Free action on a combo roll, no HIT mid-chain, +1 per link from slot 2, chain growth g = 0.25.
    /// </summary>
    public static class ChainRulesConfigTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ChainRulesConfig Tests ===\n");
            _run = _passed = _failed = 0;

            var defaults = new ComboSystemConfig();
            TestBase.AssertTrue(defaults.ResetChainOnNormalHit, "Default: a plain HIT resets the chain (D2.1)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(defaults.FreeActionOnComboHit, "Default: a combo roll earns a free action (D2.8)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(defaults.MissBelowComboMidChain, "Default: no HIT band mid-chain (D2.8)", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, defaults.ComboThresholdRisePerSlot, "Default: +1 per link (D2.9)", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, defaults.ComboThresholdRiseStartSlot, "Default: the rise starts at slot 2", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(defaults.ComboChainGrowth - 0.25) < 1e-9, "Default: g = 0.25 (D2.12)", ref _run, ref _passed, ref _failed);

            var live = GameConfiguration.Instance.ComboSystem;
            TestBase.AssertTrue(live.FreeActionOnComboHit, "Active: free action on a combo roll", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(live.MissBelowComboMidChain, "Active: no HIT band mid-chain", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, live.ComboThresholdRisePerSlot, "Active: +1 per link", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(live.ComboChainGrowth - 0.25) < 1e-9, $"Active: g = 0.25 (is {live.ComboChainGrowth})", ref _run, ref _passed, ref _failed);
            // D2.1 (Q1): a plain HIT resets the chain even when the active patch turns the knob off.
            Console.WriteLine($"  (info) active comboSystem.resetChainOnNormalHit = {live.ResetChainOnNormalHit}");
            TestBase.AssertTrue(ComboSystemConfig.EffectiveResetChainOnNormalHit(live), "Active: a plain HIT resets the chain (D2.1 wins over the patch)", ref _run, ref _passed, ref _failed);
            var patchOff = new ComboSystemConfig { ResetChainOnNormalHit = false };
            TestBase.AssertTrue(ComboSystemConfig.EffectiveResetChainOnNormalHit(patchOff), "A patch value of false can't undo D2.1", ref _run, ref _passed, ref _failed);
            bool old = ComboSystemConfig.PlainHitAlwaysResetsChain;
            try
            {
                ComboSystemConfig.PlainHitAlwaysResetsChain = false;
                TestBase.AssertTrue(!ComboSystemConfig.EffectiveResetChainOnNormalHit(patchOff), "Switch off: the patch value decides again", ref _run, ref _passed, ref _failed);
            }
            finally { ComboSystemConfig.PlainHitAlwaysResetsChain = old; }

            TestBase.PrintSummary("ChainRulesConfig Tests", _run, _passed, _failed);
        }
    }
}
