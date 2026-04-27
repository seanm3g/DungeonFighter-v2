using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

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
        StatBonuses
    }

    public static class JsonArraySheetConverter
    {
        /// <summary>
        /// Preferred column order for the weapons sheet / <c>Weapons.json</c> round-trip.
        /// <c>dps</c> and <c>balance</c> are sheet-only balancing helpers and are dropped when importing CSV → JSON.
        /// Runtime fields use JSON keys <c>baseDamage</c> (whole number), inclusive <c>damageBonusMin</c> / <c>damageBonusMax</c>
        /// (rolled onto base when loot is generated), then <c>attackSpeed</c> (fractional allowed;
        /// 1 = baseline swing spacing, &gt;1 slower, &lt;1 faster in combat).
        /// </summary>
        public static readonly string[] WeaponsCanonicalHeaders =
        {
            "type", "name", "dps", "balance", "baseDamage", "damageBonusMin", "damageBonusMax", "attackSpeed", "tier", "attributeRequirements", "tags", "Compelled Action"
        };

        public static readonly string[] ModificationsCanonicalHeaders =
            { "DiceResult", "ItemRank", "Name", "Description", "Effect", "MinValue", "MaxValue" };

        public static readonly string[] ArmorCanonicalHeaders =
            { "slot", "name", "armor", "tier", "attributeRequirements", "tags" };

        /// <summary>
        /// Column order for ENEMIES tab — nested stats are one column each (<c>baseAttributes.strength</c>, …).
        /// Push writes two header rows: category band (<c>base attributes</c>, <c>growth</c>) then short names.
        /// Import accepts that layout or a legacy single row of dotted headers; legacy <c>overrides.*</c> columns are promoted into HP fields and dropped.
        /// </summary>
        public static readonly string[] EnemiesCanonicalHeaders =
        {
            "name", "archetype",
            "baseAttributes.strength", "baseAttributes.agility", "baseAttributes.technique", "baseAttributes.intelligence",
            "growthPerLevel.strength", "growthPerLevel.agility", "growthPerLevel.technique", "growthPerLevel.intelligence",
            "baseHealth", "healthGrowthPerLevel",
            "actions", "isLiving", "description", "colorOverride", "tags"
        };

        private static readonly string[] EnemyNestedObjectNames = { "overrides", "baseAttributes", "growthPerLevel" };

        /// <summary>Historical JSON / sheet root keys that belong in nested overrides / growth columns (not trailing extras).</summary>
        private static readonly HashSet<string> EnemyLegacyRootStatKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "strength", "agility", "technique", "intelligence"
        };

        /// <summary>ENEMIES tab uses two header rows (category band + short names); other JSON-array tabs use one.</summary>
        public static int GetTabularSheetHeaderRowCount(GameDataTabularSheetKind kind) =>
            kind == GameDataTabularSheetKind.Enemies ? 2 : 1;

        /// <summary>Row-1 category labels on the ENEMIES sheet (repeated per column under that group).</summary>
        public const string EnemySheetCategoryOverrides = "overrides";

        public const string EnemySheetCategoryBaseAttributes = "base attributes";

        public const string EnemySheetCategoryGrowth = "growth";

        public static readonly string[] EnvironmentsCanonicalHeaders =
            { "name", "description", "theme", "isHostile", "actions", "enemies" };

        /// <summary>Column order for DUNGEONS tab — matches <c>Dungeons.json</c> / runtime dungeon records.</summary>
        public static readonly string[] DungeonsCanonicalHeaders =
            { "name", "theme", "minLevel", "maxLevel", "possibleEnemies", "colorOverride" };

        public static readonly string[] StatBonusesCanonicalHeaders =
            { "Name", "Description", "Value", "Rarity", "StatType", "ItemRank" };

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
                _ => Array.Empty<string>()
            };

        /// <summary>
        /// Builds sheet rows from a JSON array file body. Row 0 = headers (or ENEMIES: rows 0–1 = category band + short headers).
        /// </summary>
        public static List<IList<object>> BuildPushValueRows(string jsonFileText, GameDataTabularSheetKind kind)
        {
            if (kind == GameDataTabularSheetKind.Enemies)
                return BuildEnemyPushValueRows(jsonFileText);

            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = GetCanonicalHeaders(kind).ToList();
            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);
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
                    if (!el.TryGetProperty(h, out var prop))
                    {
                        row.Add("");
                        continue;
                    }
                    row.Add(JsonElementToCellString(prop));
                }
                rows.Add(row);
            }

            return rows;
        }

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
                    if (EnemyLegacyRootStatKeys.Contains(p.Name))
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
            if (string.Equals(canonicalHeader, "baseHealth", StringComparison.Ordinal))
                return EnemySheetCategoryBaseAttributes;
            if (canonicalHeader.StartsWith("growthPerLevel.", StringComparison.Ordinal))
                return EnemySheetCategoryGrowth;
            if (string.Equals(canonicalHeader, "healthGrowthPerLevel", StringComparison.Ordinal))
                return EnemySheetCategoryGrowth;
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

        private static string GetEnemyPushCell(JsonElement el, string header)
        {
            if (header.Contains('.', StringComparison.Ordinal))
            {
                if (TryGetPropertyPath(el, header, out var at))
                    return JsonElementToCellString(at);
                if (TryGetEnemyLegacyRootForCanonicalColumn(el, header, out var legacy))
                    return JsonElementToCellString(legacy);
                return "";
            }

            if (!el.TryGetProperty(header, out var prop))
                return "";
            return JsonElementToCellString(prop);
        }

        /// <summary>
        /// When nested <c>growthPerLevel.*</c> is absent but the row still has historical root <c>strength</c>, <c>agility</c>, …,
        /// surface those values in the canonical growth columns (push only).
        /// </summary>
        private static bool TryGetEnemyLegacyRootForCanonicalColumn(JsonElement el, string dottedHeader, out JsonElement found)
        {
            found = default;
            if (el.ValueKind != JsonValueKind.Object)
                return false;

            if (string.Equals(dottedHeader, "growthPerLevel.strength", StringComparison.OrdinalIgnoreCase))
            {
                if (!el.TryGetProperty("strength", out found) || found.ValueKind != JsonValueKind.Number)
                    return false;
                return found.GetDouble() < 0.45;
            }
            if (string.Equals(dottedHeader, "growthPerLevel.agility", StringComparison.OrdinalIgnoreCase))
            {
                if (!el.TryGetProperty("agility", out found) || found.ValueKind != JsonValueKind.Number)
                    return false;
                return found.GetDouble() < 0.45;
            }
            if (string.Equals(dottedHeader, "growthPerLevel.technique", StringComparison.OrdinalIgnoreCase))
            {
                if (!el.TryGetProperty("technique", out found) || found.ValueKind != JsonValueKind.Number)
                    return false;
                return found.GetDouble() < 0.45;
            }
            if (string.Equals(dottedHeader, "growthPerLevel.intelligence", StringComparison.OrdinalIgnoreCase))
            {
                if (!el.TryGetProperty("intelligence", out found) || found.ValueKind != JsonValueKind.Number)
                    return false;
                return found.GetDouble() < 0.45;
            }

            return false;
        }

        /// <summary>Legacy sheet <c>overrides.health</c> (merged object) is folded into HP fields, then the object is removed.</summary>
        private static void PromoteLegacyEnemyOverridesAndRemove(JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("overrides", out var onode) || onode is null)
                return;

            JsonObject? ov = onode as JsonObject;
            if (ov is null && onode is JsonValue jvStr && jvStr.TryGetValue<string>(out var raw) && !string.IsNullOrWhiteSpace(raw))
            {
                var t = raw.Trim();
                if (t.Length >= 2 && t[0] == '{')
                {
                    try
                    {
                        if (JsonNode.Parse(t) is JsonObject parsed)
                            ov = parsed;
                    }
                    catch
                    {
                        // leave ov null
                    }
                }
            }

            if (ov is null)
                return;

            if (ov.TryGetPropertyValue("health", out var hn) && hn != null && TryGetWeaponNumericFromJsonNode(hn, out var healthMul))
            {
                if (obj.TryGetPropertyValue("baseHealth", out var bh) && bh != null && TryGetWeaponNumericFromJsonNode(bh, out var b0))
                    obj["baseHealth"] = JsonValue.Create(b0 * healthMul);
                else if (obj.TryGetPropertyValue("healthGrowthPerLevel", out var gh) && gh != null && TryGetWeaponNumericFromJsonNode(gh, out var g0))
                    obj["healthGrowthPerLevel"] = JsonValue.Create(g0 * healthMul);
            }

            obj.Remove("overrides");
        }

        /// <summary>
        /// Moves historical root stat numbers into <c>growthPerLevel</c> so CSV → JSON matches the runtime model.
        /// </summary>
        private static void HoistEnemyLegacyRootNumbersIntoNested(JsonObject obj)
        {
            static JsonObject GetOrCreateObject(JsonObject parent, string name)
            {
                if (parent.TryGetPropertyValue(name, out var n) && n is JsonObject jo)
                    return jo;
                var created = new JsonObject();
                parent[name] = created;
                return created;
            }

            static bool TryTakeRootNumber(JsonObject o, string key, out double v)
            {
                v = 0;
                if (!o.TryGetPropertyValue(key, out var node) || node is not JsonValue jv)
                    return false;
                try
                {
                    v = jv.GetValue<double>();
                    o.Remove(key);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            double strength = double.NaN, agility = double.NaN, technique = double.NaN, intelligence = double.NaN;
            if (TryTakeRootNumber(obj, "strength", out var s)) strength = s;
            if (TryTakeRootNumber(obj, "agility", out var ag)) agility = ag;
            if (TryTakeRootNumber(obj, "technique", out var t)) technique = t;
            if (TryTakeRootNumber(obj, "intelligence", out var i)) intelligence = i;

            var growth = GetOrCreateObject(obj, "growthPerLevel");

            if (!double.IsNaN(strength))
            {
                if (!growth.ContainsKey("strength"))
                    growth["strength"] = JsonValue.Create(strength);
                else
                    obj["strength"] = JsonValue.Create(strength);
            }

            if (!double.IsNaN(agility))
            {
                if (!growth.ContainsKey("agility"))
                    growth["agility"] = JsonValue.Create(agility);
                else
                    obj["agility"] = JsonValue.Create(agility);
            }

            if (!double.IsNaN(technique))
            {
                if (!growth.ContainsKey("technique"))
                    growth["technique"] = JsonValue.Create(technique);
                else
                    obj["technique"] = JsonValue.Create(technique);
            }

            if (!double.IsNaN(intelligence))
            {
                if (!growth.ContainsKey("intelligence"))
                    growth["intelligence"] = JsonValue.Create(intelligence);
                else
                    obj["intelligence"] = JsonValue.Create(intelligence);
            }
        }

        private static bool TryGetPropertyPath(JsonElement el, string dottedPath, out JsonElement found)
        {
            found = default;
            var parts = dottedPath.Split('.');
            if (parts.Length < 2)
                return false;
            JsonElement cur = el;
            foreach (var part in parts)
            {
                if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(part, out cur))
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
                for (int i = 0; i < headerRow.Length; i++)
                {
                    // Google / Excel CSV exports may prefix the file with U+FEFF, which lands on the first header cell.
                    string header = (headerRow[i] ?? "").Trim().TrimStart('\uFEFF');
                    if (header.Length == 0)
                        continue;
                    string cell = i < cells.Length ? cells[i] ?? "" : "";
                    obj[header] = CellToJsonNode(cell);
                }

                if (kind == GameDataTabularSheetKind.Enemies)
                {
                    MergeEnemyFlatColumnsToNested(obj);
                    PromoteLegacyEnemyOverridesAndRemove(obj);
                    HoistEnemyLegacyRootNumbersIntoNested(obj);
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
            for (int i = 0; i < width; i++)
            {
                string cat = i < row0.Length ? row0[i]?.Trim() ?? "" : "";
                string sub = i < row1.Length ? row1[i]?.Trim() ?? "" : "";
                combined[i] = CombineEnemyImportHeaderCell(cat, sub);
            }

            headerRow = combined;
            firstDataRow = 2;
        }

        /// <summary>
        /// True when row 0 is the category band (blank over <c>name</c>, repeated group labels) and row 1 is short column names
        /// (starts with <c>name</c>), not a data row.
        /// </summary>
        private static bool EnemySheetTwoRowHeaderFormat(string[] row0, string[] row1)
        {
            if (row1.Length == 0)
                return false;
            if (!string.Equals(row1[0]?.Trim(), "name", StringComparison.OrdinalIgnoreCase))
                return false;
            if (row0.Length > 0 && !string.IsNullOrWhiteSpace(row0[0]))
                return false;
            if (!EnemySheetRowLooksLikeCategoryBand(row0) || EnemySheetRowHasDottedStatKeys(row0))
                return false;
            return true;
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
                    || string.Equals(t, EnemySheetCategoryGrowth, StringComparison.OrdinalIgnoreCase))
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

            if (category.Length == 0)
                return subHeader;

            if (string.Equals(category, EnemySheetCategoryOverrides, StringComparison.OrdinalIgnoreCase))
                return "overrides." + subHeader;

            if (string.Equals(category, EnemySheetCategoryBaseAttributes, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "baseHealth", StringComparison.OrdinalIgnoreCase))
                    return "baseHealth";
                return "baseAttributes." + subHeader;
            }

            if (string.Equals(category, EnemySheetCategoryGrowth, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "healthGrowthPerLevel", StringComparison.OrdinalIgnoreCase))
                    return "healthGrowthPerLevel";
                return "growthPerLevel." + subHeader;
            }

            return subHeader;
        }

        private static JsonSerializerOptions GetSerializerOptions(GameDataTabularSheetKind kind)
        {
            var o = new JsonSerializerOptions { WriteIndented = true };
            if (kind == GameDataTabularSheetKind.Weapons || kind == GameDataTabularSheetKind.Armor
                || kind == GameDataTabularSheetKind.Enemies || kind == GameDataTabularSheetKind.Environments
                || kind == GameDataTabularSheetKind.Dungeons)
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
            NormalizeEnemyActionsFromSheet(obj);
            NormalizeEnemyTagsFromSheet(obj);
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

            var parts = s.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var arr = new JsonArray();
            foreach (var p in parts)
                arr.Add(p);
            obj["actions"] = arr;
        }

        /// <summary>
        /// Sheet <c>tags</c> may be a JSON array, a single token, or comma / semicolon / pipe lists (same as in-game settings).
        /// Coerce to a canonical lowercase <c>tags</c> JSON array so <see cref="EnemyData.Tags"/> deserializes reliably.
        /// </summary>
        private static void NormalizeEnemyTagsFromSheet(JsonObject obj)
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
        /// Drops legacy <c>Weight</c>, maps lowercase <c>rarity</c> to <c>Rarity</c>, and defaults blank affix tier to Common.
        /// </summary>
        private static void NormalizeStatBonusesJsonArrayRow(JsonObject obj)
        {
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
