using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Documents how ACTIONS tab column labels (row 2, optionally scoped by row 1 context) are used on CSV pull.
    /// Keep in sync with <see cref="SpreadsheetActionDataCsvParser"/> and Documentation/GOOGLE_SHEETS_INTEGRATION.md.
    /// </summary>
    public enum SpreadsheetColumnUsage
    {
        /// <summary>Pulled and applied in combat via ActionData → Action.</summary>
        RuntimeCombat,

        /// <summary>Pulled; affects loot/pool assignment, not combat math.</summary>
        LootPools,

        /// <summary>Pulled into Actions.json / Settings; not applied in combat today.</summary>
        JsonRoundTrip,

        /// <summary>Ingested on pull but not wired to runtime Action (known gap).</summary>
        IngestedNotRuntime,

        /// <summary>Known to push serializer but not read by <see cref="SpreadsheetActionDataCsvParser"/> on pull.</summary>
        NotIngested,

        /// <summary>Reserved sheet column (e.g. column F formula); never pulled.</summary>
        SheetFormula,
    }

    public readonly record struct SpreadsheetColumnUsageEntry(
        string NormalizedLabel,
        SpreadsheetColumnUsage Usage,
        string? ContextHint = null);

    public static class SpreadsheetActionColumnUsage
    {
        private static readonly SpreadsheetColumnUsageEntry[] Registry =
        {
            // Core
            new("ACTION", SpreadsheetColumnUsage.RuntimeCombat),
            new("DESCRIPTION", SpreadsheetColumnUsage.JsonRoundTrip),
            new("RARITY", SpreadsheetColumnUsage.LootPools),
            new("CATEGORY", SpreadsheetColumnUsage.LootPools),
            new("TAGS", SpreadsheetColumnUsage.LootPools),
            new("TAG", SpreadsheetColumnUsage.LootPools),
            new("DPS", SpreadsheetColumnUsage.JsonRoundTrip),
            new("DPS(%)", SpreadsheetColumnUsage.JsonRoundTrip),
            new("#OFHITS", SpreadsheetColumnUsage.RuntimeCombat),
            new("DAMAGE", SpreadsheetColumnUsage.RuntimeCombat),
            new("DAMAGE(%)", SpreadsheetColumnUsage.RuntimeCombat),
            new("SPEED", SpreadsheetColumnUsage.RuntimeCombat),
            new("SPEED(X)", SpreadsheetColumnUsage.RuntimeCombat),
            new("DURATION", SpreadsheetColumnUsage.RuntimeCombat),
            new("CADENCE", SpreadsheetColumnUsage.RuntimeCombat),
            new("OPENER", SpreadsheetColumnUsage.RuntimeCombat),
            new("FINISHER", SpreadsheetColumnUsage.RuntimeCombat),

            // Hero dice
            new("ACCUARCY", SpreadsheetColumnUsage.RuntimeCombat, "HERODICEROLLMODIFICATIONS"),
            new("ACCURACY", SpreadsheetColumnUsage.RuntimeCombat, "HERODICEROLLMODIFICATIONS"),
            new("HIT", SpreadsheetColumnUsage.RuntimeCombat, "HERODICEROLLMODIFICATIONS"),
            new("COMBO", SpreadsheetColumnUsage.RuntimeCombat, "HERODICEROLLMODIFICATIONS"),
            new("CRIT", SpreadsheetColumnUsage.RuntimeCombat, "HERODICEROLLMODIFICATIONS"),
            new("CRITMISS", SpreadsheetColumnUsage.RuntimeCombat, "HERODICEROLLMODIFICATIONS"),

            // Enemy dice
            new("ACCUARCY", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYDICEMODIFICATIONS"),
            new("ACCURACY", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYDICEMODIFICATIONS"),
            new("HIT", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYDICEMODIFICATIONS"),
            new("COMBO", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYDICEMODIFICATIONS"),
            new("CRIT", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYDICEMODIFICATIONS"),
            new("CRITMISS", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYDICEMODIFICATIONS"),

            // Attributes (combat only when CADENCE keyword bonuses apply)
            new("STR", SpreadsheetColumnUsage.RuntimeCombat, "HEROATTRIBUTEMODIFICATION"),
            new("AGI", SpreadsheetColumnUsage.RuntimeCombat, "HEROATTRIBUTEMODIFICATION"),
            new("TECH", SpreadsheetColumnUsage.RuntimeCombat, "HEROATTRIBUTEMODIFICATION"),
            new("INT", SpreadsheetColumnUsage.RuntimeCombat, "HEROATTRIBUTEMODIFICATION"),
            new("STR", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYATTRIBUTEMODIFICATIONS"),
            new("AGI", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYATTRIBUTEMODIFICATIONS"),
            new("TECH", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYATTRIBUTEMODIFICATIONS"),
            new("INT", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYATTRIBUTEMODIFICATIONS"),

            // Next-action mods
            new("SPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("ACTIONSPEED", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("HEROACTIONSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("HEROSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("DAMAGEMOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("ACTIONDAMAGE", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("MULTIHITMOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("HEROMULTIHITMOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("AMP_MOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("AMPMOD", SpreadsheetColumnUsage.RuntimeCombat, "HEROBASESTATS"),
            new("SPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("ACTIONSPEED", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("ENEMYACTIONSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("ENEMYSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("DAMAGEMOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("ACTIONDAMAGE", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("MULTIHITMOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("ENEMYMULTIHITMOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("AMP_MOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("AMPMOD", SpreadsheetColumnUsage.RuntimeCombat, "ENEMYBASESTATS"),
            new("SPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("ACTIONSPEED", SpreadsheetColumnUsage.RuntimeCombat),
            new("HEROACTIONSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("HEROSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("DAMAGEMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("ACTIONDAMAGE", SpreadsheetColumnUsage.RuntimeCombat),
            new("MULTIHITMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("HEROMULTIHITMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("ENEMYACTIONSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("ENEMYSPEEDMOD", SpreadsheetColumnUsage.RuntimeCombat),
            new("ENEMYMULTIHITMOD", SpreadsheetColumnUsage.RuntimeCombat),

            // Heal
            new("HEAL", SpreadsheetColumnUsage.RuntimeCombat, "HEROHEAL"),
            new("HEAL", SpreadsheetColumnUsage.RuntimeCombat),
            new("MAXHEALTH", SpreadsheetColumnUsage.IngestedNotRuntime, "HEROHEAL"),
            new("MAXHEALTH", SpreadsheetColumnUsage.IngestedNotRuntime),

            // Status (enemy target block + unscoped fallbacks)
            new("STUN", SpreadsheetColumnUsage.RuntimeCombat),
            new("POISON", SpreadsheetColumnUsage.RuntimeCombat),
            new("BURN", SpreadsheetColumnUsage.RuntimeCombat),
            new("BLEED", SpreadsheetColumnUsage.RuntimeCombat),
            new("WEAKEN", SpreadsheetColumnUsage.RuntimeCombat),
            new("EXPOSE", SpreadsheetColumnUsage.RuntimeCombat),
            new("SLOW", SpreadsheetColumnUsage.RuntimeCombat),
            new("VULNERABILITY", SpreadsheetColumnUsage.RuntimeCombat),
            new("HARDEN", SpreadsheetColumnUsage.RuntimeCombat),
            new("SILENCE", SpreadsheetColumnUsage.RuntimeCombat),
            new("PIERCE", SpreadsheetColumnUsage.RuntimeCombat),
            new("STATDRAIN", SpreadsheetColumnUsage.RuntimeCombat),
            new("FORTIFY", SpreadsheetColumnUsage.IngestedNotRuntime),
            new("CONSUME", SpreadsheetColumnUsage.IngestedNotRuntime),
            new("FOCUS", SpreadsheetColumnUsage.RuntimeCombat),
            new("CLENSE", SpreadsheetColumnUsage.IngestedNotRuntime),
            new("LIFESTEAL", SpreadsheetColumnUsage.RuntimeCombat),
            new("CONFUSE", SpreadsheetColumnUsage.RuntimeCombat),
            new("REFLECT", SpreadsheetColumnUsage.IngestedNotRuntime),

            // Target (column M — enemy/self/environment; empty = enemy)
            new("TARGET", SpreadsheetColumnUsage.RuntimeCombat),

            // Combo routing (ingested subset)
            new("JUMP", SpreadsheetColumnUsage.RuntimeCombat),
            new("SHIFT", SpreadsheetColumnUsage.RuntimeCombat),
            new("JUMPRELATIVE", SpreadsheetColumnUsage.RuntimeCombat),
            new("DISRUPT", SpreadsheetColumnUsage.RuntimeCombat),

            // Not ingested on CSV pull (push/Settings round-trip only)
            new("ISDEFAULTACTION", SpreadsheetColumnUsage.NotIngested),
            new("WEAPONTYPES", SpreadsheetColumnUsage.NotIngested),
            new("CHAINLENGTH", SpreadsheetColumnUsage.NotIngested),
            new("CHAINPOSITION", SpreadsheetColumnUsage.NotIngested),
            new("RESET", SpreadsheetColumnUsage.NotIngested),
            new("RESETBLOCKERBUFFER", SpreadsheetColumnUsage.NotIngested),
            new("MODIFYBASEDONCHAINPOISITION", SpreadsheetColumnUsage.NotIngested),
            new("MODIFYBASEDONCHAINPOSITION", SpreadsheetColumnUsage.NotIngested),
            new("GRACE", SpreadsheetColumnUsage.NotIngested),
            new("LOOPCHAIN", SpreadsheetColumnUsage.NotIngested),
            new("SHUFFLE", SpreadsheetColumnUsage.NotIngested),
            new("REPLACEACTION", SpreadsheetColumnUsage.NotIngested),
            new("DISTANCEFROMXSLOT", SpreadsheetColumnUsage.NotIngested),
            new("REPLACENEXTROLL", SpreadsheetColumnUsage.NotIngested),
            new("HIGHEST/LOWESTROLL", SpreadsheetColumnUsage.RuntimeCombat),
            new("HIGHESTLOWESTROLL", SpreadsheetColumnUsage.RuntimeCombat),
            new("DICEROLLS", SpreadsheetColumnUsage.RuntimeCombat),
            new("EXPLODINGDICETHRESHOLD", SpreadsheetColumnUsage.NotIngested),
            new("CURSE", SpreadsheetColumnUsage.NotIngested),
            new("SKIP", SpreadsheetColumnUsage.NotIngested),
            new("ONHIT", SpreadsheetColumnUsage.NotIngested),
            new("ONMISS", SpreadsheetColumnUsage.NotIngested),
            new("ONCRIT", SpreadsheetColumnUsage.NotIngested),
            new("ONKILL", SpreadsheetColumnUsage.NotIngested),
            new("ONROOMSCLEARED", SpreadsheetColumnUsage.NotIngested),
            new("ONROLLVALUE", SpreadsheetColumnUsage.NotIngested),
            new("TRIGGERCONDITIONS", SpreadsheetColumnUsage.NotIngested),
            new("STATBONUSESJSON", SpreadsheetColumnUsage.NotIngested),
            new("THRESHOLDSJSON", SpreadsheetColumnUsage.NotIngested),
            new("ACCUMULATIONSJSON", SpreadsheetColumnUsage.NotIngested),
            new("CHAINPOSITIONBONUSESJSON", SpreadsheetColumnUsage.NotIngested),
            new("THRESHOLDCATEGORY", SpreadsheetColumnUsage.NotIngested),
            new("THRESHOLDAMOUNT", SpreadsheetColumnUsage.NotIngested),
            new("BONUS", SpreadsheetColumnUsage.NotIngested),
            new("BONUSATTRIBUTE", SpreadsheetColumnUsage.NotIngested),
            new("VALUE", SpreadsheetColumnUsage.NotIngested),
            new("ATTRIBUTE", SpreadsheetColumnUsage.NotIngested),
            new("MODIFYROOM", SpreadsheetColumnUsage.NotIngested),
        };

        private static readonly HashSet<string> IngestedNormalizedLabels = BuildIngestedLabelSet();

        private static HashSet<string> BuildIngestedLabelSet()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in Registry)
            {
                if (entry.Usage == SpreadsheetColumnUsage.NotIngested
                    || entry.Usage == SpreadsheetColumnUsage.SheetFormula)
                    continue;

                string key = string.IsNullOrEmpty(entry.ContextHint)
                    ? entry.NormalizedLabel
                    : entry.ContextHint + "|" + entry.NormalizedLabel;
                set.Add(key);
            }
            return set;
        }

        public static SpreadsheetColumnUsage? TryGetUsage(string? rawLabel, string? contextHint = null)
        {
            if (string.IsNullOrWhiteSpace(rawLabel))
                return null;

            string normLabel = SpreadsheetHeader.NormalizeLabel(rawLabel);
            string normCtx = string.IsNullOrWhiteSpace(contextHint)
                ? ""
                : SpreadsheetHeader.NormalizeLabel(contextHint);

            SpreadsheetColumnUsage? best = null;
            foreach (var entry in Registry)
            {
                if (!string.Equals(entry.NormalizedLabel, normLabel, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(entry.ContextHint))
                {
                    if (string.Equals(entry.ContextHint, normCtx, StringComparison.OrdinalIgnoreCase))
                        return entry.Usage;
                    continue;
                }

                if (string.IsNullOrEmpty(normCtx))
                    best = entry.Usage;
                else if (best == null)
                    best = entry.Usage;
            }

            return best;
        }

        public static bool IsIngestedOnPull(string? rawLabel, string? contextHint = null)
        {
            if (string.IsNullOrWhiteSpace(rawLabel))
                return false;
            return IngestedNormalizedLabels.Contains(NormalizeKey(rawLabel, contextHint));
        }

        /// <summary>
        /// Header columns whose labels are not read by the CSV parser on pull.
        /// </summary>
        public static IReadOnlyList<string> GetLabelsIgnoredOnPull(SpreadsheetHeader header)
        {
            var ignored = new List<string>();
            for (int i = 0; i < header.LabelByIndex.Count; i++)
            {
                if (i == SheetsPushUtilities.ActionsSheetPreservedFormulaColumnZeroBased)
                    continue;

                string raw = header.LabelByIndex[i] ?? "";
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string ctx = i < header.ContextByIndex.Count ? header.ContextByIndex[i] : "";
                if (IsIngestedOnPull(raw, ctx))
                    continue;

                var usage = TryGetUsage(raw, ctx);
                if (usage == SpreadsheetColumnUsage.NotIngested)
                {
                    ignored.Add(FormatColumnRef(raw, ctx));
                    continue;
                }

                if (usage == null)
                    ignored.Add(FormatColumnRef(raw, ctx));
            }

            return ignored
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Labels ingested on pull but not applied in combat (JsonRoundTrip, IngestedNotRuntime).
        /// </summary>
        public static IReadOnlyList<string> GetLabelsStoredButNotCombat(SpreadsheetHeader header)
        {
            var list = new List<string>();
            for (int i = 0; i < header.LabelByIndex.Count; i++)
            {
                string raw = header.LabelByIndex[i] ?? "";
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string ctx = i < header.ContextByIndex.Count ? header.ContextByIndex[i] : "";
                var usage = TryGetUsage(raw, ctx);
                if (usage is SpreadsheetColumnUsage.JsonRoundTrip or SpreadsheetColumnUsage.IngestedNotRuntime)
                    list.Add($"{FormatColumnRef(raw, ctx)} ({usage})");
            }

            return list
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void PrintPullColumnUsageSummary(SpreadsheetHeader? header)
        {
            if (header == null)
                return;

            Console.WriteLine("ACTIONS sheet column usage (pull):");
            Console.WriteLine("  Column F: sheet formula only (never pulled; preserved on push).");

            var ignored = GetLabelsIgnoredOnPull(header);
            if (ignored.Count > 0)
                Console.WriteLine("  Ignored on pull: " + string.Join(", ", ignored));
            else
                Console.WriteLine("  Ignored on pull: (none — all labeled columns match ingested or known-not-ingested registry).");

            var nonCombat = GetLabelsStoredButNotCombat(header);
            if (nonCombat.Count > 0)
                Console.WriteLine("  Stored but not used in combat: " + string.Join(", ", nonCombat));
        }

        private static string NormalizeKey(string rawLabel, string? contextHint)
        {
            string label = SpreadsheetHeader.NormalizeLabel(rawLabel);
            if (string.IsNullOrWhiteSpace(contextHint))
                return label;
            return SpreadsheetHeader.NormalizeLabel(contextHint) + "|" + label;
        }

        private static string FormatColumnRef(string rawLabel, string? contextHint)
        {
            if (string.IsNullOrWhiteSpace(contextHint))
                return rawLabel.Trim();
            return $"{contextHint.Trim()} / {rawLabel.Trim()}";
        }
    }
}
