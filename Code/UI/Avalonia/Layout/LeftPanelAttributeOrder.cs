using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// STR/AGI/TECH/INT row order for the left STATS panel. Default is the fixed order; clicking an attribute row
    /// toggles highest → lowest (ties keep the default order). Once a hybrid attribute node is learned (e.g. Will),
    /// its two parent stats collapse into one hybrid row (WILL = STR + INT) where the first parent sat.
    /// </summary>
    public static class LeftPanelAttributeOrder
    {
        public readonly record struct Row(string Label, string HoverId, string PrimaryName, int Value, bool IsHybrid = false);

        private static readonly string[] Codes = { "STR", "AGI", "TEC", "INT" };
        private static readonly string[] HoverIds = { "stat:str", "stat:agi", "stat:tec", "stat:int" };

        public static List<Row> Build(Character character, bool sortByValue)
        {
            var rows = new List<Row>
            {
                new Row("STR", HoverIds[0], "Strength", character.GetEffectiveStrength()),
                new Row("AGI", HoverIds[1], "Agility", character.GetEffectiveAgility()),
                new Row("TECH", HoverIds[2], "Technique", character.GetEffectiveTechnique()),
                new Row("INT", HoverIds[3], "Intelligence", character.GetEffectiveIntelligence()),
            };

            if (HybridTrees.PrimaryAttributeHybrid(character) is HybridInfo h)
            {
                int a = Array.IndexOf(Codes, h.StatA);
                int b = Array.IndexOf(Codes, h.StatB);
                if (a >= 0 && b >= 0)
                {
                    var hybridRow = new Row(h.Attribute.ToUpperInvariant(), HybridHoverId(h), h.Attribute,
                        HybridTrees.SecondaryAttribute(character, h), IsHybrid: true);
                    rows.RemoveAt(Math.Max(a, b));
                    rows[Math.Min(a, b)] = hybridRow;
                }
            }

            return sortByValue ? rows.OrderByDescending(r => r.Value).ToList() : rows;
        }

        /// <summary>Hover id of a hybrid attribute row, e.g. <c>stat:will</c>.</summary>
        public static string HybridHoverId(HybridInfo h) => "stat:" + h.Attribute.ToLowerInvariant();

        /// <summary>Hybrid attribute named by a <c>stat:will</c>-style hover key, if any.</summary>
        public static HybridInfo? HybridForHoverKey(string? key) =>
            key != null && key.StartsWith("stat:", StringComparison.Ordinal)
                ? HybridTrees.ByAttributeName(key.Substring("stat:".Length))
                : null;

        /// <summary>True for the clickable <c>lphover:stat:str|agi|tec|int</c> rows (and hybrid rows like <c>stat:will</c>).</summary>
        public static bool IsAttributeHoverId(string? value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith(LeftPanelHoverState.Prefix, StringComparison.Ordinal))
                return false;
            string id = value.Substring(LeftPanelHoverState.Prefix.Length);
            return HoverIds.Contains(id, StringComparer.Ordinal) || HybridForHoverKey(id) != null;
        }
    }
}
