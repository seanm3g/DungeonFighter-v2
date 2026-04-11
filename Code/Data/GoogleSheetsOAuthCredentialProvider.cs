using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;

namespace RPGGame.Data
{
    /// <summary>
    /// Desktop OAuth 2.0 flow for Google Sheets API: opens a browser on first use, then stores a refresh token on disk.
    /// </summary>
    public static class GoogleSheetsOAuthCredentialProvider
    {
        /// <summary>Stable id for <see cref="FileDataStore"/> so the same refresh token is reused.</summary>
        public const string DefaultUserId = "actions-push";

        /// <summary>
        /// Authorizes read/write access to spreadsheets for the signed-in Google user.
        /// </summary>
        /// <param name="clientSecretsPath">Path to the OAuth 2.0 Desktop client JSON from Google Cloud Console.</param>
        /// <param name="tokenStoreDirectory">Absolute directory where refresh tokens are persisted.</param>
        public static async Task<UserCredential> AuthorizeAsync(
            string clientSecretsPath,
            string tokenStoreDirectory,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(clientSecretsPath))
                throw new FileNotFoundException("OAuth client secrets file not found. Download the Desktop client JSON from Google Cloud Console.", clientSecretsPath);

            Directory.CreateDirectory(tokenStoreDirectory);

            await using var stream = new FileStream(clientSecretsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                new[] { SheetsService.Scope.Spreadsheets },
                DefaultUserId,
                cancellationToken,
                new FileDataStore(tokenStoreDirectory, fullPath: true)).ConfigureAwait(false);
        }
    }
}
