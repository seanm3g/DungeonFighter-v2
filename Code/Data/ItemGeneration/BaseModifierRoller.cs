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
    /// Base item modifiers: the base's built-in line. Since 2026-11-15 each base's modifier is fixed in the catalog
    /// (<c>modifier</c> in Weapons.json / Armor.json, written by Scripts/build-item-mods.py) and
    /// <see cref="FromCatalog"/> only rolls its values. <see cref="Roll"/> (the <c>GameData/BaseModifiers.json</c> pools) is
    /// the fallback for saved items whose base is no longer in the catalog. It changes other things the hero
    /// does (a named action or a tag's actions apply a status, class hits apply a DoT, a DoT's potency). It never grants a
    /// % damage / speed bonus. The result is one <see cref="CrossWires"/> rule stored on <see cref="Item.ImplicitRule"/>, e.g.
    /// <c>ONCONNECT EVERY 2 IF TAG bludgeon -&gt; APPLY stagger 1</c> or <c>WHILE -&gt; POTENCY bleed 20%</c>.
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
        }

        private static readonly System.Text.RegularExpressions.Regex RangeToken =
            new(@"\{(-?[0-9]+(?:\.[0-9]+)?)-(-?[0-9]+(?:\.[0-9]+)?)\}", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Rolls a catalog modifier (Weapons.json / Armor.json <c>modifier</c>): each <c>{lo-hi}</c> becomes a whole number in
        /// that range. The rule text (action, tag, condition) is fixed to the base; only the values roll. Null when the
        /// template is empty or does not parse.
        /// </summary>
        public static Rolled? FromCatalog(string? template, Random rng)
        {
            if (string.IsNullOrWhiteSpace(template))
                return null;
            double first = 0;
            bool seen = false;
            string rule = RangeToken.Replace(template.Trim(), m =>
            {
                double lo = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                double hi = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (hi < lo)
                    (lo, hi) = (hi, lo);
                int v = (int)Math.Round(lo + rng.NextDouble() * (hi - lo), MidpointRounding.AwayFromZero);
                if (!seen)
                {
                    first = v;
                    seen = true;
                }
                return v.ToString(CultureInfo.InvariantCulture);
            });
            var parts = SplitRules(rule);
            var influence = new List<string>();
            foreach (string part in parts)
            {
                // Passive (WHILE) and triggered rules both work: high-tier bases change what an action does
                // (ONCONNECT IF TAG x -> APPLY brittle 1) or feed the sequence (-> NEXT DAMAGE 20%).
                var parsed = CrossWireRule.Parse(part, "base modifier");
                if (parsed == null)
                    return null;
                foreach (var c in parsed.Conditions)
                {
                    if (c.Subject == "action")
                    {
                        var data = ActionLoader.GetAllActionData()
                            .FirstOrDefault(a => string.Equals(a.Name?.Trim(), c.Key, StringComparison.OrdinalIgnoreCase));
                        influence.AddRange(data?.Tags ?? new List<string>());
                    }
                    else if (c.Subject is "tag" or "class" or "target" or "prevtag" or "nexttag" or "weapon")
                        influence.Add(c.Key);
                }
                if (parsed.Verb is "POTENCY" or "APPLY" or "ADD" or "MINT" or "TAGAS")
                    influence.Add(parsed.Store);
                if (parsed.IfTag != null)
                    influence.Add(parsed.IfTag);
            }
            return new Rolled(string.Join("; ", parts), first, influence);
        }

        /// <summary>Sets <paramref name="item"/>'s modifier from a catalog row (starter gear, lab items); no-op without one.</summary>
        public static void ApplyCatalogModifier(Item item, string? template, Random rng)
        {
            if (item == null || CommonItemRules.MustBePlain(item) || FromCatalog(template, rng) is not { } rolled)
                return; // Rebuild 2.1 (D0.7): Commons, starters included, carry no modifier.
            item.ImplicitRule = rolled.Rule;
        }

        /// <summary>
        /// Rarity scope (D4.2, D6.5, Phase 10 step 6): modifiers that reach into other systems (RANK a skill node,
        /// TAGAS another tag, SETCOUNT a set) need Epic or better; below that a modifier is self-contained. Returns the
        /// rule with the out-of-scope parts dropped (empty when none is left).
        /// </summary>
        public static string ScopeToRarity(string? rule, string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rule) || RarityOrder.IndexOf(rarity) >= EpicIndex)
                return rule ?? "";
            return string.Join("; ", SplitRules(rule).Where(p => !ReachesOut(p)));
        }

        public const int EpicIndex = 3;

        /// <summary>True for a rule part that uses RANK, TAGAS or SETCOUNT.</summary>
        public static bool ReachesOut(string part) =>
            CrossWireRule.Parse(part, "scope") is { Verb: "RANK" or "TAGAS" or "SETCOUNT" };

        /// <summary>The rules of a modifier (<see cref="Item.ImplicitRule"/> may hold several, <c>;</c>-separated).</summary>
        public static List<string> SplitRules(string? rule) =>
            (rule ?? "").Split(';').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

        /// <summary>The catalog modifier template of the base called <paramref name="baseName"/>; null when none.</summary>
        public static string? CatalogModifierFor(string? baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName))
                return null;
            Dictionary<string, string?> map;
            lock (Lock)
            {
                if (_catalog == null)
                {
                    // Built once (cleared by Reload): legacy saves call this per item on load.
                    var cache = LootDataCache.Load();
                    _catalog = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var w in cache.WeaponData ?? new List<WeaponData>())
                        if (!string.IsNullOrWhiteSpace(w.Name))
                            _catalog.TryAdd(w.Name.Trim(), w.Modifier);
                    foreach (var r in cache.ArmorData ?? new List<ArmorData>())
                        if (!string.IsNullOrWhiteSpace(r.Name))
                            _catalog.TryAdd(r.Name.Trim(), r.Modifier);
                }
                map = _catalog;
            }
            return map.TryGetValue(baseName.Trim(), out var found) ? found : null;
        }

        private static Dictionary<string, string?>? _catalog;

        /// <summary>One rolled modifier: the rule text, its value, and tokens the roller's influence tilt can use.</summary>
        public readonly record struct Rolled(string Rule, double Value, IReadOnlyList<string> Influence);

        private static readonly object Lock = new();
        private static ModifierFile? _file;

        public static string FilePath => GameConstants.GetGameDataFilePath("BaseModifiers.json");

        public static void Reload()
        {
            lock (Lock)
            {
                _file = null;
                _catalog = null;
            }
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
                    return new Rolled($"ONCONNECT EVERY 2 IF ACTION {pick.Name.Trim().Replace(' ', '_')} -> APPLY {FoeStatus(cls)} 1", value,
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
                    return new Rolled($"ONCONNECT EVERY 2 IF TAG {tag} -> APPLY {FoeStatus(cls)} 1", value, new[] { tag });
                }
                case "class":
                    if (cls.Length == 0)
                        return null;
                    return new Rolled($"ONCONNECT EVERY 3 IF CLASS {cls} -> APPLY bleed 1", value, new[] { cls });
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

        /// <summary>The foe status a class's fallback modifiers apply (matches the catalog generator's first pick).</summary>
        private static string FoeStatus(string cls) => cls switch
        {
            "barbarian" => "stagger", "warrior" => "rattle", "rogue" => "brittle", "wizard" => "weaken", _ => "expose"
        };

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
