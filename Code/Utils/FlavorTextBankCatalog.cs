using System.Text;
using System.Text.RegularExpressions;

namespace RPGGame
{
    /// <summary>Descriptor for one editable string bank inside FlavorText.json.</summary>
    public sealed class FlavorTextBankInfo
    {
        public FlavorTextBankInfo(string pathId, string section, string displayLabel, bool isTemplateBank)
        {
            PathId = pathId;
            Section = section;
            DisplayLabel = displayLabel;
            IsTemplateBank = isTemplateBank;
        }

        public string PathId { get; }
        public string Section { get; }
        public string DisplayLabel { get; }
        /// <summary>True for phrase/template banks (combat narratives, location descriptions, room contexts).</summary>
        public bool IsTemplateBank { get; }

        public override string ToString() => DisplayLabel;
    }

    /// <summary>Generate modes for the Flavor Text settings lab.</summary>
    public enum FlavorTextGenerateMode
    {
        CharacterName,
        LocationName,
        LocationDescription,
        RoomContext,
        ClassQualifier,
        SelectedBank,
        CombatNarrative
    }

    /// <summary>
    /// Flattens FlavorTextData into editable banks and sample generators for the settings editor.
    /// </summary>
    public static class FlavorTextBankCatalog
    {
        private static readonly Regex PathSegment = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        public static readonly IReadOnlyDictionary<string, string> SamplePlaceholders =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = "Aric",
                ["player"] = "Aric Stormrider",
                ["enemy"] = "Goblin Scout",
                ["effect"] = "falling rocks crash down"
            };

        public static IReadOnlyList<FlavorTextBankInfo> EnumerateBanks(FlavorTextData? data = null)
        {
            data ??= FlavorText.GetData();
            var list = new List<FlavorTextBankInfo>();

            list.Add(new("names.characterFirstNames", "Names", "Names / First names", false));
            list.Add(new("names.characterLastNames", "Names", "Names / Last names", false));
            list.Add(new("names.bossNames", "Names", "Names / Boss names", false));
            list.Add(new("items.consumableNames", "Items", "Items / Consumable names", false));
            list.Add(new("environments.locationNames", "Environments", "Environments / Location names", false));

            foreach (var theme in data.Environments.LocationDescriptions.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(new(
                    $"environments.locationDescriptions.{theme}",
                    "Environments",
                    $"Environments / Location descriptions / {theme}",
                    true));
            }

            foreach (var theme in data.Environments.RoomContexts.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                var roomMap = data.Environments.RoomContexts[theme];
                foreach (var roomType in roomMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(new(
                        $"environments.roomContexts.{theme}.{roomType}",
                        "Environments",
                        $"Environments / Room contexts / {theme} / {roomType}",
                        true));
                }
            }

            list.Add(new("classQualifiers.classNames.barbarian", "Class Qualifiers", "Class Qualifiers / Class names / Barbarian", false));
            list.Add(new("classQualifiers.classNames.warrior", "Class Qualifiers", "Class Qualifiers / Class names / Warrior", false));
            list.Add(new("classQualifiers.classNames.rogue", "Class Qualifiers", "Class Qualifiers / Class names / Rogue", false));
            list.Add(new("classQualifiers.classNames.wizard", "Class Qualifiers", "Class Qualifiers / Class names / Wizard", false));
            list.Add(new("classQualifiers.classNames.fighter", "Class Qualifiers", "Class Qualifiers / Class names / Fighter", false));
            list.Add(new("classQualifiers.barbarianQualifiers", "Class Qualifiers", "Class Qualifiers / Barbarian", false));
            list.Add(new("classQualifiers.warriorQualifiers", "Class Qualifiers", "Class Qualifiers / Warrior", false));
            list.Add(new("classQualifiers.rogueQualifiers", "Class Qualifiers", "Class Qualifiers / Rogue", false));
            list.Add(new("classQualifiers.wizardQualifiers", "Class Qualifiers", "Class Qualifiers / Wizard", false));
            list.Add(new("classQualifiers.fighterQualifiers", "Class Qualifiers", "Class Qualifiers / Fighter", false));

            foreach (var key in data.CombatNarratives.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(new(
                    $"combatNarratives.{key}",
                    "Combat Narratives",
                    $"Combat Narratives / {key}",
                    true));
            }

            return list;
        }

        public static string[] GetBank(FlavorTextData data, string pathId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(pathId)) return Array.Empty<string>();

            if (TryGetFlatArray(data, pathId, out var arr))
                return arr ?? Array.Empty<string>();

            var parts = pathId.Split('.');
            if (parts.Length == 3
                && parts[0] == "environments"
                && parts[1] == "locationDescriptions"
                && data.Environments.LocationDescriptions.TryGetValue(parts[2], out var desc))
            {
                return desc ?? Array.Empty<string>();
            }

            if (parts.Length == 4
                && parts[0] == "environments"
                && parts[1] == "roomContexts"
                && data.Environments.RoomContexts.TryGetValue(parts[2], out var themeMap)
                && themeMap.TryGetValue(parts[3], out var contexts))
            {
                return contexts ?? Array.Empty<string>();
            }

            if (parts.Length == 2
                && parts[0] == "combatNarratives"
                && data.CombatNarratives.TryGetValue(parts[1], out var narratives))
            {
                return narratives ?? Array.Empty<string>();
            }

            return Array.Empty<string>();
        }

        public static bool SetBank(FlavorTextData data, string pathId, string[] entries)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(pathId)) return false;
            entries ??= Array.Empty<string>();

            if (TrySetFlatArray(data, pathId, entries))
                return true;

            var parts = pathId.Split('.');
            if (parts.Length == 3
                && parts[0] == "environments"
                && parts[1] == "locationDescriptions"
                && IsSafeSegment(parts[2]))
            {
                data.Environments.LocationDescriptions[parts[2]] = entries;
                return true;
            }

            if (parts.Length == 4
                && parts[0] == "environments"
                && parts[1] == "roomContexts"
                && IsSafeSegment(parts[2])
                && IsSafeSegment(parts[3]))
            {
                if (!data.Environments.RoomContexts.TryGetValue(parts[2], out var themeMap))
                {
                    themeMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                    data.Environments.RoomContexts[parts[2]] = themeMap;
                }
                themeMap[parts[3]] = entries;
                return true;
            }

            if (parts.Length == 2
                && parts[0] == "combatNarratives"
                && IsSafeSegment(parts[1]))
            {
                data.CombatNarratives[parts[1]] = entries;
                return true;
            }

            return false;
        }

        public static string EntriesToMultiline(string[] entries)
        {
            if (entries == null || entries.Length == 0) return "";
            return string.Join(System.Environment.NewLine, entries);
        }

        public static string[] MultilineToEntries(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0)
                .ToArray();
        }

        public static IReadOnlyList<string> ListLocationThemes(FlavorTextData? data = null)
        {
            data ??= FlavorText.GetData();
            return data.Environments.LocationDescriptions.Keys
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<string> ListRoomTypes(FlavorTextData? data = null, string? theme = null)
        {
            data ??= FlavorText.GetData();
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in data.Environments.RoomContexts)
            {
                if (!string.IsNullOrEmpty(theme)
                    && !string.Equals(kvp.Key, theme, StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var roomType in kvp.Value.Keys)
                    types.Add(roomType);
            }
            return types.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static IReadOnlyList<string> ListCombatEventKeys(FlavorTextData? data = null)
        {
            data ??= FlavorText.GetData();
            return data.CombatNarratives.Keys
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string GenerateSample(
            FlavorTextGenerateMode mode,
            FlavorTextData? data = null,
            string? bankPathId = null,
            string? theme = null,
            string? roomType = null,
            string? combatEventKey = null,
            WeaponType? classPath = null,
            int classPoints = 0)
        {
            data ??= FlavorText.GetData();
            return mode switch
            {
                FlavorTextGenerateMode.CharacterName => BuildCharacterName(data),
                FlavorTextGenerateMode.LocationName => FlavorText.GetRandomName(data.Environments.LocationNames),
                FlavorTextGenerateMode.LocationDescription => GenerateLocationDescriptionSample(data, theme),
                FlavorTextGenerateMode.RoomContext => GenerateRoomContextSample(data, theme, roomType),
                FlavorTextGenerateMode.ClassQualifier => FlavorText.GetClassQualifier(classPath, classPoints),
                FlavorTextGenerateMode.SelectedBank => GenerateFromBank(data, bankPathId),
                FlavorTextGenerateMode.CombatNarrative => GenerateCombatNarrativeSample(data, combatEventKey),
                _ => ""
            };
        }

        public static string GenerateMany(
            FlavorTextGenerateMode mode,
            int count,
            FlavorTextData? data = null,
            string? bankPathId = null,
            string? theme = null,
            string? roomType = null,
            string? combatEventKey = null,
            WeaponType? classPath = null,
            int classPoints = 0)
        {
            count = Math.Clamp(count, 1, 50);
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append(GenerateSample(mode, data, bankPathId, theme, roomType, combatEventKey, classPath, classPoints));
            }
            return sb.ToString();
        }

        private static string BuildCharacterName(FlavorTextData data)
        {
            return $"{FlavorText.GetRandomName(data.Names.CharacterFirstNames)} {FlavorText.GetRandomName(data.Names.CharacterLastNames)}";
        }

        private static string GenerateLocationDescriptionSample(FlavorTextData data, string? theme)
        {
            if (!string.IsNullOrWhiteSpace(theme)
                && data.Environments.LocationDescriptions.TryGetValue(theme, out var desc)
                && desc.Length > 0)
            {
                return FlavorText.GetRandomName(desc);
            }
            var themes = data.Environments.LocationDescriptions.Keys.ToArray();
            if (themes.Length == 0) return "A mysterious location.";
            string pick = FlavorText.GetRandomName(themes);
            return data.Environments.LocationDescriptions.TryGetValue(pick, out var arr) && arr.Length > 0
                ? FlavorText.GetRandomName(arr)
                : "A mysterious location.";
        }

        private static string GenerateRoomContextSample(FlavorTextData data, string? theme, string? roomType)
        {
            theme ??= data.Environments.RoomContexts.Keys.FirstOrDefault() ?? "Generic";
            roomType ??= ListRoomTypes(data, theme).FirstOrDefault() ?? "chamber";

            if (data.Environments.RoomContexts.TryGetValue(theme, out var map)
                && map.TryGetValue(roomType, out var contexts)
                && contexts.Length > 0)
            {
                return FlavorText.GetRandomName(contexts);
            }

            return FlavorText.GenerateRoomContext(theme, roomType);
        }

        private static string GenerateFromBank(FlavorTextData data, string? bankPathId)
        {
            if (string.IsNullOrWhiteSpace(bankPathId)) return "(no bank selected)";
            var entries = GetBank(data, bankPathId);
            if (entries.Length == 0) return "(empty bank)";
            string pick = FlavorText.GetRandomName(entries);
            if (bankPathId.StartsWith("combatNarratives.", StringComparison.OrdinalIgnoreCase))
                return FillSamplePlaceholders(pick);
            return pick;
        }

        private static string GenerateCombatNarrativeSample(FlavorTextData data, string? eventKey)
        {
            eventKey ??= data.CombatNarratives.Keys.FirstOrDefault() ?? "criticalHit";
            if (!data.CombatNarratives.TryGetValue(eventKey, out var narratives) || narratives.Length == 0)
            {
                var provider = new NarrativeTextProvider();
                return FillSamplePlaceholders(provider.GetFallbackNarrative(eventKey));
            }
            return FillSamplePlaceholders(FlavorText.GetRandomName(narratives));
        }

        public static string FillSamplePlaceholders(string template)
        {
            var provider = new NarrativeTextProvider();
            return provider.ReplacePlaceholders(template, new Dictionary<string, string>(SamplePlaceholders));
        }

        private static readonly Regex CategoryRefPattern = new(@"<([A-Za-z0-9_]+)>", RegexOptions.Compiled);

        /// <summary>Unique category keys referenced by &lt;key&gt; tags in a form template (order preserved).</summary>
        public static IReadOnlyList<string> ExtractCategoryRefs(string? template)
        {
            if (string.IsNullOrEmpty(template)) return Array.Empty<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            foreach (Match match in CategoryRefPattern.Matches(template))
            {
                string key = match.Groups[1].Value;
                if (seen.Add(key))
                    list.Add(key);
            }
            return list;
        }

        /// <summary>
        /// Ensures every &lt;category&gt; ref in <paramref name="template"/> exists in <paramref name="data"/>.Categories
        /// (creates empty lists for missing keys). Returns the keys that were created.
        /// </summary>
        public static IReadOnlyList<string> EnsureCategoriesForTemplate(FlavorTextData data, string? template)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Categories ??= new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (data.Categories.Comparer != StringComparer.OrdinalIgnoreCase)
                data.Categories = new Dictionary<string, string[]>(data.Categories, StringComparer.OrdinalIgnoreCase);

            var created = new List<string>();
            foreach (string key in ExtractCategoryRefs(template))
            {
                if (!data.Categories.ContainsKey(key))
                {
                    data.Categories[key] = Array.Empty<string>();
                    created.Add(key);
                }
            }
            return created;
        }

        public static string[] GetCategory(FlavorTextData data, string categoryKey)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(categoryKey)) return Array.Empty<string>();
            return data.Categories != null
                   && data.Categories.TryGetValue(categoryKey, out var entries)
                ? entries ?? Array.Empty<string>()
                : Array.Empty<string>();
        }

        public static bool SetCategory(FlavorTextData data, string categoryKey, string[] entries)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(categoryKey) || !IsSafeSegment(categoryKey)) return false;
            data.Categories ??= new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (data.Categories.Comparer != StringComparer.OrdinalIgnoreCase)
                data.Categories = new Dictionary<string, string[]>(data.Categories, StringComparer.OrdinalIgnoreCase);
            data.Categories[categoryKey] = entries ?? Array.Empty<string>();
            return true;
        }

        /// <summary>
        /// Replaces each &lt;category&gt; with a random entry from the matching global list.
        /// Missing or empty categories leave the &lt;tag&gt; intact. Then applies sample {name}/{player}/… fills.
        /// </summary>
        public static string ExpandFormTemplate(
            string? template,
            IReadOnlyDictionary<string, string[]>? categories,
            Random? random = null)
        {
            if (string.IsNullOrEmpty(template)) return "";
            categories ??= new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            random ??= new Random();

            string expanded = CategoryRefPattern.Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                if (!TryGetCategoryEntries(categories, key, out var entries) || entries.Length == 0)
                    return match.Value;
                return entries[random.Next(entries.Length)];
            });

            return FillSamplePlaceholders(expanded);
        }

        public static string GenerateFormSample(FlavorTextData data, string formId, Random? random = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(formId)
                || data.Forms == null
                || !data.Forms.TryGetValue(formId, out var form)
                || form == null)
            {
                return "(no form selected)";
            }
            return ExpandFormTemplate(form.Template, data.Categories, random);
        }

        public static string GenerateFormMany(FlavorTextData data, string formId, int count, Random? random = null)
        {
            count = Math.Clamp(count, 1, 50);
            random ??= new Random();
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append(GenerateFormSample(data, formId, random));
            }
            return sb.ToString();
        }

        /// <summary>Valid form/category id: letters, digits, underscore (max 64).</summary>
        public static bool TrySanitizeId(string? raw, out string id, out string? error)
        {
            id = "";
            error = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Id is required.";
                return false;
            }
            string trimmed = raw.Trim().Replace(' ', '_').Replace('-', '_');
            trimmed = Regex.Replace(trimmed, "_+", "_").Trim('_');
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 64 || !PathSegment.IsMatch(trimmed))
            {
                error = "Id may use letters, numbers, and underscores (max 64 characters).";
                return false;
            }
            id = trimmed;
            return true;
        }

        private static bool TryGetCategoryEntries(
            IReadOnlyDictionary<string, string[]> categories,
            string key,
            out string[] entries)
        {
            entries = Array.Empty<string>();
            if (categories.TryGetValue(key, out var direct) && direct != null)
            {
                entries = direct;
                return true;
            }
            // Case-insensitive fallback when comparer is ordinal.
            foreach (var kvp in categories)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    entries = kvp.Value ?? Array.Empty<string>();
                    return true;
                }
            }
            return false;
        }

        private static bool IsSafeSegment(string segment) =>
            !string.IsNullOrEmpty(segment) && PathSegment.IsMatch(segment);

        private static bool TryGetFlatArray(FlavorTextData data, string pathId, out string[]? arr)
        {
            arr = pathId switch
            {
                "names.characterFirstNames" => data.Names.CharacterFirstNames,
                "names.characterLastNames" => data.Names.CharacterLastNames,
                "names.bossNames" => data.Names.BossNames,
                "items.consumableNames" => data.Items.ConsumableNames,
                "environments.locationNames" => data.Environments.LocationNames,
                "classQualifiers.classNames.barbarian" => data.ClassQualifiers.ClassNames.Barbarian,
                "classQualifiers.classNames.warrior" => data.ClassQualifiers.ClassNames.Warrior,
                "classQualifiers.classNames.rogue" => data.ClassQualifiers.ClassNames.Rogue,
                "classQualifiers.classNames.wizard" => data.ClassQualifiers.ClassNames.Wizard,
                "classQualifiers.classNames.fighter" => data.ClassQualifiers.ClassNames.Fighter,
                "classQualifiers.barbarianQualifiers" => data.ClassQualifiers.BarbarianQualifiers,
                "classQualifiers.warriorQualifiers" => data.ClassQualifiers.WarriorQualifiers,
                "classQualifiers.rogueQualifiers" => data.ClassQualifiers.RogueQualifiers,
                "classQualifiers.wizardQualifiers" => data.ClassQualifiers.WizardQualifiers,
                "classQualifiers.fighterQualifiers" => data.ClassQualifiers.FighterQualifiers,
                _ => null
            };
            return arr != null || pathId is
                "names.characterFirstNames" or "names.characterLastNames" or "names.bossNames"
                or "items.consumableNames" or "environments.locationNames"
                or "classQualifiers.classNames.barbarian" or "classQualifiers.classNames.warrior"
                or "classQualifiers.classNames.rogue" or "classQualifiers.classNames.wizard"
                or "classQualifiers.classNames.fighter"
                or "classQualifiers.barbarianQualifiers" or "classQualifiers.warriorQualifiers"
                or "classQualifiers.rogueQualifiers" or "classQualifiers.wizardQualifiers"
                or "classQualifiers.fighterQualifiers";
        }

        private static bool TrySetFlatArray(FlavorTextData data, string pathId, string[] entries)
        {
            switch (pathId)
            {
                case "names.characterFirstNames": data.Names.CharacterFirstNames = entries; return true;
                case "names.characterLastNames": data.Names.CharacterLastNames = entries; return true;
                case "names.bossNames": data.Names.BossNames = entries; return true;
                case "items.consumableNames": data.Items.ConsumableNames = entries; return true;
                case "environments.locationNames": data.Environments.LocationNames = entries; return true;
                case "classQualifiers.classNames.barbarian": data.ClassQualifiers.ClassNames.Barbarian = entries; return true;
                case "classQualifiers.classNames.warrior": data.ClassQualifiers.ClassNames.Warrior = entries; return true;
                case "classQualifiers.classNames.rogue": data.ClassQualifiers.ClassNames.Rogue = entries; return true;
                case "classQualifiers.classNames.wizard": data.ClassQualifiers.ClassNames.Wizard = entries; return true;
                case "classQualifiers.classNames.fighter": data.ClassQualifiers.ClassNames.Fighter = entries; return true;
                case "classQualifiers.barbarianQualifiers": data.ClassQualifiers.BarbarianQualifiers = entries; return true;
                case "classQualifiers.warriorQualifiers": data.ClassQualifiers.WarriorQualifiers = entries; return true;
                case "classQualifiers.rogueQualifiers": data.ClassQualifiers.RogueQualifiers = entries; return true;
                case "classQualifiers.wizardQualifiers": data.ClassQualifiers.WizardQualifiers = entries; return true;
                case "classQualifiers.fighterQualifiers": data.ClassQualifiers.FighterQualifiers = entries; return true;
                default: return false;
            }
        }
    }
}
