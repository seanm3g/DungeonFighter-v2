using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Actions;

namespace RPGGame.Data
{
    /// <summary>
    /// ACTIONS sheet CADENCES band: per-family triples
    /// (<c>TURN</c> enable, <c>TURN DURATION</c>, <c>TURN →</c> mechanic pointers) — replaces compacted DURATION/CADENCE/MECHANICS.
    /// </summary>
    public static class ActionCadenceSheetColumns
    {
        public const string ContextBand = "CADENCES";

        public readonly record struct Family(
            string CadenceToken,
            string EnableLabel,
            string DurationLabel,
            string MechanicsLabel);

        public static readonly Family[] Families =
        {
            new("TURN", "TURN", "TURN DURATION", "TURN →"),
            new("ACTION", "ACTION", "ACTION DURATION", "ACTION →"),
            new("FIGHT", "FIGHT", "FIGHT DURATION", "FIGHT →"),
            new("DUNGEON", "DUNGEON", "DUNGEON DURATION", "DUNGEON →"),
        };

        public static (SpreadsheetHeader Header, bool ColumnsAdded) EnsureHeader(SpreadsheetHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            var contexts = header.ContextByIndex.ToList();
            var labels = header.LabelByIndex.ToList();
            bool added = false;

            while (contexts.Count < labels.Count)
                contexts.Add("");
            while (labels.Count < contexts.Count)
                labels.Add("");

            foreach (var family in Families)
            {
                if (FindCadencesColumn(contexts, labels, family.EnableLabel) < 0)
                {
                    Append(contexts, labels, family.EnableLabel);
                    added = true;
                }

                if (FindCadencesColumn(contexts, labels, family.DurationLabel) < 0)
                {
                    Append(contexts, labels, family.DurationLabel);
                    added = true;
                }

                if (FindCadencesColumn(contexts, labels, family.MechanicsLabel) < 0)
                {
                    Append(contexts, labels, family.MechanicsLabel);
                    added = true;
                }
            }

            if (!added)
                return (header, false);

            return (new SpreadsheetHeader(contexts, labels, header.LabelRowIndex, header.DataStartRowIndex), true);
        }

        /// <summary>
        /// Zero-based column indices for legacy cadence layout to delete on ACTIONS push:
        /// old per-family <c>TURN CADENCE</c> / <c>ACTION CADENCE</c> / … triples (often sheet columns K–V)
        /// and compact <c>DURATION</c> / <c>CADENCE</c> / <c>MECHANICS</c> (often W–Y).
        /// Does not include the authoritative <see cref="ContextBand"/> triples.
        /// </summary>
        public static IReadOnlyList<int> CollectLegacyColumnIndicesToRemove(SpreadsheetHeader header)
        {
            if (header == null)
                return Array.Empty<int>();

            int count = Math.Min(header.ContextByIndex.Count, header.LabelByIndex.Count);
            if (count == 0)
                return Array.Empty<int>();

            var filled = SpreadsheetHeader.FillMergedContext(
                header.ContextByIndex.Take(count).Select(c => c ?? "").ToArray());
            var remove = new List<int>();
            for (int i = 0; i < count; i++)
            {
                string ctx = SpreadsheetHeader.NormalizeLabel(filled[i]);
                string label = SpreadsheetHeader.NormalizeLabel(header.LabelByIndex[i]);
                if (IsLegacyCadenceColumn(ctx, label))
                    remove.Add(i);
            }

            return remove;
        }

        /// <summary>
        /// Contiguous inclusive ranges from <see cref="CollectLegacyColumnIndicesToRemove"/>,
        /// ordered high→low so Sheets <c>DeleteDimension</c> calls do not invalidate earlier indices.
        /// </summary>
        public static IReadOnlyList<(int StartInclusive, int EndExclusive)> BuildDescendingDeleteRanges(
            IReadOnlyList<int> zeroBasedIndices)
        {
            if (zeroBasedIndices == null || zeroBasedIndices.Count == 0)
                return Array.Empty<(int, int)>();

            var sorted = zeroBasedIndices.Distinct().OrderBy(i => i).ToList();
            var ascending = new List<(int Start, int End)>();
            int rangeStart = sorted[0];
            int prev = sorted[0];
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] == prev + 1)
                {
                    prev = sorted[i];
                    continue;
                }

                ascending.Add((rangeStart, prev + 1));
                rangeStart = sorted[i];
                prev = sorted[i];
            }

            ascending.Add((rangeStart, prev + 1));
            ascending.Reverse();
            return ascending;
        }

        /// <summary>Returns a header with the given zero-based columns removed (order preserved).</summary>
        public static SpreadsheetHeader RemoveColumns(SpreadsheetHeader header, IReadOnlyList<int> zeroBasedIndices)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));
            if (zeroBasedIndices == null || zeroBasedIndices.Count == 0)
                return header;

            var drop = new HashSet<int>(zeroBasedIndices);
            int count = Math.Max(header.ContextByIndex.Count, header.LabelByIndex.Count);
            var contexts = new List<string>(count);
            var labels = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                if (drop.Contains(i))
                    continue;
                contexts.Add(i < header.ContextByIndex.Count ? header.ContextByIndex[i] ?? "" : "");
                labels.Add(i < header.LabelByIndex.Count ? header.LabelByIndex[i] ?? "" : "");
            }

            return new SpreadsheetHeader(contexts, labels, header.LabelRowIndex, header.DataStartRowIndex);
        }

        internal static bool IsLegacyCadenceColumn(string normalizedContext, string normalizedLabel)
        {
            if (string.IsNullOrEmpty(normalizedLabel))
                return false;

            // Authoritative CADENCES band — keep.
            if (normalizedContext == SpreadsheetHeader.NormalizeLabel(ContextBand))
                return false;

            // Pre-CADENCES per-family contexts (e.g. "TURN CADENCE") and their duration/→ siblings.
            if (normalizedContext is "TURNCADENCE" or "ACTIONCADENCE" or "FIGHTCADENCE" or "DUNGEONCADENCE")
                return true;

            // Compact DURATION / CADENCE / MECHANICS (old K/L/M, later W/X/Y).
            if (normalizedLabel == "DURATION" && normalizedContext == "STATUSEFFECT")
                return true;
            if (normalizedLabel == "CADENCE")
                return true;
            if (normalizedLabel == "MECHANICS")
                return true;

            return false;
        }

        public static void ReadFromRow(SpreadsheetActionData data, string[] columns, SpreadsheetHeader header)
        {
            if (data == null || columns == null || header == null)
                return;

            var bundles = new List<ActionCadenceBundle>();
            foreach (var family in Families)
            {
                string enable = header.GetValue(columns, ContextBand, family.EnableLabel, allowUnscopedLabelFallback: false);
                string duration = header.GetValue(columns, ContextBand, family.DurationLabel, allowUnscopedLabelFallback: false);
                string mechanics = header.GetValue(columns, ContextBand, family.MechanicsLabel, allowUnscopedLabelFallback: false);

                // Duration / → labels are unique enough to allow unscoped fallback; enable labels ACTION/TURN are not.
                if (string.IsNullOrWhiteSpace(duration))
                    duration = header.GetValue(columns, null, family.DurationLabel);
                if (string.IsNullOrWhiteSpace(mechanics))
                    mechanics = header.GetValue(columns, null, family.MechanicsLabel);

                var bundle = new ActionCadenceBundle
                {
                    Cadence = family.CadenceToken,
                    Enable = enable?.Trim() ?? "",
                    Duration = duration?.Trim() ?? "",
                    Mechanics = mechanics?.Trim() ?? ""
                };
                if (!bundle.IsEnabled)
                    continue;
                bundles.Add(bundle);
            }

            if (bundles.Count > 0)
                data.CadenceBundlesJson = JsonSerializer.Serialize(bundles);
        }

        public static void WriteToRow(SpreadsheetHeader header, string[] row, SpreadsheetActionData data)
        {
            if (header == null || row == null || data == null)
                return;

            var byCadence = LoadBundles(data)
                .ToDictionary(b => CadenceKeywords.Normalize(b.Cadence), StringComparer.OrdinalIgnoreCase);

            foreach (var family in Families)
            {
                string enable = "";
                string duration = "";
                string mechanics = "";
                if (byCadence.TryGetValue(family.CadenceToken, out var bundle))
                {
                    enable = string.IsNullOrWhiteSpace(bundle.Enable) && bundle.IsEnabled ? "1" : (bundle.Enable ?? "");
                    duration = bundle.Duration ?? "";
                    if (string.IsNullOrWhiteSpace(duration) && bundle.IsEnabled)
                        duration = bundle.ResolveDurationCount().ToString(CultureInfo.InvariantCulture);
                    mechanics = bundle.Mechanics ?? "";
                }

                header.SetCell(row, ContextBand, family.EnableLabel, enable, allowUnscopedLabelFallback: false);
                header.SetCell(row, ContextBand, family.DurationLabel, duration, allowUnscopedLabelFallback: true);
                header.SetCell(row, ContextBand, family.MechanicsLabel, mechanics, allowUnscopedLabelFallback: true);
            }
        }

        public static List<ActionCadenceBundle> LoadBundles(SpreadsheetActionData data)
        {
            var list = new List<ActionCadenceBundle>();
            if (data == null)
                return list;

            if (!string.IsNullOrWhiteSpace(data.CadenceBundlesJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<ActionCadenceBundle>>(data.CadenceBundlesJson);
                    if (parsed != null)
                    {
                        foreach (var b in parsed)
                        {
                            if (b == null || string.IsNullOrWhiteSpace(b.Cadence))
                                continue;
                            b.Cadence = CadenceKeywords.Normalize(b.Cadence);
                            if (string.Equals(b.Cadence, "ATTACK", StringComparison.OrdinalIgnoreCase)
                                || string.IsNullOrEmpty(b.Cadence))
                                b.Cadence = CadenceKeywords.NormalizeFromRow(b.Cadence, data);
                            list.Add(b);
                        }
                    }
                }
                catch (JsonException)
                {
                    // fall through
                }
            }

            if (list.Count == 0)
                list.AddRange(BuildLegacySingleBundle(data));

            return list.Where(b => b.IsEnabled).ToList();
        }

        /// <summary>
        /// Builds editor cadence blocks from sheet bundles; magnitudes come from detail columns on <paramref name="data"/>.
        /// </summary>
        public static List<CadenceEditorBlock> BuildEditorBlocks(SpreadsheetActionData data)
        {
            var blocks = new List<CadenceEditorBlock>();
            foreach (var bundle in LoadBundles(data))
            {
                var block = new CadenceEditorBlock
                {
                    Cadence = ToEditorCadence(bundle.Cadence),
                    Duration = Math.Max(1, bundle.ResolveDurationCount())
                };
                foreach (string id in bundle.ParseMechanicIds())
                {
                    foreach (var row in ResolveMechanicRows(data, id))
                        block.Mechanics.Add(row);
                }

                if (block.Mechanics.Count > 0 || bundle.IsEnabled)
                    blocks.Add(block);
            }

            return blocks.Where(b => b.Mechanics.Count > 0).ToList();
        }

        public static void ApplyBundlesToSpreadsheetRow(SpreadsheetActionData data, IEnumerable<CadenceEditorBlock>? blocks)
        {
            if (data == null)
                return;

            var bundles = new List<ActionCadenceBundle>();
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    if (block == null || block.Mechanics == null || block.Mechanics.Count == 0)
                        continue;
                    string cadence = CadenceKeywords.Normalize(block.Cadence);
                    if (string.IsNullOrEmpty(cadence))
                        cadence = CadenceKeywords.Turn;
                    bundles.Add(new ActionCadenceBundle
                    {
                        Cadence = cadence,
                        Enable = "1",
                        Duration = Math.Max(1, block.Duration).ToString(CultureInfo.InvariantCulture),
                        Mechanics = string.Join(", ",
                            block.Mechanics
                                .Where(m => !string.IsNullOrWhiteSpace(m.MechanicId))
                                .Select(m => ActionMechanicsRegistry.NormalizeMechanicId(m.MechanicId))
                                .Distinct(StringComparer.OrdinalIgnoreCase))
                    });
                }
            }

            data.CadenceBundlesJson = bundles.Count > 0 ? JsonSerializer.Serialize(bundles) : "";
            // Clear legacy compact columns when new band is authoritative
            if (bundles.Count > 0)
            {
                data.Cadence = "";
                data.Duration = "";
                data.Mechanics = "";
            }
        }

        private static List<ActionCadenceBundle> BuildLegacySingleBundle(SpreadsheetActionData data)
        {
            if (string.IsNullOrWhiteSpace(data.Cadence) && string.IsNullOrWhiteSpace(data.Mechanics))
                return new List<ActionCadenceBundle>();

            string cadence = CadenceKeywords.NormalizeFromRow(data.Cadence, data);
            if (string.IsNullOrEmpty(cadence) && !string.IsNullOrWhiteSpace(data.Mechanics))
                cadence = ActionMechanicsRegistry.ResolveDefaultCadence(
                              ActionMechanicsRegistry.ParseMechanicsCell(data.Mechanics))
                          ?? CadenceKeywords.Turn;

            if (string.IsNullOrEmpty(cadence))
                return new List<ActionCadenceBundle>();

            string duration = data.Duration?.Trim() ?? "";
            if (string.IsNullOrEmpty(duration))
                duration = "1";

            string mechanics = data.Mechanics?.Trim() ?? "";
            // If MECHANICS empty, infer from populated detail columns (legacy ProcessBonuses behavior uses all columns)
            if (string.IsNullOrEmpty(mechanics))
            {
                var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(data)
                    .Where(id => ActionMechanicsRegistry.RequiresCadenceDuration(id)
                                 || ActionMechanicsRegistry.GetAllowedCadencesForMechanic(id).Count > 0)
                    .ToList();
                // Prefer dice/mod ids that ProcessBonuses would collect
                mechanics = string.Join(", ", FilterLegacyCollectable(detected));
            }

            if (string.IsNullOrWhiteSpace(mechanics) && string.IsNullOrWhiteSpace(data.Cadence))
                return new List<ActionCadenceBundle>();

            return new List<ActionCadenceBundle>
            {
                new ActionCadenceBundle
                {
                    Cadence = cadence,
                    Enable = "1",
                    Duration = duration,
                    Mechanics = mechanics
                }
            };
        }

        private static IEnumerable<string> FilterLegacyCollectable(IEnumerable<string> ids)
        {
            foreach (string id in ids)
            {
                string n = ActionMechanicsRegistry.NormalizeMechanicId(id);
                if (n.StartsWith("hero_", StringComparison.OrdinalIgnoreCase)
                    || n.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase)
                    || n is "weaken" or "slow" or "vulnerability" or "harden" or "focus" or "confuse"
                        or "stat_drain" or "fortify" or "disrupt" or "heal" or "max_health"
                        or "advantage" or "disadvantage" or "pierce")
                    yield return n;
            }
        }

        public static IEnumerable<CadenceMechanicRow> ResolveMechanicRows(SpreadsheetActionData data, string mechanicId)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId);
            if (string.IsNullOrEmpty(id) || data == null)
                yield break;

            switch (id)
            {
                case "hero_accuracy":
                    if (TryQty(data.HeroAccuracy, out double ha)) yield return Row(id, ha);
                    break;
                case "hero_hit_threshold":
                    if (TryQty(data.HeroHit, out double hh)) yield return Row(id, hh);
                    break;
                case "hero_combo_threshold":
                    if (TryQty(data.HeroCombo, out double hc)) yield return Row(id, hc);
                    break;
                case "hero_crit_threshold":
                    if (TryQty(data.HeroCrit, out double hcr)) yield return Row(id, hcr);
                    break;
                case "hero_crit_miss_threshold":
                    if (TryQty(data.HeroCritMiss, out double hcm)) yield return Row(id, hcm);
                    break;
                case "enemy_accuracy":
                    if (TryQty(data.EnemyAccuracy, out double ea)) yield return Row(id, ea);
                    break;
                case "enemy_hit_threshold":
                    if (TryQty(data.EnemyHit, out double eh)) yield return Row(id, eh);
                    break;
                case "enemy_combo_threshold":
                    if (TryQty(data.EnemyCombo, out double ec)) yield return Row(id, ec);
                    break;
                case "enemy_crit_threshold":
                    if (TryQty(data.EnemyCrit, out double ecr)) yield return Row(id, ecr);
                    break;
                case "enemy_crit_miss_threshold":
                    if (TryQty(data.EnemyCritMiss, out double ecm)) yield return Row(id, ecm);
                    break;
                case "hero_next_action_speed":
                    if (TryMod(data.SpeedMod, id, out double hs)) yield return Row(id, hs);
                    break;
                case "hero_next_action_damage":
                    if (TryMod(data.DamageMod, id, out double hd)) yield return Row(id, hd);
                    break;
                case "hero_next_action_multihit":
                    if (TryMod(data.MultiHitMod, id, out double hm)) yield return Row(id, hm);
                    break;
                case "hero_next_action_amp":
                    if (TryMod(data.AmpMod, id, out double hap)) yield return Row(id, hap);
                    break;
                case "enemy_next_action_speed":
                    if (TryMod(data.EnemySpeedMod, id, out double es)) yield return Row(id, es);
                    break;
                case "enemy_next_action_damage":
                    if (TryMod(data.EnemyDamageMod, id, out double ed)) yield return Row(id, ed);
                    break;
                case "enemy_next_action_multihit":
                    if (TryMod(data.EnemyMultiHitMod, id, out double em)) yield return Row(id, em);
                    break;
                case "enemy_next_action_amp":
                    if (TryMod(data.EnemyAmpMod, id, out double eap)) yield return Row(id, eap);
                    break;
                case "hero_weapon_speed":
                    if (TryQty(data.WeaponSpeedMod, out double hws)) yield return Row(id, hws);
                    break;
                case "hero_weapon_damage":
                    if (TryQty(data.WeaponDamageMod, out double hwd)) yield return Row(id, hwd);
                    break;
                case "enemy_weapon_speed":
                    if (TryQty(data.EnemyWeaponSpeedMod, out double ews)) yield return Row(id, ews);
                    break;
                case "enemy_weapon_damage":
                    if (TryQty(data.EnemyWeaponDamageMod, out double ewd)) yield return Row(id, ewd);
                    break;
                case "hero_stat_bonus":
                    foreach (var (sub, val) in ReadHeroStats(data))
                        yield return Row(id, val, sub);
                    break;
                case "enemy_stat_bonus":
                    foreach (var (sub, val) in ReadEnemyStats(data))
                        yield return Row(id, val, sub);
                    break;
                case "weaken":
                    if (TryStatus(data.Weaken, out double w)) yield return Row(id, w);
                    break;
                case "slow":
                    if (TryStatus(data.Slow, out double sl)) yield return Row(id, sl);
                    break;
                case "vulnerability":
                    if (TryStatus(data.Vulnerability, out double v)) yield return Row(id, v);
                    break;
                case "harden":
                    if (TryStatus(data.Harden, out double har)) yield return Row(id, har);
                    break;
                case "focus":
                    if (TryStatus(data.Focus, out double fo)) yield return Row(id, fo);
                    break;
                case "confuse":
                    if (TryStatus(data.Confuse, out double co)) yield return Row(id, co);
                    break;
                case "stat_drain":
                    if (TryStatus(data.StatDrain, out double sd)) yield return Row(id, sd);
                    break;
                case "fortify":
                    if (TryStatus(data.Fortify, out double ft)) yield return Row(id, ft);
                    break;
                case "pierce":
                    if (TryStatus(data.Pierce, out double pi)) yield return Row(id, pi);
                    break;
                case "disrupt":
                    if (TryStatus(data.Disrupt, out double di)) yield return Row(id, di);
                    break;
                case "heal":
                    if (TryQty(data.HeroHeal, out double heal)) yield return Row(id, heal);
                    break;
                case "max_health":
                    if (TryQty(data.HeroHealMaxHealth, out double mh)) yield return Row(id, mh);
                    break;
                default:
                    // Pointer present but unknown magnitude column — keep as status-like apply (qty 1)
                    yield return Row(id, 1);
                    break;
            }
        }

        private static CadenceMechanicRow Row(string id, double qty, string stat = "") =>
            new CadenceMechanicRow { MechanicId = id, Quantity = qty, StatSubType = stat };

        private static bool TryQty(string? cell, out double qty)
        {
            qty = 0;
            if (string.IsNullOrWhiteSpace(cell))
                return false;
            qty = SpreadsheetActionData.ParseNumericValue(cell);
            return qty != 0 || cell.Trim() == "0";
        }

        private static bool TryMod(string? cell, string mechanicId, out double qty)
        {
            qty = 0;
            if (string.IsNullOrWhiteSpace(cell))
                return false;
            if (ActionMechanicsRegistry.IsPercentQuantityMechanic(mechanicId))
            {
                if (ModifierParser.ParsePercent(cell) is { } p)
                {
                    qty = p * 100.0;
                    return qty != 0;
                }
            }

            qty = ModifierParser.ParseValue(cell) ?? SpreadsheetActionData.ParseNumericValue(cell);
            return qty != 0;
        }

        private static bool TryStatus(string? cell, out double qty)
        {
            qty = 0;
            if (string.IsNullOrWhiteSpace(cell))
                return false;
            qty = SpreadsheetActionData.ParseNumericValue(cell);
            if (qty == 0)
                qty = 1;
            return true;
        }

        private static IEnumerable<(string Sub, double Val)> ReadHeroStats(SpreadsheetActionData data)
        {
            if (TryQty(data.HeroSTR, out double s)) yield return ("STR", s);
            if (TryQty(data.HeroAGI, out double a)) yield return ("AGI", a);
            if (TryQty(data.HeroTECH, out double t)) yield return ("TECH", t);
            if (TryQty(data.HeroINT, out double i)) yield return ("INT", i);
        }

        private static IEnumerable<(string Sub, double Val)> ReadEnemyStats(SpreadsheetActionData data)
        {
            if (TryQty(data.EnemySTR, out double s)) yield return ("STR", s);
            if (TryQty(data.EnemyAGI, out double a)) yield return ("AGI", a);
            if (TryQty(data.EnemyTECH, out double t)) yield return ("TECH", t);
            if (TryQty(data.EnemyINT, out double i)) yield return ("INT", i);
        }

        private static string ToEditorCadence(string cadence)
        {
            string n = CadenceKeywords.Normalize(cadence);
            return n switch
            {
                "TURN" => "Turn",
                "ACTION" => "Action",
                "FIGHT" => "Fight",
                "DUNGEON" => "Dungeon",
                "CHAIN" => "Chain",
                _ => string.IsNullOrEmpty(n) ? "Turn" : char.ToUpperInvariant(n[0]) + n.Substring(1).ToLowerInvariant()
            };
        }

        private static void Append(List<string> contexts, List<string> labels, string label)
        {
            contexts.Add(ContextBand);
            labels.Add(label);
        }

        private static int FindCadencesColumn(List<string> contexts, List<string> labels, string label)
        {
            string want = SpreadsheetHeader.NormalizeLabel(label);
            string ctx = SpreadsheetHeader.NormalizeLabel(ContextBand);
            for (int i = 0; i < labels.Count; i++)
            {
                if (SpreadsheetHeader.NormalizeLabel(labels[i]) != want)
                    continue;
                string c = i < contexts.Count ? SpreadsheetHeader.NormalizeLabel(contexts[i]) : "";
                if (c == ctx)
                    return i;
            }

            return -1;
        }

        private static int FindColumn(List<string> contexts, List<string> labels, string label)
        {
            int scoped = FindCadencesColumn(contexts, labels, label);
            if (scoped >= 0)
                return scoped;

            string want = SpreadsheetHeader.NormalizeLabel(label);
            for (int i = 0; i < labels.Count; i++)
            {
                if (SpreadsheetHeader.NormalizeLabel(labels[i]) == want)
                    return i;
            }

            return -1;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }

            return "";
        }
    }
}
