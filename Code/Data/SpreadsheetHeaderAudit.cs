using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.Data
{
    /// <summary>
    /// Validates ACTIONS tab row-1 context + row-2 labels against ingestion expectations.
    /// </summary>
    public static class SpreadsheetHeaderAudit
    {
        private static readonly (string? Context, string Label, string Purpose)[] RequiredColumns =
        {
            (null, "ACTION", "Action name"),
            (null, "DESCRIPTION", "Description"),
            ("STATUS EFFECT", "DURATION", "Cadence duration (TURN/ACTION application count, e.g. x2)"),
            (null, "CADENCE", "Cadence keyword (TURN / ACTION / FIGHT / DUNGEON; legacy ATTACK/ABILITY accepted on import)"),
            (null, "TARGET", "Effect target (enemy / self / environment) — optional on some layouts"),
            ("ENEMY TARGET", "STUN", "Legacy column; stun is item-applied only (not pulled to combat)"),
            ("ENEMY TARGET", "POISON", "Legacy column; poison is item-applied only (not pulled to combat)"),
            ("ENEMY TARGET", "BURN", "Legacy column; burn is item-applied only (not pulled to combat)"),
            ("ENEMY TARGET", "BLEED", "Legacy column; bleed is item-applied only (not pulled to combat)"),
            ("HERO BASE STATS", "ACTION DAMAGE", "Hero DAMAGE_MOD for next-action bonuses"),
        };

        /// <summary>Prints a column letter map and validation warnings to the console.</summary>
        public static void PrintAudit(SpreadsheetHeader? header)
        {
            foreach (var line in BuildAuditLines(header))
                Console.WriteLine(line);
        }

        public static IReadOnlyList<string> BuildAuditLines(SpreadsheetHeader? header)
        {
            var lines = new List<string>();
            if (header == null)
            {
                lines.Add("Spreadsheet header audit: no two-row header detected (using legacy index fallback).");
                return lines;
            }

            lines.Add("=== ACTIONS sheet header audit ===");
            lines.Add("Row 1 = section context, row 2 = column labels (matched by label + context, not fixed letters).");
            lines.Add("");

            var filledContext = SpreadsheetHeader.FillMergedContext(header.ContextByIndex.ToArray());
            int width = Math.Max(header.LabelByIndex.Count, filledContext.Length);

            lines.Add("Column map (first 45 columns):");
            for (int i = 0; i < Math.Min(width, 45); i++)
            {
                string letter = IndexToExcelColumn(i);
                string ctx = i < filledContext.Length ? filledContext[i].Trim() : "";
                string lbl = i < header.LabelByIndex.Count ? (header.LabelByIndex[i] ?? "").Trim() : "";
                if (string.IsNullOrEmpty(lbl) && string.IsNullOrEmpty(ctx))
                    continue;
                lines.Add($"  {letter,4}  ctx={FormatCell(ctx),-28}  lbl={FormatCell(lbl)}");
            }

            lines.Add("");
            lines.Add("Required columns:");
            foreach (var req in RequiredColumns)
            {
                int idx = header.GetColumnIndex(req.Context, req.Label);
                string letter = idx >= 0 ? IndexToExcelColumn(idx) : "MISSING";
                string status = idx >= 0 ? "OK" : "MISSING";
                string ctxPart = string.IsNullOrEmpty(req.Context) ? "(any)" : req.Context;
                lines.Add($"  [{status}] {letter,4}  {ctxPart} / {req.Label} — {req.Purpose}");
            }

            int cadenceDurationIdx = header.GetColumnIndex("STATUS EFFECT", "DURATION", allowUnscopedLabelFallback: false);
            int cadenceIdx = header.GetColumnIndex(null, "CADENCE");
            if (cadenceDurationIdx >= 0 && cadenceIdx >= 0)
            {
                lines.Add("");
                lines.Add("STATUS EFFECT semantics:");
                lines.Add($"  DURATION is column {IndexToExcelColumn(cadenceDurationIdx)} (cadence duration — how many TURN/ACTION applications).");
                lines.Add($"  CADENCE is column {IndexToExcelColumn(cadenceIdx)} (keyword: TURN, ACTION, FIGHT, DUNGEON, …; legacy ATTACK/ABILITY on import).");
                if (cadenceDurationIdx > cadenceIdx)
                    lines.Add("  WARNING: DURATION appears after CADENCE — expected DURATION then CADENCE on canonical layout.");
            }

            var ignored = SpreadsheetActionColumnUsage.GetLabelsIgnoredOnPull(header);
            if (ignored.Count > 0)
            {
                lines.Add("");
                lines.Add($"Ignored on pull ({ignored.Count} labels): {string.Join(", ", ignored.Take(12))}{(ignored.Count > 12 ? ", …" : "")}");
            }

            lines.Add("=== end header audit ===");
            return lines;
        }

        private static string FormatCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "—";
            value = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return value.Length > 26 ? value.Substring(0, 23) + "..." : value;
        }

        /// <summary>0-based index to Excel column letters (A, B, …, Z, AA, …).</summary>
        public static string IndexToExcelColumn(int index)
        {
            if (index < 0) return "?";
            var sb = new StringBuilder();
            int n = index + 1;
            while (n > 0)
            {
                n--;
                sb.Insert(0, (char)('A' + (n % 26)));
                n /= 26;
            }
            return sb.ToString();
        }
    }
}
