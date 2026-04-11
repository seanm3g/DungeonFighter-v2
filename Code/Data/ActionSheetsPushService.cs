using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Pushes local <c>Actions.json</c> (<see cref="SpreadsheetActionJson"/>) to an editable Google Sheet using OAuth 2.0 (Desktop).
    /// </summary>
    public static class ActionSheetsPushService
    {
        /// <summary>
        /// Overwrites all data rows below the header block; preserves header rows. Requires Sheets API and OAuth sign-in (browser on first run).
        /// </summary>
        public static async Task PushActionsToGoogleSheetsAsync(
            string? actionsJsonPath = null,
            string? pushConfigPath = null,
            CancellationToken cancellationToken = default)
        {
            pushConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.json")
                ?? GameConstants.GetGameDataFilePath("SheetsPushConfig.json");
            actionsJsonPath ??= GameConstants.TryGetExistingGameDataFilePath("Actions.json")
                ?? GameConstants.GetGameDataFilePath("Actions.json");

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

            var cfg = SheetsPushConfig.Load(pushConfigPath);
            cfg.Validate();

            string secretsPath = cfg.ResolveOAuthClientSecretsPath(pushConfigPath);
            if (!File.Exists(secretsPath))
                throw new FileNotFoundException("OAuth client secrets file not found.", secretsPath);

            if (!File.Exists(actionsJsonPath))
                throw new FileNotFoundException("Actions.json not found.", actionsJsonPath);

            var actions = JsonLoader.LoadJson<List<SpreadsheetActionJson>>(
                actionsJsonPath,
                useCache: false,
                fallbackValue: new List<SpreadsheetActionJson>());
            if (actions == null || actions.Count == 0)
                throw new InvalidOperationException("No actions to push (Actions.json empty or invalid).");

            string tokenDir = cfg.ResolveOAuthTokenStoreDirectory(pushConfigPath);
            var credential = await GoogleSheetsOAuthCredentialProvider.AuthorizeAsync(secretsPath, tokenDir, cancellationToken)
                .ConfigureAwait(false);

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DungeonFighter Actions Push"
            });

            string sheet = EscapeSheetName(cfg.ActionsSheetTabName);

            SpreadsheetHeader? header;
            int firstDataRowOneBased;

            // Prefer the same CSV source as pull (SheetsConfig.actionsSheetUrl) so column labels/order match Update-from-Sheets / Resync.
            string sheetsConfigPath = GameConstants.TryGetExistingGameDataFilePath("SheetsConfig.json")
                ?? GameConstants.GetGameDataFilePath("SheetsConfig.json");
            var sheetsCfg = SheetsConfig.Load(sheetsConfigPath);
            if (!string.IsNullOrWhiteSpace(sheetsCfg.ActionsSheetUrl))
            {
                try
                {
                    var csvResult = await SpreadsheetActionParser.ParseCsvAsync(sheetsCfg.ActionsSheetUrl)
                        .ConfigureAwait(false);
                    if (csvResult.Header != null)
                    {
                        header = csvResult.Header;
                        firstDataRowOneBased = header.DataStartRowIndex + 1;
                    }
                    else
                    {
                        (header, firstDataRowOneBased) = await FetchHeaderFromSheetApiAsync(
                            service, cfg, sheet, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sheets push: could not use CSV header from SheetsConfig ({ex.Message}); using live sheet preview.");
                    (header, firstDataRowOneBased) = await FetchHeaderFromSheetApiAsync(
                        service, cfg, sheet, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                (header, firstDataRowOneBased) = await FetchHeaderFromSheetApiAsync(
                    service, cfg, sheet, cancellationToken).ConfigureAwait(false);
            }

            if (header == null)
                throw new InvalidOperationException("Could not parse header rows. Set actionsSheetUrl in GameData/SheetsConfig.json to match the published CSV used for pull, or check actionsSheetTabName and header rows on the tab.");

            var bodyRows = new List<IList<object>>();
            foreach (var json in actions)
            {
                var data = json.ToSpreadsheetActionData();
                if (!data.IsValid())
                    continue;
                var cells = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);
                bodyRows.Add(cells.Select(c => (object)c).ToList());
            }

            if (bodyRows.Count == 0)
                throw new InvalidOperationException("No valid action rows to push (every row failed IsValid).");

            string clearRange = $"{sheet}!A{firstDataRowOneBased}:ZZ5000";
            await service.Spreadsheets.Values
                .Clear(new ClearValuesRequest(), cfg.SpreadsheetId, clearRange)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string updateRange = $"{sheet}!A{firstDataRowOneBased}";
            var valueRange = new ValueRange
            {
                MajorDimension = "ROWS",
                Values = bodyRows
            };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, cfg.SpreadsheetId, updateRange);
            // RAW keeps values as literal strings (matches CSV export / pull), avoiding Sheets re-interpreting numbers or dates.
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<(SpreadsheetHeader? Header, int FirstDataRowOneBased)> FetchHeaderFromSheetApiAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            string escapedSheetName,
            CancellationToken cancellationToken)
        {
            int previewRows = Math.Max(5, cfg.PreviewRowCount);
            string previewRange = $"{escapedSheetName}!A1:ZZ{previewRows}";

            var previewResponse = await service.Spreadsheets.Values
                .Get(cfg.SpreadsheetId, previewRange)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            var rowStrings = NormalizeToStringRows(previewResponse.Values);
            return SpreadsheetActionParser.BuildHeaderFromSheetRows(rowStrings);
        }

        private static string EscapeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "'Sheet1'";
            return "'" + name.Replace("'", "''", StringComparison.Ordinal) + "'";
        }

        private static List<string[]> NormalizeToStringRows(IList<IList<object>>? values)
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
    }
}
