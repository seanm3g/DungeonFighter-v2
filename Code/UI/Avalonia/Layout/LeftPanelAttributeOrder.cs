using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// STR/AGI/TECH/INT row order for the left STATS panel. Default is the fixed order; clicking an attribute row
    /// toggles highest → lowest (ties keep the default order). Once a hybrid attribute node is learned (e.g. Will),
    /// the four base stats stay as they are and the hybrid row (WILL = STR + INT) is appended after them (never sorted in).
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

            if (sortByValue)
                rows = rows.OrderByDescending(r => r.Value).ToList();

            if (HybridTrees.PrimaryAttributeHybrid(character) is HybridInfo h
                && Array.IndexOf(Codes, h.StatA) >= 0 && Array.IndexOf(Codes, h.StatB) >= 0)
            {
                rows.Add(new Row(h.Attribute.ToUpperInvariant(), HybridHoverId(h), h.Attribute,
                    HybridTrees.SecondaryAttribute(character, h), IsHybrid: true));
            }

            return rows;
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
