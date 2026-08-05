using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Sheets.v4;

namespace RPGGame.Data
{
    /// <summary>Validates spreadsheet id and tab titles before destructive Sheets API calls.</summary>
    public static class SheetsPushPreflight
    {
        /// <summary>
        /// Fetches tab title → sheetId (gid) for <paramref name="spreadsheetId"/>; throws
        /// <see cref="InvalidOperationException"/> when the spreadsheet is missing or inaccessible (often HTTP 404).
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, int>> FetchTabTitleToSheetIdOrThrowAsync(
            SheetsService service,
            string spreadsheetId,
            CancellationToken cancellationToken)
        {
            try
            {
                var req = service.Spreadsheets.Get(spreadsheetId);
                req.Fields = "sheets(properties(title,sheetId))";
                var meta = await req.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                var map = new Dictionary<string, int>(StringComparer.Ordinal);
                if (meta.Sheets != null)
                {
                    foreach (var s in meta.Sheets)
                    {
                        string? t = s.Properties?.Title;
                        int? id = s.Properties?.SheetId;
                        if (!string.IsNullOrEmpty(t) && id.HasValue && !map.ContainsKey(t))
                            map[t] = id.Value;
                    }
                }

                return map;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    "Google Sheets returned NotFound for this spreadsheetId. " +
                    "If spreadsheetId looks like e/2PACX-… it is a publish-only key: open the spreadsheet in the browser, copy the Edit URL (…/spreadsheets/d/<long-id>/edit), " +
                    "put it in GameData/SheetsConfig.json as spreadsheetEditUrl, save from Balance Tuning (or run push again so id sync runs), or paste the real id into SheetsPushConfig.json. " +
                    "Otherwise confirm the signed-in Google account can open this spreadsheet in the browser.",
                    ex);
            }
        }

        /// <summary>
        /// Fetches tab titles for <paramref name="spreadsheetId"/>; throws <see cref="InvalidOperationException"/>
        /// with guidance when the spreadsheet is missing or inaccessible (often HTTP 404).
        /// </summary>
        public static async Task<IReadOnlyList<string>> FetchTabTitlesOrThrowAsync(
            SheetsService service,
            string spreadsheetId,
            CancellationToken cancellationToken)
        {
            var map = await FetchTabTitleToSheetIdOrThrowAsync(service, spreadsheetId, cancellationToken)
                .ConfigureAwait(false);
            return map.Keys.ToList();
        }

        /// <summary>Browser edit URL for one tab (<c>…/edit?gid=…</c>).</summary>
        public static string BuildTabEditUrl(string spreadsheetId, int sheetId) =>
            "https://docs.google.com/spreadsheets/d/" + spreadsheetId.Trim() + "/edit?gid=" + sheetId;

        /// <summary>Throws if any required tab title is missing (exact match, case-sensitive).</summary>
        public static void EnsureTabsPresent(
            IReadOnlyCollection<string> existingTabTitles,
            IEnumerable<string> requiredTabTitles)
        {
            var set = new HashSet<string>(existingTabTitles, StringComparer.Ordinal);
            var missing = new List<string>();
            foreach (string? raw in requiredTabTitles)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                string t = raw.Trim();
                if (!set.Contains(t))
                    missing.Add(t);
            }

            if (missing.Count == 0)
                return;

            string missingStr = string.Join(", ", missing.Select(x => "'" + x + "'"));
            string available = string.Join(", ", set.OrderBy(x => x, StringComparer.Ordinal).Select(x => "'" + x + "'"));
            throw new InvalidOperationException(
                $"Google Sheet tab(s) not found on this spreadsheet: {missingStr}. " +
                $"Existing tabs: {available}. " +
                "Update actionsSheetTabName and related fields in SheetsPushConfig.json to match tab names exactly (including spaces and capitalization).");
        }

        /// <summary>
        /// Fetches metadata then ensures every non-empty required tab exists.
        /// Returns title → sheetId (gid) for building edit links after push.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, int>> EnsureSpreadsheetAccessAndTabsAsync(
            SheetsService service,
            string spreadsheetId,
            IEnumerable<string> requiredTabTitles,
            CancellationToken cancellationToken)
        {
            var map = await FetchTabTitleToSheetIdOrThrowAsync(service, spreadsheetId, cancellationToken)
                .ConfigureAwait(false);
            EnsureTabsPresent(map.Keys.ToList(), requiredTabTitles);
            return map;
        }
    }
}
