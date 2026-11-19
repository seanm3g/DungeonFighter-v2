using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Class-tree text helpers. The class-tree effects themselves are rules on the nodes (Synthesis = MINTBONUS,
    /// Markets = DROP, Cadence = COMBO PER TAGS, Dagger Mastery = APPLY bleed PER EXTRAHITS, …).
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        /// <summary>
        /// Player lines for learned rule nodes that ride this convert's store (ONCASH:&lt;its keyword&gt; or a rule naming the
        /// action). Empty when none are learned.
        /// </summary>
        public static string DescribeEquippedConvertMastery(Character? hero, string? actionName)
        {
            if (hero == null || hero is Enemy || string.IsNullOrWhiteSpace(actionName))
                return "";
            string? keyword = MaterialBuildsLoader.FindByConvertAction(actionName)?.Keyword;
            var lines = new List<string>();
            foreach (var (node, rule) in CrossWires.LearnedNodeRules(hero))
            {
                bool rides = (keyword != null && rule.When.Equals("ONCASH:" + keyword, StringComparison.OrdinalIgnoreCase))
                    || rule.Conditions.Any(c => c.Subject == "action" && string.Equals(c.Key, actionName, StringComparison.OrdinalIgnoreCase));
                if (rides)
                    lines.Add($"{node.Name}: {RPGGame.UI.RuleText.Line(rule)}");
            }
            return string.Join(" ", lines);
        }
    }
}
