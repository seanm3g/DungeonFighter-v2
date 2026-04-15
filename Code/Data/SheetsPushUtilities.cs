using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Shared helpers for Google Sheets API push (OAuth client, tab name escaping, value normalization).</summary>
    public static class SheetsPushUtilities
    {
        private static readonly char[] InvisibleSheetChars =
            { '\u200B', '\u200C', '\u200D', '\uFEFF', '\u2060', '\u180E' };

        /// <summary>First 0-based column index reserved for on-sheet formulas on the Actions tab (column E).</summary>
        public const int ActionsSheetPreservedFormulaFirstZeroBased = 4;

        /// <summary>Last 0-based column index reserved for on-sheet formulas on the Actions tab (column F).</summary>
        public const int ActionsSheetPreservedFormulaLastZeroBased = 5;

        /// <summary>First 0-based column index written after the preserved E–F block (column G).</summary>
        public const int ActionsSheetPushDataResumeColumnZeroBased = ActionsSheetPreservedFormulaLastZeroBased + 1;

        /// <summary>Converts a 0-based column index to A1 column letters (0 → A, 4 → E, 25 → Z, 26 → AA).</summary>
        public static string ColumnIndexToA1Letters(int zeroBasedColumnIndex)
        {
            if (zeroBasedColumnIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(zeroBasedColumnIndex));

            int dividend = zeroBasedColumnIndex + 1;
            var stack = new Stack<char>();
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                stack.Push((char)('A' + modulo));
                dividend = (dividend - modulo) / 26;
            }
            return new string(stack.ToArray());
        }

        /// <summary>
        /// Splits a full action row into A–D and G+ value lists, omitting columns E–F (<see cref="ActionsSheetPreservedFormulaFirstZeroBased"/>–<see cref="ActionsSheetPreservedFormulaLastZeroBased"/>).
        /// </summary>
        public static (List<object> ColumnsAD, List<object> ColumnsGPlus) SplitActionPushRowPreservingColumnsEF(IList<object> fullRow)
        {
            int n = fullRow.Count;
            var ad = new List<object>(4);
            for (int i = 0; i < Math.Min(4, n); i++)
                ad.Add(NormalizeCellValueForUpload(fullRow[i]));

            int resume = ActionsSheetPushDataResumeColumnZeroBased;
            var gPlus = new List<object>(Math.Max(0, n - resume));
            for (int i = resume; i < n; i++)
                gPlus.Add(NormalizeCellValueForUpload(fullRow[i]));

            return (ad, gPlus);
        }

        /// <summary>
        /// Normalizes a string for sheet output: strips invisible/format characters and treats any string with no
        /// visible content (letters, digits, symbols, etc.) as empty so the Sheets API receives <c>""</c> per
        /// <see href="https://developers.google.com/sheets/api/reference/rest/v4/spreadsheets.values#ValueRange"/>.
        /// </summary>
        public static string NormalizeSheetString(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            if (s.IndexOfAny(InvisibleSheetChars) >= 0)
            {
                var sb = new StringBuilder(s.Length);
                foreach (char ch in s)
                {
                    if (ch is '\u200B' or '\u200C' or '\u200D' or '\uFEFF' or '\u2060' or '\u180E')
                        continue;
                    sb.Append(ch);
                }
                s = sb.ToString();
            }

            if (IsOnlyIgnorableBlankCharacters(s))
                return "";

            return s;
        }

        /// <summary>True if the string has no printable content (only whitespace, format, control, separators).</summary>
        private static bool IsOnlyIgnorableBlankCharacters(string s)
        {
            foreach (char ch in s)
            {
                if (char.IsWhiteSpace(ch))
                    continue;

                switch (CharUnicodeInfo.GetUnicodeCategory(ch))
                {
                    case UnicodeCategory.Format:
                    case UnicodeCategory.Control:
                    case UnicodeCategory.SpaceSeparator:
                    case UnicodeCategory.LineSeparator:
                    case UnicodeCategory.ParagraphSeparator:
                        continue;
                }

                if (ch is '\u200B' or '\u200C' or '\u200D' or '\uFEFF' or '\u2060' or '\u180E')
                    continue;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Last-mile pass: every cell in every <see cref="ValueRange"/> is normalized again before the HTTP request
        /// (covers types that bypassed earlier steps). Per API contract, empty cells must be <c>""</c>, not <c>null</c>
        /// (null skips the cell and can leave stale content).
        /// </summary>
        public static void NormalizeValueRangeGridsForUpload(IEnumerable<ValueRange> ranges)
        {
            foreach (ValueRange? vr in ranges)
            {
                if (vr?.Values == null)
                    continue;
                foreach (IList<object>? row in vr.Values)
                {
                    if (row == null)
                        continue;
                    for (int i = 0; i < row.Count; i++)
                        row[i] = NormalizeCellValueForUpload(row[i]);
                }
            }
        }

        /// <summary>
        /// Coerces blank values to an empty string for upload. Handles <see cref="string"/>, <see cref="char"/>, and
        /// <see cref="JsonElement"/> (string kind) so stray types never bypass whitespace rules.
        /// </summary>
        public static object NormalizeCellValueForUpload(object? value)
        {
            if (value == null)
                return "";
            if (value is string s)
                return NormalizeSheetString(s);
            if (value is char ch)
                return char.IsWhiteSpace(ch) ? "" : ch.ToString();
            if (value is JsonElement je)
            {
                return je.ValueKind switch
                {
                    JsonValueKind.String => NormalizeSheetString(je.GetString()),
                    JsonValueKind.Null or JsonValueKind.Undefined => "",
                    _ => value
                };
            }

            return value;
        }

        /// <summary>
        /// Applies <see cref="NormalizeCellValueForUpload"/> to every cell in-place, optionally skipping header row(s).
        /// </summary>
        /// <param name="firstRowIndexToNormalize">Use <c>1</c> when row 0 is column headers that must not be altered.</param>
        public static void NormalizeRowsInPlaceForUpload(IList<IList<object>> rows, int firstRowIndexToNormalize = 0)
        {
            for (int r = firstRowIndexToNormalize; r < rows.Count; r++)
            {
                IList<object>? row = rows[r];
                if (row == null)
                    continue;
                for (int c = 0; c < row.Count; c++)
                    row[c] = NormalizeCellValueForUpload(row[c]);
            }
        }

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
