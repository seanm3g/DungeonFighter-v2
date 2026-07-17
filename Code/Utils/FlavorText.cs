using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>Authoring form: template text with &lt;category&gt; placeholders (id = dictionary key).</summary>
    public class FlavorFormDefinition
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";
        [JsonPropertyName("template")]
        public string Template { get; set; } = "";
    }

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
        /// <summary>Event key → narrative templates (preserves all JSON keys on round-trip).</summary>
        [JsonPropertyName("combatNarratives")]
        public Dictionary<string, string[]> CombatNarratives { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Authoring forms keyed by id (not yet wired into live game call sites).</summary>
        [JsonPropertyName("forms")]
        public Dictionary<string, FlavorFormDefinition> Forms { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Global shared category lists referenced by form templates as &lt;key&gt;.</summary>
        [JsonPropertyName("categories")]
        public Dictionary<string, string[]> Categories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
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
        [JsonPropertyName("roomContexts")]
        public Dictionary<string, Dictionary<string, string[]>> RoomContexts { get; set; } = new();
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
        private static string? _resolvedPath;
        private static readonly object _lock = new object();
        private static readonly Random _random = new Random();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

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

        /// <summary>Clears the cache and reloads from disk.</summary>
        public static void Reload()
        {
            lock (_lock)
            {
                _data = null;
                _resolvedPath = null;
                LoadData();
            }
        }

        /// <summary>Absolute or relative path used for the last successful load, or best candidate for save.</summary>
        public static string? GetResolvedFilePath()
        {
            if (!string.IsNullOrEmpty(_resolvedPath) && File.Exists(_resolvedPath))
                return _resolvedPath;

            string? existing = GameConstants.TryGetExistingGameDataFilePath(GameConstants.FlavorTextJson);
            if (existing != null)
                return existing;

            return JsonLoader.FindGameDataFile(GameConstants.FlavorTextJson);
        }

        /// <summary>Writes <paramref name="data"/> to FlavorText.json and replaces the live cache.</summary>
        public static void SaveData(FlavorTextData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string? path = GetResolvedFilePath();
            if (string.IsNullOrEmpty(path))
            {
                string? settingsDir = GameConstants.GetSettingsDirectory();
                path = settingsDir != null
                    ? Path.Combine(settingsDir, GameConstants.FlavorTextJson)
                    : Path.Combine("GameData", GameConstants.FlavorTextJson);
            }

            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            NormalizeData(data);
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);

            lock (_lock)
            {
                _resolvedPath = path;
                _data = data;
            }
        }

        private static void LoadData()
        {
            try
            {
                string? foundPath = GameConstants.TryGetExistingGameDataFilePath(GameConstants.FlavorTextJson);
                if (foundPath == null)
                {
                    foreach (string path in new[]
                    {
                        Path.Combine("GameData", GameConstants.FlavorTextJson),
                        Path.Combine("..", "GameData", GameConstants.FlavorTextJson),
                        Path.Combine("..", "..", "GameData", GameConstants.FlavorTextJson),
                        GameConstants.FlavorTextJson
                    })
                    {
                        if (File.Exists(path))
                        {
                            foundPath = path;
                            break;
                        }
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var loaded = JsonSerializer.Deserialize<FlavorTextData>(jsonContent, JsonOptions);
                    NormalizeData(loaded);
                    _data = loaded ?? new FlavorTextData();
                    _resolvedPath = foundPath;
                }
                else
                {
                    UIManager.WriteSystemLine("Warning: FlavorText.json not found in any expected location");
                    _data = new FlavorTextData();
                    _resolvedPath = null;
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Warning: Could not load FlavorText.json: {ex.Message}");
                _data = new FlavorTextData();
                _resolvedPath = null;
            }
        }

        private static void NormalizeData(FlavorTextData? data)
        {
            if (data == null) return;
            data.Names ??= new NamesData();
            data.Items ??= new ItemsData();
            data.Environments ??= new EnvironmentsData();
            data.ClassQualifiers ??= new ClassQualifiersData();
            data.ClassQualifiers.ClassNames ??= new ClassNamesData();
            data.Environments.LocationDescriptions ??= new Dictionary<string, string[]>();
            data.Environments.RoomContexts ??= new Dictionary<string, Dictionary<string, string[]>>();
            if (data.CombatNarratives == null || data.CombatNarratives.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                var source = data.CombatNarratives ?? new Dictionary<string, string[]>();
                data.CombatNarratives = new Dictionary<string, string[]>(source, StringComparer.OrdinalIgnoreCase);
            }
            if (data.Forms == null || data.Forms.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                var source = data.Forms ?? new Dictionary<string, FlavorFormDefinition>();
                data.Forms = new Dictionary<string, FlavorFormDefinition>(source, StringComparer.OrdinalIgnoreCase);
            }
            foreach (var kvp in data.Forms.ToList())
            {
                if (kvp.Value == null)
                    data.Forms[kvp.Key] = new FlavorFormDefinition();
                else
                {
                    kvp.Value.DisplayName ??= "";
                    kvp.Value.Template ??= "";
                    if (string.IsNullOrWhiteSpace(kvp.Value.DisplayName))
                        kvp.Value.DisplayName = kvp.Key;
                }
            }
            if (data.Categories == null || data.Categories.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                var source = data.Categories ?? new Dictionary<string, string[]>();
                data.Categories = new Dictionary<string, string[]>(source, StringComparer.OrdinalIgnoreCase);
            }
            foreach (var key in data.Categories.Keys.ToList())
            {
                var entries = data.Categories[key];
                if (entries == null)
                {
                    data.Categories[key] = Array.Empty<string>();
                    continue;
                }
                data.Categories[key] = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.TrimEnd())
                    .ToArray();
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

            if (data.Environments.LocationDescriptions.TryGetValue("Generic", out string[]? genericDescriptions) && genericDescriptions.Length > 0)
            {
                return GetRandomName(genericDescriptions);
            }

            return "A mysterious location.";
        }


        public static string GetClassQualifier(WeaponType? primaryPath, int classPoints, string? legacyClassTitle = null)
        {
            var data = GetData();
            string[] qualifiers;
            if (primaryPath != null)
            {
                qualifiers = primaryPath.Value switch
                {
                    WeaponType.Mace => data.ClassQualifiers.BarbarianQualifiers,
                    WeaponType.Sword => data.ClassQualifiers.WarriorQualifiers,
                    WeaponType.Dagger => data.ClassQualifiers.RogueQualifiers,
                    WeaponType.Wand => data.ClassQualifiers.WizardQualifiers,
                    _ => data.ClassQualifiers.FighterQualifiers
                };
            }
            else
            {
                var cfg = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
                if (!string.IsNullOrEmpty(legacyClassTitle)
                    && string.Equals(legacyClassTitle.Trim(), cfg.DefaultNoPointsClassName, StringComparison.OrdinalIgnoreCase))
                {
                    qualifiers = data.ClassQualifiers.FighterQualifiers;
                }
                else
                {
                    string lower = (legacyClassTitle ?? "").ToLowerInvariant();
                    if (data.ClassQualifiers.ClassNames.Barbarian.Contains(lower))
                        qualifiers = data.ClassQualifiers.BarbarianQualifiers;
                    else if (data.ClassQualifiers.ClassNames.Warrior.Contains(lower))
                        qualifiers = data.ClassQualifiers.WarriorQualifiers;
                    else if (data.ClassQualifiers.ClassNames.Rogue.Contains(lower))
                        qualifiers = data.ClassQualifiers.RogueQualifiers;
                    else if (data.ClassQualifiers.ClassNames.Wizard.Contains(lower))
                        qualifiers = data.ClassQualifiers.WizardQualifiers;
                    else
                        qualifiers = data.ClassQualifiers.FighterQualifiers;
                }
            }

            if (qualifiers.Length == 0)
                return "";
            int index = classPoints % qualifiers.Length;
            return qualifiers[index];
        }
        public static string GetClassQualifier(string className, int classPoints) =>
            GetClassQualifier(null, classPoints, className);

        public static string GenerateRoomContext(string theme, string roomType)
        {
            var data = GetData();

            if (data.Environments.RoomContexts.TryGetValue(theme, out var themeContexts))
            {
                if (themeContexts.TryGetValue(roomType.ToLower(), out string[]? contexts) && contexts.Length > 0)
                {
                    return GetRandomName(contexts);
                }
            }

            if (data.Environments.RoomContexts.TryGetValue("Generic", out var genericContexts))
            {
                if (genericContexts.TryGetValue(roomType.ToLower(), out string[]? genericContext) && genericContext.Length > 0)
                {
                    return GetRandomName(genericContext);
                }
            }

            return "";
        }
    }
}
