using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Data;

namespace RPGGame
{
    public enum SkillNodeType
    {
        Action,
        Passive,
        Mastery,
        Rule
    }

    public sealed class SkillTreeNodeDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("branch")]
        public string Branch { get; set; } = "";

        [JsonPropertyName("tier")]
        public int Tier { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Passive";

        [JsonPropertyName("cost")]
        public int Cost { get; set; }

        /// <summary>Maximum purchasable ranks (each rank costs <see cref="Cost"/> again). Defaults to 1.</summary>
        [JsonPropertyName("maxRank")]
        public int MaxRank { get; set; } = 1;

        [JsonPropertyName("requires")]
        public List<string> Requires { get; set; } = new();

        [JsonPropertyName("effect")]
        public string Effect { get; set; } = "";

        [JsonPropertyName("payoff")]
        public string Payoff { get; set; } = "";

        [JsonPropertyName("unlockActionName")]
        public string? UnlockActionName { get; set; }

        /// <summary>Custom runtime handler id for SkillEffectRouter.</summary>
        [JsonPropertyName("customEffectId")]
        public string? CustomEffectId { get; set; }

        /// <summary>Flat DAMAGE_MOD applied per learned rank on hit (pack/sink nodes).</summary>
        [JsonPropertyName("damageModPerRank")]
        public int DamageModPerRank { get; set; }

        /// <summary>HP healed on hit per learned rank (pack/sink nodes).</summary>
        [JsonPropertyName("healOnHitPerRank")]
        public int HealOnHitPerRank { get; set; }

        [JsonIgnore]
        public SkillNodeType ParsedType =>
            Enum.TryParse<SkillNodeType>(Type, ignoreCase: true, out var t) ? t : SkillNodeType.Passive;
    }

    public sealed class SkillTreeDefinition
    {
        [JsonPropertyName("classKey")]
        public string ClassKey { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("weapon")]
        public string Weapon { get; set; } = "";

        [JsonPropertyName("stat")]
        public string Stat { get; set; } = "";

        [JsonPropertyName("identity")]
        public string Identity { get; set; } = "";

        [JsonPropertyName("nodes")]
        public List<SkillTreeNodeDefinition> Nodes { get; set; } = new();

        public WeaponType? ResolveWeaponType(ClassPresentationConfig? presentation = null)
        {
            presentation ??= GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            if (ClassActionsUnlockConfig.TryResolveClassKeyToWeaponType(ClassKey, presentation, out var wt))
                return wt;
            if (ClassActionsUnlockConfig.TryResolveClassKeyToWeaponType(Weapon, presentation, out wt))
                return wt;
            return null;
        }
    }

    public sealed class SkillTreesConfig
    {
        private static readonly JsonSerializerOptions JsonRead = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Tier index 0..4 → default Skill Point cost when a node omits/zeros cost (roots stay 0).</summary>
        public static readonly int[] TierCosts = { 0, 1, 1, 1, 1 };

        [JsonPropertyName("trees")]
        public List<SkillTreeDefinition> Trees { get; set; } = new();

        private Dictionary<string, SkillTreeNodeDefinition>? _nodesById;
        private Dictionary<WeaponType, SkillTreeDefinition>? _treesByWeapon;

        public static SkillTreesConfig CreateEmpty() => new();

        public static SkillTreesConfig? TryLoadFromGameDataFile(string? fileName = null)
        {
            fileName ??= GameConstants.SkillTreesJson;
            string? path = JsonLoader.FindGameDataFile(fileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                string json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<SkillTreesConfig>(json, JsonRead);
                return cfg?.Normalize();
            }
            catch
            {
                return null;
            }
        }

        public SkillTreesConfig Normalize()
        {
            Trees ??= new List<SkillTreeDefinition>();
            foreach (var tree in Trees)
            {
                tree.ClassKey = (tree.ClassKey ?? "").Trim();
                tree.Title = (tree.Title ?? "").Trim();
                tree.Weapon = (tree.Weapon ?? "").Trim();
                tree.Stat = (tree.Stat ?? "").Trim();
                tree.Identity = (tree.Identity ?? "").Trim();
                tree.Nodes ??= new List<SkillTreeNodeDefinition>();
                foreach (var node in tree.Nodes)
                {
                    node.Id = (node.Id ?? "").Trim();
                    node.Name = (node.Name ?? "").Trim();
                    node.Branch = (node.Branch ?? "").Trim();
                    node.Type = string.IsNullOrWhiteSpace(node.Type) ? "Passive" : node.Type.Trim();
                    node.Effect = node.Effect ?? "";
                    node.Payoff = node.Payoff ?? "";
                    node.Requires ??= new List<string>();
                    node.Requires = node.Requires
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r.Trim())
                        .ToList();
                    node.Tier = Math.Clamp(node.Tier, 0, 4);
                    if (node.MaxRank <= 0)
                        node.MaxRank = 1;
                    if (node.DamageModPerRank < 0)
                        node.DamageModPerRank = 0;
                    if (node.HealOnHitPerRank < 0)
                        node.HealOnHitPerRank = 0;
                    if (!string.IsNullOrWhiteSpace(node.UnlockActionName))
                        node.UnlockActionName = node.UnlockActionName.Trim();
                    if (!string.IsNullOrWhiteSpace(node.CustomEffectId))
                        node.CustomEffectId = node.CustomEffectId.Trim();
                    else
                        node.CustomEffectId = node.Id;
                }

                PromoteLevelOneAsRoot(tree);

                foreach (var node in tree.Nodes)
                {
                    if (node.Cost <= 0 && node.Tier >= 0 && node.Tier < TierCosts.Length)
                        node.Cost = TierCosts[node.Tier];

                    // Action unlocks are always a single 1-SP purchase.
                    if (node.ParsedType == SkillNodeType.Action && node.Cost > 0)
                    {
                        node.Cost = 1;
                        node.MaxRank = 1;
                    }
                }
            }

            _nodesById = null;
            _treesByWeapon = null;
            return this;
        }

        /// <summary>
        /// Class Upgrades sheet often lists "Level 1 - {Class}" as a T1 node under the identity root.
        /// In-game that node is the free Core root so material tags are the first most basic skill.
        /// </summary>
        internal static void PromoteLevelOneAsRoot(SkillTreeDefinition tree)
        {
            if (tree?.Nodes == null || tree.Nodes.Count == 0)
                return;

            SkillTreeNodeDefinition? level1 = null;
            if (!string.IsNullOrWhiteSpace(tree.ClassKey))
            {
                string want = "Level 1 - " + tree.ClassKey.Trim();
                level1 = tree.Nodes.FirstOrDefault(n =>
                    n.Name.Equals(want, StringComparison.OrdinalIgnoreCase));
            }
            level1 ??= tree.Nodes.FirstOrDefault(n =>
                n.Name.StartsWith("Level 1 -", StringComparison.OrdinalIgnoreCase));
            if (level1 == null || string.IsNullOrWhiteSpace(level1.Id))
                return;

            // Already the free Core root — leave graph alone (avoids re-promoting after save/load).
            if (level1.Tier == 0
                && level1.Requires.Count == 0
                && string.Equals(level1.Branch, "Core", StringComparison.OrdinalIgnoreCase))
                return;

            string level1Id = level1.Id;
            var formerRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string req in level1.Requires)
                formerRoots.Add(req);
            foreach (var n in tree.Nodes)
            {
                if (n.Tier == 0
                    && string.Equals(n.Branch, "Core", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(n.Id, level1Id, StringComparison.OrdinalIgnoreCase))
                    formerRoots.Add(n.Id);
            }

            level1.Tier = 0;
            level1.Cost = 0;
            level1.Requires = new List<string>();
            level1.Branch = "Core";

            foreach (var node in tree.Nodes)
            {
                if (string.Equals(node.Id, level1Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isFormerRoot = formerRoots.Contains(node.Id);
                bool isRite = string.Equals(node.Branch, "Rite", StringComparison.OrdinalIgnoreCase);

                if (isFormerRoot)
                {
                    if (node.Tier <= 0)
                        node.Tier = 1;
                    node.Requires = new List<string> { level1Id };
                    if (node.Cost <= 0)
                        node.Cost = TierCosts[Math.Clamp(node.Tier, 0, TierCosts.Length - 1)];
                    continue;
                }

                if (isRite && node.Tier == 0 && node.Requires.Count == 0)
                {
                    node.Requires = new List<string> { level1Id };
                    continue;
                }

                if (node.Requires.Count == 0)
                    continue;

                var remapped = new List<string>();
                foreach (string req in node.Requires)
                {
                    string next = formerRoots.Contains(req) ? level1Id : req;
                    if (!remapped.Any(r => string.Equals(r, next, StringComparison.OrdinalIgnoreCase))
                        && !string.Equals(next, node.Id, StringComparison.OrdinalIgnoreCase))
                        remapped.Add(next);
                }
                node.Requires = remapped;
            }
        }

        [JsonIgnore]
        public IReadOnlyDictionary<string, SkillTreeNodeDefinition> NodesById
        {
            get
            {
                if (_nodesById != null) return _nodesById;
                var map = new Dictionary<string, SkillTreeNodeDefinition>(StringComparer.OrdinalIgnoreCase);
                foreach (var tree in Trees)
                {
                    foreach (var node in tree.Nodes)
                    {
                        if (!string.IsNullOrWhiteSpace(node.Id))
                            map[node.Id] = node;
                    }
                }
                _nodesById = map;
                return map;
            }
        }

        public SkillTreeDefinition? GetTreeForWeapon(WeaponType weaponType)
        {
            if (_treesByWeapon == null)
            {
                var map = new Dictionary<WeaponType, SkillTreeDefinition>();
                var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
                foreach (var tree in Trees)
                {
                    var wt = tree.ResolveWeaponType(pres);
                    if (wt != null)
                        map[wt.Value] = tree;
                }
                _treesByWeapon = map;
            }
            return _treesByWeapon.TryGetValue(weaponType, out var t) ? t : null;
        }

        public SkillTreeNodeDefinition? GetNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return null;
            return NodesById.TryGetValue(nodeId, out var n) ? n : null;
        }

        public string? GetRootNodeId(WeaponType weaponType)
        {
            var tree = GetTreeForWeapon(weaponType);
            if (tree == null) return null;

            var level1 = tree.Nodes.FirstOrDefault(n =>
                n.Tier == 0
                && n.Name.StartsWith("Level 1 -", StringComparison.OrdinalIgnoreCase));
            if (level1 != null)
                return level1.Id;

            var core = tree.Nodes.FirstOrDefault(n =>
                n.Tier == 0
                && string.Equals(n.Branch, "Core", StringComparison.OrdinalIgnoreCase));
            if (core != null)
                return core.Id;

            return tree.Nodes.FirstOrDefault(n => n.Tier == 0)?.Id;
        }

        public IEnumerable<string> AllUnlockActionNames()
        {
            return Trees
                .SelectMany(t => t.Nodes)
                .Select(n => n.UnlockActionName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)!;
        }

        /// <summary>Spent Skill Points for ranks on <paramref name="path"/> (cost × rank per node).</summary>
        public int GetSpentSkillPoints(IReadOnlyDictionary<string, int>? learnedRanks, WeaponType path)
        {
            var tree = GetTreeForWeapon(path);
            if (tree == null || learnedRanks == null || learnedRanks.Count == 0) return 0;
            int spent = 0;
            foreach (var node in tree.Nodes)
            {
                if (!learnedRanks.TryGetValue(node.Id, out int rank) || rank <= 0)
                    continue;
                spent += Math.Max(0, node.Cost) * rank;
            }
            return spent;
        }
    }
}
