using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// STR/AGI/TECH/INT row order for the left STATS panel. Default is the fixed order; clicking an attribute row
    /// toggles highest → lowest (ties keep the default order).
    /// </summary>
    public static class LeftPanelAttributeOrder
    {
        public readonly record struct Row(string Label, string HoverId, string PrimaryName, int Value);

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
            return sortByValue ? rows.OrderByDescending(r => r.Value).ToList() : rows;
        }

        /// <summary>True for the clickable <c>lphover:stat:str|agi|tec|int</c> rows.</summary>
        public static bool IsAttributeHoverId(string? value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith(LeftPanelHoverState.Prefix, StringComparison.Ordinal))
                return false;
            string id = value.Substring(LeftPanelHoverState.Prefix.Length);
            return HoverIds.Contains(id, StringComparer.Ordinal);
        }
    }
}
