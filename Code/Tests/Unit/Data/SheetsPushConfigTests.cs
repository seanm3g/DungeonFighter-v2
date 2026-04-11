using System.IO;
using System.Text.Json;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class SheetsPushConfigTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== SheetsPushConfig Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestDeserializeOAuthTemplateShape(ref testsRun, ref testsPassed, ref testsFailed);
            TestResolvePathsRelativeToConfigFile(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("SheetsPushConfig Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestDeserializeOAuthTemplateShape(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestDeserializeOAuthTemplateShape));
            const string json = """
            {
              "spreadsheetId": "abc123",
              "actionsSheetTabName": "Actions",
              "oauthClientSecretsPath": "secrets/client.json",
              "oauthTokenStorePath": "SheetsOAuthToken",
              "previewRowCount": 5
            }
            """;
            var cfg = JsonSerializer.Deserialize<SheetsPushConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            TestBase.AssertTrue(cfg != null, "deserializes", ref testsRun, ref testsPassed, ref testsFailed);
            if (cfg == null) return;
            TestBase.AssertEqual("abc123", cfg.SpreadsheetId, "spreadsheetId", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("Actions", cfg.ActionsSheetTabName, "actionsSheetTabName", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("secrets/client.json", cfg.OAuthClientSecretsPath, "oauthClientSecretsPath", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("SheetsOAuthToken", cfg.OAuthTokenStorePath, "oauthTokenStorePath", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(5, cfg.PreviewRowCount, "previewRowCount", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestResolvePathsRelativeToConfigFile(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestResolvePathsRelativeToConfigFile));
            var cfg = new SheetsPushConfig
            {
                OAuthClientSecretsPath = "sub/secret.json",
                OAuthTokenStorePath = "tok"
            };
            string fakeConfig = Path.Combine(Path.GetTempPath(), "GameData", "SheetsPushConfig.json");
            string expectedSecrets = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fakeConfig)!, "sub", "secret.json"));
            string expectedTok = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fakeConfig)!, "tok"));
            TestBase.AssertEqual(expectedSecrets, cfg.ResolveOAuthClientSecretsPath(fakeConfig), "ResolveOAuthClientSecretsPath", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(expectedTok, cfg.ResolveOAuthTokenStoreDirectory(fakeConfig), "ResolveOAuthTokenStoreDirectory", ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
