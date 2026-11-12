using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Synergy layer for the rules bot (DUNGEONSIM --bot synergy): scores owned actions and skill nodes by how well
    /// they feed the hero's worn material sets (each set's synthesis WHEN) and learned skill nodes. Weights live in
    /// <c>GameData/TuningProfiles/synergy-bot.json</c> (kept apart from rules-bot.json, which the miner rewrites).
    /// Action features come from the Actions.json sheet rows (tags, hits, speed, damage, hero roll bonuses, statuses).
    /// </summary>
    public static class RulesSynergy
    {
        public sealed class SynergyData
        {
            /// <summary>Owned actions below this total (mined priority + synergy) stay off the strip, unless a fill-slot node is learned.</summary>
            public double MinScore { get; set; } = 0.5;
            /// <summary>Per set piece past 2 (up to 5) the WHEN weights grow by this fraction.</summary>
            public double SetPieceGrowth { get; set; } = 0.25;
            /// <summary>Node ranks past this count no longer add weight.</summary>
            public int MaxRankScale { get; set; } = 3;
            /// <summary>Skill score bonus for a node in the branch named after the hero's main worn set material.</summary>
            public double BranchBonus { get; set; } = 0.35;
            /// <summary>Skill score bonus for <see cref="SetNodes"/> while any home-class set is formed.</summary>
            public double SetNodeBonus { get; set; } = 0.25;
            /// <summary>Scale on a node's "node:" weights summed over the current strip, when ranking skills.</summary>
            public double SkillFromStripScale { get; set; } = 1.0;
            /// <summary>Nodes that pay off per set piece or per mint (kin, synth, age): favored while a home set is worn.</summary>
            public List<string> SetNodes { get; set; } = new();
            /// <summary>Nodes that pay per filled strip slot: when learned, the strip fills every slot.</summary>
            public List<string> FillSlotNodes { get; set; } = new();
            /// <summary>"when:ONCRITICAL" / "node:b-combo" → feature → weight. See <see cref="Features"/> for feature names.</summary>
            public Dictionary<string, Dictionary<string, double>> Rules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            /// <summary>
            /// Verb preset (action <c>verb</c> column) → weight, applied only while its gear condition holds (see
            /// <see cref="VerbSynergy"/>). Keys are <see cref="VerbKind"/> names (Cash, Scale, Invest, …).
            /// </summary>
            public Dictionary<string, double> VerbWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            /// <summary>
            /// Item affix template (prefix Effect: LISTEN, TUNE, STORECOND, COUNTSDOUBLE, OPENER, FINISHER, BLOODPRICE,
            /// CROSSWIRE) → value in <see cref="Profiles.ItemValueModel.AdjectivePrefixRate"/> units while its gear
            /// condition holds (<see cref="AffixValue"/>); 0 otherwise. Unlisted templates keep the flat prefix value.
            /// </summary>
            public Dictionary<string, double> AffixWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            /// <summary>Skill score bonus for a cross-wire node whose source store the worn sets mint.</summary>
            public double CrossWireNodeBonus { get; set; } = 0.3;
            /// <summary>Skill score bonus for a Persistence capstone (Rite branch, carries stores between fights).</summary>
            public double PersistNodeBonus { get; set; } = 0.2;
        }

        private static SynergyData? _data;
        private static Dictionary<string, Dictionary<string, double>>? _features;

        public static SynergyData Data
        {
            get
            {
                if (_data != null)
                    return _data;
                string path = GameConstants.ResolveGameDataPath("TuningProfiles", "synergy-bot.json");
                _data = File.Exists(path)
                    ? JsonSerializer.Deserialize<SynergyData>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip }) ?? new SynergyData()
                    : new SynergyData();
                _data.Rules = CanonicalizeWhenKeys(_data.Rules);
                return _data;
            }
        }

        // ---------------------------------------------------------------- features

        /// <summary>
        /// Feature vector of an action by sheet name: tag:&lt;tag&gt;, hits (extra hits), fast (1 - speed), slowAction,
        /// heavy (damage - 100%), hit / crit / combo (hero roll bonuses), slow / weaken / expose / vuln / harden / pierce /
        /// confuse / disrupt, finisher / opener, statbuff, heal, oncrit, onkill, fx:&lt;word&gt; (bleed, burn, poison, stun,
        /// fortify, focus named in the cadence/trigger bundles).
        /// </summary>
        public static IReadOnlyDictionary<string, double> Features(string actionName)
        {
            _features ??= LoadFeatures();
            return _features.TryGetValue(actionName ?? "", out var f) ? f : EmptyFeatures;
        }

        private static readonly Dictionary<string, double> EmptyFeatures = new();

        private static Dictionary<string, Dictionary<string, double>> LoadFeatures()
        {
            var result = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
            string path = GameConstants.ResolveGameDataPath("Actions.json");
            if (!File.Exists(path))
                return result;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return result;
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                string name = Str(row, "action");
                if (name.Length == 0)
                    continue;
                var f = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                foreach (string tag in Str(row, "tags").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    f["tag:" + tag.ToLowerInvariant()] = 1;
                double hits = Math.Max(0, Num(row, "numberOfHits", 1) - 1) + Math.Max(0, Num(row, "multiHitMod", 0));
                Set(f, "hits", hits);
                double speed = Num(row, "speed", 1);
                Set(f, "fast", Math.Max(0, 1 - speed));
                Set(f, "slowAction", Math.Max(0, speed - 1));
                Set(f, "heavy", Math.Max(0, Num(row, "damage", 100) / 100.0 - 1));
                Set(f, "hit", Math.Clamp(Num(row, "heroHit", 0), -3, 3));
                Set(f, "crit", Math.Clamp(Num(row, "heroCrit", 0), -3, 3));
                Set(f, "combo", Math.Clamp(Num(row, "heroCombo", 0), -3, 3));
                foreach (var (field, feature) in new[] { ("slow", "slow"), ("weaken", "weaken"), ("expose", "expose"), ("vulnerability", "expose"),
                    ("harden", "harden"), ("pierce", "pierce"), ("confuse", "confuse"), ("disrupt", "disrupt"), ("onCrit", "oncrit"), ("onKill", "onkill") })
                    if (Num(row, field, 0) != 0)
                        f[feature] = 1;
                if (Str(row, "finisher").Equals("true", StringComparison.OrdinalIgnoreCase)) f["finisher"] = 1;
                if (Str(row, "opener").Equals("true", StringComparison.OrdinalIgnoreCase)) f["opener"] = 1;
                if (new[] { "heroSTR", "heroAGI", "heroTECH", "heroINT" }.Any(k => Num(row, k, 0) != 0)) f["statbuff"] = 1;
                if (Num(row, "heroHeal", 0) != 0 || Str(row, "lifesteal").Length > 0) f["heal"] = 1;
                string bundles = (Str(row, "cadenceBundlesJson") + " " + Str(row, "triggerBundlesJson")).ToLowerInvariant();
                foreach (string word in new[] { "bleed", "burn", "poison", "stun", "fortify", "focus" })
                    if (bundles.Contains(word))
                        f["fx:" + word] = 1;
                if (f.ContainsKey("tag:focus"))
                    f["fx:focus"] = 1;
                result[name] = f;
            }
            return result;
        }

        private static void Set(Dictionary<string, double> f, string key, double v)
        {
            if (Math.Abs(v) > 1e-9)
                f[key] = v;
        }

        private static string Str(JsonElement row, string key) =>
            row.TryGetProperty(key, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()) : "";

        private static double Num(JsonElement row, string key, double fallback)
        {
            string s = Str(row, key).Trim().TrimEnd('%');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : fallback;
        }

        /// <summary>
        /// <c>when:&lt;WHEN&gt;</c> keys resolve through <see cref="RPGGame.Combat.Events.TriggerEvents"/>, so an alias key
        /// (<c>when:ONRECEIVEHIT</c>) matches the canonical token a material build reports (<c>ONTAKEHIT</c>).
        /// A canonical key wins over an alias key for the same event.
        /// </summary>
        public static Dictionary<string, Dictionary<string, double>> CanonicalizeWhenKeys(
            Dictionary<string, Dictionary<string, double>>? rules)
        {
            var result = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
            if (rules == null)
                return result;
            foreach (var (key, weights) in rules)
            {
                if (!key.StartsWith("when:", StringComparison.OrdinalIgnoreCase))
                {
                    result[key] = weights;
                    continue;
                }
                string raw = key[5..];
                string canonicalKey = "when:" + RPGGame.Combat.Events.TriggerEvents.NormalizeToken(raw);
                bool isCanonical = string.Equals(RPGGame.Combat.Events.TriggerEvents.Strip(raw), canonicalKey[5..], StringComparison.OrdinalIgnoreCase);
                if (isCanonical || !result.ContainsKey(canonicalKey))
                    result[canonicalKey] = weights;
            }
            return result;
        }

        // ---------------------------------------------------------------- active sources

        /// <summary>The weighted rule sources the hero has active now: each formed set's WHEN and each learned node.</summary>
        public static List<(string Source, double Scale)> ActiveSources(Character hero)
        {
            var sources = new List<(string, double)>();
            foreach (var (material, count) in FormedSets(hero))
            {
                string when = MaterialBuildsLoader.FindByMaterial(material)?.WhenToken ?? "";
                if (when.Length > 0)
                    sources.Add(("when:" + when, 1 + Data.SetPieceGrowth * (Math.Min(count, 5) - MaterialBuildData.StackUnlockCount)));
            }
            foreach (string key in Data.Rules.Keys.Where(k => k.StartsWith("node:", StringComparison.OrdinalIgnoreCase)))
            {
                int rank = hero.Progression.GetSkillRank(key[5..]);
                if (rank > 0)
                    sources.Add((key, Math.Min(rank, Math.Max(1, Data.MaxRankScale))));
            }
            return sources;
        }

        /// <summary>Worn materials with at least the set-unlock count, most pieces first.</summary>
        public static List<(string Material, int Count)> FormedSets(Character hero) =>
            MaterialSetController.EnumerateEquipped(hero)
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Material))
                .Select(i => ItemMaterialRules.RemapLegacyMaterial(i!.Material))
                .Where(m => !string.IsNullOrWhiteSpace(m) && MaterialBuildsLoader.FindByMaterial(m) != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(m => (m!, MaterialSetController.CountEquipped(hero, m)))
                .Where(x => x.Item2 >= MaterialBuildData.StackUnlockCount)
                .OrderByDescending(x => x.Item2)
                .ToList();

        /// <summary>Synergy points of one action under the active sources.</summary>
        public static double ActionSynergy(string actionName, IReadOnlyList<(string Source, double Scale)> sources)
        {
            var f = Features(actionName);
            if (f.Count == 0)
                return 0;
            double total = 0;
            foreach (var (source, scale) in sources)
                if (Data.Rules.TryGetValue(source, out var weights))
                    total += scale * Dot(weights, f);
            return total;
        }

        /// <summary>
        /// Gear-aware score for a verb preset: the weight from <see cref="SynergyData.VerbWeights"/> when the preset
        /// has something to work with. Attuned-store verbs (Cash, Scale, Invest, Distill) need the attuned material's
        /// set formed; Liquidate / Transmute need two formed sets; Index scales with distinct materials past 2;
        /// Detonate, Apply, Barrier and Palm always count. 0 for non-verb actions.
        /// </summary>
        public static double VerbSynergy(Character hero, string actionName)
        {
            var spec = VerbSpec.Parse(ActionLoader.GetActionData(actionName)?.Verb);
            if (spec == null || !Data.VerbWeights.TryGetValue(spec.Kind.ToString(), out double w) || w == 0)
                return 0;
            var formed = FormedSets(hero);
            string? attuned = Attunement.GetMaterial(hero);
            bool attunedFormed = attuned != null
                && formed.Any(s => string.Equals(s.Material, attuned, StringComparison.OrdinalIgnoreCase));
            return spec.Kind switch
            {
                VerbKind.Cash or VerbKind.Scale or VerbKind.Invest or VerbKind.Distill => attunedFormed ? w : 0,
                VerbKind.Liquidate => formed.Count >= 2 ? w : 0,
                VerbKind.Transmute => formed.Count >= 2 || ConversionTable.OwnsAny(hero) ? w : 0,
                VerbKind.Index => w * Math.Max(0, Attunement.CountDistinctMaterials(hero) - 2),
                _ => w
            };
        }

        // ---------------------------------------------------------------- affixes, cross-wires, capstones

        /// <summary>Mint keywords of <paramref name="materials"/> (each set material's currency).</summary>
        public static HashSet<string> KeywordsOf(IEnumerable<string> materials) =>
            new(materials.Select(m => MaterialBuildsLoader.FindByMaterial(m)?.Keyword ?? "").Where(k => k.Length > 0),
                StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A cross-wire pays only if its source can happen: a mint store named by <c>ONCASH:&lt;store&gt;</c> or
        /// <c>IF &lt;store&gt;&gt;=n</c> must be one the planned sets mint. Rules on other events always count.
        /// </summary>
        public static bool CrossWireUseful(CrossWireRule rule, ISet<string> keywords)
        {
            foreach (string? store in new[] { SourceStore(rule.When), rule.IfStore })
                if (!string.IsNullOrWhiteSpace(store) && Stores.TryGet(store, out var def) && def.Kind == StoreKind.Mint
                    && !keywords.Contains(def.Id))
                    return false;
            return true;
        }

        private static string? SourceStore(string when)
        {
            int colon = when.IndexOf(':');
            return colon > 0 && TriggerEvents.Strip(when[..colon]).Equals("ONCASH", StringComparison.OrdinalIgnoreCase)
                ? when[(colon + 1)..]
                : null;
        }

        /// <summary>
        /// Gear-aware adjustment to <see cref="Profiles.ItemValueModel.Score"/> for affix templates on
        /// <paramref name="item"/>, given the set materials the hero is building (<paramref name="planned"/>). The flat
        /// adjective value the model already counted is replaced by <see cref="SynergyData.AffixWeights"/>[effect] ×
        /// the prefix rate when the condition holds, else 0: LISTEN / TUNE / STORECOND / COUNTSDOUBLE need the piece's
        /// material in the plan; CROSSWIRE needs <see cref="CrossWireUseful"/>; OPENER / FINISHER / BLOODPRICE always hold.
        /// </summary>
        public static double AffixValue(Item item, ICollection<string> planned)
        {
            if (item?.Modifications == null || Data.AffixWeights.Count == 0)
                return 0;
            string material = ItemMaterialRules.RemapLegacyMaterial(item.Material) ?? "";
            bool inPlan = material.Length > 0 && planned.Contains(material, StringComparer.OrdinalIgnoreCase);
            HashSet<string>? keywords = null;
            double rate = Profiles.ItemValueModel.AdjectivePrefixRate;
            double delta = 0;
            foreach (var m in item.Modifications)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.Effect) || !Data.AffixWeights.TryGetValue(m.Effect, out double w))
                    continue;
                if (m.GetPrefixCategory() != ModificationPrefixCategory.Adjective)
                    continue;
                bool holds = m.Effect.ToUpperInvariant() switch
                {
                    "LISTEN" or "TUNE" or "STORECOND" or "COUNTSDOUBLE" => inPlan,
                    "CROSSWIRE" => CrossWireRule.Parse(m.CrossWire) is { } rule
                                   && CrossWireUseful(rule, keywords ??= KeywordsOf(planned)),
                    _ => true
                };
                delta += (holds ? w * rate : 0) - rate;
            }
            return delta;
        }

        /// <summary>Skill pick bonus for cross-wire nodes the worn sets can feed and for Persistence capstones.</summary>
        public static double NodeRuleBonus(SkillTreeNodeDefinition node, ISet<string> keywords)
        {
            double bonus = 0;
            if (CrossWireRule.Parse(node.CrossWire) is { } rule && CrossWireUseful(rule, keywords))
                bonus += Data.CrossWireNodeBonus;
            if (!string.IsNullOrWhiteSpace(node.Persist))
                bonus += Data.PersistNodeBonus;
            return bonus;
        }

        /// <summary>"node:" weights of <paramref name="nodeId"/> averaged over <paramref name="strip"/>'s features.</summary>
        public static double NodeStripSynergy(string nodeId, IReadOnlyList<string> strip)
        {
            if (strip.Count == 0 || !Data.Rules.TryGetValue("node:" + nodeId, out var weights))
                return 0;
            return strip.Sum(a => Dot(weights, Features(a))) / strip.Count;
        }

        public static bool FillsSlots(Character hero) =>
            Data.FillSlotNodes.Any(id => hero.Progression.GetSkillRank(id) > 0);

        private static double Dot(Dictionary<string, double> weights, IReadOnlyDictionary<string, double> f)
        {
            double sum = 0;
            foreach (var (feature, w) in weights)
                if (f.TryGetValue(feature, out double v))
                    sum += w * v;
            return sum;
        }
    }
}
