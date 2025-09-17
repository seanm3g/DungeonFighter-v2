using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    public class FlavorTextData
    {
        [JsonPropertyName("names")]
        public NamesData Names { get; set; } = new();
        [JsonPropertyName("items")]
        public ItemsData Items { get; set; } = new();
        [JsonPropertyName("environments")]
        public EnvironmentsData Environments { get; set; } = new();
        [JsonPropertyName("classQualifiers")]
        public ClassQualifiersData ClassQualifiers { get; set; } = new();
    }

    public class NamesData
    {
        [JsonPropertyName("characterFirstNames")]
        public string[] CharacterFirstNames { get; set; } = Array.Empty<string>();
        [JsonPropertyName("characterLastNames")]
        public string[] CharacterLastNames { get; set; } = Array.Empty<string>();
        [JsonPropertyName("bossNames")]
        public string[] BossNames { get; set; } = Array.Empty<string>();
    }

    public class ItemsData
    {
        [JsonPropertyName("consumableNames")]
        public string[] ConsumableNames { get; set; } = Array.Empty<string>();
    }

    public class EnvironmentsData
    {
        [JsonPropertyName("locationNames")]
        public string[] LocationNames { get; set; } = Array.Empty<string>();
        [JsonPropertyName("locationDescriptions")]
        public Dictionary<string, string[]> LocationDescriptions { get; set; } = new();
    }

    public class ClassQualifiersData
    {
        [JsonPropertyName("classNames")]
        public ClassNamesData ClassNames { get; set; } = new();
        [JsonPropertyName("barbarianQualifiers")]
        public string[] BarbarianQualifiers { get; set; } = Array.Empty<string>();
        [JsonPropertyName("warriorQualifiers")]
        public string[] WarriorQualifiers { get; set; } = Array.Empty<string>();
        [JsonPropertyName("rogueQualifiers")]
        public string[] RogueQualifiers { get; set; } = Array.Empty<string>();
        [JsonPropertyName("wizardQualifiers")]
        public string[] WizardQualifiers { get; set; } = Array.Empty<string>();
        [JsonPropertyName("fighterQualifiers")]
        public string[] FighterQualifiers { get; set; } = Array.Empty<string>();
    }

    public class ClassNamesData
    {
        [JsonPropertyName("barbarian")]
        public string[] Barbarian { get; set; } = Array.Empty<string>();
        [JsonPropertyName("warrior")]
        public string[] Warrior { get; set; } = Array.Empty<string>();
        [JsonPropertyName("rogue")]
        public string[] Rogue { get; set; } = Array.Empty<string>();
        [JsonPropertyName("wizard")]
        public string[] Wizard { get; set; } = Array.Empty<string>();
        [JsonPropertyName("fighter")]
        public string[] Fighter { get; set; } = Array.Empty<string>();
    }

    public static class FlavorText
    {
        private static FlavorTextData? _data;
        private static readonly object _lock = new object();
        private static readonly Random _random = new Random();

        public static FlavorTextData GetData()
        {
            if (_data == null)
            {
                lock (_lock)
                {
                    if (_data == null)
                    {
                        LoadData();
                    }
                }
            }
            return _data!;
        }

        private static void LoadData()
        {
            try
            {
                // Try multiple possible paths for the JSON file
                string[] possiblePaths = {
                    Path.Combine("GameData", "FlavorText.json"),
                    Path.Combine("..", "GameData", "FlavorText.json"),
                    Path.Combine("..", "..", "GameData", "FlavorText.json"),
                    "FlavorText.json"
                };
                
                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }
                
                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    _data = JsonSerializer.Deserialize<FlavorTextData>(jsonContent);
                }
                else
                {
                    Console.WriteLine("Warning: FlavorText.json not found in any expected location");
                    _data = new FlavorTextData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load FlavorText.json: {ex.Message}");
                _data = new FlavorTextData();
            }
        }

        // Helper Methods
        public static string GetRandomName(string[] nameList)
        {
            if (nameList == null || nameList.Length == 0)
                return "Unknown";
            return nameList[_random.Next(nameList.Length)];
        }

        public static string GenerateCharacterName()
        {
            var data = GetData();
            return $"{GetRandomName(data.Names.CharacterFirstNames)} {GetRandomName(data.Names.CharacterLastNames)}";
        }

        public static string GenerateLocationName()
        {
            var data = GetData();
            return GetRandomName(data.Environments.LocationNames);
        }

        public static string GenerateLocationDescription()
        {
            var data = GetData();
            // Get a random theme and then a random description from that theme
            var themes = data.Environments.LocationDescriptions.Keys.ToArray();
            if (themes.Length == 0) return "A mysterious location.";
            
            string randomTheme = GetRandomName(themes);
            return GenerateLocationDescription(randomTheme);
        }

        public static string GenerateLocationDescription(string theme)
        {
            var data = GetData();
            if (data.Environments.LocationDescriptions.TryGetValue(theme, out string[]? descriptions) && descriptions.Length > 0)
            {
                return GetRandomName(descriptions);
            }
            
            // Fallback to generic descriptions if theme not found
            if (data.Environments.LocationDescriptions.TryGetValue("Generic", out string[]? genericDescriptions) && genericDescriptions.Length > 0)
            {
                return GetRandomName(genericDescriptions);
            }
            
            return "A mysterious location.";
        }

        public static string GetClassQualifier(string className, int classPoints)
        {
            var data = GetData();
            
            // Determine which qualifier array to use based on class
            string[] qualifiers;
            string lowerClassName = className.ToLower();
            
            if (data.ClassQualifiers.ClassNames.Barbarian.Contains(lowerClassName))
            {
                qualifiers = data.ClassQualifiers.BarbarianQualifiers;
            }
            else if (data.ClassQualifiers.ClassNames.Warrior.Contains(lowerClassName))
            {
                qualifiers = data.ClassQualifiers.WarriorQualifiers;
            }
            else if (data.ClassQualifiers.ClassNames.Rogue.Contains(lowerClassName))
            {
                qualifiers = data.ClassQualifiers.RogueQualifiers;
            }
            else if (data.ClassQualifiers.ClassNames.Wizard.Contains(lowerClassName))
            {
                qualifiers = data.ClassQualifiers.WizardQualifiers;
            }
            else
            {
                qualifiers = data.ClassQualifiers.FighterQualifiers;
            }

            // Select qualifier based on class points for variety
            if (qualifiers.Length == 0)
            {
                return ""; // Return empty string if no qualifiers available
            }
            
            int index = classPoints % qualifiers.Length;
            return qualifiers[index];
        }
    }
} 