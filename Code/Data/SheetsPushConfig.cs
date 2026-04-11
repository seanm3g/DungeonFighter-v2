using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// OAuth 2.0 client and spreadsheet targets for pushing <see cref="SpreadsheetActionJson"/> rows via the Google Sheets API.
    /// Use <see cref="GameConstants.GetGameDataFilePath"/> with <c>SheetsPushConfig.json</c> (typically gitignored; copy from template).
    /// </summary>
    public class SheetsPushConfig
    {
        [JsonPropertyName("spreadsheetId")]
        public string SpreadsheetId { get; set; } = "";

        /// <summary>Tab name exactly as shown in Google Sheets (e.g. the gid= tab).</summary>
        [JsonPropertyName("actionsSheetTabName")]
        public string ActionsSheetTabName { get; set; } = "Sheet1";

        /// <summary>Path to the OAuth 2.0 Desktop client JSON from Google Cloud Console. Relative paths are resolved from the config file directory.</summary>
        [JsonPropertyName("oauthClientSecretsPath")]
        public string OAuthClientSecretsPath { get; set; } = "";

        /// <summary>Directory under GameData (or absolute path) where OAuth refresh tokens are stored. Default: SheetsOAuthToken.</summary>
        [JsonPropertyName("oauthTokenStorePath")]
        public string OAuthTokenStorePath { get; set; } = "SheetsOAuthToken";

        /// <summary>Rows fetched from the top of the sheet to detect context + label headers (min 2).</summary>
        [JsonPropertyName("previewRowCount")]
        public int PreviewRowCount { get; set; } = 5;

        public static SheetsPushConfig Load(string? configPath = null)
        {
            configPath ??= GameConstants.GetGameDataFilePath("SheetsPushConfig.json");

            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<SheetsPushConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return config ?? new SheetsPushConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading SheetsPushConfig from {configPath}: {ex.Message}");
            }

            return new SheetsPushConfig();
        }

        /// <summary>Writes this config to JSON (indented). Creates parent directory if needed.</summary>
        public void Save(string? configPath = null)
        {
            configPath ??= GameConstants.GetGameDataFilePath("SheetsPushConfig.json");

            try
            {
                string? directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving SheetsPushConfig to {configPath}: {ex.Message}");
                throw;
            }
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SpreadsheetId))
                throw new InvalidOperationException("SheetsPushConfig: spreadsheetId is required.");
            if (string.IsNullOrWhiteSpace(ActionsSheetTabName))
                throw new InvalidOperationException("SheetsPushConfig: actionsSheetTabName is required.");
            if (string.IsNullOrWhiteSpace(OAuthClientSecretsPath))
                throw new InvalidOperationException("SheetsPushConfig: oauthClientSecretsPath is required (OAuth 2.0 Desktop client JSON).");
        }

        /// <summary>Absolute path to the OAuth client secrets JSON file.</summary>
        public string ResolveOAuthClientSecretsPath(string configFilePath)
        {
            string raw = OAuthClientSecretsPath.Trim();
            if (Path.IsPathRooted(raw))
                return Path.GetFullPath(raw);
            string? dir = Path.GetDirectoryName(configFilePath);
            if (string.IsNullOrEmpty(dir))
                return Path.GetFullPath(raw);
            return Path.GetFullPath(Path.Combine(dir, raw));
        }

        /// <summary>Absolute directory for <see cref="Google.Apis.Util.Store.FileDataStore"/> token files.</summary>
        public string ResolveOAuthTokenStoreDirectory(string configFilePath)
        {
            string raw = string.IsNullOrWhiteSpace(OAuthTokenStorePath) ? "SheetsOAuthToken" : OAuthTokenStorePath.Trim();
            if (Path.IsPathRooted(raw))
                return Path.GetFullPath(raw);
            string? dir = Path.GetDirectoryName(configFilePath);
            if (string.IsNullOrEmpty(dir))
                return Path.GetFullPath(raw);
            return Path.GetFullPath(Path.Combine(dir, raw));
        }
    }
}
