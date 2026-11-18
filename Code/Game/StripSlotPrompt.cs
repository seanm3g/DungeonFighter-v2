using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Playtest 2026-11-18: a strip slot can open (level 2) while the hero already owns a spare action (the Wizard's
    /// BOLT, the first skill's CONCENTRATE) and nothing says so. The game only prompts; it never fills the slot itself.
    /// </summary>
    public static class StripSlotPrompt
    {
        /// <summary>Open strip slots (capacity minus actions in the strip), never negative.</summary>
        public static int EmptySlots(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return 0;
            try
            {
                return Math.Max(0, ComboSequenceMaxHelper.GetEffectiveMax(hero) - hero.GetComboActions().Count);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>The first pool action not in the strip while a slot is open; else null.</summary>
        public static string? SpareAction(Character? hero)
        {
            if (EmptySlots(hero) <= 0)
                return null;
            var inStrip = hero!.GetComboActions().Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return hero.GetActionPool()
                .Select(a => a.Name)
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n) && !inStrip.Contains(n));
        }

        /// <summary>True when the Actions tab should carry its marker: a slot is open and an action could fill it.</summary>
        public static bool ShouldPrompt(Character? hero) => SpareAction(hero) != null;

        /// <summary>One line for the hub and the level-up block, or null when there is nothing to fill.</summary>
        public static string? HintLine(Character? hero)
        {
            string? spare = SpareAction(hero);
            if (spare == null)
                return null;
            int open = EmptySlots(hero);
            string slots = open == 1 ? "an empty strip slot" : $"{open} empty strip slots";
            return $"You have {slots}: add {spare.ToUpperInvariant()} on the Actions tab.";
        }
    }
}
