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
            TestBase.AssertEqual(1.0, ActionUtilities.SlotMultiplier(1.3, 0, hero: true), "Slot 1 is ×1.0", ref _run, ref _passed, ref _failed);
            AssertClose(1.3, ActionUtilities.SlotMultiplier(1.3, 1, hero: true), "Slot 2 is ×AMP");
            AssertClose(1.3 * 1.3 * 1.3, ActionUtilities.SlotMultiplier(1.3, 3, hero: true), "Slot 4 is ×AMP^3 (no linear, no per-slot %)");
            AssertClose(1.3 * 1.3, ActionUtilities.SlotMultiplier(1.3, 2, hero: false), "Enemies use the same formula (D0.11)");
            TestBase.PrintSummary("SingleAmpFormula Tests", _run, _passed, _failed);
        }

        private static void AssertClose(double expected, double actual, string message) =>
            TestBase.AssertTrue(Math.Abs(expected - actual) < 1e-9, $"{message} (expected {expected:0.####}, got {actual:0.####})", ref _run, ref _passed, ref _failed);
    }
}
