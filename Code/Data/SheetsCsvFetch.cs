using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RPGGame.Data
{
    /// <summary>
    /// Loads CSV text from a local path or HTTPS URL. Google Sheets document/edit links are normalized to
    /// <c>/export?format=csv&amp;gid=…</c> via <see cref="GoogleSheetsUrlHelper.TryBuildSpreadsheetCsvExportUrl"/>.
    /// </summary>
    public static class SheetsCsvFetch
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; DungeonFighter-SheetsSync/1.0)");
            return c;
        }

        /// <summary>
        /// Reads CSV text from a file path or HTTP(S) URL.
        /// </summary>
        public static async Task<string> ReadCsvTextAsync(string pathOrUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                throw new ArgumentException("Path or URL is required.", nameof(pathOrUrl));

            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                string fetchUrl = pathOrUrl;
                if (GoogleSheetsUrlHelper.TryBuildSpreadsheetCsvExportUrl(pathOrUrl, out string exportUrl))
                    fetchUrl = exportUrl;

                using var response = await Http.GetAsync(
                    fetchUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Unauthorized
                    || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException(
                        "Google Sheets refused anonymous access (HTTP " + (int)response.StatusCode + "). "
                        + "Open the spreadsheet → Share → set General access to \"Anyone with the link\" as Viewer, "
                        + "or use File → Share → Publish to the web and put the published CSV link in SheetsConfig.json.");
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!File.Exists(pathOrUrl))
                throw new FileNotFoundException($"CSV file not found: {pathOrUrl}");

            return await File.ReadAllTextAsync(pathOrUrl, cancellationToken).ConfigureAwait(false);
        }
    }
}
