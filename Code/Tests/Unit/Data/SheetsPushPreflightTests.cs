using System;
using System.IO;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class SheetsPushPreflightTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== SheetsPushPreflight Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestAllPresent(ref testsRun, ref testsPassed, ref testsFailed);
            TestSkipsEmptyRequired(ref testsRun, ref testsPassed, ref testsFailed);
            TestMissingTabMessage(ref testsRun, ref testsPassed, ref testsFailed);
            TestCaseSensitive(ref testsRun, ref testsPassed, ref testsFailed);
            TestLoadPushConfigWithSheetsIdSync(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("SheetsPushPreflight Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestAllPresent(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestAllPresent));
            try
            {
                SheetsPushPreflight.EnsureTabsPresent(new[] { "ACTIONS", "Sheet1" }, new[] { "ACTIONS" });
                TestBase.AssertTrue(true, "no throw when tab exists", ref testsRun, ref testsPassed, ref testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, "unexpected: " + ex.Message, ref testsRun, ref testsPassed, ref testsFailed);
            }
        }

        private static void TestSkipsEmptyRequired(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestSkipsEmptyRequired));
            try
            {
                SheetsPushPreflight.EnsureTabsPresent(new[] { "ACTIONS" }, new[] { "ACTIONS", "", "   " });
                TestBase.AssertTrue(true, "empty required skipped", ref testsRun, ref testsPassed, ref testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, "unexpected: " + ex.Message, ref testsRun, ref testsPassed, ref testsFailed);
            }
        }

        private static void TestMissingTabMessage(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestMissingTabMessage));
            try
            {
                SheetsPushPreflight.EnsureTabsPresent(new[] { "Sheet1", "Data" }, new[] { "ACTIONS" });
                TestBase.AssertTrue(false, "expected InvalidOperationException", ref testsRun, ref testsPassed, ref testsFailed);
            }
            catch (InvalidOperationException ex)
            {
                bool ok = ex.Message.Contains("'ACTIONS'", StringComparison.Ordinal)
                    && ex.Message.Contains("'Sheet1'", StringComparison.Ordinal)
                    && ex.Message.Contains("'Data'", StringComparison.Ordinal);
                TestBase.AssertTrue(ok, "message lists missing and existing tabs", ref testsRun, ref testsPassed, ref testsFailed);
            }
        }

        private static void TestCaseSensitive(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestCaseSensitive));
            try
            {
                SheetsPushPreflight.EnsureTabsPresent(new[] { "actions" }, new[] { "ACTIONS" });
                TestBase.AssertTrue(false, "expected InvalidOperationException", ref testsRun, ref testsPassed, ref testsFailed);
            }
            catch (InvalidOperationException)
            {
                TestBase.AssertTrue(true, "case mismatch rejected", ref testsRun, ref testsPassed, ref testsFailed);
            }
        }

        private static void TestLoadPushConfigWithSheetsIdSync(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestLoadPushConfigWithSheetsIdSync));
            string dir = Path.Combine(Path.GetTempPath(), "df-load-push-sync-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(dir);
                string pushPath = Path.Combine(dir, "SheetsPushConfig.json");
                string sheetsPath = Path.Combine(dir, "SheetsConfig.json");

                const string apiId = "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms";
                const string editUrl = "https://docs.google.com/spreadsheets/d/" + apiId + "/edit#gid=0";

                File.WriteAllText(pushPath,
                    """
                    {
                      "spreadsheetId": "e/2PACX-1vTD25Fiu9OIwSaBildDnGlE8aaouIyTjO6XlFqgY5XdSwgOh462ZcVueJKsbb4kSQ",
                      "actionsSheetTabName": "ACTIONS",
                      "oauthClientSecretsPath": "secrets/client.json",
                      "oauthTokenStorePath": "SheetsOAuthToken",
                      "previewRowCount": 5
                    }
                    """);

                File.WriteAllText(sheetsPath, "{\"spreadsheetEditUrl\":\"" + editUrl + "\",\"actionsSheetUrl\":\"\"}\n");

                var cfg = SheetsPushUtilities.LoadPushConfigWithSheetsIdSync(pushPath, sheetsPath);
                TestBase.AssertEqual(apiId, cfg.SpreadsheetId, "spreadsheetId synced from edit url", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, recursive: true);
                }
                catch { /* ignore */ }
            }
        }
    }
}
