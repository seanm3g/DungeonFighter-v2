using System;
using System.IO;
using System.Text.Json;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class GoogleSheetsUrlHelperTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== GoogleSheetsUrlHelper Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestStandardSpreadsheetUrl(ref testsRun, ref testsPassed, ref testsFailed);
            TestPublishedSpreadsheetUrl(ref testsRun, ref testsPassed, ref testsFailed);
            TestRejectsNonSheetsUrl(ref testsRun, ref testsPassed, ref testsFailed);
            TestRejectsNullOrEmpty(ref testsRun, ref testsPassed, ref testsFailed);
            TestSyncSkipsMissingPushConfigFile(ref testsRun, ref testsPassed, ref testsFailed);
            TestSyncUpdatesTempPushConfig(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("GoogleSheetsUrlHelper Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestStandardSpreadsheetUrl(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestStandardSpreadsheetUrl));
            const string url = "https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit#gid=0";
            bool ok = GoogleSheetsUrlHelper.TryExtractSpreadsheetId(url, out string id);
            TestBase.AssertTrue(ok, "parses", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms", id, "spreadsheetId", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestPublishedSpreadsheetUrl(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestPublishedSpreadsheetUrl));
            const string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTD25Fiu9OIwSaBildDnGlE8aaouIyTjO6XlFqgY5XdSwgOh462ZcVueJKsbb4kSQ/pub?gid=2020359111&single=true&output=csv";
            bool ok = GoogleSheetsUrlHelper.TryExtractSpreadsheetId(url, out string id);
            TestBase.AssertTrue(ok, "parses", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("e/2PACX-1vTD25Fiu9OIwSaBildDnGlE8aaouIyTjO6XlFqgY5XdSwgOh462ZcVueJKsbb4kSQ", id, "spreadsheetId", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestRejectsNonSheetsUrl(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestRejectsNonSheetsUrl));
            bool ok = GoogleSheetsUrlHelper.TryExtractSpreadsheetId("https://example.com/foo", out _);
            TestBase.AssertTrue(!ok, "rejects non-sheets", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestRejectsNullOrEmpty(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestRejectsNullOrEmpty));
            TestBase.AssertTrue(!GoogleSheetsUrlHelper.TryExtractSpreadsheetId(null, out _), "null", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!GoogleSheetsUrlHelper.TryExtractSpreadsheetId("", out _), "empty", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!GoogleSheetsUrlHelper.TryExtractSpreadsheetId("   ", out _), "whitespace", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestSyncSkipsMissingPushConfigFile(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestSyncSkipsMissingPushConfigFile));
            string missing = Path.Combine(Path.GetTempPath(), "df-sheets-push-missing-" + Guid.NewGuid().ToString("N") + ".json");
            bool updated = GoogleSheetsUrlHelper.TrySyncSpreadsheetIdToPushConfig(
                "https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit",
                missing);
            TestBase.AssertTrue(!updated, "no file -> false", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestSyncUpdatesTempPushConfig(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestSyncUpdatesTempPushConfig));
            string temp = Path.Combine(Path.GetTempPath(), "df-sheets-push-sync-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                const string initialJson = """
                {
                  "spreadsheetId": "old_spreadsheet_id",
                  "actionsSheetTabName": "Actions",
                  "oauthClientSecretsPath": "secrets/client.json",
                  "oauthTokenStorePath": "SheetsOAuthToken",
                  "previewRowCount": 5
                }
                """;
                File.WriteAllText(temp, initialJson);

                const string url = "https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/export?format=csv";
                bool updated = GoogleSheetsUrlHelper.TrySyncSpreadsheetIdToPushConfig(url, temp);
                TestBase.AssertTrue(updated, "returns true when id changes", ref testsRun, ref testsPassed, ref testsFailed);

                string after = File.ReadAllText(temp);
                var cfg = JsonSerializer.Deserialize<SheetsPushConfig>(after, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                TestBase.AssertTrue(cfg != null, "deserializes", ref testsRun, ref testsPassed, ref testsFailed);
                if (cfg != null)
                {
                    TestBase.AssertEqual("1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms", cfg.SpreadsheetId, "spreadsheetId updated", ref testsRun, ref testsPassed, ref testsFailed);
                    TestBase.AssertEqual("Actions", cfg.ActionsSheetTabName, "tab name preserved", ref testsRun, ref testsPassed, ref testsFailed);
                    TestBase.AssertEqual("secrets/client.json", cfg.OAuthClientSecretsPath, "oauth path preserved", ref testsRun, ref testsPassed, ref testsFailed);
                }

                bool second = GoogleSheetsUrlHelper.TrySyncSpreadsheetIdToPushConfig(url, temp);
                TestBase.AssertTrue(!second, "no-op when already synced", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { /* ignore */ }
            }
        }
    }
}
