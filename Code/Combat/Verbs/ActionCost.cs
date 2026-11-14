using System;
using System.Globalization;
using RPGGame.Combat.Events;

namespace RPGGame
{
    /// <summary>
    /// Action <c>cost</c> column (Documentation/BUILD_SYSTEM_CONDENSED.md §4: shared field on every action):
    /// <c>HP n</c> pays n% of max HP when the action resolves (hit or miss), never dropping below 1 HP, and raises
    /// ONSELFCOST. Other cost kinds (store, item condition) are reserved.
    /// </summary>
    public static class ActionCost
    {
        public static bool TryParse(string? raw, out string kind, out double amount)
        {
            kind = "";
            amount = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            var parts = raw.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !double.TryParse(parts[1].TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out amount) || amount <= 0)
                return false;
            kind = parts[0].ToUpperInvariant();
            return kind == "HP";
        }

        /// <summary>Pays the action's cost. Returns HP paid (0 when the action has no cost).</summary>
        public static int Pay(Character hero, Action? action)
        {
            if (hero is Enemy || !TryParse(action?.Cost, out string kind, out double pct))
                return 0;
            int hp = (int)Math.Round(hero.GetEffectiveMaxHealth() * pct / 100.0);
            hp = Math.Min(hp, Math.Max(0, hero.CurrentHealth - 1));
            if (hp <= 0)
                return 0;
            hero.TakeDamage(hp);
            HeroEventHub.OnSelfCost(hero, kind);
            return hp;
        }

        public static string? Describe(Action? action) =>
            TryParse(action?.Cost, out _, out double pct)
                ? $"Costs {pct.ToString("0.#", CultureInfo.InvariantCulture)}% max HP"
                : null;
    }
}
