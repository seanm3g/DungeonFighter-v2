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
            /// <summary>
            /// Combo slot rise (comboSystem.comboThresholdRisePerSlot &gt; 0): blend each untimed action's mined strip
            /// position toward its score rank (best first) by this weight, since later slots are reached far less often.
            /// 0 = mined order only. Cadence-timed actions keep their timing position.
            /// </summary>
            public double RiseOrderWeight { get; set; }
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
            /// <summary>
            /// Strip score of an owned action the miner never saw (2026-11-15: the mined lists name 60 of ~190 actions, none of
            /// the 11-11 status rows or the cadence-timing rows), before its grant value (<see cref="GrantWeights"/>).
            /// </summary>
            public double UnminedBase { get; set; } = 0.35;
            /// <summary>
            /// Grant features -> strip points for unmined actions: dmgGrant / ampGrant / speedGrant are the total percent an
            /// action grants over its cadence duration / 100 (ramp and decay summed, every-n pulses counted, perSlot at slot 2);
            /// hit / combo / crit are roll points per grant.
            /// </summary>
            public Dictionary<string, double> GrantWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            /// <summary>Strip points per 10% of worn action-scoped DAMAGE rules (base modifiers, RULE prefixes, nodes) a swing of the action gets; SPEED counts 0.8 of it.</summary>
            public double RuleBonusPer10Pct { get; set; } = 0.15;
            /// <summary>Verb weight share for APPLY &lt;status&gt; when nothing the hero has (set WHEN, node, conversion) uses that status.</summary>
            public double UnusedApplyShare { get; set; } = 0.5;
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
                // APPLY <status> verb rows: their status is the action's payload (fx:<status>), so status nodes and WHENs see it.
                string verb = Str(row, "verb").Trim();
                if (verb.StartsWith("APPLY ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = verb.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                        f["fx:" + parts[1].ToLowerInvariant()] = 1;
                }
                double grants = GrantMultiplier(Str(row, "cadenceBundlesJson"));
                if (grants > 0)
                {
                    Set(f, "dmgGrant", grants * Num(row, "damageMod", 0) / 100.0);
                    Set(f, "ampGrant", grants * Num(row, "ampMod", 0) / 100.0);
                    Set(f, "speedGrant", grants * Num(row, "speedMod", 0) / 100.0);
                }
                result[name] = f;
            }
            return result;
        }

        /// <summary>
        /// How many full copies a row's first cadence bundle grants: its duration, with ramp / decay summing 1..n,
        /// <c>every</c> n counting only the pulses and <c>perSlot</c> valued at slot 2. 0 without a cadence bundle.
        /// </summary>
        internal static double GrantMultiplier(string cadenceBundlesJson)
        {
            if (string.IsNullOrWhiteSpace(cadenceBundlesJson))
                return 0;
            try
            {
                using var doc = JsonDocument.Parse(cadenceBundlesJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                    return 0;
                var b = doc.RootElement[0];
                double n = b.TryGetProperty("duration", out var d) && double.TryParse(d.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double dv) ? Math.Max(1, dv) : 1;
                double copies = n;
                if (b.TryGetProperty("timing", out var t) && t.ValueKind == JsonValueKind.Object)
                {
                    int every = t.TryGetProperty("every", out var e) && e.TryGetInt32(out int ev) ? ev : 1;
                    double pulses = every > 1 ? Math.Floor(n / every) : n;
                    string shape = t.TryGetProperty("shape", out var sh) ? sh.GetString() ?? "" : "";
                    copies = shape is "ramp" or "decay" ? pulses * (pulses + 1) / 2 : pulses;
                    if (t.TryGetProperty("perSlot", out var ps) && ps.ValueKind == JsonValueKind.True)
                        copies *= 2;
                }
                return copies;
            }
            catch (JsonException)
            {
                return 0;
            }
        }

        /// <summary>Strip score of an owned action no mined list names: <see cref="SynergyData.UnminedBase"/> + its grant features.</summary>
        public static double UnminedScore(string actionName) => Data.UnminedBase + Dot(Data.GrantWeights, Features(actionName));

        /// <summary>
        /// Strip points from worn action-scoped rules (base modifiers "+n% damage with X", RULE prefixes, skill nodes with
        /// IF ACTION / TAG / CLASS) that a swing of <paramref name="action"/> would get.
        /// </summary>
        public static double ActionRuleBonus(Character hero, RPGGame.Action? action)
        {
            if (action == null)
                return 0;
            double pct = CrossWires.Passive(hero, "DAMAGE", null, action, actionOnly: true)
                         + 0.8 * CrossWires.Passive(hero, "SPEED", null, action, actionOnly: true);
            return Data.RuleBonusPer10Pct * pct / 10.0;
        }

        /// <summary>
        /// Where an action's cadence timing wants it on a strip of <paramref name="length"/> slots, as 0 (first) .. 1 (last);
        /// null when position doesn't matter. slotIf / perSlot place the action itself; an ACTION grant aimed at a slot
        /// (slot n / last) wants its source early, one aimed at the next cycle's first slots (slot first) wants it late.
        /// </summary>
        public static double? TimingPosition(string actionName, int length)
        {
            var timing = ActionLoader.GetAction(actionName)?.ActionAttackBonuses?.BonusGroups?.FirstOrDefault()?.Timing;
            if (timing == null || length <= 1)
                return null;
            double At(int slot1) => Math.Clamp((slot1 - 1) / (double)(length - 1), 0, 1);
            string slotIf = (timing.SlotIf ?? "").Trim().ToLowerInvariant();
            if (slotIf.Length > 0)
            {
                if (slotIf == "first") return 0;
                if (slotIf == "last") return 1;
                if (slotIf == "even") return At(2);
                if (slotIf == "odd") return 0;
                string digits = new string(slotIf.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int k))
                    return slotIf.StartsWith("<") ? 0 : At(k);
            }
            if (timing.PerSlot)
                return At(Math.Min(3, length));
            string slot = (timing.Slot ?? "").Trim().ToLowerInvariant();
            if (slot == "first")
                return 1;
            if (slot.Length > 0)
                return 0;
            return null;
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
                VerbKind.Apply => StatusUsed(hero, Features(actionName)) ? w : w * Data.UnusedApplyShare,
                VerbKind.Detonate => OwnsStatusApplier(hero, actionName) ? w : 0,
                _ => w
            };
        }

        private static readonly string[] DotStatuses = { "bleed", "burn", "poison", "acid" };

        /// <summary>True when an active source (set WHEN or learned node) weights the action's fx:&lt;status&gt;, or a conversion uses it.</summary>
        private static bool StatusUsed(Character hero, IReadOnlyDictionary<string, double> features)
        {
            var fx = features.Keys.Where(k => k.StartsWith("fx:", StringComparison.OrdinalIgnoreCase)).ToList();
            if (fx.Count == 0)
                return false;
            foreach (var (source, _) in ActiveSources(hero))
                if (Data.Rules.TryGetValue(source, out var weights) && fx.Any(k => weights.TryGetValue(k, out double v) && v > 0))
                    return true;
            return ConversionTable.OwnsAny(hero);
        }

        /// <summary>DETONATE needs a status to blow up: another owned action applies a damage-over-time status.</summary>
        private static bool OwnsStatusApplier(Character hero, string self) =>
            hero.GetActionPool().Any(a => !string.Equals(a.Name, self, StringComparison.OrdinalIgnoreCase)
                && DotStatuses.Any(s => Features(a.Name).ContainsKey("fx:" + s)));

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
            if (colon <= 0)
                return null;
            string head = TriggerEvents.Strip(when[..colon]);
            return head.Equals("ONCASH", StringComparison.OrdinalIgnoreCase) || head.Equals("ONSTOREFULL", StringComparison.OrdinalIgnoreCase)
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
                    "CROSSWIRE" or "RULE" => CrossWires.RulesOf(m, item.Name).ToList() is { Count: > 0 } rules
                                   && rules.All(r => CrossWireUseful(r, keywords ??= KeywordsOf(planned))),
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
