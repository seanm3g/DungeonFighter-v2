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

        [JsonPropertyName("weaponsSheetTabName")]
        public string WeaponsSheetTabName { get; set; } = "";

        [JsonPropertyName("modificationsSheetTabName")]
        public string ModificationsSheetTabName { get; set; } = "";

        [JsonPropertyName("armorSheetTabName")]
        public string ArmorSheetTabName { get; set; } = "";

        [JsonPropertyName("classPresentationSheetTabName")]
        public string ClassPresentationSheetTabName { get; set; } = "";

        [JsonPropertyName("enemiesSheetTabName")]
        public string EnemiesSheetTabName { get; set; } = "";

        [JsonPropertyName("environmentsSheetTabName")]
        public string EnvironmentsSheetTabName { get; set; } = "";

        [JsonPropertyName("dungeonsSheetTabName")]
        public string DungeonsSheetTabName { get; set; } = "";

        [JsonPropertyName("statBonusesSheetTabName")]
        public string StatBonusesSheetTabName { get; set; } = "";

        /// <summary>Path to the OAuth 2.0 Desktop client JSON from Google Cloud Console. Relative paths are resolved from the config file directory.</summary>
        [JsonPropertyName("oauthClientSecretsPath")]
        public string OAuthClientSecretsPath { get; set; } = "";

        /// <summary>Directory under GameData (or absolute path) where OAuth refresh tokens are stored. Default: SheetsOAuthToken.</summary>
        [JsonPropertyName("oauthTokenStorePath")]
        public string OAuthTokenStorePath { get; set; } = "SheetsOAuthToken";

        /// <summary>Rows fetched from the top of the sheet to detect context + label headers (min 2).</summary>
        [JsonPropertyName("previewRowCount")]
        public int PreviewRowCount { get; set; } = 5;

        public const string DefaultWeaponsSheetTabName = "WEAPONS";
        public const string DefaultModificationsSheetTabName = "MODIFICATIONS";
        public const string DefaultArmorSheetTabName = "ARMOR";
        public const string DefaultClassPresentationSheetTabName = "CLASSES";
        public const string DefaultEnemiesSheetTabName = "ENEMIES";
        public const string DefaultEnvironmentsSheetTabName = "ENVIRONMENTS";

        public const string DefaultDungeonsSheetTabName = "DUNGEONS";

        public const string DefaultStatBonusesSheetTabName = "SUFFIXES";

        /// <summary>
        /// When <b>all</b> optional tab names (weapons / modifications / armor / classes / enemies / environments / dungeons / stat bonuses) are blank, assigns the
        /// conventional names from <c>SheetsPushConfig.template.json</c> so a first push can populate those tabs.
        /// </summary>
        /// <returns>True if defaults were applied.</returns>
        public bool ApplyDefaultOptionalSheetTabNamesWhenAllUnset()
        {
            if (!string.IsNullOrWhiteSpace(WeaponsSheetTabName)
                || !string.IsNullOrWhiteSpace(ModificationsSheetTabName)
                || !string.IsNullOrWhiteSpace(ArmorSheetTabName)
                || !string.IsNullOrWhiteSpace(ClassPresentationSheetTabName)
                || !string.IsNullOrWhiteSpace(EnemiesSheetTabName)
                || !string.IsNullOrWhiteSpace(EnvironmentsSheetTabName)
                || !string.IsNullOrWhiteSpace(DungeonsSheetTabName)
                || !string.IsNullOrWhiteSpace(StatBonusesSheetTabName))
                return false;

            WeaponsSheetTabName = DefaultWeaponsSheetTabName;
            ModificationsSheetTabName = DefaultModificationsSheetTabName;
            ArmorSheetTabName = DefaultArmorSheetTabName;
            ClassPresentationSheetTabName = DefaultClassPresentationSheetTabName;
            EnemiesSheetTabName = DefaultEnemiesSheetTabName;
            EnvironmentsSheetTabName = DefaultEnvironmentsSheetTabName;
            DungeonsSheetTabName = DefaultDungeonsSheetTabName;
            StatBonusesSheetTabName = DefaultStatBonusesSheetTabName;
            return true;
        }

        /// <summary>
        /// Fills <see cref="EnemiesSheetTabName"/> / <see cref="EnvironmentsSheetTabName"/> when they are still blank.
        /// Older <c>SheetsPushConfig.json</c> files often set weapons/armor/etc. but omitted these keys; without tab names,
        /// <see cref="GameDataSheetsPushService"/> skips pushing <c>Enemies.json</c> and <c>Rooms.json</c>.
        /// </summary>
        /// <returns>True if either field was updated.</returns>
        public bool ApplyDefaultEnemiesAndEnvironmentsTabNamesIfUnset()
        {
            bool changed = false;
            if (string.IsNullOrWhiteSpace(EnemiesSheetTabName))
            {
                EnemiesSheetTabName = DefaultEnemiesSheetTabName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(EnvironmentsSheetTabName))
            {
                EnvironmentsSheetTabName = DefaultEnvironmentsSheetTabName;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Fills <see cref="DungeonsSheetTabName"/> when still blank (older configs that never listed a DUNGEONS tab).
        /// </summary>
        public bool ApplyDefaultDungeonsTabNameIfUnset()
        {
            if (!string.IsNullOrWhiteSpace(DungeonsSheetTabName))
                return false;
            DungeonsSheetTabName = DefaultDungeonsSheetTabName;
            return true;
        }

        /// <summary>Fills <see cref="StatBonusesSheetTabName"/> when still blank (newer field on older push configs).</summary>
        public bool ApplyDefaultStatBonusesTabNameIfUnset()
        {
            if (!string.IsNullOrWhiteSpace(StatBonusesSheetTabName))
                return false;
            StatBonusesSheetTabName = DefaultStatBonusesSheetTabName;
            return true;
        }

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
            string sid = SpreadsheetId.Trim();
            if (sid.StartsWith("e/", StringComparison.OrdinalIgnoreCase)
                || sid.Contains("PACX", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "SheetsPushConfig: spreadsheetId is a publish-only document key (e/2PACX…). The Sheets API returns 404 for that value. " +
                    "Use the spreadsheet **Edit** link from the browser (…/spreadsheets/d/<long-id>/edit…): set GameData/SheetsConfig.json → spreadsheetEditUrl and save from Settings → Balance Tuning, or paste the real id into spreadsheetId.");
            }
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
