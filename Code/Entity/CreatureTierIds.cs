using System;

namespace RPGGame
{
    /// <summary>
    /// Combat-narrative creature voice: native fauna / feral Earth-stock / techno-echo (Genesis-adjacent).
    /// Distinct from substance tags (<c>living</c>/<c>undead</c>/…) and from <see cref="Enemy.IsLiving"/>.
    /// </summary>
    public static class CreatureTierIds
    {
        public const string NativeFauna = "nativeFauna";
        public const string FeralStock = "feralStock";
        public const string TechnoEcho = "technoEcho";

        public static readonly string[] All = { NativeFauna, FeralStock, TechnoEcho };

        /// <summary>Banks that may use <c>{bank}_{tier}</c> with generic fallback.</summary>
        public static readonly string[] NarrativeBanks =
        {
            "firstBlood",
            "criticalHit",
            "criticalMiss",
            "below50Percent",
            "below10Percent",
            "enemyDefeated",
            "enemyTaunt"
        };

        public static bool IsValid(string? raw) => TryCanonicalize(raw, out _);

        public static bool TryCanonicalize(string? raw, out string canonical)
        {
            canonical = "";
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string t = raw.Trim();
            if (t.Equals(NativeFauna, StringComparison.OrdinalIgnoreCase)
                || t.Equals("pure native fauna", StringComparison.OrdinalIgnoreCase))
            {
                canonical = NativeFauna;
                return true;
            }

            if (t.Equals(FeralStock, StringComparison.OrdinalIgnoreCase)
                || t.Equals("feral earth-stock", StringComparison.OrdinalIgnoreCase)
                || t.Equals("feral earth stock", StringComparison.OrdinalIgnoreCase))
            {
                canonical = FeralStock;
                return true;
            }

            if (t.Equals(TechnoEcho, StringComparison.OrdinalIgnoreCase)
                || t.Equals("techno-echo", StringComparison.OrdinalIgnoreCase)
                || t.Equals("genesis-adjacent", StringComparison.OrdinalIgnoreCase)
                || t.Equals("genesis adjacent", StringComparison.OrdinalIgnoreCase))
            {
                canonical = TechnoEcho;
                return true;
            }

            return false;
        }

        public static string SuffixedBank(string baseBank, string canonicalTier) =>
            $"{baseBank}_{canonicalTier}";
    }
}
