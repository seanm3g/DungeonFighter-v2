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
                int before = player.Level;
                player.Level = System.Math.Clamp(player.Level + delta, LabLevelMin, LabLevelMax);
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
    }
}
