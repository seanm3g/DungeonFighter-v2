using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Rebuild step 5: the 7-status core and the retired ids that load as their replacements (D3.13-D3.16).</summary>
    public static class CoreStatusesTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CoreStatuses Tests ===\n");
            _run = _passed = _failed = 0;
            using var core = CoreStatuses.Pin(true);

            foreach (var (from, to) in new[] { ("poison", "bleed"), ("stun", "stagger"), ("slow", "stagger"), ("acid", "expose"),
                         ("vulnerability", "expose"), ("pierce", "expose"), ("rollpenalty", "rattle"), ("fortify", "harden"),
                         ("brittle", "mark"), ("bleed", "bleed"), ("RAGE", "RAGE") })
                TestBase.AssertEqual(to, CoreStatuses.Resolve(from), $"{from} loads as {to}", ref _run, ref _passed, ref _failed);
            foreach (string dropped in new[] { "silence", "statdrain", "disrupt", "hpregen", "confusion" })
                TestBase.AssertNull(CoreStatuses.Resolve(dropped), $"{dropped} is dropped", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Guard", CoreStatuses.Label("harden"), "Harden reads as Guard", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(7, CoreStatuses.Core.Length, "seven core statuses", ref _run, ref _passed, ref _failed);

            // Applying a retired status through the registry lands its replacement.
            var registry = new EffectHandlerRegistry();
            var action = new Action { Name = "TEST POKE" };
            var target = MockFactories.CreateMockEnemy("Dummy", 5, 200);
            var results = new List<string>();
            bool stun = registry.ApplyEffect("stun", target, action, results);
            TestBase.AssertTrue(stun && Stores.Get(target, "stagger") > 0, $"a Stun lands as Stagger ({Stores.Get(target, "stagger")})", ref _run, ref _passed, ref _failed);
            bool silence = registry.ApplyEffect("silence", target, action, results);
            TestBase.AssertTrue(!silence, "a dropped status applies nothing", ref _run, ref _passed, ref _failed);

            // Stores: a retired status id reads its replacement; hero resources stay themselves.
            TestBase.AssertTrue(Stores.TryGet("poison", out var p) && p.Id == "bleed", "Stores reads poison as bleed", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Stores.TryGet("temphp", out var t) && t.Id == "temphp", "Temp HP stays a resource", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Stores.TryGet("RAGE", out var r) && string.Equals(r.Id, "RAGE", StringComparison.OrdinalIgnoreCase), "material stores are untouched", ref _run, ref _passed, ref _failed);

            using (CoreStatuses.Pin(false))
                TestBase.AssertEqual("poison", CoreStatuses.Resolve("poison"), "the legacy switch keeps old ids", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("CoreStatuses Tests", _run, _passed, _failed);
        }
    }
}
