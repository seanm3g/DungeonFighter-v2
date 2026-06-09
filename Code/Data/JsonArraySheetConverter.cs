using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame;
using RPGGame.World.Tags;

namespace RPGGame.Data
{
    /// <summary>Push/pull for JSON array game files on a single-header-row sheet tab.</summary>
    public enum GameDataTabularSheetKind
    {
        Weapons,
        Modifications,
        Armor,
        /// <summary><c>Enemies.json</c> — enemy archetypes, base attributes, and per-level growth.</summary>
        Enemies,
        /// <summary><c>Rooms.json</c> — room / environment definitions (sheet tab often named ENVIRONMENTS).</summary>
        Environments,
        /// <summary><c>Dungeons.json</c> — dungeon definitions: theme, levels, <c>possibleEnemies</c> (sheet tab often named DUNGEONS).</summary>
        Dungeons,
        /// <summary><c>StatBonuses.json</c> — rolled item suffix names / stat lines (sheet tab often named SUFFIXES).</summary>
        StatBonuses,
        /// <summary><c>Consumables.json</c> — room-search food and dungeon potions (sheet tab CONSUMABLES).</summary>
        Consumables
    }

    public static class JsonArraySheetConverter
    {
        /// <summary>
        /// Preferred column order for the weapons sheet / <c>Weapons.json</c> round-trip.
        /// <c>dps</c> and <c>balance</c> are sheet-only balancing helpers and are dropped when importing CSV → JSON.
        /// Runtime fields use JSON keys <c>baseDamage</c> (whole number), inclusive <c>damageBonusMin</c> / <c>damageBonusMax</c>
        /// (rolled onto base when loot is generated), then <c>attackSpeed</c> (fractional allowed;
        /// 1 = baseline swing spacing, &gt;1 slower, &lt;1 faster in combat).
        /// Optional <c>extraActionSlots</c> / <c>extraActionSlotsMin</c> / <c>extraActionSlotsMax</c> roll combo-strip slots (see <see cref="ItemGenerator.RollCatalogExtraActionSlots"/>).
        /// </summary>
        public static readonly string[] WeaponsCanonicalHeaders =
        {
            "type", "name", "dps", "balance", "baseDamage", "damageBonusMin", "damageBonusMax", "attackSpeed", "tier",
            "extraActionSlots", "extraActionSlotsMin", "extraActionSlotsMax",
            "attributeRequirements", "tags", "Compelled Action"
        };

        /// <summary>
        /// PREFIX tab columns A–N: dice tier, name/description/effect, rolled magnitude, category, optional stat gate,
        /// legacy <c>ATTRIBUTE REQUREMENT</c> typo column, min/max/rolled, and registry <c>tags</c>.
        /// Legacy JSON may still use <c>MinValue</c>/<c>MaxValue</c>; sheet <c>value</c> maps to both on import.
        /// </summary>
        public static readonly string[] ModificationsCanonicalHeaders =
        {
            "DiceResult", "ItemRank", "Name", "Description", "Effect", "value", "prefixCategory",
            "ATTRIBUTE REQUIREMENT", "REQUIREMENT VALUE",
            "ATTRIBUTE REQUREMENT", "MaxValue", "MinValue", "RolledValue", "tags"
        };

        /// <summary>
        /// ARMOR tab column order for <see cref="GameDataSheetsPushService"/> — matches the armor spreadsheet template
        /// (<c>tags</c> after <c>armor</c>, ALL CAPS core stats, <c># OF ACTION SLOTS</c> / <c># OF BONUS ACTIONS</c>,
        /// then <c>tier</c> and requirement columns). Extra JSON keys (e.g. <c>attackSpeed</c>, slot range columns) append after these.
        /// </summary>
        public static readonly string[] ArmorCanonicalHeaders =
        {
            "slot", "name", "armor", "tags",
            "STRENGTH", "AGILITY", "TECHNIQUE", "INTELLIGENCE", "HIT", "COMBO", "CRIT",
            "# OF ACTION SLOTS", "# OF BONUS ACTIONS",
            "tier", "attributeRequirements", "requirement value"
        };

        /// <summary>
        /// Column order for ENEMIES tab (A–U) — placement columns, nested stats, HEALTH band, then combat metadata.
        /// Push writes two header rows: category band (<c>base attributes</c>, <c>growth</c>, <c>HEALTH</c>) then short names.
        /// Import accepts that layout or a legacy single row of dotted headers; legacy <c>overrides.*</c> columns are promoted into HP fields and dropped.
        /// </summary>
        public static readonly string[] EnemiesCanonicalHeaders =
        {
            "region", "biome", "location", "rarity", "name", "tags", "archetype",
            "baseAttributes.strength", "baseAttributes.agility", "baseAttributes.technique", "baseAttributes.intelligence",
            "growthPerLevel.strength", "growthPerLevel.agility", "growthPerLevel.technique", "growthPerLevel.intelligence",
            "baseHealth", "healthGrowthPerLevel",
            "actions", "isLiving", "description", "colorOverride"
        };

        private static readonly string[] EnemyNestedObjectNames = { "baseAttributes", "growthPerLevel" };

        /// <summary>ENEMIES tab uses two header rows (category band + short names); other JSON-array tabs use one.</summary>
        public static int GetTabularSheetHeaderRowCount(GameDataTabularSheetKind kind) =>
            kind == GameDataTabularSheetKind.Enemies ? 2 : 1;

        /// <summary>Row-1 category labels on the ENEMIES sheet (repeated per column under that group).</summary>
        public const string EnemySheetCategoryOverrides = "overrides";

        public const string EnemySheetCategoryBaseAttributes = "base attributes";

        public const string EnemySheetCategoryGrowth = "growth";

        public const string EnemySheetCategoryHealth = "HEALTH";

        public static readonly string[] EnvironmentsCanonicalHeaders =
            { "region", "biome", "location", "tags", "description", "actions", "enemies" };

        /// <summary>Column order for DUNGEONS tab — matches <c>Dungeons.json</c> / runtime dungeon records.</summary>
        public static readonly string[] DungeonsCanonicalHeaders =
            { "name", "theme", "minLevel", "maxLevel", "possibleEnemies", "colorOverride" };

        /// <summary>Eight columns A–H on the SUFFIXES tab; matches <c>StatBonuses.json</c> / <see cref="StatBonus"/>.</summary>
        public static readonly string[] StatBonusesCanonicalHeaders =
            { "Name", "Description", "Value", "Rarity", "StatType", "ItemRank", "Mechanics", "Requirements" };

        /// <summary>CONSUMABLES tab column order for <c>Consumables.json</c>.</summary>
        public static readonly string[] ConsumablesCanonicalHeaders =
            { "displayName", "internalKind", "effect", "potency" };

        private static readonly HashSet<string> ConsumablesAuthorizedJsonKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "displayName", "internalKind", "effect", "potency"
        };

        /// <summary>Exact JSON keys on <see cref="StatBonus"/> — ordinal match so a raw sheet <c>mechanics</c> bracket string is not kept beside parsed <c>Mechanics</c>.</summary>
        private static readonly HashSet<string> StatBonusAuthorizedJsonKeys = new(StringComparer.Ordinal)
        {
            "Name", "Description", "Value", "Rarity", "StatType", "ItemRank", "Mechanics", "Requirements"
        };

        public static IReadOnlyList<string> GetCanonicalHeaders(GameDataTabularSheetKind kind) =>
            kind switch
            {
                GameDataTabularSheetKind.Weapons => WeaponsCanonicalHeaders,
                GameDataTabularSheetKind.Modifications => ModificationsCanonicalHeaders,
                GameDataTabularSheetKind.Armor => ArmorCanonicalHeaders,
                GameDataTabularSheetKind.Enemies => EnemiesCanonicalHeaders,
                GameDataTabularSheetKind.Environments => EnvironmentsCanonicalHeaders,
                GameDataTabularSheetKind.Dungeons => DungeonsCanonicalHeaders,
                GameDataTabularSheetKind.StatBonuses => StatBonusesCanonicalHeaders,
                GameDataTabularSheetKind.Consumables => ConsumablesCanonicalHeaders,
                _ => Array.Empty<string>()
            };

        /// <summary>
        /// Concatenates two JSON roots when each is (or parses as) an array: <paramref name="firstJson"/> elements first, then <paramref name="secondJson"/>.
        /// Whitespace-only, invalid JSON, or a non-array root for a part yields that part contributing no elements.
        /// Matches <see cref="LootDataCache"/> loading <c>Modifications.json</c> then <c>PrefixMaterialQuality.json</c> for sheet push.
        /// </summary>
        public static string MergeJsonRootArrays(string? firstJson, string? secondJson)
        {
            static JsonArray ParseArrayOrEmpty(string? text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return new JsonArray();
                try
                {
                    var n = JsonNode.Parse(text.Trim());
                    return n is JsonArray arr ? arr : new JsonArray();
                }
                catch (JsonException)
                {
                    return new JsonArray();
                }
            }

            var a = ParseArrayOrEmpty(firstJson);
            var b = ParseArrayOrEmpty(secondJson);
            var merged = new JsonArray();
            foreach (var x in a)
                merged.Add(x?.DeepClone());
            foreach (var x in b)
                merged.Add(x?.DeepClone());
            return merged.ToJsonString();
        }

        /// <summary>
        /// Reverse of <see cref="MergeJsonRootArrays"/> for the Prefix tab: rows whose <c>prefixCategory</c> is Material or Quality
        /// (see <see cref="ItemPrefixHelper.ParsePrefixCategory"/>) are written to <c>PrefixMaterialQuality.json</c>; all others
        /// (Adjective, empty, null) to <c>Modifications.json</c>. Matches <see cref="LootDataCache"/> loading order.
        /// </summary>
        public static (string coreModificationsJson, string prefixMaterialQualityJson) SplitModificationsMergedJson(
            string mergedArrayJson)
        {
            JsonNode? root = JsonNode.Parse(string.IsNullOrWhiteSpace(mergedArrayJson) ? "[]" : mergedArrayJson.Trim());
            if (root is not JsonArray arr)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var core = new JsonArray();
            var pmq = new JsonArray();
            var opts = GetSerializerOptions(GameDataTabularSheetKind.Modifications);

            foreach (var el in arr)
            {
                if (el is not JsonObject o)
                    continue;

                var slot = ItemPrefixHelper.ParsePrefixCategory(ReadModificationPrefixCategoryRaw(o));
                if (slot == ModificationPrefixCategory.Material || slot == ModificationPrefixCategory.Quality)
                    pmq.Add(el.DeepClone());
                else
                    core.Add(el.DeepClone());
            }

            return (core.ToJsonString(opts), pmq.ToJsonString(opts));
        }

        private static string? ReadModificationPrefixCategoryRaw(JsonObject o)
        {
            foreach (string key in new[] { "prefixCategory", "PrefixCategory" })
            {
                if (!o.TryGetPropertyValue(key, out JsonNode? node) || node is null)
                    continue;
                if (node is JsonValue jv)
                {
                    if (jv.TryGetValue(out string? s))
                        return string.IsNullOrEmpty(s) ? s : s.Trim();
                    try
                    {
                        var v = jv.GetValue<string?>();
                        return string.IsNullOrEmpty(v) ? v : v.Trim();
                    }
                    catch (InvalidOperationException)
                    {
                        return jv.ToString()?.Trim();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Builds sheet rows from a JSON array file body. Row 0 = headers (or ENEMIES: rows 0–1 = category band + short headers).
        /// </summary>
        public static List<IList<object>> BuildPushValueRows(string jsonFileText, GameDataTabularSheetKind kind)
        {
            if (kind == GameDataTabularSheetKind.Armor)
                return BuildArmorPushValueRows(jsonFileText);

            if (kind == GameDataTabularSheetKind.Enemies)
                return BuildEnemyPushValueRows(jsonFileText);

            if (kind == GameDataTabularSheetKind.Environments)
                return BuildEnvironmentPushValueRows(jsonFileText);

            if (kind == GameDataTabularSheetKind.Dungeons)
                return BuildDungeonsPushValueRows(jsonFileText);

            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = GetCanonicalHeaders(kind).ToList();
            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);
            // SUFFIXES tab is fixed A–G; do not emit helper/junk JSON keys as extra columns.
            // CONSUMABLES tab is fixed A–D.
            if (kind != GameDataTabularSheetKind.StatBonuses && kind != GameDataTabularSheetKind.Consumables)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object)
                        continue;
                    foreach (var p in el.EnumerateObject())
                    {
                        if (!canonical.Any(c => string.Equals(c, p.Name, StringComparison.Ordinal)))
                            extraKeys.Add(p.Name);
                    }
                }
            }

            var headers = new List<string>(canonical);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                {
                    if (!TryGetJsonPropertyCaseInsensitive(el, h, out var prop))
                    {
                        row.Add("");
                        continue;
                    }

                    if (kind == GameDataTabularSheetKind.StatBonuses
                        && string.Equals(h, "Requirements", StringComparison.OrdinalIgnoreCase)
                        && prop.ValueKind == JsonValueKind.Object)
                    {
                        row.Add(FormatStatBonusRequirementsForSheetCell(prop));
                        continue;
                    }

                    if (string.Equals(h, "tags", StringComparison.OrdinalIgnoreCase)
                        && prop.ValueKind == JsonValueKind.Array)
                    {
                        row.Add(FormatTagsArrayForSheetCell(prop));
                        continue;
                    }

                    row.Add(JsonElementToCellString(prop));
                }
                rows.Add(row);
            }

            return rows;
        }

        private static List<IList<object>> BuildArmorPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = ArmorCanonicalHeaders.ToList();
            var handledJson = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "slot", "name", "armor", "tags", "strength", "agility", "technique", "intelligence",
                "hit", "combo", "crit", "extraActionSlots", "extraActionSlotsMin", "extraActionSlotsMax",
                "minActionBonuses", "tier", "attributeRequirements"
            };

            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                foreach (var p in el.EnumerateObject())
                {
                    if (!handledJson.Contains(p.Name))
                        extraKeys.Add(p.Name);
                }
            }

            var headers = new List<string>(canonical);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetArmorPushCellValue(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static object GetArmorPushCellValue(JsonElement el, string header)
        {
            if (string.Equals(header, "attributeRequirements", StringComparison.Ordinal))
            {
                GetArmorAttributeRequirementCells(el, out string attrCol, out _);
                return attrCol;
            }

            if (string.Equals(header, "requirement value", StringComparison.Ordinal))
            {
                GetArmorAttributeRequirementCells(el, out _, out string reqVal);
                return reqVal;
            }

            string? jsonName = MapArmorSheetHeaderToJsonProperty(header);
            if (jsonName != null && TryGetJsonPropertyCaseInsensitive(el, jsonName, out var mapped))
            {
                if (string.Equals(jsonName, "tags", StringComparison.OrdinalIgnoreCase) && mapped.ValueKind == JsonValueKind.Array)
                    return FormatTagsArrayForSheetCell(mapped);
                return JsonElementToCellString(mapped);
            }

            if (TryGetJsonPropertyCaseInsensitive(el, header, out var direct))
            {
                if (string.Equals(header, "tags", StringComparison.OrdinalIgnoreCase) && direct.ValueKind == JsonValueKind.Array)
                    return FormatTagsArrayForSheetCell(direct);
                return JsonElementToCellString(direct);
            }

            return "";
        }

        private static string? MapArmorSheetHeaderToJsonProperty(string header) =>
            header switch
            {
                "STRENGTH" => "strength",
                "AGILITY" => "agility",
                "TECHNIQUE" => "technique",
                "INTELLIGENCE" => "intelligence",
                "HIT" => "hit",
                "COMBO" => "combo",
                "CRIT" => "crit",
                "# OF ACTION SLOTS" => "extraActionSlots",
                "# OF BONUS ACTIONS" => "minActionBonuses",
                "slot" => "slot",
                "name" => "name",
                "armor" => "armor",
                "tags" => "tags",
                "tier" => "tier",
                _ => null
            };

        private static void GetArmorAttributeRequirementCells(JsonElement el, out string attrCol, out string reqValCol)
        {
            attrCol = "";
            reqValCol = "";
            if (!TryGetJsonPropertyCaseInsensitive(el, "attributeRequirements", out var ar) || ar.ValueKind == JsonValueKind.Null)
                return;

            if (ar.ValueKind == JsonValueKind.Object)
            {
                var props = ar.EnumerateObject().ToList();
                if (props.Count == 1)
                {
                    var p = props[0];
                    attrCol = ArmorRequirementKeyToSheetStatColumn(p.Name);
                    reqValCol = p.Value.ValueKind == JsonValueKind.Number
                        ? JsonElementToCellString(p.Value)
                        : "";
                }
                else
                    attrCol = ar.GetRawText();
            }
            else if (ar.ValueKind == JsonValueKind.String)
            {
                attrCol = ar.GetString() ?? "";
                if (TryGetJsonPropertyCaseInsensitive(el, "requirement value", out var rv) &&
                    rv.ValueKind is JsonValueKind.Number or JsonValueKind.String)
                    reqValCol = JsonElementToCellString(rv);
            }
        }

        private static string ArmorRequirementKeyToSheetStatColumn(string requirementPropertyName)
        {
            string canon = Item.CanonicalizeAttributeRequirementKey(requirementPropertyName);
            return canon switch
            {
                "strength" => "STRENGTH",
                "agility" => "AGILITY",
                "technique" => "TECHNIQUE",
                "intelligence" => "INTELLIGENCE",
                "primary" => "PRIMARY",
                "secondary" => "SECONDARY",
                "neglected" => "NEGLECTED",
                "weakness" => "WEAKNESS",
                _ => string.IsNullOrEmpty(canon) ? "" : canon.ToUpperInvariant()
            };
        }

        private static bool TryGetJsonPropertyCaseInsensitive(JsonElement el, string name, out JsonElement prop)
        {
            prop = default;
            if (el.ValueKind != JsonValueKind.Object)
                return false;
            foreach (var p in el.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    prop = p.Value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Renders a parsed Requirements object (<c>{strength:5,primary:15}</c>) back to the bracket cell form used by sheet authors.</summary>
        internal static string FormatStatBonusRequirementsForSheetCell(JsonElement obj)
        {
            if (obj.ValueKind != JsonValueKind.Object)
                return "";

            var parts = new List<string>();
            foreach (var prop in obj.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(prop.Name))
                    continue;
                int value;
                if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    if (prop.Value.TryGetInt32(out int iv))
                        value = iv;
                    else if (prop.Value.TryGetDouble(out double dv))
                        value = (int)Math.Round(dv);
                    else
                        continue;
                }
                else
                {
                    continue;
                }
                parts.Add($"{prop.Name}:{value.ToString(CultureInfo.InvariantCulture)}");
            }

            return parts.Count == 0 ? "" : "[" + string.Join(",", parts) + "]";
        }

        private static List<IList<object>> BuildEnvironmentPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var headers = EnvironmentsCanonicalHeaders.ToList();
            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetEnvironmentPushCell(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static string GetEnvironmentPushCell(JsonElement el, string header)
        {
            return header switch
            {
                "region" => GetEnvironmentStringProperty(el, "region"),
                "biome" => GetEnvironmentBiomeCell(el),
                "location" => GetEnvironmentLocationCell(el),
                "tags" => TryGetJsonPropertyCaseInsensitive(el, "tags", out var tags)
                    ? FormatTagsArrayForSheetCell(tags)
                    : "",
                "description" => GetEnvironmentStringProperty(el, "description"),
                "actions" => TryGetJsonPropertyCaseInsensitive(el, "actions", out var actions)
                    ? FormatEnvironmentWeightedListForSheetCell(actions)
                    : "",
                "enemies" => TryGetJsonPropertyCaseInsensitive(el, "enemies", out var enemies)
                    ? FormatEnvironmentWeightedListForSheetCell(enemies)
                    : "",
                _ => ""
            };
        }

        private static string GetEnvironmentLocationCell(JsonElement el)
        {
            if (TryGetJsonPropertyCaseInsensitive(el, "location", out var loc))
                return JsonStringToCell(loc);
            if (TryGetJsonPropertyCaseInsensitive(el, "name", out var name))
                return JsonStringToCell(name);
            return "";
        }

        private static string GetEnvironmentBiomeCell(JsonElement el)
        {
            if (TryGetJsonPropertyCaseInsensitive(el, "biome", out var biome))
                return JsonStringToCell(biome);
            if (TryGetJsonPropertyCaseInsensitive(el, "theme", out var theme))
                return JsonStringToCell(theme);
            return "";
        }

        private static string GetEnvironmentStringProperty(JsonElement el, string name) =>
            TryGetJsonPropertyCaseInsensitive(el, name, out var prop) ? JsonStringToCell(prop) : "";

        /// <summary>Sheet-friendly: <c>Action A|Action B</c> or <c>Wolf:0.7|Spider:0.3</c> when weights differ from 1.</summary>
        internal static string FormatEnvironmentWeightedListForSheetCell(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String)
                return JsonStringToCell(el);

            if (el.ValueKind != JsonValueKind.Array)
                return JsonElementToCellString(el);

            var entries = new List<(string name, double weight)>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(s))
                        entries.Add((s, 1.0));
                    continue;
                }

                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (!TryGetJsonPropertyCaseInsensitive(item, "name", out var nameEl))
                    continue;

                var name = nameEl.GetString()?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                double weight = 1.0;
                if (TryGetJsonPropertyCaseInsensitive(item, "weight", out var wEl) && wEl.ValueKind == JsonValueKind.Number)
                    weight = wEl.GetDouble();

                entries.Add((name, weight));
            }

            if (entries.Count == 0)
                return "";

            bool useWeights = entries.Any(e => Math.Abs(e.weight - 1.0) > 0.0001);
            if (!useWeights)
                return string.Join("|", entries.Select(e => e.name));

            return string.Join("|", entries.Select(e =>
                Math.Abs(e.weight - 1.0) <= 0.0001
                    ? e.name
                    : $"{e.name}:{e.weight.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        }

        private static List<IList<object>> BuildDungeonsPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var headers = DungeonsCanonicalHeaders.ToList();
            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetDungeonsPushCell(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static string GetDungeonsPushCell(JsonElement el, string header) =>
            header switch
            {
                "name" => GetEnvironmentStringProperty(el, "name"),
                "theme" => GetEnvironmentStringProperty(el, "theme"),
                "minLevel" => TryGetJsonPropertyCaseInsensitive(el, "minLevel", out var min)
                    ? JsonElementToCellString(min)
                    : "",
                "maxLevel" => TryGetJsonPropertyCaseInsensitive(el, "maxLevel", out var max)
                    ? JsonElementToCellString(max)
                    : "",
                "possibleEnemies" => TryGetJsonPropertyCaseInsensitive(el, "possibleEnemies", out var pe)
                    ? FormatPipeDelimitedStringArrayForSheetCell(pe)
                    : "",
                "colorOverride" => TryGetJsonPropertyCaseInsensitive(el, "colorOverride", out var color)
                    ? JsonElementToCellString(color)
                    : "",
                _ => ""
            };

        private static List<IList<object>> BuildEnemyPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = EnemiesCanonicalHeaders.ToList();
            var flatCanon = new HashSet<string>(canonical, StringComparer.OrdinalIgnoreCase);
            var nestedParents = new HashSet<string>(EnemyNestedObjectNames, StringComparer.OrdinalIgnoreCase);
            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                foreach (var p in el.EnumerateObject())
                {
                    if (flatCanon.Contains(p.Name))
                        continue;
                    if (nestedParents.Contains(p.Name))
                        continue;
                    if (EnemyLegacyRootStatPropertyNames.Contains(p.Name))
                        continue;
                    extraKeys.Add(p.Name);
                }
            }

            var headers = new List<string>(canonical);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(EnemyCategoryLabelForCanonicalHeader).Select(s => (object)s).ToList());
            rows.Add(headers.Select(EnemyShortHeaderForCanonicalHeader).Select(s => (object)s).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetEnemyPushCell(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static string EnemyCategoryLabelForCanonicalHeader(string canonicalHeader)
        {
            if (canonicalHeader.StartsWith("baseAttributes.", StringComparison.Ordinal))
                return EnemySheetCategoryBaseAttributes;
            if (canonicalHeader.StartsWith("growthPerLevel.", StringComparison.Ordinal))
                return EnemySheetCategoryGrowth;
            if (string.Equals(canonicalHeader, "baseHealth", StringComparison.OrdinalIgnoreCase)
                || string.Equals(canonicalHeader, "healthGrowthPerLevel", StringComparison.OrdinalIgnoreCase))
                return EnemySheetCategoryHealth;
            return "";
        }

        /// <summary>Second header row: dotted keys with group prefix removed for overrides / baseAttributes / growthPerLevel.</summary>
        private static string EnemyShortHeaderForCanonicalHeader(string canonicalHeader)
        {
            if (canonicalHeader.StartsWith("baseAttributes.", StringComparison.Ordinal))
                return canonicalHeader["baseAttributes.".Length..];
            if (canonicalHeader.StartsWith("growthPerLevel.", StringComparison.Ordinal))
                return canonicalHeader["growthPerLevel.".Length..];
            return canonicalHeader;
        }

        private static readonly HashSet<string> EnemyLegacyRootStatPropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "strength", "agility", "technique", "intelligence"
        };

        private static string GetEnemyPushCell(JsonElement el, string header)
        {
            if (header.StartsWith("baseAttributes.", StringComparison.Ordinal))
            {
                if (TryGetPropertyPathCaseInsensitive(el, header, out var nested))
                    return JsonElementToCellString(nested);
                string leaf = header["baseAttributes.".Length..];
                if (TryGetJsonPropertyCaseInsensitive(el, leaf, out var rootStat))
                    return JsonElementToCellString(rootStat);
                return "";
            }

            if (header.StartsWith("growthPerLevel.", StringComparison.Ordinal))
            {
                if (TryGetPropertyPathCaseInsensitive(el, header, out var nestedG))
                    return JsonElementToCellString(nestedG);
                string leafG = header["growthPerLevel.".Length..];
                if (TryGetJsonPropertyCaseInsensitive(el, leafG, out var rootG))
                    return JsonElementToCellString(rootG);
                return "";
            }

            if (header.Contains('.', StringComparison.Ordinal))
            {
                if (TryGetPropertyPathCaseInsensitive(el, header, out var at))
                    return JsonElementToCellString(at);
                return "";
            }

            if (!TryGetJsonPropertyCaseInsensitive(el, header, out var prop))
                return "";
            if (string.Equals(header, "tags", StringComparison.OrdinalIgnoreCase))
                return FormatTagsArrayForSheetCell(prop);
            if (string.Equals(header, "actions", StringComparison.OrdinalIgnoreCase))
                return FormatPipeDelimitedStringArrayForSheetCell(prop);
            return JsonElementToCellString(prop);
        }

        /// <summary>Sheet-friendly plain string list: <c>A|B|C</c> (pull accepts this via pipe normalization).</summary>
        internal static string FormatPipeDelimitedStringArrayForSheetCell(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String)
                return JsonStringToCell(el);

            if (el.ValueKind != JsonValueKind.Array)
                return JsonElementToCellString(el);

            var parts = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;
                var s = item.GetString()?.Trim();
                if (!string.IsNullOrEmpty(s))
                    parts.Add(s);
            }

            return parts.Count == 0 ? "" : string.Join("|", parts);
        }

        /// <summary>Sheet-friendly tag list: comma-separated, lowercase (pull accepts this via <see cref="NormalizeTagsFromSheet"/>).</summary>
        internal static string FormatTagsArrayForSheetCell(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Array)
                return JsonElementToCellString(el);

            var raw = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(s))
                        raw.Add(s);
                }
            }

            var normalized = GameDataTagHelper.NormalizeDistinct(raw);
            if (normalized.Count == 0)
                return "";
            return string.Join(", ", normalized.Select(t => t.ToLowerInvariant()));
        }

        private static bool TryGetPropertyPath(JsonElement el, string dottedPath, out JsonElement found) =>
            TryGetPropertyPathCaseInsensitive(el, dottedPath, out found);

        private static bool TryGetPropertyPathCaseInsensitive(JsonElement el, string dottedPath, out JsonElement found)
        {
            found = default;
            var parts = dottedPath.Split('.');
            if (parts.Length < 2)
                return false;
            JsonElement cur = el;
            foreach (var part in parts)
            {
                if (cur.ValueKind != JsonValueKind.Object || !TryGetJsonPropertyCaseInsensitive(cur, part, out cur))
                    return false;
            }

            found = cur;
            return true;
        }

        /// <summary>Converts CSV (header row + data) into pretty-printed JSON array file text.</summary>
        public static string CsvToJsonArrayText(string csvContent, GameDataTabularSheetKind kind)
        {
            var table = SimpleGameDataCsvParser.ParseToRows(csvContent);
            if (table.Count == 0)
                throw new InvalidOperationException("CSV has no rows.");

            TryGetEnemyImportHeaderRowAndFirstDataIndex(table, kind, out var headerRow, out int firstDataRow);

            var arr = new JsonArray();
            for (int r = firstDataRow; r < table.Count; r++)
            {
                var cells = table[r];
                if (cells.Length == 0 || cells.All(string.IsNullOrWhiteSpace))
                    continue;

                var obj = new JsonObject();
                int headerCount = headerRow.Length;
                if (kind == GameDataTabularSheetKind.Consumables)
                    headerCount = Math.Min(headerCount, ConsumablesCanonicalHeaders.Length);
                for (int i = 0; i < headerCount; i++)
                {
                    // Google / Excel CSV exports may prefix the file with U+FEFF, which lands on the first header cell.
                    string header = (headerRow[i] ?? "").Trim().TrimStart('\uFEFF');
                    // Live SUFFIXES tab: column A holds affix names ("of Protection") but often has a blank header cell.
                    if (kind == GameDataTabularSheetKind.StatBonuses && header.Length == 0 && i == 0)
                        header = "Name";
                    if (header.Length == 0)
                        continue;
                    string cell = i < cells.Length ? cells[i] ?? "" : "";
                    JsonNode? parsed = CellToJsonNode(cell);
                    if (parsed != null)
                        obj[header] = parsed;
                }

                if (kind == GameDataTabularSheetKind.Enemies)
                {
                    MergeEnemyFlatColumnsToNested(obj);
                    NormalizeEnemyJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Dungeons)
                {
                    NormalizeDungeonsJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Weapons)
                {
                    NormalizeWeaponsJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.StatBonuses)
                {
                    NormalizeStatBonusesJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Modifications)
                {
                    GameDataJsonNormalizer.NormalizeModificationsImportRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Armor)
                {
                    GameDataJsonNormalizer.NormalizeArmorDataRow(obj);
                    NormalizeTagsFromSheet(obj);
                }
                else if (kind == GameDataTabularSheetKind.Consumables)
                {
                    NormalizeConsumablesJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Environments)
                {
                    NormalizeEnvironmentJsonArrayRow(obj);
                }

                arr.Add(obj);
            }

            return arr.ToJsonString(GetSerializerOptions(kind));
        }

        /// <summary>
        /// ENEMIES may use row 0 = category labels and row 1 = short field names; otherwise row 0 = dotted / top-level headers.
        /// </summary>
        /// <summary>Sets <paramref name="headerRow"/> and <paramref name="firstDataRow"/> for ENEMIES two-row headers or single-row fallback.</summary>
        private static void TryGetEnemyImportHeaderRowAndFirstDataIndex(
            List<string[]> table,
            GameDataTabularSheetKind kind,
            out string[] headerRow,
            out int firstDataRow)
        {
            headerRow = table[0];
            firstDataRow = 1;
            if (kind != GameDataTabularSheetKind.Enemies || table.Count < 2)
                return;

            var row0 = table[0];
            var row1 = table[1];
            // Avoid treating legacy single-row CSV (column "overrides" JSON blob) as a category band.
            if (!EnemySheetTwoRowHeaderFormat(row0, row1))
                return;

            int width = Math.Max(row0.Length, row1.Length);
            var combined = new string[width];
            string carryCategory = "";
            for (int i = 0; i < width; i++)
            {
                string cat = i < row0.Length ? row0[i]?.Trim() ?? "" : "";
                string sub = i < row1.Length ? row1[i]?.Trim() ?? "" : "";
                // Some sheets export "merged" category headers: the first column in a block has the category label
                // and the remaining columns are blank. Carry the last seen category across blanks so the short
                // headers can still be combined into dotted keys.
                if (!string.IsNullOrWhiteSpace(cat))
                    carryCategory = cat;
                else if (!string.IsNullOrWhiteSpace(carryCategory))
                    cat = carryCategory;
                combined[i] = CombineEnemyImportHeaderCell(cat, sub);
            }

            headerRow = combined;
            firstDataRow = 2;
        }

        /// <summary>
        /// True when row 0 is the category band (blank over general columns, repeated group labels) and row 1 is short column names
        /// (contains <c>name</c> somewhere), not a data row.
        /// </summary>
        private static bool EnemySheetTwoRowHeaderFormat(string[] row0, string[] row1)
        {
            if (row1.Length == 0)
                return false;
            if (!EnemySheetRowContainsNameHeader(row1))
                return false;
            if (!EnemySheetRowLooksLikeCategoryBand(row0) || EnemySheetRowHasDottedStatKeys(row0))
                return false;
            return true;
        }

        private static bool EnemySheetRowContainsNameHeader(string[] row)
        {
            foreach (var cell in row)
            {
                if (string.Equals(cell?.Trim(), "name", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool EnemySheetRowLooksLikeCategoryBand(string[] row)
        {
            foreach (var cell in row)
            {
                string t = cell?.Trim() ?? "";
                if (t.Length == 0)
                    continue;
                if (string.Equals(t, EnemySheetCategoryOverrides, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, EnemySheetCategoryBaseAttributes, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, EnemySheetCategoryGrowth, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, EnemySheetCategoryHealth, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool EnemySheetRowHasDottedStatKeys(string[] row)
        {
            foreach (var cell in row)
            {
                string t = cell?.Trim() ?? "";
                if (t.Contains("overrides.", StringComparison.Ordinal)
                    || t.Contains("baseAttributes.", StringComparison.Ordinal)
                    || t.Contains("growthPerLevel.", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string CombineEnemyImportHeaderCell(string category, string subHeader)
        {
            category = category.Trim().TrimStart('\uFEFF');
            subHeader = subHeader.Trim().TrimStart('\uFEFF');
            if (subHeader.Length == 0)
                return "";

            // Sheet row-0 blanks reuse the last category label (Excel merges). Columns after growthPerLevel.*
            // (actions, isLiving, …) have blank row-0 but must not inherit "growth" or actions land in growthPerLevel.actions.
            if (EnemyImportSubHeaderIsRootEnemyColumn(subHeader))
                return NormalizeEnemyRootHeader(subHeader);

            if (category.Length == 0)
                return subHeader;

            if (string.Equals(category, EnemySheetCategoryOverrides, StringComparison.OrdinalIgnoreCase))
                return "overrides." + subHeader;

            if (string.Equals(category, EnemySheetCategoryBaseAttributes, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "baseHealth", StringComparison.OrdinalIgnoreCase))
                    return "baseHealth";
                return "baseAttributes." + NormalizeEnemyStatLeafHeader(subHeader);
            }

            if (string.Equals(category, EnemySheetCategoryGrowth, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "healthGrowthPerLevel", StringComparison.OrdinalIgnoreCase))
                    return "healthGrowthPerLevel";
                return "growthPerLevel." + NormalizeEnemyStatLeafHeader(subHeader);
            }

            if (string.Equals(category, EnemySheetCategoryHealth, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "baseHealth", StringComparison.OrdinalIgnoreCase))
                    return "baseHealth";
                if (string.Equals(subHeader, "healthGrowthPerLevel", StringComparison.OrdinalIgnoreCase))
                    return "healthGrowthPerLevel";
            }

            return NormalizeEnemyRootHeader(subHeader);
        }

        private static string NormalizeEnemyStatLeafHeader(string subHeader) =>
            subHeader.Trim().ToLowerInvariant();

        private static string NormalizeEnemyRootHeader(string subHeader)
        {
            string t = subHeader.Trim();
            return EnemyRootHeaderCanonicalNames.TryGetValue(t, out var canon) ? canon : t;
        }

        private static readonly Dictionary<string, string> EnemyRootHeaderCanonicalNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["region"] = "region",
                ["biome"] = "biome",
                ["location"] = "location",
                ["rarity"] = "rarity",
                ["name"] = "name",
                ["tags"] = "tags",
                ["archetype"] = "archetype",
                ["actions"] = "actions",
                ["isLiving"] = "isLiving",
                ["description"] = "description",
                ["colorOverride"] = "colorOverride",
                ["baseHealth"] = "baseHealth",
                ["healthGrowthPerLevel"] = "healthGrowthPerLevel"
            };

        /// <summary>Short headers on ENEMIES row-1 that are top-level JSON fields, not growth/baseAttributes sub-keys.</summary>
        private static bool EnemyImportSubHeaderIsRootEnemyColumn(string subHeader) =>
            EnemyRootHeaderCanonicalNames.ContainsKey(subHeader.Trim());

        private static JsonSerializerOptions GetSerializerOptions(GameDataTabularSheetKind kind)
        {
            var o = new JsonSerializerOptions { WriteIndented = true };
            if (kind == GameDataTabularSheetKind.Weapons || kind == GameDataTabularSheetKind.Armor
                || kind == GameDataTabularSheetKind.Enemies || kind == GameDataTabularSheetKind.Environments
                || kind == GameDataTabularSheetKind.Dungeons || kind == GameDataTabularSheetKind.Consumables)
                o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            else
                o.PropertyNamingPolicy = null; // Modifications, StatBonuses — PascalCase columns match JSON files
            return o;
        }

        private static string JsonStringToCell(JsonElement el)
        {
            return SheetsPushUtilities.NormalizeSheetString(el.GetString());
        }

        private static string JsonElementToCellString(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Null => "",
                JsonValueKind.String => JsonStringToCell(el),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.Object or JsonValueKind.Array => el.GetRawText(),
                _ => el.GetRawText()
            };
        }

        internal static JsonNode? CellToJsonNode(string cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return null;

            cell = cell.Trim();
            if (cell.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            if (cell.Length > 0 && (cell[0] == '{' || cell[0] == '['))
            {
                try
                {
                    return JsonNode.Parse(cell);
                }
                catch
                {
                    return JsonValue.Create(cell);
                }
            }

            if (bool.TryParse(cell, out bool b))
                return JsonValue.Create(b);

            if (double.TryParse(cell, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                if (d >= long.MinValue && d <= long.MaxValue && Math.Abs(d - (long)d) < 1e-9)
                    return JsonValue.Create((long)d);
                return JsonValue.Create(d);
            }

            return JsonValue.Create(cell);
        }

        /// <summary>
        /// Merges dotted headers (<c>overrides.health</c>, <c>baseAttributes.strength</c>, …) into nested objects and removes those keys.
        /// Flat values override the same keys on an existing legacy <c>overrides</c> / <c>baseAttributes</c> / <c>growthPerLevel</c> JSON cell.
        /// </summary>
        private static void MergeEnemyFlatColumnsToNested(JsonObject obj)
        {
            var buckets = new Dictionary<string, Dictionary<string, JsonNode?>>(StringComparer.Ordinal)
            {
                ["overrides"] = new Dictionary<string, JsonNode?>(StringComparer.Ordinal),
                ["baseAttributes"] = new Dictionary<string, JsonNode?>(StringComparer.Ordinal),
                ["growthPerLevel"] = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
            };

            var keysToRemove = new List<string>();
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                int dot = key.IndexOf('.', StringComparison.Ordinal);
                if (dot <= 0)
                    continue;
                string prefix = key[..dot];
                string suffix = key[(dot + 1)..];
                if (!buckets.TryGetValue(prefix, out var bucket))
                    continue;

                if (!obj.TryGetPropertyValue(key, out var valNode))
                    continue;

                keysToRemove.Add(key);
                if (valNode is null || IsJsonNodeNullOrMissing(valNode))
                    continue;
                bucket[suffix] = valNode;
            }

            foreach (var key in keysToRemove)
                obj.Remove(key);

            foreach (var (prefix, bucket) in buckets)
            {
                if (bucket.Count == 0)
                    continue;

                var merged = new JsonObject();
                foreach (var (sk, sv) in bucket)
                {
                    if (sv is null || IsJsonNodeNullOrMissing(sv))
                        continue;
                    merged[sk] = sv;
                }

                if (merged.Count == 0)
                    continue;

                if (obj.TryGetPropertyValue(prefix, out var existing) && existing is JsonObject eo)
                {
                    foreach (var (sk, sv) in merged)
                    {
                        if (sv is null || IsJsonNodeNullOrMissing(sv))
                            continue;
                        eo[sk] = sv;
                    }
                }
                else
                {
                    obj[prefix] = merged;
                }
            }
        }

        private static bool IsJsonNodeNullOrMissing(JsonNode n)
        {
            if (n is null)
                return true;
            return n.GetValueKind() == JsonValueKind.Null;
        }

        /// <summary>
        /// When <c>actions</c> is a plain string (not JSON), treat <c>|</c> as delimiter so sheet authors can write
        /// <c>JAB|TAUNT</c> instead of a JSON array.
        /// </summary>
        private static void NormalizeEnemyJsonArrayRow(JsonObject obj)
        {
            CanonicalizeEnemyImportRowKeys(obj);
            PromoteMisplacedEnemyHealthFields(obj);
            StripEnemyLegacyRootStatsWhenNestedPresent(obj);
            NormalizeEnemyActionsFromSheet(obj);
            NormalizeTagsFromSheet(obj);
            NormalizeEnemyArchetypeFromSheet(obj);
        }

        private static void NormalizeEnemyArchetypeFromSheet(JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("archetype", out var node) || node == null)
                return;

            var raw = node.GetValue<string>()?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                obj.Remove("archetype");
                return;
            }

            var canonical = TagDefinitions.CanonicalizeEnemyArchetype(raw);
            if (canonical != null && TagDefinitions.IsValidEnemyArchetype(canonical))
                obj["archetype"] = canonical;
            else
                obj["archetype"] = raw;
        }

        /// <summary>Moves HP keys mistakenly nested under <c>growthPerLevel</c> / <c>baseAttributes</c> to root (legacy sheet bands).</summary>
        private static void PromoteMisplacedEnemyHealthFields(JsonObject obj)
        {
            static void Hoist(JsonObject root, JsonObject nested, string nestedKey, string rootKey)
            {
                if (!nested.TryGetPropertyValue(nestedKey, out var val) || val is null || IsJsonNodeNullOrMissing(val))
                    return;
                if (!root.ContainsKey(rootKey))
                    root[rootKey] = val.DeepClone();
                nested.Remove(nestedKey);
            }

            if (obj.TryGetPropertyValue("growthPerLevel", out var gpNode) && gpNode is JsonObject gp)
            {
                Hoist(obj, gp, "baseHealth", "baseHealth");
                Hoist(obj, gp, "healthGrowthPerLevel", "healthGrowthPerLevel");
            }

            if (obj.TryGetPropertyValue("baseAttributes", out var baNode) && baNode is JsonObject ba)
                Hoist(obj, ba, "baseHealth", "baseHealth");
        }

        /// <summary>Drops mistaken root <c>strength</c>/<c>agility</c>/… when nested stat objects were already merged.</summary>
        private static void StripEnemyLegacyRootStatsWhenNestedPresent(JsonObject obj)
        {
            bool hasBase = obj.ContainsKey("baseAttributes");
            bool hasGrowth = obj.ContainsKey("growthPerLevel");
            if (!hasBase && !hasGrowth)
                return;

            foreach (string stat in EnemyLegacyRootStatPropertyNames)
                obj.Remove(stat);
        }

        /// <summary>Maps mixed-case sheet headers to lowercase camelCase JSON keys before nested merge.</summary>
        private static void CanonicalizeEnemyImportRowKeys(JsonObject obj)
        {
            var renames = new List<(string oldKey, string newKey)>();
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                int dot = key.IndexOf('.', StringComparison.Ordinal);
                if (dot > 0)
                {
                    string prefix = key[..dot];
                    string suffix = key[(dot + 1)..];
                    string canonPrefix = prefix.Equals("baseAttributes", StringComparison.OrdinalIgnoreCase)
                        ? "baseAttributes"
                        : prefix.Equals("growthPerLevel", StringComparison.OrdinalIgnoreCase)
                            ? "growthPerLevel"
                            : prefix.Equals("overrides", StringComparison.OrdinalIgnoreCase)
                                ? "overrides"
                                : prefix;
                    string canonKey = canonPrefix + "." + suffix.Trim().ToLowerInvariant();
                    if (!string.Equals(key, canonKey, StringComparison.Ordinal))
                        renames.Add((key, canonKey));
                    continue;
                }

                string canonRoot = NormalizeEnemyRootHeader(key);
                if (!string.Equals(key, canonRoot, StringComparison.Ordinal))
                    renames.Add((key, canonRoot));
            }

            foreach (var (oldKey, newKey) in renames)
            {
                if (!obj.TryGetPropertyValue(oldKey, out var val))
                    continue;
                if (obj.ContainsKey(newKey))
                    obj.Remove(oldKey);
                else
                {
                    obj.Remove(oldKey);
                    if (val is not null)
                        obj[newKey] = val;
                }
            }
        }

        /// <summary>
        /// When <c>actions</c> is a plain string (not JSON), treat <c>|</c> as delimiter so sheet authors can write
        /// <c>JAB|TAUNT</c> instead of a JSON array.
        /// </summary>
        private static void NormalizeEnemyActionsFromSheet(JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("actions", out var node) || node is null)
                return;

            if (node is JsonArray)
                return;

            if (node is not JsonValue jv || !jv.TryGetValue<string>(out var s) || string.IsNullOrWhiteSpace(s))
                return;

            // ENEMIES sheet often uses one of:
            // - Pipe list: JAB|TAUNT
            // - JSON array: ["JAB","TAUNT"]
            // - Quoted CSV-style list in a single cell:
            //     "JAB",
            //     "TAUNT"
            // Normalize all non-JSON strings into a canonical string array of action names.
            s = s.Trim();

            char[] splitChars = s.Contains('|', StringComparison.Ordinal)
                ? new[] { '|' }
                : new[] { ',', '\n', '\r', ';' };

            var parts = s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim().Trim('"'))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parts.Length == 0)
                return;

            var arr = new JsonArray();
            foreach (var p in parts)
                arr.Add(p);
            obj["actions"] = arr;
        }

        /// <summary>
        /// Sheet <c>tags</c> may be a JSON array, a single token, or comma / semicolon / pipe lists (same as in-game settings).
        /// Coerce to a canonical lowercase <c>tags</c> JSON array for reliable catalog deserialization.
        /// </summary>
        private static void NormalizeEnvironmentJsonArrayRow(JsonObject obj)
        {
            CanonicalizeEnvironmentImportRowKeys(obj);
            NormalizeTagsFromSheet(obj);
            NormalizeEnvironmentWeightedListFromSheet(obj, "actions");
            NormalizeEnvironmentWeightedListFromSheet(obj, "enemies");
        }

        private static void CanonicalizeEnvironmentImportRowKeys(JsonObject obj)
        {
            var renames = new List<(string oldKey, string newKey)>();
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!EnvironmentRootHeaderCanonicalNames.TryGetValue(key.Trim(), out var canon))
                    continue;
                if (!string.Equals(key, canon, StringComparison.Ordinal))
                    renames.Add((key, canon));
            }

            foreach (var (oldKey, newKey) in renames)
            {
                if (!obj.TryGetPropertyValue(oldKey, out var val))
                    continue;
                if (obj.ContainsKey(newKey))
                    obj.Remove(oldKey);
                else
                {
                    obj.Remove(oldKey);
                    if (val is not null)
                        obj[newKey] = val;
                }
            }

            // Legacy Rooms.json used "Location" PascalCase without other fields.
            if (obj.TryGetPropertyValue("Location", out var locNode) && locNode is not null
                && !obj.ContainsKey("location"))
            {
                obj["location"] = locNode.DeepClone();
                obj.Remove("Location");
            }

            if (obj.TryGetPropertyValue("name", out var nameNode) && nameNode is not null
                && !obj.ContainsKey("location"))
            {
                obj["location"] = nameNode.DeepClone();
            }
        }

        private static readonly Dictionary<string, string> EnvironmentRootHeaderCanonicalNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["region"] = "region",
                ["biome"] = "biome",
                ["location"] = "location",
                ["tags"] = "tags",
                ["description"] = "description",
                ["actions"] = "actions",
                ["enemies"] = "enemies",
                ["name"] = "location",
                ["theme"] = "biome"
            };

        /// <summary>
        /// Coerces sheet <c>actions</c> / <c>enemies</c> cells into <c>{ name, weight }</c> JSON arrays.
        /// </summary>
        private static void NormalizeEnvironmentWeightedListFromSheet(JsonObject obj, string property)
        {
            if (!obj.TryGetPropertyValue(property, out var node) || node is null || IsJsonNodeNullOrMissing(node))
                return;

            if (node is JsonArray arr)
            {
                var normalized = new JsonArray();
                foreach (var item in arr)
                {
                    if (item is JsonObject o && o.TryGetPropertyValue("name", out _))
                    {
                        normalized.Add(item.DeepClone());
                        continue;
                    }

                    if (item is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                        normalized.Add(BuildWeightedEntry(s.Trim(), 1.0));
                }

                if (normalized.Count > 0)
                    obj[property] = normalized;
                return;
            }

            if (node is not JsonValue strVal || !strVal.TryGetValue<string>(out var cell) || string.IsNullOrWhiteSpace(cell))
                return;

            var parts = SplitEnvironmentSheetList(cell);
            if (parts.Length == 0)
                return;

            var outArr = new JsonArray();
            foreach (var part in parts)
            {
                var (name, weight) = ParseEnvironmentWeightedToken(part);
                if (!string.IsNullOrWhiteSpace(name))
                    outArr.Add(BuildWeightedEntry(name, weight));
            }

            obj[property] = outArr;
        }

        private static string[] SplitEnvironmentSheetList(string cell)
        {
            var s = cell.Trim();
            char[] splitChars = s.Contains('|', StringComparison.Ordinal)
                ? new[] { '|' }
                : new[] { ',', '\n', '\r', ';' };
            return s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim().Trim('"'))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }

        private static (string name, double weight) ParseEnvironmentWeightedToken(string token)
        {
            int colon = token.IndexOf(':');
            if (colon > 0 && colon < token.Length - 1
                && double.TryParse(token[(colon + 1)..].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var w))
            {
                return (token[..colon].Trim(), w);
            }

            return (token, 1.0);
        }

        private static JsonObject BuildWeightedEntry(string name, double weight) =>
            new JsonObject { ["name"] = name, ["weight"] = weight };

        internal static void NormalizeTagsFromSheet(JsonObject obj)
        {
            string? tagsKey = null;
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, "tags", StringComparison.OrdinalIgnoreCase))
                {
                    tagsKey = key;
                    break;
                }
            }

            if (tagsKey is null)
                return;

            if (!obj.TryGetPropertyValue(tagsKey, out var node) || node is null || IsJsonNodeNullOrMissing(node))
            {
                obj.Remove(tagsKey);
                return;
            }

            List<string> normalized;
            if (node is JsonArray arr)
            {
                var raw = new List<string>();
                foreach (var item in arr)
                {
                    if (item is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                        raw.Add(s);
                }

                normalized = GameDataTagHelper.NormalizeDistinct(raw);
            }
            else if (node is JsonValue jv && jv.TryGetValue<string>(out var cell) && !string.IsNullOrWhiteSpace(cell))
                normalized = GameDataTagHelper.ParseCommaSeparatedTags(cell);
            else
            {
                obj.Remove(tagsKey);
                return;
            }

            if (!string.Equals(tagsKey, "tags", StringComparison.Ordinal))
                obj.Remove(tagsKey);

            if (normalized.Count == 0)
                return;

            var outArr = new JsonArray();
            foreach (var t in normalized)
                outArr.Add(t);
            obj["tags"] = outArr;
        }

        /// <summary>
        /// When <c>possibleEnemies</c> is a plain string (not JSON), treat <c>|</c> as delimiter so sheet authors can write
        /// <c>Goblin|Spider|Wolf</c> instead of a JSON array.
        /// </summary>
        private static void NormalizeDungeonsJsonArrayRow(JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("possibleEnemies", out var node) || node is null)
                return;

            if (node is JsonArray)
                return;

            if (node is not JsonValue jv || !jv.TryGetValue<string>(out var s) || string.IsNullOrWhiteSpace(s))
                return;

            var parts = s.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var arr = new JsonArray();
            foreach (var p in parts)
                arr.Add(p);
            obj["possibleEnemies"] = arr;
        }

        /// <summary>
        /// Drops legacy <c>Weight</c>, maps lowercase <c>rarity</c> to <c>Rarity</c>, defaults blank affix tier to Common,
        /// renames human SUFFIXES sheet headers, parses bracketed <c>[STAT:value,...]</c> mechanics into <c>Mechanics</c> JSON array,
        /// and removes sheet-derived helper columns (e.g. split Mechanic 1 / 2 columns).
        /// </summary>
        private static void NormalizeStatBonusesJsonArrayRow(JsonObject obj)
        {
            RenameStatBonusSheetImportHeaders(obj);
            RemoveStatBonusSheetDerivedColumns(obj);
            TryApplyStatBonusBracketMechanics(obj);
            TryApplyStatBonusBracketRequirements(obj);

            string? rarityText = null;
            var hadWeight = false;
            foreach (var kvp in obj.ToList())
            {
                if (string.Equals(kvp.Key, "Weight", StringComparison.OrdinalIgnoreCase))
                {
                    hadWeight = true;
                    obj.Remove(kvp.Key);
                    continue;
                }

                if (string.Equals(kvp.Key, "rarity", StringComparison.OrdinalIgnoreCase))
                {
                    rarityText ??= StatBonusRarityCellToString(kvp.Value);
                    if (!string.Equals(kvp.Key, "Rarity", StringComparison.Ordinal))
                        obj.Remove(kvp.Key);
                }
            }

            if (rarityText == null && obj.TryGetPropertyValue("Rarity", out var rn) && rn is not null)
                rarityText = StatBonusRarityCellToString(rn);

            if (string.IsNullOrWhiteSpace(rarityText) && hadWeight)
                rarityText = "Common";

            var final = string.IsNullOrWhiteSpace(rarityText) ? "Common" : rarityText.Trim();
            obj["Rarity"] = JsonValue.Create(final);
            RemoveStatBonusUnknownJsonKeys(obj);
        }

        /// <summary>After import normalization, drops helper/junk sheet columns (e.g. <c>tags</c>, split mechanic cols) not stored on <see cref="StatBonus"/>.</summary>
        internal static void RemoveStatBonusUnknownJsonKeys(JsonObject obj)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!StatBonusAuthorizedJsonKeys.Contains(key))
                    obj.Remove(key);
            }
        }

        /// <summary>Maps human CONSUMABLES sheet headers to camelCase JSON keys and drops extra columns.</summary>
        private static void NormalizeConsumablesJsonArrayRow(JsonObject obj)
        {
            MoveConsumableJsonKeyIfPresent(obj, "displayName", "Display name", "displayName", "name", "Name");
            MoveConsumableJsonKeyIfPresent(obj, "internalKind", "Internal kind", "internalKind", "kind", "Kind");
            MoveConsumableJsonKeyIfPresent(obj, "effect", "Effect (dungeon-scoped until run ends)", "Effect", "effect");
            MoveConsumableJsonKeyIfPresent(obj, "potency", "Typical potency*", "Typical potency", "potency", "Typical Potency");

            if (obj.TryGetPropertyValue("potency", out JsonNode? potNode) && potNode is JsonValue jv)
            {
                string formatted = FormatConsumablePotencyCell(jv);
                if (formatted.Length > 0)
                    obj["potency"] = JsonValue.Create(formatted);
            }

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!ConsumablesAuthorizedJsonKeys.Contains(key))
                    obj.Remove(key);
            }
        }

        private static void MoveConsumableJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
        {
            if (obj.TryGetPropertyValue(canonicalKey, out JsonNode? existing) && existing != null)
                return;

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                bool matchesAlias = false;
                foreach (string alias in sourceAliases)
                {
                    if (string.Equals(key, alias, StringComparison.OrdinalIgnoreCase))
                    {
                        matchesAlias = true;
                        break;
                    }
                }

                if (!matchesAlias)
                    continue;
                if (!obj.TryGetPropertyValue(key, out JsonNode? val) || val is null)
                    continue;
                obj.Remove(key);
                obj[canonicalKey] = val;
                return;
            }
        }

        private static string FormatConsumablePotencyCell(JsonValue jv)
        {
            if (jv.TryGetValue<string>(out string? s) && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
            if (jv.TryGetValue<int>(out int i))
                return i.ToString(CultureInfo.InvariantCulture);
            if (jv.TryGetValue<long>(out long l))
                return l.ToString(CultureInfo.InvariantCulture);
            if (jv.TryGetValue<double>(out double d))
            {
                double r = Math.Round(d);
                if (Math.Abs(d - r) < 1e-9)
                    return ((long)r).ToString(CultureInfo.InvariantCulture);
                return d.ToString("0.###", CultureInfo.InvariantCulture);
            }

            return "";
        }

        /// <summary>Maps common Google Sheets column titles to <c>StatBonuses.json</c> property names.</summary>
        private static void RenameStatBonusSheetImportHeaders(JsonObject obj)
        {
            MoveStatBonusJsonKeyIfPresent(obj, "Name", "Suffix tags", "Suffix Tags", "suffix tags", "Suffix", "suffix name", "Suffix name", "Affix name");
            MoveStatBonusJsonKeyIfPresent(obj, "Requirements", "stat requirement", "Stat requirement", "Stat Requirement", "requirement", "Requirement", "requirements");
            MoveStatBonusJsonKeyIfPresent(obj, "Mechanics", "Mechanics (bracket)", "mechanics bracket", "Bracket");
            MoveStatBonusJsonKeyIfPresent(obj, "Description", "description");
            MoveStatBonusJsonKeyIfPresent(obj, "StatType", "stat type", "Stat type");
            MoveStatBonusJsonKeyIfPresent(obj, "Value", "value");
            MoveStatBonusJsonKeyIfPresent(obj, "Rarity", "rarity");
        }

        /// <summary>When <paramref name="canonicalKey"/> is missing, copies the first matching alias key onto it and removes the alias.</summary>
        private static void MoveStatBonusJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, canonicalKey, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            foreach (var alias in sourceAliases)
            {
                foreach (var kvp in obj.ToList())
                {
                    if (!string.Equals(kvp.Key, alias, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!obj.TryGetPropertyValue(kvp.Key, out var val))
                        continue;
                    obj.Remove(kvp.Key);
                    obj[canonicalKey] = val?.DeepClone() ?? JsonValue.Create("");
                    return;
                }
            }
        }

        /// <summary>Removes split mechanic columns from the sheet export (keeps <c>Mechanics</c>).</summary>
        private static void RemoveStatBonusSheetDerivedColumns(JsonObject obj)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, "Mechanics", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (key.StartsWith("Mechanic", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("Mechanc", StringComparison.OrdinalIgnoreCase) || // sheet typo "Mechanc i2"
                    key.StartsWith("Mechaniv", StringComparison.OrdinalIgnoreCase)) // sheet typo "Mechaniv 1 value"
                    obj.Remove(key);
            }
        }

        /// <summary>
        /// Fills <c>Mechanics</c> from a bracket cell <c>[ARMOR:5,MAX HEALTH:15]</c> or from any column whose string value matches that pattern.
        /// When mechanics are present, mirrors the first pair onto legacy <c>StatType</c>/<c>Value</c> for older tooling.
        /// </summary>
        private static void TryApplyStatBonusBracketMechanics(JsonObject obj)
        {
            string? bracketSource = null;

            if (obj.TryGetPropertyValue("Mechanics", out var mechNode) && mechNode is not null && !IsJsonNodeNullOrMissing(mechNode))
            {
                if (mechNode is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                    bracketSource = s.Trim();
                else if (mechNode is JsonArray existingArr && existingArr.Count > 0)
                {
                    NormalizeStatBonusMechanicsArrayStatTypes(existingArr);
                    return;
                }
            }

            if (string.IsNullOrEmpty(bracketSource))
            {
                foreach (var kvp in obj)
                {
                    // The Requirements column is also bracket-formatted but parsed separately.
                    if (string.Equals(kvp.Key, "Requirements", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (kvp.Value is JsonValue jv2 && jv2.TryGetValue<string>(out var cell) && cell != null)
                    {
                        string t = cell.Trim();
                        if (t.Length >= 2 && t[0] == '[' && t[^1] == ']')
                        {
                            bracketSource = t;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(bracketSource))
                return;

            var parsed = ParseStatBonusBracketMechanics(bracketSource);
            if (parsed == null || parsed.Count == 0)
                return;

            obj["Mechanics"] = parsed;
            if (parsed.Count > 0 && parsed[0] is JsonObject first)
            {
                if (first.TryGetPropertyValue("StatType", out var st) && st is JsonValue stv && stv.TryGetValue<string>(out var statType))
                    obj["StatType"] = JsonValue.Create(statType);
                if (first.TryGetPropertyValue("Value", out var vn) && vn is JsonValue vv)
                    obj["Value"] = vv.DeepClone();
            }
        }

        private static void NormalizeStatBonusMechanicsArrayStatTypes(JsonArray arr)
        {
            foreach (var el in arr)
            {
                if (el is not JsonObject o)
                    continue;
                if (!o.TryGetPropertyValue("StatType", out var stNode) || stNode is not JsonValue stv || !stv.TryGetValue<string>(out var raw) || raw == null)
                    continue;
                string canon = NormalizeStatBonusSheetStatType(raw);
                o["StatType"] = JsonValue.Create(canon);
            }
        }

        /// <summary>
        /// Fills <c>Requirements</c> from a bracket cell <c>[strength:5,primary:15]</c> on the SUFFIXES sheet
        /// (or whatever cell currently holds the value after header rename / pre-existing JSON). Duplicate keys
        /// keep the maximum value. Already-parsed object form is canonicalized in place. Unknown keys are dropped.
        /// </summary>
        private static void TryApplyStatBonusBracketRequirements(JsonObject obj)
        {
            string? bracketSource = null;

            if (obj.TryGetPropertyValue("Requirements", out var reqNode) && reqNode is not null && !IsJsonNodeNullOrMissing(reqNode))
            {
                if (reqNode is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                {
                    bracketSource = s.Trim();
                }
                else if (reqNode is JsonObject existingObj && existingObj.Count > 0)
                {
                    obj["Requirements"] = NormalizeStatBonusRequirementsObject(existingObj);
                    return;
                }
                else if (reqNode is JsonArray existingArr && existingArr.Count > 0)
                {
                    var fromArr = StatBonusRequirementsArrayToObject(existingArr);
                    if (fromArr != null)
                    {
                        obj["Requirements"] = fromArr;
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(bracketSource))
                return;

            var parsed = ParseStatBonusBracketRequirements(bracketSource);
            if (parsed == null || parsed.Count == 0)
            {
                obj.Remove("Requirements");
                return;
            }

            obj["Requirements"] = parsed;
        }

        /// <summary>
        /// Parses <c>[strength:5,primary:15]</c> into a <see cref="JsonObject"/> mapping canonical lowercase keys to
        /// integer thresholds. Unknown keys (after <see cref="Item.CanonicalizeAttributeRequirementKey"/> normalization)
        /// are accepted but stored as authored; non-numeric values are dropped. Duplicate keys collapse to the maximum.
        /// </summary>
        internal static JsonObject? ParseStatBonusBracketRequirements(string bracketCell)
        {
            string t = bracketCell.Trim();
            if (t.Length < 3 || t[0] != '[' || t[^1] != ']')
                return null;

            string inner = t[1..^1].Trim();
            if (inner.Length == 0)
                return null;

            var segments = inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in segments)
            {
                int colon = segment.LastIndexOf(':');
                if (colon <= 0 || colon >= segment.Length - 1)
                    continue;

                string rawKey = segment[..colon].Trim();
                string rawVal = segment[(colon + 1)..].Trim();
                if (rawKey.Length == 0)
                    continue;

                if (!double.TryParse(rawVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    continue;

                string canonical = Item.CanonicalizeAttributeRequirementKey(rawKey);
                if (string.IsNullOrEmpty(canonical))
                    continue;

                int intValue = (int)Math.Round(value);
                if (aggregated.TryGetValue(canonical, out int cur))
                    aggregated[canonical] = Math.Max(cur, intValue);
                else
                    aggregated[canonical] = intValue;
            }

            if (aggregated.Count == 0)
                return null;

            var result = new JsonObject();
            foreach (var kv in aggregated)
                result[kv.Key] = JsonValue.Create(kv.Value);
            return result;
        }

        /// <summary>Canonicalizes already-parsed <c>Requirements</c> object keys (e.g. JSON files written before normalization).</summary>
        private static JsonObject NormalizeStatBonusRequirementsObject(JsonObject existing)
        {
            var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in existing)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value is not JsonValue jv)
                    continue;
                int value;
                if (jv.TryGetValue<int>(out int iv))
                    value = iv;
                else if (jv.TryGetValue<double>(out double dv))
                    value = (int)Math.Round(dv);
                else
                    continue;

                string canonical = Item.CanonicalizeAttributeRequirementKey(kvp.Key);
                if (string.IsNullOrEmpty(canonical))
                    continue;

                if (aggregated.TryGetValue(canonical, out int cur))
                    aggregated[canonical] = Math.Max(cur, value);
                else
                    aggregated[canonical] = value;
            }

            var result = new JsonObject();
            foreach (var kv in aggregated)
                result[kv.Key] = JsonValue.Create(kv.Value);
            return result;
        }

        /// <summary>Converts a list-form requirements array (<c>[{key,value},…]</c>) into the canonical dictionary form.</summary>
        private static JsonObject? StatBonusRequirementsArrayToObject(JsonArray arr)
        {
            var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var el in arr)
            {
                if (el is not JsonObject o)
                    continue;
                string? key = null;
                int? value = null;
                foreach (var kvp in o)
                {
                    if (kvp.Value is not JsonValue jv)
                        continue;
                    if (key == null && (string.Equals(kvp.Key, "Key", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(kvp.Key, "StatType", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(kvp.Key, "Attribute", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (jv.TryGetValue<string>(out var s))
                            key = s;
                    }
                    else if (value == null && (string.Equals(kvp.Key, "Value", StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(kvp.Key, "Required", StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(kvp.Key, "Threshold", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (jv.TryGetValue<int>(out int iv))
                            value = iv;
                        else if (jv.TryGetValue<double>(out double dv))
                            value = (int)Math.Round(dv);
                    }
                }

                if (string.IsNullOrWhiteSpace(key) || value == null)
                    continue;
                string canonical = Item.CanonicalizeAttributeRequirementKey(key);
                if (string.IsNullOrEmpty(canonical))
                    continue;

                if (aggregated.TryGetValue(canonical, out int cur))
                    aggregated[canonical] = Math.Max(cur, value.Value);
                else
                    aggregated[canonical] = value.Value;
            }

            if (aggregated.Count == 0)
                return null;

            var result = new JsonObject();
            foreach (var kv in aggregated)
                result[kv.Key] = JsonValue.Create(kv.Value);
            return result;
        }

        /// <summary>Parses <c>[KEY:value,...]</c> where value is numeric; keys may contain spaces (e.g. <c>MAX HEALTH</c>).</summary>
        internal static JsonArray? ParseStatBonusBracketMechanics(string bracketCell)
        {
            string t = bracketCell.Trim();
            if (t.Length < 3 || t[0] != '[' || t[^1] != ']')
                return null;

            string inner = t[1..^1].Trim();
            if (inner.Length == 0)
                return null;

            var segments = inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var arr = new JsonArray();
            foreach (var segment in segments)
            {
                int colon = segment.LastIndexOf(':');
                if (colon <= 0 || colon >= segment.Length - 1)
                    continue;

                string rawKey = segment[..colon].Trim();
                string rawVal = segment[(colon + 1)..].Trim();
                if (rawKey.Length == 0)
                    continue;

                if (!double.TryParse(rawVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    continue;

                var pair = new JsonObject
                {
                    ["StatType"] = JsonValue.Create(NormalizeStatBonusSheetStatType(rawKey)),
                    ["Value"] = JsonValue.Create(value)
                };
                arr.Add(pair);
            }

            return arr.Count > 0 ? arr : null;
        }

        /// <summary>
        /// Normalizes SUFFIXES sheet mechanic keys to values used in <c>StatBonuses.json</c> / <see cref="EquipmentBonusCalculator"/>.
        /// </summary>
        internal static string NormalizeStatBonusSheetStatType(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                return "";

            string collapsed = string.Join(" ", rawKey.Trim()
                .Replace('_', ' ')
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            string upper = collapsed.ToUpperInvariant();

            return upper switch
            {
                "ARMOR" => "Armor",
                "HEALTH" => "Health",
                "MAX HEALTH" => "Health",
                "MAXHEALTH" => "Health",
                "MAX ATTACK SPEED" => "AttackSpeed",
                "MAXATTACKSPEED" => "AttackSpeed",
                "ATTACK SPEED" => "AttackSpeed",
                "ATTACKSPEED" => "AttackSpeed",
                "SPEED" => "AttackSpeed",
                "DAMAGE" => "Damage",
                "BASE DAMAGE" => "Damage",
                "BASEDAMAGE" => "Damage",
                "WEAPON DAMAGE" => "Damage",
                "WEAPONDAMAGE" => "Damage",
                "HEALTH REGEN" => "HealthRegen",
                "HEALTHREGEN" => "HealthRegen",
                "ROLL" => "RollBonus",
                "ROLLBONUS" => "RollBonus",
                "ROLL BONUS" => "RollBonus",
                "MAGICFIND" => "MagicFind",
                "MAGIC FIND" => "MagicFind",
                "HIT" => "HIT",
                "COMBO" => "COMBO",
                "CRIT" => "CRIT",
                "ACCURACY" => "ACCURACY",
                "STR" or "STRENGTH" => "STR",
                "AGI" or "AGILITY" => "AGI",
                "TEC" or "TECHNIQUE" => "TEC",
                "INT" or "INTELLIGENCE" => "INT",
                "ALL" => "ALL",
                _ => collapsed
            };
        }

        private static string? StatBonusRarityCellToString(JsonNode? n)
        {
            if (n is null || IsJsonNodeNullOrMissing(n))
                return null;
            if (n is JsonValue jv)
            {
                if (jv.TryGetValue<string>(out var s))
                    return s;
                if (jv.TryGetValue<double>(out var d))
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return n.ToJsonString().Trim('"');
        }

        /// <summary>
        /// Removes sheet-only columns (<c>dps</c>, <c>balance</c>) and normalizes combat stats to canonical JSON keys
        /// <c>baseDamage</c> (integer, ≥ 1), optional inclusive <c>damageBonusMin</c> / <c>damageBonusMax</c> (sheet headers include the typo <c>Min BOnus</c> and <c>Max Bonus</c>), then <c>attackSpeed</c> (number).
        /// </summary>
        private static void NormalizeWeaponsJsonArrayRow(JsonObject obj)
        {
            RemoveWeaponJsonKeyIgnoreCase(obj, "dps");
            RemoveWeaponJsonKeyIgnoreCase(obj, "balance");

            // Google Sheets often use spaced headers (e.g. "Base Damage", "Attack Speed") before canonical camelCase.
            RenameWeaponJsonKeyIfPresent(obj, "baseDamage", "base damage", "basedamage");
            RenameWeaponJsonKeyIfPresent(obj, "attackSpeed", "attack speed", "attackspeed");

            CanonicalizeWeaponField(obj, "baseDamage", preferWholeNumber: true);
            CanonicalizeWeaponField(obj, "attackSpeed", preferWholeNumber: false);
            NormalizeWeaponDamageBonusRange(obj);
            NormalizeTagsFromSheet(obj);
        }

        /// <summary>When a row uses a human-readable header instead of JSON camelCase, move its value onto <paramref name="canonicalKey"/>.</summary>
        private static void RenameWeaponJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] alternateHeaders)
        {
            if (obj.ContainsKey(canonicalKey))
                return;

            foreach (var alt in alternateHeaders)
            {
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    if (!string.Equals(key, alt, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!obj.TryGetPropertyValue(key, out var node))
                    {
                        obj.Remove(key);
                        return;
                    }

                    obj.Remove(key);
                    if (node is not null && !IsJsonNodeNullOrMissing(node))
                        obj[canonicalKey] = node;
                    return;
                }
            }
        }

        /// <summary>Google Sheet / CSV headers for min damage bonus (case-insensitive); includes common typo <c>Min BOnus</c>.</summary>
        private static readonly string[] WeaponDamageBonusMinHeaderAliases =
        {
            "damageBonusMin", "minDamageBonus", "minBonus", "min bonus",
            "Min BOnus", "Min Bonus"
        };

        private static readonly string[] WeaponDamageBonusMaxHeaderAliases =
        {
            "damageBonusMax", "maxDamageBonus", "maxBonus", "max bonus", "Max Bonus"
        };

        /// <summary>Consumes known sheet/legacy keys and writes canonical <c>damageBonusMin</c> / <c>damageBonusMax</c> (non-negative integers, min ≤ max).</summary>
        private static void NormalizeWeaponDamageBonusRange(JsonObject obj)
        {
            int minV = ConsumeWeaponIntByHeaderAliases(obj, WeaponDamageBonusMinHeaderAliases);
            int maxV = ConsumeWeaponIntByHeaderAliases(obj, WeaponDamageBonusMaxHeaderAliases);
            minV = Math.Max(0, minV);
            maxV = Math.Max(0, maxV);
            if (maxV < minV)
                (minV, maxV) = (maxV, minV);

            obj["damageBonusMin"] = JsonValue.Create(minV);
            obj["damageBonusMax"] = JsonValue.Create(maxV);
        }

        /// <summary>Finds the first object key matching any alias (ordinal case-insensitive), removes it, and returns its integer value.</summary>
        private static int ConsumeWeaponIntByHeaderAliases(JsonObject obj, string[] aliases)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                bool matches = false;
                foreach (var a in aliases)
                {
                    if (string.Equals(key, a, StringComparison.OrdinalIgnoreCase))
                    {
                        matches = true;
                        break;
                    }
                }

                if (!matches)
                    continue;

                int v = 0;
                if (obj.TryGetPropertyValue(key, out var node) && node is not null && !IsJsonNodeNullOrMissing(node)
                    && TryGetWeaponNumericFromJsonNode(node, out double d))
                    v = (int)Math.Round(d, MidpointRounding.AwayFromZero);
                obj.Remove(key);
                return v;
            }

            return 0;
        }

        private static void RemoveWeaponJsonKeyIgnoreCase(JsonObject obj, string logicalName)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, logicalName, StringComparison.OrdinalIgnoreCase))
                    obj.Remove(key);
            }
        }

        /// <summary>Renames matching keys to <paramref name="canonicalKey"/> (camelCase) and coerces values for runtime.</summary>
        private static void CanonicalizeWeaponField(JsonObject obj, string canonicalKey, bool preferWholeNumber)
        {
            string? foundKey = null;
            foreach (var key in obj.Select(kvp => kvp.Key))
            {
                if (string.Equals(key, canonicalKey, StringComparison.OrdinalIgnoreCase))
                {
                    foundKey = key;
                    break;
                }
            }

            if (foundKey is null)
                return;

            if (!obj.TryGetPropertyValue(foundKey, out var node) || node is null || IsJsonNodeNullOrMissing(node))
            {
                if (!string.Equals(foundKey, canonicalKey, StringComparison.Ordinal))
                    obj.Remove(foundKey);
                return;
            }

            JsonNode? coerced = preferWholeNumber ? CoerceWeaponBaseDamageJson(node) : CoerceWeaponAttackSpeedJson(node);
            if (!string.Equals(foundKey, canonicalKey, StringComparison.Ordinal))
                obj.Remove(foundKey);

            if (coerced is null || IsJsonNodeNullOrMissing(coerced))
                obj.Remove(canonicalKey);
            else
                obj[canonicalKey] = coerced;
        }

        private static JsonNode? CoerceWeaponBaseDamageJson(JsonNode node)
        {
            if (!TryGetWeaponNumericFromJsonNode(node, out double d))
                return null;
            int rounded = (int)Math.Max(1, Math.Round(d, MidpointRounding.AwayFromZero));
            return JsonValue.Create(rounded);
        }

        private static JsonNode? CoerceWeaponAttackSpeedJson(JsonNode node)
        {
            if (!TryGetWeaponNumericFromJsonNode(node, out double d))
                return null;
            if (double.IsNaN(d) || double.IsInfinity(d) || d <= 0)
                return null;
            return JsonValue.Create(d);
        }

        private static bool TryGetWeaponNumericFromJsonNode(JsonNode node, out double value)
        {
            value = 0;
            if (node is not JsonValue jv)
                return false;

            if (jv.TryGetValue<double>(out value))
                return true;
            if (jv.TryGetValue<long>(out var l))
            {
                value = l;
                return true;
            }

            if (jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
            {
                s = s.Trim().TrimEnd('%');
                return double.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out value);
            }

            return false;
        }
    }
}
