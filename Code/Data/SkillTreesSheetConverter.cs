using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Class Upgrades tab ↔ <c>SkillTrees.json</c>: flat node rows grouped by Class/Tree/Weapon into nested trees.
    /// </summary>
    public static class SkillTreesSheetConverter
    {
        public static readonly string[] CanonicalHeaders =
        {
            "Class", "Tree", "Weapon", "Name", "Effect", "Payoff",
            "UnlockAction", "Requires", "Type", "Stat", "Id", "Branch", "Tier", "Cost", "CustomEffectId"
        };

        private static readonly HashSet<string> NodeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Passive", "Action", "Mastery", "Rule"
        };

        private static readonly Regex UnlockActionPattern = new(
            @"^[A-Z0-9][A-Z0-9' \-]*$",
            RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonWrite = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public static string CsvToSkillTreesJsonText(string csvContent, SkillTreesConfig? preserveIdentityFrom = null)
        {
            var cfg = ParseCsvToConfig(csvContent, preserveIdentityFrom);
            return ToJsonText(cfg);
        }

        public static string ToJsonText(SkillTreesConfig cfg) =>
            JsonSerializer.Serialize(cfg ?? SkillTreesConfig.CreateEmpty(), JsonWrite);

        public static SkillTreesConfig ParseCsvToConfig(string csvContent, SkillTreesConfig? preserveIdentityFrom = null)
        {
            var rows = SimpleGameDataCsvParser.ParseToRows(csvContent);
            var trees = new List<SkillTreeDefinition>();
            if (rows.Count == 0)
                return new SkillTreesConfig { Trees = trees }.Normalize();

            if (!TryFindHeaderRow(rows, out int headerRow, out var cols))
                return new SkillTreesConfig { Trees = trees }.Normalize();

            var identityByClass = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (preserveIdentityFrom?.Trees != null)
            {
                foreach (var t in preserveIdentityFrom.Trees)
                {
                    if (!string.IsNullOrWhiteSpace(t.ClassKey) && !string.IsNullOrWhiteSpace(t.Identity))
                        identityByClass[t.ClassKey.Trim()] = t.Identity.Trim();
                }
            }

            // classKey → tree under construction (first-seen metadata wins)
            var byClass = new Dictionary<string, SkillTreeDefinition>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            for (int r = headerRow + 1; r < rows.Count; r++)
            {
                var cells = rows[r];
                if (cells == null || cells.Length == 0)
                    continue;

                string[] fields = ExtractCanonicalFields(cells, cols);
                HealUnlockRequiresShift(fields);

                string classKey = fields[0].Trim();
                string id = fields[10].Trim();
                string name = fields[3].Trim();
                if (string.IsNullOrEmpty(classKey) || string.IsNullOrEmpty(id))
                    continue;
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(fields[4].Trim()))
                    continue;

                if (!byClass.TryGetValue(classKey, out var tree))
                {
                    tree = new SkillTreeDefinition
                    {
                        ClassKey = classKey,
                        Title = fields[1].Trim(),
                        Weapon = fields[2].Trim(),
                        Stat = fields[9].Trim(),
                        Identity = identityByClass.TryGetValue(classKey, out var idn) ? idn : "",
                        Nodes = new List<SkillTreeNodeDefinition>()
                    };
                    byClass[classKey] = tree;
                    order.Add(classKey);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(tree.Title) && !string.IsNullOrWhiteSpace(fields[1]))
                        tree.Title = fields[1].Trim();
                    if (string.IsNullOrWhiteSpace(tree.Weapon) && !string.IsNullOrWhiteSpace(fields[2]))
                        tree.Weapon = fields[2].Trim();
                    if (string.IsNullOrWhiteSpace(tree.Stat) && !string.IsNullOrWhiteSpace(fields[9]))
                        tree.Stat = fields[9].Trim();
                }

                int.TryParse(fields[12].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tier);
                int.TryParse(fields[13].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cost);
                string unlock = fields[6].Trim();
                string custom = fields[14].Trim();
                string type = fields[8].Trim();
                if (string.IsNullOrWhiteSpace(type) || !NodeTypes.Contains(type))
                    type = "Passive";

                tree.Nodes.Add(new SkillTreeNodeDefinition
                {
                    Id = id,
                    Name = name,
                    Branch = fields[11].Trim(),
                    Tier = tier,
                    Type = type,
                    Cost = cost,
                    Requires = ParseRequires(fields[7]),
                    Effect = fields[4] ?? "",
                    Payoff = fields[5] ?? "",
                    UnlockActionName = string.IsNullOrWhiteSpace(unlock) ? null : unlock,
                    CustomEffectId = string.IsNullOrWhiteSpace(custom) ? null : custom
                });
            }

            foreach (string key in order)
                trees.Add(byClass[key]);

            return new SkillTreesConfig { Trees = trees }.Normalize();
        }

        public static List<IList<object>> BuildPushValueRows(SkillTreesConfig? cfg)
        {
            cfg ??= SkillTreesConfig.CreateEmpty();
            cfg.Normalize();
            var rows = new List<IList<object>> { new List<object>(CanonicalHeaders) };

            foreach (var tree in cfg.Trees ?? Enumerable.Empty<SkillTreeDefinition>())
            {
                foreach (var node in tree.Nodes ?? Enumerable.Empty<SkillTreeNodeDefinition>())
                {
                    rows.Add(new List<object>
                    {
                        tree.ClassKey ?? "",
                        tree.Title ?? "",
                        tree.Weapon ?? "",
                        node.Name ?? "",
                        node.Effect ?? "",
                        node.Payoff ?? "",
                        node.UnlockActionName ?? "",
                        string.Join(",", node.Requires ?? new List<string>()),
                        node.Type ?? "Passive",
                        tree.Stat ?? "",
                        node.Id ?? "",
                        node.Branch ?? "",
                        node.Tier.ToString(CultureInfo.InvariantCulture),
                        node.Cost.ToString(CultureInfo.InvariantCulture),
                        node.CustomEffectId ?? node.Id ?? ""
                    });
                }
            }

            return rows;
        }

        /// <summary>
        /// Sheet rows often omit the empty Requires cell when UnlockAction is blank (or put Requires in the UnlockAction column).
        /// Heal so Type/Stat/Id/Branch/Tier/Cost/CustomEffectId land on the correct columns.
        /// </summary>
        internal static void HealUnlockRequiresShift(string[] fields)
        {
            if (fields == null || fields.Length < 15)
                return;

            string unlock = (fields[6] ?? "").Trim();
            string requires = (fields[7] ?? "").Trim();

            if (IsNodeType(requires) && string.IsNullOrEmpty(unlock))
            {
                ShiftRightFrom(fields, 7);
                fields[7] = "";
                return;
            }

            if (IsNodeType(requires) && !string.IsNullOrEmpty(unlock) && !LooksLikeUnlockAction(unlock))
            {
                ShiftRightFrom(fields, 6);
                fields[6] = "";
                return;
            }

            if (IsNodeType(unlock) && !IsNodeType(requires) && string.IsNullOrEmpty(requires))
            {
                ShiftRightFrom(fields, 6);
                ShiftRightFrom(fields, 7);
                fields[6] = "";
                fields[7] = "";
            }
        }

        internal static bool LooksLikeUnlockAction(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            string s = value.Trim();
            if (IsNodeType(s))
                return false;
            if (!UnlockActionPattern.IsMatch(s))
                return false;
            return s.Any(char.IsLetter);
        }

        private static bool IsNodeType(string value) =>
            !string.IsNullOrWhiteSpace(value) && NodeTypes.Contains(value.Trim());

        private static void ShiftRightFrom(string[] fields, int start)
        {
            for (int i = fields.Length - 1; i > start; i--)
                fields[i] = fields[i - 1];
            fields[start] = "";
        }

        private static List<string> ParseRequires(string cell)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(cell))
                return list;
            foreach (var part in cell.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string p = part.Trim();
                if (p.Length > 0)
                    list.Add(p);
            }
            return list;
        }

        private static string[] ExtractCanonicalFields(string[] cells, ColumnMap cols)
        {
            string[] fields = new string[CanonicalHeaders.Length];
            for (int i = 0; i < fields.Length; i++)
                fields[i] = "";

            void Set(int canonicalIndex, int sheetCol)
            {
                if (sheetCol < 0 || sheetCol >= cells.Length)
                    return;
                fields[canonicalIndex] = cells[sheetCol] ?? "";
            }

            Set(0, cols.Class);
            Set(1, cols.Tree);
            Set(2, cols.Weapon);
            Set(3, cols.Name);
            Set(4, cols.Effect);
            Set(5, cols.Payoff);
            Set(6, cols.UnlockAction);
            Set(7, cols.Requires);
            Set(8, cols.Type);
            Set(9, cols.Stat);
            Set(10, cols.Id);
            Set(11, cols.Branch);
            Set(12, cols.Tier);
            Set(13, cols.Cost);
            Set(14, cols.CustomEffectId);
            return fields;
        }

        private static bool TryFindHeaderRow(List<string[]> rows, out int headerRow, out ColumnMap cols)
        {
            headerRow = -1;
            cols = default;
            for (int r = 0; r < Math.Min(rows.Count, 20); r++)
            {
                var cells = rows[r];
                if (cells.Length < 8)
                    continue;

                var map = new ColumnMap
                {
                    Class = -1, Tree = -1, Weapon = -1, Name = -1, Effect = -1, Payoff = -1,
                    UnlockAction = -1, Requires = -1, Type = -1, Stat = -1, Id = -1,
                    Branch = -1, Tier = -1, Cost = -1, CustomEffectId = -1
                };

                for (int col = 0; col < cells.Length; col++)
                {
                    string h = (cells[col] ?? "").Trim().ToLowerInvariant();
                    if (h.Length == 0) continue;

                    if (h is "class" or "classkey")
                        map.Class = col;
                    else if (h is "tree" or "title")
                        map.Tree = col;
                    else if (h is "weapon")
                        map.Weapon = col;
                    else if (h is "name")
                        map.Name = col;
                    else if (h is "effect")
                        map.Effect = col;
                    else if (h is "payoff")
                        map.Payoff = col;
                    else if (h is "unlockaction" or "unlockactionname" or "unlock action")
                        map.UnlockAction = col;
                    else if (h is "requires" or "require")
                        map.Requires = col;
                    else if (h is "type")
                        map.Type = col;
                    else if (h is "stat")
                        map.Stat = col;
                    else if (h is "id" or "nodeid" or "node id")
                        map.Id = col;
                    else if (h is "branch")
                        map.Branch = col;
                    else if (h is "tier")
                        map.Tier = col;
                    else if (h is "cost")
                        map.Cost = col;
                    else if (h is "customeffectid" or "custom effect id" or "effectid")
                        map.CustomEffectId = col;
                }

                if (map.Class >= 0 && map.Id >= 0 && map.Name >= 0 && map.Type >= 0)
                {
                    headerRow = r;
                    cols = map;
                    return true;
                }
            }

            return false;
        }

        private struct ColumnMap
        {
            public int Class, Tree, Weapon, Name, Effect, Payoff;
            public int UnlockAction, Requires, Type, Stat, Id, Branch, Tier, Cost, CustomEffectId;
        }
    }
}
