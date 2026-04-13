using System;
using RPGGame.UI;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Action Lab: left panel STATS rows use <c>lphover:stat:*</c> hit targets; HERO level line uses <c>lphover:hero:level</c>;
    /// HP bar uses <see cref="HeroHpHoverId"/> (damage/heal clicks, not ±1 stat deltas).
    /// Left-click / right-click deltas are applied by the canvas mouse handler in Action Lab state.
    /// </summary>
    public static class ActionLabLeftPanelStatAdjustment
    {
        /// <summary>Matches left-panel STATS hover ids (e.g. <c>lphover:stat:str</c>).</summary>
        public static string StatHoverPrefix => LeftPanelHoverState.Prefix + "stat:";

        /// <summary>Level line in HERO section (<c>Lvl N Class</c>).</summary>
        public static string HeroLevelHoverId => LeftPanelHoverState.Prefix + "hero:level";

        /// <summary>HP bar + numeric row in HERO section (<c>lphover:hero:hp</c>).</summary>
        public static string HeroHpHoverId => LeftPanelHoverState.Prefix + "hero:hp";

        /// <summary>
        /// Returns true if <paramref name="fullHoverValue"/> is a STATS row we adjust in the lab, and applies the delta.
        /// </summary>
        public static bool TryApply(Character player, string fullHoverValue, int delta)
        {
            if (delta == 0)
                return false;

            if (string.Equals(fullHoverValue, HeroLevelHoverId, System.StringComparison.Ordinal))
            {
                int before = player.Level;
                player.ApplyActionLabLevelDelta(delta);
                ActionInteractionLabSession.Current?.RecordLabPanelLevelDelta(player.Level - before);
                return true;
            }

            if (!fullHoverValue.StartsWith(StatHoverPrefix, System.StringComparison.Ordinal))
                return false;

            string key = fullHoverValue.Substring(StatHoverPrefix.Length);
            switch (key)
            {
                case "str":
                case "damage":
                {
                    int before = player.Stats.Strength;
                    player.Stats.Strength = System.Math.Max(1, player.Stats.Strength + delta);
                    ActionInteractionLabSession.Current?.RecordLabPanelStatDelta("str", player.Stats.Strength - before);
                    return true;
                }
                case "agi":
                case "speed":
                {
                    int before = player.Stats.Agility;
                    player.Stats.Agility = System.Math.Max(1, player.Stats.Agility + delta);
                    ActionInteractionLabSession.Current?.RecordLabPanelStatDelta("agi", player.Stats.Agility - before);
                    return true;
                }
                case "tec":
                {
                    int before = player.Stats.Technique;
                    player.Stats.Technique = System.Math.Max(1, player.Stats.Technique + delta);
                    ActionInteractionLabSession.Current?.RecordLabPanelStatDelta("tec", player.Stats.Technique - before);
                    return true;
                }
                case "int":
                {
                    int before = player.Stats.Intelligence;
                    player.Stats.Intelligence = System.Math.Max(1, player.Stats.Intelligence + delta);
                    ActionInteractionLabSession.Current?.RecordLabPanelStatDelta("int", player.Stats.Intelligence - before);
                    return true;
                }
                case "armor":
                {
                    int before = player.ActionLabArmorBonus;
                    player.ActionLabArmorBonus = System.Math.Max(0, player.ActionLabArmorBonus + delta);
                    ActionInteractionLabSession.Current?.RecordLabPanelStatDelta("armor", player.ActionLabArmorBonus - before);
                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>
        /// Action Lab: left-click on the hero HP bar — lose 5% of effective max HP per click (at least 1 when max &gt; 0), current HP floored at 0.
        /// </summary>
        public static void ApplyHeroHpClickDamagePercent(Character player)
        {
            int max = player.GetEffectiveMaxHealth();
            if (max <= 0)
                return;
            int loss = Math.Max(1, (int)Math.Ceiling(max * 0.05));
            player.CurrentHealth = Math.Max(0, player.CurrentHealth - loss);
        }

        /// <summary>
        /// Action Lab: right-click on the hero HP bar — restore 5 HP without exceeding effective max.
        /// </summary>
        public static void ApplyHeroHpRightClickHeal(Character player, int healAmount = 5)
        {
            if (healAmount <= 0)
                return;
            int max = player.GetEffectiveMaxHealth();
            player.CurrentHealth = Math.Min(max, player.CurrentHealth + healAmount);
        }
    }
}
