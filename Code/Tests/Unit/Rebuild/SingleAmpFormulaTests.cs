using System;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 1.3 (D2.3): AMP = INT curve × gear AMP; slot k deals AMP^(k−1); the duplicate AMP paths are off.</summary>
    public static class SingleAmpFormulaTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SingleAmpFormula Tests ===\n");
            _run = _passed = _failed = 0;
            var combo = GameConfiguration.Instance.ComboSystem;
            var combat = GameConfiguration.Instance.Combat;
            bool oldSingle = combo.SingleAmpFormula, oldLinear = combo.ComboAmplifierLinearSlots;
            double oldPct = combo.ComboSlotDamagePercentPerStep, oldInt = combat.IntelligenceAmpPerPoint;
            try
            {
                TestBase.AssertTrue(new ComboSystemConfig().SingleAmpFormula, "The single formula is on by default", ref _run, ref _passed, ref _failed);

                combo.SingleAmpFormula = true;
                combo.ComboAmplifierLinearSlots = true;      // ignored while the single formula is on
                combo.ComboSlotDamagePercentPerStep = 10;    // ignored too
                combat.IntelligenceAmpPerPoint = 0.01;       // INT potency: ignored too

                TestBase.AssertEqual(1.0, ActionUtilities.SlotMultiplier(1.3, 0, hero: true), "Slot 1 is ×1.0", ref _run, ref _passed, ref _failed);
                AssertClose(1.3, ActionUtilities.SlotMultiplier(1.3, 1, hero: true), "Slot 2 is ×AMP");
                AssertClose(1.3 * 1.3 * 1.3, ActionUtilities.SlotMultiplier(1.3, 3, hero: true), "Slot 4 is ×AMP^3 (no linear, no per-slot %)");
                AssertClose(1.3 * 1.3, ActionUtilities.SlotMultiplier(1.3, 2, hero: false), "Enemies use the same formula (D0.11)");

                var hero = TestDataBuilders.Character().WithName("Amp").WithLevel(10).Build();
                hero.Stats.Intelligence = 40;
                TestBase.AssertEqual(1.0, hero.GetIntelligenceAmpMultiplier(), "INT potency no longer multiplies AMP", ref _run, ref _passed, ref _failed);

                combo.SingleAmpFormula = false;
                TestBase.AssertTrue(hero.GetIntelligenceAmpMultiplier() > 1.0, "Switching the flag off restores INT potency (switch off before deleting)", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(ActionUtilities.SlotMultiplier(1.3, 3, hero: true) != Math.Pow(1.3, 3), "Switching the flag off restores the old slot math", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combo.SingleAmpFormula = oldSingle;
                combo.ComboAmplifierLinearSlots = oldLinear;
                combo.ComboSlotDamagePercentPerStep = oldPct;
                combat.IntelligenceAmpPerPoint = oldInt;
            }
            TestBase.PrintSummary("SingleAmpFormula Tests", _run, _passed, _failed);
        }

        private static void AssertClose(double expected, double actual, string message) =>
            TestBase.AssertTrue(Math.Abs(expected - actual) < 1e-9, $"{message} (expected {expected:0.####}, got {actual:0.####})", ref _run, ref _passed, ref _failed);
    }
}
