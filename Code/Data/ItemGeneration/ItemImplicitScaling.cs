using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace RPGGame.Data.ItemGeneration
{
    /// <summary>
    /// Scales a base item's implicit trigger with item level. Only continuous magnitudes scale: percent mechanics
    /// (next-action / this-swing damage, amp, speed) by <see cref="ItemGenerationConfig.ImplicitPercentPerItemLevel"/>,
    /// flat ones (heal, armor, weapon damage) by <see cref="ItemGenerationConfig.ImplicitFlatPerItemLevel"/>.
    /// Discrete values (thresholds, faces, multihit, +1 attribute, statuses, strip moves) and bundles that already
    /// scale with the hero (<see cref="ActionTriggerBundle.ScaleFrom"/>) stay as cataloged. The scaled value is
    /// written to the bundle (saved with the item) and into its description.
    /// </summary>
    public static class ItemImplicitScaling
    {
        private static readonly string[] PercentMechanics =
        {
            "hero_next_action_damage", "hero_next_action_amp", "hero_next_action_speed",
            "hero_action_damage", "hero_action_amp", "hero_action_speed"
        };

        private static readonly string[] FlatMechanics = { "heal", "armor", "hero_armor", "hero_weapon_damage" };

        /// <summary>The multiplier for this bundle at <paramref name="itemLevel"/>; 1 when it does not scale.</summary>
        public static double Multiplier(ActionTriggerBundle bundle, int itemLevel, ItemGenerationConfig cfg)
        {
            if (bundle.Value is not { } v || v <= 0 || !string.IsNullOrWhiteSpace(bundle.ScaleFrom) || itemLevel <= 1)
                return 1.0;
            var ids = bundle.ParseMechanicIds()
                .Select(id => ActionMechanicsRegistry.NormalizeMechanicId(id.Split(':')[0].Trim()))
                .ToList();
            if (ids.Count != 1)
                return 1.0;
            double perLevel = PercentMechanics.Contains(ids[0], StringComparer.OrdinalIgnoreCase) ? cfg.ImplicitPercentPerItemLevel
                : FlatMechanics.Contains(ids[0], StringComparer.OrdinalIgnoreCase) ? cfg.ImplicitFlatPerItemLevel
                : 0;
            return 1.0 + Math.Max(0, perLevel) * (itemLevel - 1);
        }

        /// <summary>Scales <paramref name="bundle"/> in place; returns true when its value changed.</summary>
        public static bool Apply(ActionTriggerBundle bundle, int itemLevel, ItemGenerationConfig cfg)
        {
            double mult = Multiplier(bundle, itemLevel, cfg);
            if (mult <= 1.0 || bundle.Value is not { } old)
                return false;
            double scaled = Math.Max(old, Math.Round(old * mult));
            if (scaled == old)
                return false;
            bundle.Value = scaled;
            bundle.Description = RewriteNumber(bundle.Description, old, scaled);
            return true;
        }

        /// <summary>Replaces the first standalone <paramref name="old"/> in <paramref name="text"/> with <paramref name="now"/>.</summary>
        internal static string? RewriteNumber(string? text, double old, double now)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            string from = old.ToString("0.##", CultureInfo.InvariantCulture);
            string to = now.ToString("0.##", CultureInfo.InvariantCulture);
            var re = new Regex(@"(?<![\d.])" + Regex.Escape(from) + @"(?![\d.])");
            return re.Replace(text, to, 1);
        }
    }
}
