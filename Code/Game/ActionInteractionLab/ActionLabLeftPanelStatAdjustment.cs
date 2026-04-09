using RPGGame.UI;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Action Lab: left panel STATS rows use <c>lphover:stat:*</c> hit targets; HERO level line uses <c>lphover:hero:level</c>.
    /// Left-click / right-click deltas are applied by the canvas mouse handler in Action Lab state.
    /// </summary>
    public static class ActionLabLeftPanelStatAdjustment
    {
        /// <summary>Matches left-panel STATS hover ids (e.g. <c>lphover:stat:str</c>).</summary>
        public static string StatHoverPrefix => LeftPanelHoverState.Prefix + "stat:";

        /// <summary>Level line in HERO section (<c>Lvl N Class</c>).</summary>
        public static string HeroLevelHoverId => LeftPanelHoverState.Prefix + "hero:level";

        private const int LabLevelMin = 1;
        private const int LabLevelMax = 99;

        /// <summary>
        /// Returns true if <paramref name="fullHoverValue"/> is a STATS row we adjust in the lab, and applies the delta.
        /// </summary>
        public static bool TryApply(Character player, string fullHoverValue, int delta)
        {
            if (delta == 0)
                return false;

            if (string.Equals(fullHoverValue, HeroLevelHoverId, System.StringComparison.Ordinal))
            {
                player.Level = System.Math.Clamp(player.Level + delta, LabLevelMin, LabLevelMax);
                return true;
            }

            if (!fullHoverValue.StartsWith(StatHoverPrefix, System.StringComparison.Ordinal))
                return false;

            string key = fullHoverValue.Substring(StatHoverPrefix.Length);
            switch (key)
            {
                case "str":
                case "damage":
                    player.Stats.Strength = System.Math.Max(1, player.Stats.Strength + delta);
                    return true;
                case "agi":
                case "speed":
                    player.Stats.Agility = System.Math.Max(1, player.Stats.Agility + delta);
                    return true;
                case "tec":
                    player.Stats.Technique = System.Math.Max(1, player.Stats.Technique + delta);
                    return true;
                case "int":
                    player.Stats.Intelligence = System.Math.Max(1, player.Stats.Intelligence + delta);
                    return true;
                case "armor":
                    player.ActionLabArmorBonus = System.Math.Max(0, player.ActionLabArmorBonus + delta);
                    return true;
                default:
                    return false;
            }
        }
    }
}
