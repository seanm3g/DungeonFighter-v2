using RPGGame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>Load/save <c>Enemies.json</c> from Game Settings (same JSON shape as <see cref="EnemyLoader"/>).</summary>
    public class EnemiesDataService
    {
        private readonly Action<string, bool>? showStatusMessage;

        public EnemiesDataService(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public List<EnemyData> LoadEnemies()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Enemies.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<EnemyData>>(json, options) ?? new List<EnemyData>();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading enemies: {ex.Message}", false);
            }

            return new List<EnemyData>();
        }

        public void SaveEnemies(List<EnemyData> enemies)
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Enemies.json");
                if (filePath != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    string json = JsonSerializer.Serialize(enemies, options);
                    File.WriteAllText(filePath, json);
                }
                else
                {
                    showStatusMessage?.Invoke("Could not find Enemies.json file", false);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving enemies: {ex.Message}", false);
                throw;
            }
        }
    }
}
