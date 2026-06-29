using System;
using RPGGame.Config.BalancePatches;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    public static class BalancePatchMetadataTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== BalancePatchMetadata Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestGeneratePatchIdFromSpaces(ref testsRun, ref testsPassed, ref testsFailed);
            TestGeneratePatchIdPreservesDashes(ref testsRun, ref testsPassed, ref testsFailed);
            TestGeneratePatchIdFromUnderscores(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("BalancePatchMetadata Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestGeneratePatchIdFromSpaces(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestGeneratePatchIdFromSpaces));
            string today = DateTime.Now.ToString("yyyyMMdd");
            string patchId = BalancePatchMetadata.GeneratePatchId("Aggressive Enemies", "1.2");
            TestBase.AssertEqual($"aggressive-enemies-v1.2-{today}", patchId, "spaces become dashes", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestGeneratePatchIdPreservesDashes(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestGeneratePatchIdPreservesDashes));
            string today = DateTime.Now.ToString("yyyyMMdd");
            string patchId = BalancePatchMetadata.GeneratePatchId("enhanced-enemies-v1", "1.0");
            TestBase.AssertEqual($"enhanced-enemies-v1-v1.0-{today}", patchId, "preserves dashes in name", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestGeneratePatchIdFromUnderscores(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestGeneratePatchIdFromUnderscores));
            string today = DateTime.Now.ToString("yyyyMMdd");
            string patchId = BalancePatchMetadata.GeneratePatchId("iteration_1_balance", "1.1");
            TestBase.AssertEqual($"iteration-1-balance-v1.1-{today}", patchId, "underscores become dashes", ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
