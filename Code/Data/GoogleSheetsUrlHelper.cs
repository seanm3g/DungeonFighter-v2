using System;
using System.IO;
using System.Text.RegularExpressions;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Parses Google Sheets document URLs for spreadsheet IDs and published CSV URL templates.
    /// </summary>
    public static class GoogleSheetsUrlHelper
    {
        private const string SpreadsheetsDMarker = "/spreadsheets/d/";

        /// <summary>
        /// Extracts the path segment after <c>/d/</c> (standard id, or <c>e/2PACX-…</c> for published docs).
        /// </summary>
        public static bool TryExtractSpreadsheetId(string? url, out string spreadsheetId)
        {
            spreadsheetId = "";
            if (string.IsNullOrWhiteSpace(url))
                return false;

            int i = url.IndexOf(SpreadsheetsDMarker, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                return false;

            i += SpreadsheetsDMarker.Length;
            if (i >= url.Length)
                return false;

            int end = i;
            while (end < url.Length && url[end] != '/' && url[end] != '?' && url[end] != '#')
                end++;

            string first = url.Substring(i, end - i).Trim();
            if (string.IsNullOrEmpty(first))
                return false;

            if (string.Equals(first, "e", StringComparison.OrdinalIgnoreCase))
            {
                if (end >= url.Length || url[end] != '/')
                    return false;
                int j = end + 1;
                int end2 = j;
                while (end2 < url.Length && url[end2] != '/' && url[end2] != '?' && url[end2] != '#')
                    end2++;
                if (end2 <= j)
                    return false;
                string second = url.Substring(j, end2 - j).Trim();
                if (string.IsNullOrEmpty(second))
                    return false;
                spreadsheetId = "e/" + second;
                return true;
            }

            spreadsheetId = first;
            return true;
        }

        /// <summary>
        /// Spreadsheet id accepted by the Google Sheets API v4 (mutations). Published <c>e/2PACX-…</c> keys are rejected.
        /// </summary>
        public static bool TryExtractApiSpreadsheetId(string? url, out string spreadsheetId)
        {
            spreadsheetId = "";
            if (!TryExtractSpreadsheetId(url, out string raw))
                return false;

            if (raw.StartsWith("e/", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(raw, "e", StringComparison.OrdinalIgnoreCase))
                return false;
            if (raw.IndexOf("PACX", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (raw.Length < 10)
                return false;

            spreadsheetId = raw;
            return true;
        }

        /// <summary>
        /// For a standard (non–<c>e/2PACX</c>) spreadsheet URL, returns the unauthenticated CSV export URL
        /// (<c>…/export?format=csv&amp;gid=…</c>). Browser <c>/edit</c> and <c>/view</c> links return HTML, not CSV—using them
        /// with <see cref="HttpClient"/> makes parsers build bogus headers and push appears to succeed with empty rows.
        /// </summary>
        /// <returns>False if the URL is not a Google Sheets document with an API spreadsheet id, or is already a non-edit form we do not rewrite.</returns>
        public static bool TryBuildSpreadsheetCsvExportUrl(string? url, out string csvExportUrl)
        {
            csvExportUrl = "";
            if (string.IsNullOrWhiteSpace(url))
                return false;
            if (url.IndexOf("docs.google.com/spreadsheets", StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            if (!TryExtractApiSpreadsheetId(url, out string id))
                return false;

            if (url.Contains("/export?format=csv", StringComparison.OrdinalIgnoreCase)
                || url.Contains("/export?format=tsv", StringComparison.OrdinalIgnoreCase))
            {
                csvExportUrl = url.Trim();
                return true;
            }

            if (url.Contains("output=csv", StringComparison.OrdinalIgnoreCase))
            {
                csvExportUrl = url.Trim();
                return true;
            }

            if (!TryExtractGidParameter(url, out string gid) || string.IsNullOrEmpty(gid))
                gid = "0";

            csvExportUrl = "https://docs.google.com/spreadsheets/d/" + id + "/export?format=csv&gid=" + gid;
            return true;
        }

        /// <summary>Replaces <c>gid=…</c> in a Google Sheets published/export URL (same spreadsheet, different tab).</summary>
        public static string ReplaceGidInPublishedGoogleSheetsUrl(string url, string newGid)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;
            string g = (newGid ?? "").Trim();
            if (g.Length == 0)
                return url;

            if (Regex.IsMatch(url, @"\bgid=\d+", RegexOptions.IgnoreCase))
                return Regex.Replace(url, @"\bgid=\d+", "gid=" + g, RegexOptions.IgnoreCase);

            char sep = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return url.TrimEnd('&', '?') + sep + "gid=" + g;
        }

        /// <summary>Reads the first <c>gid=digits</c> from a Sheets URL query string.</summary>
        public static bool TryExtractGidParameter(string? url, out string gid)
        {
            gid = "";
            if (string.IsNullOrWhiteSpace(url))
                return false;
            var m = Regex.Match(url, @"\bgid=(\d+)", RegexOptions.IgnoreCase);
            if (!m.Success)
                return false;
            gid = m.Groups[1].Value;
            return true;
        }

        /// <summary>True if both URLs are the same except for <c>gid=</c> (same published/export document).</summary>
        public static bool SamePublishedSheetsUrlIgnoringGid(string? urlA, string? urlB)
        {
            if (string.IsNullOrWhiteSpace(urlA) || string.IsNullOrWhiteSpace(urlB))
                return false;
            string norm(string u) => Regex.Replace(u.Trim(), @"\bgid=\d+", "gid=0", RegexOptions.IgnoreCase);
            return string.Equals(norm(urlA), norm(urlB), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Updates <see cref="SheetsPushConfig.SpreadsheetId"/> from the first URL that yields an API spreadsheet id.
        /// Order: <see cref="SheetsConfig.SpreadsheetEditUrl"/>, then <see cref="SheetsConfig.ActionsSheetUrl"/>, then other CSV URLs.
        /// Does <b>not</b> use published <c>e/2PACX-…</c> keys (they work for public CSV GET but cause API 404 on push).
        /// </summary>
        public static bool TrySyncSpreadsheetIdToPushConfigFromSheetsConfig(SheetsConfig sheetsConfig, string? pushConfigPath = null)
        {
            try
            {
                pushConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.json")
                    ?? GameConstants.GetGameDataFilePath("SheetsPushConfig.json");
                if (!File.Exists(pushConfigPath))
                    return false;

                var cfg = SheetsPushConfig.Load(pushConfigPath);
                string?[] candidates =
                {
                    sheetsConfig.SpreadsheetEditUrl,
                    sheetsConfig.ActionsSheetUrl,
                    sheetsConfig.WeaponsSheetUrl,
                    sheetsConfig.ModificationsSheetUrl,
                    sheetsConfig.ArmorSheetUrl,
                    sheetsConfig.ClassPresentationSheetUrl
                };

                foreach (string? u in candidates)
                {
                    if (!TryExtractApiSpreadsheetId(u, out string id))
                        continue;
                    if (string.Equals(cfg.SpreadsheetId, id, StringComparison.Ordinal))
                        continue;
                    cfg.SpreadsheetId = id;
                    cfg.Save(pushConfigPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TrySyncSpreadsheetIdToPushConfigFromSheetsConfig: {ex.Message}");
                return false;
            }
        }

        /// <summary>If <paramref name="tabUrl"/> is the same published document as <paramref name="actionsSheetUrl"/> with a different <c>gid</c>, returns that gid for UI display.</summary>
        public static bool TryGetDerivedTabGidForDisplay(string? actionsSheetUrl, string? tabUrl, out string gid)
        {
            gid = "";
            if (string.IsNullOrWhiteSpace(actionsSheetUrl) || string.IsNullOrWhiteSpace(tabUrl))
                return false;
            if (!SamePublishedSheetsUrlIgnoringGid(actionsSheetUrl, tabUrl))
                return false;
            return TryExtractGidParameter(tabUrl, out gid);
        }

        /// <summary>
        /// If <paramref name="actionsSheetUrl"/> parses to an API spreadsheet id and <c>SheetsPushConfig.json</c> exists,
        /// updates <see cref="SheetsPushConfig.SpreadsheetId"/> and saves. Other fields are preserved. Failures are non-fatal.
        /// Only accepts API-compatible ids (not published <c>e/2PACX-…</c>).
        /// </summary>
        public static bool TrySyncSpreadsheetIdToPushConfig(string? actionsSheetUrl, string? pushConfigPath = null)
        {
            try
            {
                pushConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.json")
                    ?? GameConstants.GetGameDataFilePath("SheetsPushConfig.json");
                if (!File.Exists(pushConfigPath))
                    return false;
                if (!TryExtractApiSpreadsheetId(actionsSheetUrl, out string id))
                    return false;

                var cfg = SheetsPushConfig.Load(pushConfigPath);
                if (string.Equals(cfg.SpreadsheetId, id, StringComparison.Ordinal))
                    return false;

                cfg.SpreadsheetId = id;
                cfg.Save(pushConfigPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TrySyncSpreadsheetIdToPushConfig: {ex.Message}");
                return false;
            }
        }
    }
}
