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

        [JsonPropertyName("classActionsSheetTabName")]
        public string ClassActionsSheetTabName { get; set; } = "";

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

        /// <summary>When false, OAuth push skips the Actions tab (preflight will not require it).</summary>
        [JsonPropertyName("pushActionsTab")]
        public bool PushActionsTab { get; set; } = true;

        [JsonPropertyName("pushWeaponsTab")]
        public bool PushWeaponsTab { get; set; } = true;

        [JsonPropertyName("pushModificationsTab")]
        public bool PushModificationsTab { get; set; } = true;

        [JsonPropertyName("pushArmorTab")]
        public bool PushArmorTab { get; set; } = true;

        [JsonPropertyName("pushStatBonusesTab")]
        public bool PushStatBonusesTab { get; set; } = true;

        [JsonPropertyName("pushEnemiesTab")]
        public bool PushEnemiesTab { get; set; } = true;

        [JsonPropertyName("pushEnvironmentsTab")]
        public bool PushEnvironmentsTab { get; set; } = true;

        [JsonPropertyName("pushDungeonsTab")]
        public bool PushDungeonsTab { get; set; } = true;

        [JsonPropertyName("pushClassPresentationTab")]
        public bool PushClassPresentationTab { get; set; } = true;

        [JsonPropertyName("pushClassActionsTab")]
        public bool PushClassActionsTab { get; set; } = true;

        public const string DefaultWeaponsSheetTabName = "WEAPONS";
        public const string DefaultModificationsSheetTabName = "Prefix";
        public const string DefaultArmorSheetTabName = "ARMOR";
        public const string DefaultClassPresentationSheetTabName = "CLASSES";
        public const string DefaultClassActionsSheetTabName = "CLASS ACTIONS";
        public const string DefaultEnemiesSheetTabName = "ENEMIES";
        public const string DefaultEnvironmentsSheetTabName = "ENVIRONMENTS";

        public const string DefaultDungeonsSheetTabName = "DUNGEONS";

        public const string DefaultStatBonusesSheetTabName = "SUFFIXES";

        /// <summary>
        /// When <b>all</b> optional tab names (weapons / modifications / armor / classes / class actions / enemies / environments / dungeons / stat bonuses) are blank, assigns the
        /// conventional names from <c>SheetsPushConfig.template.json</c> so a first push can populate those tabs.
        /// </summary>
        /// <returns>True if defaults were applied.</returns>
        public bool ApplyDefaultOptionalSheetTabNamesWhenAllUnset()
        {
            if (!string.IsNullOrWhiteSpace(WeaponsSheetTabName)
                || !string.IsNullOrWhiteSpace(ModificationsSheetTabName)
                || !string.IsNullOrWhiteSpace(ArmorSheetTabName)
                || !string.IsNullOrWhiteSpace(ClassPresentationSheetTabName)
                || !string.IsNullOrWhiteSpace(ClassActionsSheetTabName)
                || !string.IsNullOrWhiteSpace(EnemiesSheetTabName)
                || !string.IsNullOrWhiteSpace(EnvironmentsSheetTabName)
                || !string.IsNullOrWhiteSpace(DungeonsSheetTabName)
                || !string.IsNullOrWhiteSpace(StatBonusesSheetTabName))
                return false;

            WeaponsSheetTabName = DefaultWeaponsSheetTabName;
            ModificationsSheetTabName = DefaultModificationsSheetTabName;
            ArmorSheetTabName = DefaultArmorSheetTabName;
            ClassPresentationSheetTabName = DefaultClassPresentationSheetTabName;
            ClassActionsSheetTabName = DefaultClassActionsSheetTabName;
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

        /// <summary>Fills <see cref="ClassActionsSheetTabName"/> when still blank (configs created before CLASS ACTIONS push).</summary>
        public bool ApplyDefaultClassActionsTabNameIfUnset()
        {
            if (!string.IsNullOrWhiteSpace(ClassActionsSheetTabName))
                return false;
            ClassActionsSheetTabName = DefaultClassActionsSheetTabName;
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
                    if (config == null)
                        return new SheetsPushConfig();
                    ApplyMissingPushTabDefaults(config, json);
                    ApplyRenamedModificationsSheetTabName(config);
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading SheetsPushConfig from {configPath}: {ex.Message}");
            }

            return new SheetsPushConfig();
        }

        /// <summary>
        /// Legacy <c>SheetsPushConfig.json</c> omits <c>push*Tab</c> keys; <see cref="System.Text.Json"/> leaves <see cref="bool"/> properties <c>false</c>.
        /// Any <b>missing</b> push-tab property in the raw JSON is set to <c>true</c> (prior behavior: push that tab when configured).
        /// Explicit <c>false</c> in JSON is preserved.
        /// </summary>
        internal static void ApplyMissingPushTabDefaults(SheetsPushConfig cfg, string rawJson)
        {
            if (cfg == null)
                return;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                EnableAllPushTabs(cfg);
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return;

                static bool JsonHasPropertyIgnoreCase(JsonElement root, string name)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

                    return false;
                }

                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushActionsTab"))
                    cfg.PushActionsTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushWeaponsTab"))
                    cfg.PushWeaponsTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushModificationsTab"))
                    cfg.PushModificationsTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushArmorTab"))
                    cfg.PushArmorTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushStatBonusesTab"))
                    cfg.PushStatBonusesTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushEnemiesTab"))
                    cfg.PushEnemiesTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushEnvironmentsTab"))
                    cfg.PushEnvironmentsTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushDungeonsTab"))
                    cfg.PushDungeonsTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushClassPresentationTab"))
                    cfg.PushClassPresentationTab = true;
                if (!JsonHasPropertyIgnoreCase(doc.RootElement, "pushClassActionsTab"))
                    cfg.PushClassActionsTab = true;
            }
            catch
            {
                EnableAllPushTabs(cfg);
            }
        }

        /// <summary>
        /// Google Sheet tab was renamed from MODIFICATIONS to Prefix; configs that still use the old default tab title are updated on load.
        /// </summary>
        internal static void ApplyRenamedModificationsSheetTabName(SheetsPushConfig cfg)
        {
            if (cfg == null)
                return;
            string t = cfg.ModificationsSheetTabName?.Trim() ?? "";
            if (t.Equals("MODIFICATIONS", StringComparison.OrdinalIgnoreCase))
                cfg.ModificationsSheetTabName = DefaultModificationsSheetTabName;
        }

        private static void EnableAllPushTabs(SheetsPushConfig cfg)
        {
            cfg.PushActionsTab = true;
            cfg.PushWeaponsTab = true;
            cfg.PushModificationsTab = true;
            cfg.PushArmorTab = true;
            cfg.PushStatBonusesTab = true;
            cfg.PushEnemiesTab = true;
            cfg.PushEnvironmentsTab = true;
            cfg.PushDungeonsTab = true;
            cfg.PushClassPresentationTab = true;
            cfg.PushClassActionsTab = true;
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
