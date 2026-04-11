using System;
using System.IO;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Parses Google Sheets document URLs for spreadsheet IDs used by the Sheets API.
    /// </summary>
    public static class GoogleSheetsUrlHelper
    {
        private const string SpreadsheetsDMarker = "/spreadsheets/d/";

        /// <summary>
        /// Extracts spreadsheet id from a Google Sheets URL (standard <c>/d/{id}/</c> or published <c>/d/e/2PACX-.../</c>).
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
        /// If <paramref name="actionsSheetUrl"/> parses to a spreadsheet id and <c>SheetsPushConfig.json</c> exists,
        /// updates <see cref="SheetsPushConfig.SpreadsheetId"/> and saves. Other fields are preserved. Failures are non-fatal.
        /// </summary>
        public static bool TrySyncSpreadsheetIdToPushConfig(string? actionsSheetUrl, string? pushConfigPath = null)
        {
            try
            {
                pushConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.json")
                    ?? GameConstants.GetGameDataFilePath("SheetsPushConfig.json");
                if (!File.Exists(pushConfigPath))
                    return false;
                if (!TryExtractSpreadsheetId(actionsSheetUrl, out string id))
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
