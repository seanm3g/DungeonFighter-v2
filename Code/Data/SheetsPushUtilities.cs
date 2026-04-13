using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Shared helpers for Google Sheets API push (OAuth client, tab name escaping, value normalization).</summary>
    public static class SheetsPushUtilities
    {
        /// <summary>Resolves push config path; if missing, copies from template when available and throws with setup instructions.</summary>
        public static string EnsurePushConfigExistsOrThrow(string? pushConfigPath = null)
        {
            pushConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.json")
                ?? GameConstants.GetGameDataFilePath("SheetsPushConfig.json");

            if (!File.Exists(pushConfigPath))
            {
                string? templatePath = GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.template.json");
                if (templatePath == null)
                {
                    try
                    {
                        string? dir = Path.GetDirectoryName(Path.GetFullPath(pushConfigPath));
                        if (!string.IsNullOrEmpty(dir))
                        {
                            string candidate = Path.Combine(dir, "SheetsPushConfig.template.json");
                            if (File.Exists(candidate))
                                templatePath = candidate;
                        }
                    }
                    catch { /* ignore */ }
                }

                if (templatePath != null && File.Exists(templatePath))
                {
                    string? destDir = Path.GetDirectoryName(Path.GetFullPath(pushConfigPath));
                    if (!string.IsNullOrEmpty(destDir))
                        Directory.CreateDirectory(destDir);
                    File.Copy(templatePath, pushConfigPath, overwrite: false);
                    throw new InvalidOperationException(
                        "SheetsPushConfig.json was missing; it has been created from SheetsPushConfig.template.json at:\n" +
                        Path.GetFullPath(pushConfigPath) +
                        "\n\nEdit that file: set spreadsheetId, actionsSheetTabName, oauthClientSecretsPath (path to your Google OAuth Desktop client JSON), then run Push again.");
                }

                throw new FileNotFoundException(
                    "Sheets push config not found. Copy GameData/SheetsPushConfig.template.json to GameData/SheetsPushConfig.json (project root GameData folder), fill in spreadsheetId, tab name, and oauthClientSecretsPath, and oauth paths to your client JSON.",
                    pushConfigPath);
            }

            return pushConfigPath;
        }

        /// <summary>
        /// Loads <see cref="SheetsConfig"/> and, when possible, writes an API-compatible <c>spreadsheetId</c> into
        /// <see cref="SheetsPushConfig"/> (e.g. from <c>spreadsheetEditUrl</c>), then returns a freshly loaded push config.
        /// </summary>
        /// <param name="sheetsConfigPath">Optional explicit path to SheetsConfig.json (for tests); default resolves via <see cref="GameConstants"/>.</param>
        public static SheetsPushConfig LoadPushConfigWithSheetsIdSync(string pushConfigPath, string? sheetsConfigPath = null)
        {
            sheetsConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsConfig.json")
                ?? GameConstants.GetGameDataFilePath("SheetsConfig.json");
            var sheets = SheetsConfig.Load(sheetsConfigPath);
            GoogleSheetsUrlHelper.TrySyncSpreadsheetIdToPushConfigFromSheetsConfig(sheets, pushConfigPath);
            return SheetsPushConfig.Load(pushConfigPath);
        }

        public static string EscapeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "'Sheet1'";
            return "'" + name.Replace("'", "''", StringComparison.Ordinal) + "'";
        }

        public static List<string[]> NormalizeToStringRows(IList<IList<object>>? values)
        {
            var rows = new List<string[]>();
            if (values == null || values.Count == 0)
                return rows;

            int w = 0;
            foreach (var r in values)
                w = Math.Max(w, r?.Count ?? 0);
            if (w == 0)
                return rows;

            foreach (var r in values)
            {
                var arr = new string[w];
                for (int i = 0; i < w; i++)
                {
                    if (r != null && i < r.Count && r[i] != null)
                        arr[i] = r[i].ToString() ?? "";
                    else
                        arr[i] = "";
                }
                rows.Add(arr);
            }

            return rows;
        }

        public static async Task<SheetsService> CreateAuthorizedSheetsServiceAsync(
            SheetsPushConfig cfg,
            string pushConfigPath,
            CancellationToken cancellationToken = default)
        {
            string secretsPath = cfg.ResolveOAuthClientSecretsPath(pushConfigPath);
            if (!File.Exists(secretsPath))
                throw new FileNotFoundException("OAuth client secrets file not found.", secretsPath);

            string tokenDir = cfg.ResolveOAuthTokenStoreDirectory(pushConfigPath);
            var credential = await GoogleSheetsOAuthCredentialProvider.AuthorizeAsync(secretsPath, tokenDir, cancellationToken)
                .ConfigureAwait(false);

            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DungeonFighter Sheets Push"
            });
        }
    }
}
