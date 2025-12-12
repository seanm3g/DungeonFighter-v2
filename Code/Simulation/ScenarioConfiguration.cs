using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.Simulation
{
    /// <summary>
    /// Defines a scenario configuration that can be shared and exported
    /// Includes player build, enemies, and tuning parameters
    /// </summary>
    public class ScenarioConfiguration
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("playerConfig")]
        public PlayerConfig PlayerConfig { get; set; } = new();

        [JsonPropertyName("enemyConfigs")]
        public List<EnemyConfig> EnemyConfigs { get; set; } = new();

        [JsonPropertyName("simulationParams")]
        public SimulationParameters SimulationParams { get; set; } = new();

        [JsonPropertyName("tuningConfig")]
        public Dictionary<string, double> TuningConfig { get; set; } = new();

        public static string ToJson(ScenarioConfiguration config)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(config, options);
        }

        public static ScenarioConfiguration FromJson(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Deserialize<ScenarioConfiguration>(json)
                ?? throw new InvalidOperationException("Failed to deserialize scenario");
        }
    }

    public class PlayerConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Hero";

        [JsonPropertyName("level")]
        public int Level { get; set; } = 1;

        [JsonPropertyName("classType")]
        public string ClassType { get; set; } = "Warrior"; // Warrior, Rogue, Wizard, Barbarian

        [JsonPropertyName("baseStats")]
        public Dictionary<string, int> BaseStats { get; set; } = new()
        {
            { "strength", 10 },
            { "agility", 10 },
            { "technique", 10 }
        };

        [JsonPropertyName("equippedWeapon")]
        public string EquippedWeapon { get; set; } = "Iron Sword";

        [JsonPropertyName("equippedArmor")]
        public Dictionary<string, string> EquippedArmor { get; set; } = new();
    }

    public class EnemyConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; } = 1;

        [JsonPropertyName("enemyType")]
        public string EnemyType { get; set; } = "Goblin";

        [JsonPropertyName("baseStats")]
        public Dictionary<string, int> BaseStats { get; set; } = new();

        [JsonPropertyName("specialization")]
        public string Specialization { get; set; } = "Balanced"; // Strength, Agility, Technique, Balanced
    }

    public class SimulationParameters
    {
        [JsonPropertyName("repetitionsPerEnemy")]
        public int RepetitionsPerEnemy { get; set; } = 5;

        [JsonPropertyName("targetAverageTurns")]
        public int TargetAverageTurns { get; set; } = 10;

        [JsonPropertyName("goodBuildThreshold")]
        public int GoodBuildThreshold { get; set; } = 6;

        [JsonPropertyName("strugglingBuildThreshold")]
        public int StrugglingBuildThreshold { get; set; } = 14;

        [JsonPropertyName("desiredPhaseDistribution")]
        public List<double> DesiredPhaseDistribution { get; set; } = new() { 0.33, 0.33, 0.34 };
    }

    /// <summary>
    /// Manages scenario files and versioning
    /// </summary>
    public class ScenarioManager
    {
        private readonly string _scenarioDirectory;

        public ScenarioManager(string scenarioDirectory = "./Scenarios")
        {
            _scenarioDirectory = scenarioDirectory;
            System.IO.Directory.CreateDirectory(_scenarioDirectory);
        }

        public void SaveScenario(ScenarioConfiguration config, string filename)
        {
            var path = System.IO.Path.Combine(_scenarioDirectory, filename);
            var json = ScenarioConfiguration.ToJson(config);
            System.IO.File.WriteAllText(path, json);
        }

        public ScenarioConfiguration LoadScenario(string filename)
        {
            var path = System.IO.Path.Combine(_scenarioDirectory, filename);
            var json = System.IO.File.ReadAllText(path);
            return ScenarioConfiguration.FromJson(json);
        }

        public List<string> ListScenarios()
        {
            var files = System.IO.Directory.GetFiles(_scenarioDirectory, "*.json");
            return new List<string>(files.Select(f => System.IO.Path.GetFileName(f) ?? string.Empty).Where(f => !string.IsNullOrEmpty(f)));
        }

        public void CreateVersionSnapshot(string scenarioName, string versionLabel)
        {
            var originalPath = System.IO.Path.Combine(_scenarioDirectory, scenarioName);
            var versionPath = System.IO.Path.Combine(_scenarioDirectory, 
                $"{System.IO.Path.GetFileNameWithoutExtension(scenarioName)}_v{versionLabel}.json");
            
            System.IO.File.Copy(originalPath, versionPath, overwrite: true);
        }
    }
}
