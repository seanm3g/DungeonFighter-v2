using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Pulls Actions plus optional tabs from published CSV URLs in <see cref="SheetsConfig"/>.</summary>
    public static class GameDataSheetsPullService
    {
        private static readonly HttpClient Http = new HttpClient();

        public static async Task PullAllFromSheetsConfigAsync(
            string? sheetsConfigPath = null,
            CancellationToken cancellationToken = default)
        {
            sheetsConfigPath ??= GameConstants.TryGetExistingGameDataFilePath("SheetsConfig.json")
                ?? GameConstants.GetGameDataFilePath("SheetsConfig.json");
            var sc = SheetsConfig.Load(sheetsConfigPath);

            if (!string.IsNullOrWhiteSpace(sc.ActionsSheetUrl))
                await ActionUpdateService.UpdateFromGoogleSheetsAsync(sc.ActionsSheetUrl, null).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(sc.WeaponsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.WeaponsSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.WeaponsJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.WeaponsJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.WeaponsJson);
            }

            if (!string.IsNullOrWhiteSpace(sc.ModificationsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ModificationsSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Modifications);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.ModificationsJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.ModificationsJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.ModificationsJson);
            }

            if (!string.IsNullOrWhiteSpace(sc.ArmorSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ArmorSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.ArmorJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.ArmorJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.ArmorJson);
            }

            if (!string.IsNullOrWhiteSpace(sc.EnemiesSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.EnemiesSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.EnemiesJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.EnemiesJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.EnemiesJson);
            }

            if (!string.IsNullOrWhiteSpace(sc.EnvironmentsSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.EnvironmentsSheetUrl, cancellationToken).ConfigureAwait(false);
                string json = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
                string outPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.RoomsJson)
                    ?? GameConstants.GetGameDataFilePath(GameConstants.RoomsJson);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);
                ClearJsonCacheForGameDataFile(GameConstants.RoomsJson);
            }

            if (!string.IsNullOrWhiteSpace(sc.ClassPresentationSheetUrl))
            {
                string csv = await DownloadCsvAsync(sc.ClassPresentationSheetUrl, cancellationToken).ConfigureAwait(false);
                string tuningPath = GameConfiguration.GetTuningConfigFilePathForWrite();
                ClassPresentationSheetConverter.MergeClassPresentationFromCsvIntoTuningFile(csv, tuningPath);
                GameConfiguration.ResetInstance();
            }
        }

        private static void ClearJsonCacheForGameDataFile(string fileName)
        {
            string? path = JsonLoader.FindGameDataFile(fileName);
            if (path != null)
                JsonLoader.ClearCacheForFile(path);
        }

        private static async Task<string> DownloadCsvAsync(string url, CancellationToken cancellationToken)
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Sheet URL must start with http:// or https://", nameof(url));

            return await Http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
    }
}
