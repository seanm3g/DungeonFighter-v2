using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Pulls Actions plus optional tabs from published CSV URLs in <see cref="SheetsConfig"/> (weapons, mods, armor, stat bonuses / suffixes, consumables, enemies, environments, dungeons, classes, class actions).</summary>
    public static class GameDataSheetsPullService
    {
        public static async Task PullAllFromSheetsConfigAsync(
            string? sheetsConfigPath = null,
            SheetsPushConfig? tabSelection = null,
            CancellationToken cancellationToken = default)
        {
            sheetsConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsConfig.json")
                ?? GameConstants.GetGameDataFilePath("SheetsConfig.json");
            var sc = SheetsConfig.Load(sheetsConfigPath);
            var tabFlags = tabSelection ?? SheetsPushConfig.LoadTabFilterOrAllEnabled();

            if (tabFlags.PushActionsTab && !string.IsNullOrWhiteSpace(sc.ActionsSheetUrl))
                await ActionUpdateService.UpdateFromGoogleSheetsAsync(sc.ActionsSheetUrl, null).ConfigureAwait(false);

            if (tabFlags.PushWeaponsTab && !string.IsNullOrWhiteSpace(sc.WeaponsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.WeaponsSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.WeaponsJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.WeaponsJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.WeaponsJson);
            }

            if (tabFlags.PushModificationsTab && !string.IsNullOrWhiteSpace(sc.ModificationsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ModificationsSheetUrl, cancellationToken).ConfigureAwait(false);
                string merged = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Modifications);
                var (coreJson, prefixMqJson) = JsonArraySheetConverter.SplitModificationsMergedJson(merged);
                string modsPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.ModificationsJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.ModificationsJson);
                await File.WriteAllTextAsync(modsPath, coreJson, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.ModificationsJson);
                string pmqPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.PrefixMaterialQualityJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.PrefixMaterialQualityJson);
                await File.WriteAllTextAsync(pmqPath, prefixMqJson, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.PrefixMaterialQualityJson);
            }

            if (tabFlags.PushArmorTab && !string.IsNullOrWhiteSpace(sc.ArmorSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ArmorSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.ArmorJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.ArmorJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.ArmorJson);
            }

            if (tabFlags.PushStatBonusesTab && !string.IsNullOrWhiteSpace(sc.StatBonusesSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.StatBonusesSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.StatBonusesJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.StatBonusesJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.StatBonusesJson);
            }

            if (tabFlags.PushConsumablesTab && !string.IsNullOrWhiteSpace(sc.ConsumablesSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ConsumablesSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Consumables);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.ConsumablesJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.ConsumablesJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.ConsumablesJson);
            }

            if (tabFlags.PushEnemiesTab && !string.IsNullOrWhiteSpace(sc.EnemiesSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.EnemiesSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.EnemiesJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.EnemiesJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.EnemiesJson);
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    Console.WriteLine($"✓ Enemies pulled: {doc.RootElement.GetArrayLength()} row(s) → {outPath}");
                }
                catch
                {
                    Console.WriteLine($"✓ Enemies pulled → {outPath}");
                }
            }

            if (tabFlags.PushEnvironmentsTab && !string.IsNullOrWhiteSpace(sc.EnvironmentsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.EnvironmentsSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.RoomsJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.RoomsJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.RoomsJson);
            }

            if (tabFlags.PushDungeonsTab && !string.IsNullOrWhiteSpace(sc.DungeonsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.DungeonsSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Dungeons);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.DungeonsJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.DungeonsJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.DungeonsJson);
            }

            if (tabFlags.PushClassPresentationTab && !string.IsNullOrWhiteSpace(sc.ClassPresentationSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ClassPresentationSheetUrl, cancellationToken).ConfigureAwait(false);
                string tuningPath = GameConfiguration.GetTuningConfigFilePathForWrite();
                ClassPresentationSheetConverter.MergeClassPresentationFromCsvIntoTuningFile(csv, tuningPath);
                GameConfiguration.ResetInstance();
            }

            if (tabFlags.PushClassActionsTab && !string.IsNullOrWhiteSpace(sc.ClassActionsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ClassActionsSheetUrl, cancellationToken).ConfigureAwait(false);
                var pres = ClassActionsSheetConverter.LoadClassPresentationForImport();
                var unlockCfg = ClassActionsSheetConverter.ParseCsvToConfig(csv, pres);
                if (unlockCfg.Rules.Count == 0)
                {
                    Console.WriteLine(
                        "Warning: CLASS ACTIONS sheet produced no rules (check TIER/CLASS/ACTIONS headers and tier names). ClassActions.json was not updated.");
                }
                else
                {
                    string json = JsonSerializer.Serialize(unlockCfg, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.ClassActionsJson)
                        ?? GameConstants.GetGameDataFilePath(GameConstants.ClassActionsJson);
                    await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                    ClearJsonCacheForGameDataFile(GameConstants.ClassActionsJson);
                    GameConfiguration.ResetInstance();
                }
            }

            if (tabFlags.PushFlavorTab)
            {
                Console.WriteLine(
                    "Skipped flavor tab on PULL (push-only; FlavorText.json is not overwritten from Google Sheets).");
            }

            ReloadRuntimeCachesAfterPull();
        }

        /// <summary>
        /// Discards static loot/enemy/room caches so the next game use reads updated JSON from disk.
        /// Without this, <see cref="LootGenerator"/> keeps the pre-pull <see cref="LootDataCache"/> until restart.
        /// </summary>
        public static void ReloadRuntimeCachesAfterPull()
        {
            try
            {
                LootGenerator.Initialize();
                EnemyLoader.LoadEnemies(validate: false);
                RoomLoader.LoadRooms();
                RoomSearchConsumableCatalog.Reload();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: could not refresh in-memory data after sheet pull: " + ex.Message);
            }
        }

        private static void ClearJsonCacheForGameDataFile(string fileName)
        {
            string? path = JsonLoader.FindGameDataFile(fileName);
            if (path != null)
                JsonLoader.ClearCacheForFile(path);
        }

        private static Task<string> DownloadCsvAsync(string url, CancellationToken cancellationToken)
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Sheet URL must start with http:// or https://", nameof(url));

            return SheetsCsvFetch.ReadCsvTextAsync(url, cancellationToken);
        }
    }
}
