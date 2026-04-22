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
            TestDeserializeExtendedTabNames(ref testsRun, ref testsPassed, ref testsFailed);
            TestApplyDefaultOptionalTabNamesWhenAllUnset(ref testsRun, ref testsPassed, ref testsFailed);
            TestApplyDefaultOptionalTabNamesSkipsWhenAnySet(ref testsRun, ref testsPassed, ref testsFailed);
            TestApplyDefaultEnemiesAndEnvironmentsTabNamesWhenUnset(ref testsRun, ref testsPassed, ref testsFailed);
            TestApplyDefaultDungeonsTabNameWhenUnset(ref testsRun, ref testsPassed, ref testsFailed);
            TestApplyDefaultStatBonusesTabNameWhenUnset(ref testsRun, ref testsPassed, ref testsFailed);
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

        private static void TestDeserializeExtendedTabNames(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestDeserializeExtendedTabNames));
            const string json = """
            {
              "spreadsheetId": "x",
              "actionsSheetTabName": "ACTIONS",
              "weaponsSheetTabName": "WEAPONS",
              "modificationsSheetTabName": "MODIFICATIONS",
              "armorSheetTabName": "ARMOR",
              "classPresentationSheetTabName": "CLASSES",
              "oauthClientSecretsPath": "s.json",
              "oauthTokenStorePath": "tok",
              "previewRowCount": 3
            }
            """;
            var cfg = JsonSerializer.Deserialize<SheetsPushConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            TestBase.AssertTrue(cfg != null, "deserializes", ref testsRun, ref testsPassed, ref testsFailed);
            if (cfg == null) return;
            TestBase.AssertEqual("WEAPONS", cfg.WeaponsSheetTabName, "weaponsSheetTabName", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("CLASSES", cfg.ClassPresentationSheetTabName, "classPresentationSheetTabName", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestApplyDefaultOptionalTabNamesWhenAllUnset(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestApplyDefaultOptionalTabNamesWhenAllUnset));
            var cfg = new SheetsPushConfig
            {
                SpreadsheetId = "x",
                ActionsSheetTabName = "ACTIONS",
                OAuthClientSecretsPath = "s.json",
                OAuthTokenStorePath = "tok"
            };
            TestBase.AssertTrue(cfg.ApplyDefaultOptionalSheetTabNamesWhenAllUnset(), "returns true", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultWeaponsSheetTabName, cfg.WeaponsSheetTabName, "weapons", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultModificationsSheetTabName, cfg.ModificationsSheetTabName, "mods", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultArmorSheetTabName, cfg.ArmorSheetTabName, "armor", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultClassPresentationSheetTabName, cfg.ClassPresentationSheetTabName, "classes", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultEnemiesSheetTabName, cfg.EnemiesSheetTabName, "enemies", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultEnvironmentsSheetTabName, cfg.EnvironmentsSheetTabName, "environments", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultDungeonsSheetTabName, cfg.DungeonsSheetTabName, "dungeons", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultStatBonusesSheetTabName, cfg.StatBonusesSheetTabName, "stat bonuses / suffixes", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestApplyDefaultOptionalTabNamesSkipsWhenAnySet(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestApplyDefaultOptionalTabNamesSkipsWhenAnySet));
            var cfg = new SheetsPushConfig { WeaponsSheetTabName = "MyWeapons" };
            TestBase.AssertTrue(!cfg.ApplyDefaultOptionalSheetTabNamesWhenAllUnset(), "returns false", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("MyWeapons", cfg.WeaponsSheetTabName, "unchanged", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", cfg.ModificationsSheetTabName, "mods still empty", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestApplyDefaultEnemiesAndEnvironmentsTabNamesWhenUnset(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestApplyDefaultEnemiesAndEnvironmentsTabNamesWhenUnset));
            var cfg = new SheetsPushConfig
            {
                SpreadsheetId = "x",
                ActionsSheetTabName = "ACTIONS",
                OAuthClientSecretsPath = "s.json",
                WeaponsSheetTabName = "MyWeapons",
                EnemiesSheetTabName = "",
                EnvironmentsSheetTabName = "   "
            };
            TestBase.AssertTrue(cfg.ApplyDefaultEnemiesAndEnvironmentsTabNamesIfUnset(), "returns true", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("MyWeapons", cfg.WeaponsSheetTabName, "weapons unchanged", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultEnemiesSheetTabName, cfg.EnemiesSheetTabName, "enemies defaulted", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultEnvironmentsSheetTabName, cfg.EnvironmentsSheetTabName, "environments defaulted", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!cfg.ApplyDefaultEnemiesAndEnvironmentsTabNamesIfUnset(), "second call no-op", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestApplyDefaultDungeonsTabNameWhenUnset(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestApplyDefaultDungeonsTabNameWhenUnset));
            var cfg = new SheetsPushConfig
            {
                SpreadsheetId = "x",
                ActionsSheetTabName = "ACTIONS",
                OAuthClientSecretsPath = "s.json",
                WeaponsSheetTabName = "WEAPONS",
                DungeonsSheetTabName = ""
            };
            TestBase.AssertTrue(cfg.ApplyDefaultDungeonsTabNameIfUnset(), "returns true", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultDungeonsSheetTabName, cfg.DungeonsSheetTabName, "dungeons defaulted", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!cfg.ApplyDefaultDungeonsTabNameIfUnset(), "second call no-op", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestApplyDefaultStatBonusesTabNameWhenUnset(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestApplyDefaultStatBonusesTabNameWhenUnset));
            var cfg = new SheetsPushConfig
            {
                SpreadsheetId = "x",
                ActionsSheetTabName = "ACTIONS",
                OAuthClientSecretsPath = "s.json",
                WeaponsSheetTabName = "WEAPONS",
                StatBonusesSheetTabName = ""
            };
            TestBase.AssertTrue(cfg.ApplyDefaultStatBonusesTabNameIfUnset(), "returns true", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(SheetsPushConfig.DefaultStatBonusesSheetTabName, cfg.StatBonusesSheetTabName, "suffixes tab defaulted", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!cfg.ApplyDefaultStatBonusesTabNameIfUnset(), "second call no-op", ref testsRun, ref testsPassed, ref testsFailed);
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
