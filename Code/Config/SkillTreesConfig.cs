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

        [JsonPropertyName("requires")]
        public List<string> Requires { get; set; } = new();

        [JsonPropertyName("effect")]
        public string Effect { get; set; } = "";

        [JsonPropertyName("payoff")]
        public string Payoff { get; set; } = "";

        [JsonPropertyName("unlockActionName")]
        public string? UnlockActionName { get; set; }

        [JsonPropertyName("customEffectId")]
        public string? CustomEffectId { get; set; }

        /// <summary>Max ranks that can be bought (1 for roots/actions; typically 5 for stackables).</summary>
        [JsonPropertyName("maxRank")]
        public int MaxRank { get; set; }

        /// <summary>Optional: on hit (primary-path weapon), add this many DAMAGE_MOD % per invested rank.</summary>
        [JsonPropertyName("damageModPerRank")]
        public int DamageModPerRank { get; set; }

        /// <summary>Optional: on hit (primary-path weapon), heal this many HP per invested rank.</summary>
        [JsonPropertyName("healOnHitPerRank")]
        public int HealOnHitPerRank { get; set; }

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

        /// <summary>Tier index 0..4 → default Skill Point cost per rank (roots 0; all other tiers 1).</summary>
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
                    if (!string.IsNullOrWhiteSpace(node.UnlockActionName))
                        node.UnlockActionName = node.UnlockActionName.Trim();
                    if (!string.IsNullOrWhiteSpace(node.CustomEffectId))
                        node.CustomEffectId = node.CustomEffectId.Trim();
                    else
                        node.CustomEffectId = node.Id;

                    bool isRoot = node.Tier == 0;
                    // Flat economy: every non-root rank costs 1 Skill Point.
                    node.Cost = isRoot ? 0 : 1;
                    // Default maxRank is 1. Multi-rank (>1) is opt-in in SkillTrees.json and must
                    // scale effect magnitudes by rank (see SkillEffectRouter / pack per-rank fields).
                    if (node.MaxRank <= 0)
                        node.MaxRank = 1;
                    else
                        node.MaxRank = Math.Clamp(node.MaxRank, 1, 20);
                    if (node.DamageModPerRank < 0) node.DamageModPerRank = 0;
                    if (node.HealOnHitPerRank < 0) node.HealOnHitPerRank = 0;
                }
            }

            _nodesById = null;
            _treesByWeapon = null;
            return this;
        }

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
            return tree?.Nodes.FirstOrDefault(n => n.Tier == 0)?.Id;
        }

        public IEnumerable<string> AllUnlockActionNames()
        {
            return Trees
                .SelectMany(t => t.Nodes)
                .Select(n => n.UnlockActionName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)!;
        }

        public int GetSpentSkillPoints(IReadOnlyDictionary<string, int> learnedRanks, WeaponType path)
        {
            var tree = GetTreeForWeapon(path);
            if (tree == null) return 0;
            int spent = 0;
            foreach (var node in tree.Nodes)
            {
                if (learnedRanks != null &&
                    learnedRanks.TryGetValue(node.Id, out int rank) &&
                    rank > 0)
                {
                    spent += Math.Max(0, node.Cost) * rank;
                }
            }
            return spent;
        }
    }
}
