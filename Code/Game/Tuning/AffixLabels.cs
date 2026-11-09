using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Stable labels for an item's affix lines, used by the sim tools to track affix offers, wear and value:
    /// <c>mod:Heavy</c> (adjective prefix), <c>qual:Fine</c> (quality prefix), <c>stat:CRIT</c> (stat suffix by stat type),
    /// <c>act:Name</c> (action bonus), <c>arm:Name</c> (armor status). Material is tracked separately (Item.Material).
    /// </summary>
    public static class AffixLabels
    {
        public static List<string> Of(Item? item)
        {
            var labels = new List<string>();
            if (item == null)
                return labels;
            foreach (var m in item.Modifications ?? new List<Modification>())
            {
                if (m == null || string.IsNullOrWhiteSpace(m.Name))
                    continue;
                var cat = m.GetPrefixCategory();
                if (cat == ModificationPrefixCategory.Material)
                    continue;
                labels.Add((cat == ModificationPrefixCategory.Quality ? "qual:" : "mod:") + m.Name.Trim());
            }
            foreach (var s in item.StatBonuses ?? new List<StatBonus>())
            {
                if (s == null)
                    continue;
                string key = string.IsNullOrWhiteSpace(s.StatType) ? s.Name : s.StatType;
                if (!string.IsNullOrWhiteSpace(key))
                    labels.Add("stat:" + key.Trim().ToUpperInvariant());
            }
            foreach (var a in item.ActionBonuses ?? new List<ActionBonus>())
                if (a != null && !string.IsNullOrWhiteSpace(a.Name))
                    labels.Add("act:" + a.Name.Trim());
            foreach (var a in item.ArmorStatuses ?? new List<ArmorStatus>())
                if (a != null && !string.IsNullOrWhiteSpace(a.Name))
                    labels.Add("arm:" + a.Name.Trim());
            return labels;
        }
    }
}
