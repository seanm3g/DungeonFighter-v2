using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.Data.ItemGeneration
{
    /// <summary>
    /// Base item modifiers (<c>GameData/BaseModifiers.json</c>): the base's built-in line. It changes other things the hero
    /// does (one named action, a tag's actions, the class's actions, a status) instead of firing as a trigger. The result is
    /// one passive <see cref="CrossWires"/> rule stored on <see cref="Item.ImplicitRule"/>, e.g.
    /// <c>WHILE IF ACTION Cleave -&gt; DAMAGE 12%</c> or <c>WHILE -&gt; POTENCY bleed 20%</c>.
    /// </summary>
    public static class BaseModifierRoller
    {
        public sealed class Entry
        {
            [JsonPropertyName("kind")] public string Kind { get; set; } = "";
            [JsonPropertyName("stat")] public string Stat { get; set; } = "DAMAGE";
            [JsonPropertyName("tag")] public string? Tag { get; set; }
            [JsonPropertyName("status")] public string? Status { get; set; }
            [JsonPropertyName("min")] public double Min { get; set; }
            [JsonPropertyName("max")] public double Max { get; set; }
            [JsonPropertyName("weight")] public double Weight { get; set; } = 1;
        }

        public sealed class ModifierFile
        {
            [JsonPropertyName("tags")] public Dictionary<string, List<string>> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            [JsonPropertyName("classes")] public Dictionary<string, List<Entry>> Classes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            /// <summary>Chance a base of this rarity carries its modifier (missing rarity = 1).</summary>
            [JsonPropertyName("rarityChance")] public Dictionary<string, double> RarityChance { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Writes <paramref name="chances"/> (rarity → 0..1) into <c>rarityChance</c> in BaseModifiers.json, keeping every other
        /// field, and reloads. Used by Settings → Item Generation.
        /// </summary>
        public static void SaveRarityChances(IReadOnlyDictionary<string, double> chances, string? path = null)
        {
            lock (Lock)
            {
                // Only the rarityChance block is rewritten, so the rest of the hand-laid-out file keeps its layout.
                path ??= FilePath;
                string text = File.Exists(path) ? File.ReadAllText(path) : "{\n}\n";
                string nl = text.Contains("\r\n") ? "\r\n" : "\n";
                string body = string.Join("," + nl, chances.Select(kv =>
                    $"    \"{kv.Key}\": {Math.Round(Math.Clamp(kv.Value, 0, 1), 4).ToString(CultureInfo.InvariantCulture)}"));
                string block = "\"rarityChance\": {" + nl + body + nl + "  }";
                var existing = System.Text.RegularExpressions.Regex.Match(text, "\"rarityChance\"\\s*:\\s*\\{[^}]*\\}");
                if (existing.Success)
                    text = text.Substring(0, existing.Index) + block + text.Substring(existing.Index + existing.Length);
                else
                {
                    int brace = text.IndexOf('{');
                    text = text.Substring(0, brace + 1) + nl + "  " + block + "," + text.Substring(brace + 1);
                }
                // Refuse to write a file that no longer parses.
                JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }).Dispose();
                File.WriteAllText(path, text);
                _file = null;
            }
        }

        /// <summary>Chance an item of <paramref name="rarity"/> carries a base modifier (1 unless BaseModifiers.json lowers it).</summary>
        public static double ChanceFor(string? rarity)
        {
            var file = Load();
            return !string.IsNullOrWhiteSpace(rarity) && file.RarityChance.TryGetValue(rarity.Trim(), out double c)
                ? Math.Clamp(c, 0, 1)
                : 1.0;
        }

        /// <summary>One rolled modifier: the rule text, its value, and tokens the roller's influence tilt can use.</summary>
        public readonly record struct Rolled(string Rule, double Value, IReadOnlyList<string> Influence);

        private static readonly object Lock = new();
        private static ModifierFile? _file;

        public static string FilePath => GameConstants.GetGameDataFilePath("BaseModifiers.json");

        public static void Reload()
        {
            lock (Lock) _file = null;
        }

        private static ModifierFile Load()
        {
            lock (Lock)
            {
                if (_file != null)
                    return _file;
                var file = new ModifierFile();
                try
                {
                    if (File.Exists(FilePath))
                    {
                        var parsed = JsonSerializer.Deserialize<ModifierFile>(File.ReadAllText(FilePath),
                            new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                        if (parsed != null)
                        {
                            // Re-key case-insensitively (the deserializer builds plain dictionaries).
                            file.Tags = new Dictionary<string, List<string>>(parsed.Tags ?? new(), StringComparer.OrdinalIgnoreCase);
                            file.Classes = new Dictionary<string, List<Entry>>(parsed.Classes ?? new(), StringComparer.OrdinalIgnoreCase);
                            file.RarityChance = new Dictionary<string, double>(parsed.RarityChance ?? new(), StringComparer.OrdinalIgnoreCase);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogWarning($"BaseModifiers.json failed to load: {ex.Message}", "BaseModifierRoller");
                }
                _file = file;
                return file;
            }
        }

        /// <summary>
        /// Rolls the modifier for a base of class <paramref name="itemClass"/> (empty = class-less) at <paramref name="itemLevel"/>.
        /// Null when the class has no pool or no entry resolves (e.g. no action to name).
        /// </summary>
        public static Rolled? Roll(string? itemClass, int itemLevel, Random rng, double percentPerItemLevel)
        {
            var file = Load();
            string cls = (itemClass ?? "").Trim().ToLowerInvariant();
            if (!file.Classes.TryGetValue(cls.Length > 0 ? cls : "*", out var pool) && !file.Classes.TryGetValue("*", out pool))
                return null;
            var options = pool.Where(e => e.Weight > 0).ToList();
            double scale = 1.0 + Math.Max(0, percentPerItemLevel) * (Math.Max(1, itemLevel) - 1);
            for (int attempt = 0; attempt < 6 && options.Count > 0; attempt++)
            {
                var entry = PickWeighted(rng, options, e => e.Weight);
                double raw = entry.Min + rng.NextDouble() * Math.Max(0, entry.Max - entry.Min);
                int value = Math.Max(1, (int)Math.Round(raw * scale, MidpointRounding.AwayFromZero));
                var built = Build(entry, cls, value, rng, file);
                if (built != null)
                    return built;
                options.Remove(entry);
            }
            return null;
        }

        private static Rolled? Build(Entry e, string cls, int value, Random rng, ModifierFile file)
        {
            string stat = string.IsNullOrWhiteSpace(e.Stat) ? "DAMAGE" : e.Stat.Trim().ToUpperInvariant();
            string pct = value.ToString(CultureInfo.InvariantCulture) + "%";
            switch ((e.Kind ?? "").Trim().ToLowerInvariant())
            {
                case "action":
                {
                    var actions = ActionsFor(cls);
                    if (actions.Count == 0)
                        return null;
                    var pick = actions[rng.Next(actions.Count)];
                    return new Rolled($"WHILE IF ACTION {pick.Name.Trim().Replace(' ', '_')} -> {stat} {pct}", value,
                        (pick.Tags ?? new List<string>()).ToList());
                }
                case "tag":
                {
                    string? tag = e.Tag;
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        if (!file.Tags.TryGetValue(cls.Length > 0 ? cls : "*", out var tags) || tags.Count == 0)
                            return null;
                        tag = tags[rng.Next(tags.Count)];
                    }
                    tag = tag!.Trim().ToLowerInvariant();
                    return new Rolled($"WHILE IF TAG {tag} -> {stat} {pct}", value, new[] { tag });
                }
                case "class":
                    if (cls.Length == 0)
                        return null;
                    return new Rolled($"WHILE IF CLASS {cls} -> {stat} {pct}", value, new[] { cls });
                case "status":
                {
                    string status = (e.Status ?? "").Trim().ToLowerInvariant();
                    if (!CrossWireRule.PotencyStatuses.Contains(status))
                        return null;
                    return new Rolled($"WHILE -> POTENCY {status} {pct}", value, new[] { status });
                }
                default:
                    return null;
            }
        }

        /// <summary>Gear-rollable actions tagged for <paramref name="cls"/> (hybrids count); any gear action when class-less.</summary>
        public static List<ActionData> ActionsFor(string? cls)
        {
            string c = (cls ?? "").Trim().ToLowerInvariant();
            return ActionLoader.GetAllActionData()
                .Where(a => !string.IsNullOrWhiteSpace(a.Name) && GearActionPools.CanRollOnGear(a)
                    && !(a.Tags?.Any(t => string.Equals(t?.Trim(), "enemy", StringComparison.OrdinalIgnoreCase)) ?? false))
                .Where(a => c.Length == 0 || GearActionPools.IsClassActionFor(a.Tags, new[] { c }))
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static T PickWeighted<T>(Random rng, IReadOnlyList<T> options, Func<T, double> weight)
        {
            double total = options.Sum(o => Math.Max(0, weight(o)));
            double roll = rng.NextDouble() * total;
            foreach (var o in options)
            {
                roll -= Math.Max(0, weight(o));
                if (roll <= 0)
                    return o;
            }
            return options[options.Count - 1];
        }
    }
}
