using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Utils;

namespace RPGGame.Combat
{
    /// <summary>One authored row of <c>GameData/StatusAffinities.json</c>: enemy tag × status → multiplier.</summary>
    public sealed class StatusAffinityRule
    {
        [JsonPropertyName("tag")] public string Tag { get; set; } = "";
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        /// <summary>0 = immune (status is never applied), &lt;1 = resists DoT ticks, &gt;1 = weak to DoT ticks.</summary>
        [JsonPropertyName("multiplier")] public double Multiplier { get; set; } = 1.0;
        [JsonPropertyName("note")] public string Note { get; set; } = "";
    }

    /// <summary>Resolved affinity of one enemy toward one status (rules from all matching tags multiply).</summary>
    public readonly record struct StatusAffinityResult(string Status, double Multiplier, IReadOnlyList<StatusAffinityRule> Sources)
    {
        public bool IsImmune => Multiplier <= 0;
        public bool IsWeak => Multiplier > 1.0001;
        public bool IsResistant => Multiplier > 0 && Multiplier < 0.9999;
    }

    /// <summary>
    /// Enemy status immunities / weaknesses / resistances driven by enemy tags (plant → burn ×2, undead → poison immune, …).
    /// Immune statuses are never applied (and so never logged); DoT ticks scale by the multiplier.
    /// </summary>
    public static class StatusAffinity
    {
        public const string FileName = "StatusAffinities.json";

        private static List<StatusAffinityRule>? _rules;
        private static readonly object Sync = new object();

        public static IReadOnlyList<StatusAffinityRule> Rules
        {
            get
            {
                if (_rules != null)
                    return _rules;
                lock (Sync)
                    return _rules ??= Load();
            }
        }

        /// <summary>Test / hot-reload hook.</summary>
        public static void SetRulesForTests(IEnumerable<StatusAffinityRule>? rules) =>
            _rules = rules?.ToList();

        public static void Reload() => _rules = null;

        private static List<StatusAffinityRule> Load()
        {
            try
            {
                string path = FileManager.GetGameDataFilePath(FileName);
                if (File.Exists(path))
                {
                    var loaded = JsonSerializer.Deserialize<List<StatusAffinityRule>>(File.ReadAllText(path));
                    if (loaded != null)
                        return loaded.Where(r => !string.IsNullOrWhiteSpace(r.Tag) && !string.IsNullOrWhiteSpace(r.Status)).ToList();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log("StatusAffinity", $"Failed to load {FileName}: {ex.Message}");
            }
            return DefaultRules();
        }

        private static List<StatusAffinityRule> DefaultRules() => new()
        {
            new StatusAffinityRule { Tag = "undead", Status = "poison", Multiplier = 0, Note = "No living blood to poison" },
            new StatusAffinityRule { Tag = "undead", Status = "bleed", Multiplier = 0, Note = "No blood left to spill" },
            new StatusAffinityRule { Tag = "plant", Status = "burn", Multiplier = 2, Note = "Wood and leaf burn readily" },
        };

        /// <summary>Canonical status key (lowercase, bleed/poison/burn/acid/stun/…).</summary>
        public static string NormalizeStatus(string? status) => (status ?? "").Trim().ToLowerInvariant();

        /// <summary>Resolves the multiplier for <paramref name="status"/> on <paramref name="target"/>. Non-enemies are always 1.0.</summary>
        public static StatusAffinityResult Resolve(Actor? target, string? status)
        {
            string key = NormalizeStatus(status);
            if (target is not Enemy enemy || key.Length == 0)
                return new StatusAffinityResult(key, 1.0, Array.Empty<StatusAffinityRule>());
            return Resolve(EffectiveTags(enemy), key);
        }

        /// <summary>Enemy tags plus "undead" for non-living templates built without the factory's substance tag.</summary>
        public static IEnumerable<string> EffectiveTags(Enemy enemy)
        {
            var tags = enemy.Tags.ToList();
            if (!enemy.IsLiving && !tags.Any(t => t.Equals("undead", StringComparison.OrdinalIgnoreCase)))
                tags.Add("undead");
            return tags;
        }

        public static StatusAffinityResult Resolve(IEnumerable<string>? tags, string status)
        {
            string key = NormalizeStatus(status);
            var tagSet = new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var sources = Rules
                .Where(r => NormalizeStatus(r.Status) == key && tagSet.Contains(r.Tag.Trim()))
                .ToList();
            double mult = 1.0;
            foreach (var r in sources)
                mult *= Math.Max(0, r.Multiplier);
            return new StatusAffinityResult(key, mult, sources);
        }

        public static bool IsImmune(Actor? target, string? status) => Resolve(target, status).IsImmune;

        /// <summary>Scales a DoT tick for the target's affinity; non-enemies and neutral statuses pass through.</summary>
        public static int ScaleTick(Actor target, string status, int damage)
        {
            if (damage <= 0)
                return damage;
            var r = Resolve(target, status);
            if (r.Multiplier == 1.0)
                return damage;
            return (int)Math.Round(damage * r.Multiplier, MidpointRounding.AwayFromZero);
        }

        /// <summary>All non-neutral affinities for an enemy, grouped for HUD / tooltip display.</summary>
        public static List<StatusAffinityResult> DescribeAll(Enemy enemy)
        {
            var statuses = Rules.Select(r => NormalizeStatus(r.Status)).Distinct().ToList();
            var list = new List<StatusAffinityResult>();
            foreach (var s in statuses)
            {
                var r = Resolve(EffectiveTags(enemy), s);
                if (r.Sources.Count > 0 && Math.Abs(r.Multiplier - 1.0) > 0.0001)
                    list.Add(r);
            }
            return list;
        }

        /// <summary>HUD lines like "Immune: Bleed, Poison", "Weak: Burn ×2", "Resists: Poison ×0.5".</summary>
        public static List<string> GetHudLines(Enemy enemy)
        {
            var all = DescribeAll(enemy);
            var lines = new List<string>();
            var immune = all.Where(a => a.IsImmune).Select(a => Title(a.Status)).ToList();
            var weak = all.Where(a => a.IsWeak).Select(a => $"{Title(a.Status)} ×{a.Multiplier:0.#}").ToList();
            var resist = all.Where(a => a.IsResistant).Select(a => $"{Title(a.Status)} ×{a.Multiplier:0.##}").ToList();
            if (immune.Count > 0) lines.Add("Immune: " + string.Join(", ", immune));
            if (weak.Count > 0) lines.Add("Weak: " + string.Join(", ", weak));
            if (resist.Count > 0) lines.Add("Resists: " + string.Join(", ", resist));
            return lines;
        }

        public static string Title(string status) =>
            string.IsNullOrEmpty(status) ? status : char.ToUpperInvariant(status[0]) + status.Substring(1);
    }
}
