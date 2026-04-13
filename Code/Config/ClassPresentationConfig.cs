using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public class ClassPresentationConfig
    {
        public const int TierSlotCount = 4;

        public static readonly WeaponType[] ClassWeaponOrder =
        {
            WeaponType.Mace,
            WeaponType.Sword,
            WeaponType.Dagger,
            WeaponType.Wand
        };

        public string DefaultNoPointsClassName { get; set; } = "Fighter";

        /// <summary>Shown for weapon-path ranked titles from 1 point up to (but not including) <see cref="TierThresholds"/>[0]; fixed in code.</summary>
        public static readonly string DefaultPreTierLabel = "Novice";

        /// <summary>Between primary and secondary ranked fragments when tier bands differ; fixed in code.</summary>
        public static readonly string DefaultHybridJoiner = "-";

        public string MaceClassDisplayName { get; set; } = "Barbarian";
        public string SwordClassDisplayName { get; set; } = "Warrior";
        public string DaggerClassDisplayName { get; set; } = "Rogue";
        public string WandClassDisplayName { get; set; } = "Wizard";

        /// <summary>Weapon-path ranked title tier words for <see cref="FormatRankedTitle"/> (fixed; not in TuningConfig or Sheets).</summary>
        public static readonly string[] DefaultWeaponPathRankTierWords = { "Adept", "Expert", "Master", "Paragon" };

        /// <summary>Four ascending point gates: band 1 starts at threshold[0], …, band 4 at threshold[3].</summary>
        public int[] TierThresholds { get; set; } = { 2, 20, 60, 120 };

        /// <summary>Four tier words for solo/duo/trio (class point tier slot vs <see cref="TierThresholds"/>).</summary>
        public string[] AttributeSoloTrioTierPrefixes { get; set; } = { "Scarred", "Blooded", "Dread", "Abyssal" };

        /// <summary>Four tier words when all four stats are meaningful (quad attribute display).</summary>
        public string[] AttributeQuadTierNames { get; set; } = { "Prophet", "Godkiller", "Worldbreaker", "Eternal" };

        /// <summary>Suffix phrase for this path when it is the **third-highest** by class points (HUD shown title, 3+ active paths). Unused for solo/duo.</summary>
        public string AttributeModifierMace { get; set; } = "of Fury";
        public string AttributeModifierSword { get; set; } = "of the Vanguard";
        public string AttributeModifierDagger { get; set; } = "of the Veil";
        public string AttributeModifierWand { get; set; } = "of the Void";

        /// <summary>Step 1 duo cores (unordered path pair → title). Paths are fixed Mace/Sword/Dagger/Wand.</summary>
        public string AttributeDuoMaceSword { get; set; } = "Warbrute";
        public string AttributeDuoMaceDagger { get; set; } = "Ravager";
        public string AttributeDuoMaceWand { get; set; } = "Ragecaller";
        public string AttributeDuoSwordDagger { get; set; } = "Duelist";
        public string AttributeDuoSwordWand { get; set; } = "Spellblade";
        public string AttributeDuoDaggerWand { get; set; } = "Hexblade";

        public string GetDisplayName(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => string.IsNullOrWhiteSpace(MaceClassDisplayName) ? "Barbarian" : MaceClassDisplayName.Trim(),
            WeaponType.Sword => string.IsNullOrWhiteSpace(SwordClassDisplayName) ? "Warrior" : SwordClassDisplayName.Trim(),
            WeaponType.Dagger => string.IsNullOrWhiteSpace(DaggerClassDisplayName) ? "Rogue" : DaggerClassDisplayName.Trim(),
            WeaponType.Wand => string.IsNullOrWhiteSpace(WandClassDisplayName) ? "Wizard" : WandClassDisplayName.Trim(),
            _ => "Unknown"
        };

        public int GetClassPoints(WeaponType weaponType, int barbarianPoints, int warriorPoints, int roguePoints, int wizardPoints) =>
            weaponType switch
            {
                WeaponType.Mace => barbarianPoints,
                WeaponType.Sword => warriorPoints,
                WeaponType.Dagger => roguePoints,
                WeaponType.Wand => wizardPoints,
                _ => 0
            };

        /// <summary>
        /// -1 = no points; 0 = pre–tier-1 (1..threshold[0]−1); 1..4 = named tier bands (slot index = band−1).
        /// </summary>
        public int GetTierBandIndex(int points)
        {
            var t = EnsureNormalized().TierThresholds;
            if (points <= 0) return -1;
            if (points < t[0]) return 0;
            if (points < t[1]) return 1;
            if (points < t[2]) return 2;
            if (points < t[3]) return 3;
            return 4;
        }

        public string FormatRankedTitle(WeaponType path, int points)
        {
            var c = EnsureNormalized();
            if (points <= 0) return c.DefaultNoPointsClassName;
            var t = c.TierThresholds;
            var display = GetDisplayName(path);
            if (points < t[0])
                return DefaultPreTierLabel;

            int band = GetTierBandIndex(points);
            if (band < 1 || band > TierSlotCount)
                return DefaultPreTierLabel;

            int slot = band - 1;
            return $"{DefaultWeaponPathRankTierWords[slot]} {display}";
        }

        /// <summary>Tier slot 1..4 from class points vs <see cref="TierThresholds"/> (same gates as weapon path tiers).</summary>
        public string GetAttributeSoloTrioTierPrefix(int slot1To4)
        {
            var c = EnsureNormalized();
            int i = Math.Clamp(slot1To4, 1, TierSlotCount) - 1;
            string s = c.AttributeSoloTrioTierPrefixes[i]?.Trim() ?? "";
            return s.Length > 0 ? s : DefaultAttributeSoloTrioTierPrefixes[i];
        }

        public string GetAttributeQuadTierName(int slot1To4)
        {
            var c = EnsureNormalized();
            int i = Math.Clamp(slot1To4, 1, TierSlotCount) - 1;
            string s = c.AttributeQuadTierNames[i]?.Trim() ?? "";
            return s.Length > 0 ? s : DefaultAttributeQuadTierNames[i];
        }

        /// <summary>Suffix phrase for <paramref name="path"/>; for the HUD shown title with 3+ active paths, used for the third-highest path by class points.</summary>
        public string GetAttributeDisciplineModifier(WeaponType path) =>
            path switch
            {
                WeaponType.Mace => NonEmptyTrimOr(EnsureNormalized().AttributeModifierMace, "of Fury"),
                WeaponType.Sword => NonEmptyTrimOr(EnsureNormalized().AttributeModifierSword, "of the Vanguard"),
                WeaponType.Dagger => NonEmptyTrimOr(EnsureNormalized().AttributeModifierDagger, "of the Veil"),
                WeaponType.Wand => NonEmptyTrimOr(EnsureNormalized().AttributeModifierWand, "of the Void"),
                _ => ""
            };

        /// <summary>Unordered weapon pair → attribute duo display core (Step 1).</summary>
        public string GetAttributeDuoCoreName(WeaponType a, WeaponType b)
        {
            int ia = Array.IndexOf(ClassWeaponOrder, a);
            int ib = Array.IndexOf(ClassWeaponOrder, b);
            var low = ia < ib ? a : b;
            var high = ia < ib ? b : a;
            var c = EnsureNormalized();
            string raw = (low, high) switch
            {
                (WeaponType.Mace, WeaponType.Sword) => c.AttributeDuoMaceSword,
                (WeaponType.Mace, WeaponType.Dagger) => c.AttributeDuoMaceDagger,
                (WeaponType.Mace, WeaponType.Wand) => c.AttributeDuoMaceWand,
                (WeaponType.Sword, WeaponType.Dagger) => c.AttributeDuoSwordDagger,
                (WeaponType.Sword, WeaponType.Wand) => c.AttributeDuoSwordWand,
                (WeaponType.Dagger, WeaponType.Wand) => c.AttributeDuoDaggerWand,
                _ => ""
            };
            string s = raw?.Trim() ?? "";
            if (s.Length > 0) return s;
            return (low, high) switch
            {
                (WeaponType.Mace, WeaponType.Sword) => "Warbrute",
                (WeaponType.Mace, WeaponType.Dagger) => "Ravager",
                (WeaponType.Mace, WeaponType.Wand) => "Ragecaller",
                (WeaponType.Sword, WeaponType.Dagger) => "Duelist",
                (WeaponType.Sword, WeaponType.Wand) => "Spellblade",
                (WeaponType.Dagger, WeaponType.Wand) => "Hexblade",
                _ => "Hybrid"
            };
        }

        private static string NonEmptyTrimOr(string? value, string fallback)
        {
            string s = value?.Trim() ?? "";
            return s.Length > 0 ? s : fallback;
        }

        private static readonly string[] DefaultAttributeSoloTrioTierPrefixes = { "Scarred", "Blooded", "Dread", "Abyssal" };
        private static readonly string[] DefaultAttributeQuadTierNames = { "Prophet", "Godkiller", "Worldbreaker", "Eternal" };

        public ClassPresentationConfig EnsureNormalized()
        {
            TierThresholds = NormalizeTierThresholds(TierThresholds);

            if (string.IsNullOrWhiteSpace(DefaultNoPointsClassName))
                DefaultNoPointsClassName = "Fighter";
            else
                DefaultNoPointsClassName = DefaultNoPointsClassName.Trim();

            MaceClassDisplayName = string.IsNullOrWhiteSpace(MaceClassDisplayName) ? "Barbarian" : MaceClassDisplayName.Trim();
            SwordClassDisplayName = string.IsNullOrWhiteSpace(SwordClassDisplayName) ? "Warrior" : SwordClassDisplayName.Trim();
            DaggerClassDisplayName = string.IsNullOrWhiteSpace(DaggerClassDisplayName) ? "Rogue" : DaggerClassDisplayName.Trim();
            WandClassDisplayName = string.IsNullOrWhiteSpace(WandClassDisplayName) ? "Wizard" : WandClassDisplayName.Trim();

            AttributeSoloTrioTierPrefixes = NormalizeAttributeStringBand(AttributeSoloTrioTierPrefixes, DefaultAttributeSoloTrioTierPrefixes);
            AttributeQuadTierNames = NormalizeAttributeStringBand(AttributeQuadTierNames, DefaultAttributeQuadTierNames);

            AttributeModifierMace = NonEmptyTrimOr(AttributeModifierMace, "of Fury");
            AttributeModifierSword = NonEmptyTrimOr(AttributeModifierSword, "of the Vanguard");
            AttributeModifierDagger = NonEmptyTrimOr(AttributeModifierDagger, "of the Veil");
            AttributeModifierWand = NonEmptyTrimOr(AttributeModifierWand, "of the Void");

            AttributeDuoMaceSword = NonEmptyTrimOr(AttributeDuoMaceSword, "Warbrute");
            AttributeDuoMaceDagger = NonEmptyTrimOr(AttributeDuoMaceDagger, "Ravager");
            AttributeDuoMaceWand = NonEmptyTrimOr(AttributeDuoMaceWand, "Ragecaller");
            AttributeDuoSwordDagger = NonEmptyTrimOr(AttributeDuoSwordDagger, "Duelist");
            AttributeDuoSwordWand = NonEmptyTrimOr(AttributeDuoSwordWand, "Spellblade");
            AttributeDuoDaggerWand = NonEmptyTrimOr(AttributeDuoDaggerWand, "Hexblade");

            return this;
        }

        private static string[] NormalizeAttributeStringBand(string[]? raw, string[] defaults)
        {
            if (raw == null || raw.Length == 0)
                return (string[])defaults.Clone();
            if (raw.Length < TierSlotCount)
            {
                var list = raw.Select(x => x?.Trim() ?? "").ToList();
                while (list.Count < TierSlotCount)
                    list.Add(defaults[list.Count]);
                raw = list.Take(TierSlotCount).ToArray();
            }
            else if (raw.Length > TierSlotCount)
                raw = raw.Take(TierSlotCount).ToArray();

            for (int i = 0; i < TierSlotCount; i++)
            {
                if (string.IsNullOrWhiteSpace(raw[i]))
                    raw[i] = defaults[i];
                else
                    raw[i] = raw[i].Trim();
            }
            return raw;
        }

        private static int[] NormalizeTierThresholds(int[]? raw)
        {
            if (raw == null || raw.Length == 0)
                raw = new[] { 2, 20, 60, 120 };
            else if (raw.Length < TierSlotCount)
            {
                var list = raw.Select(x => Math.Max(1, x)).OrderBy(x => x).ToList();
                while (list.Count < TierSlotCount)
                {
                    int last = list[^1];
                    list.Add(Math.Max(last + 1, last * 2));
                }
                raw = list.Take(TierSlotCount).OrderBy(x => x).ToArray();
            }
            else if (raw.Length > TierSlotCount)
                raw = raw.Select(x => Math.Max(1, x)).OrderBy(x => x).Take(TierSlotCount).ToArray();

            raw = raw.Select(x => Math.Max(1, x)).OrderBy(x => x).ToArray();
            for (int i = 1; i < raw.Length; i++)
            {
                if (raw[i] <= raw[i - 1])
                    raw[i] = raw[i - 1] + 1;
            }
            return raw;
        }

        public int GetNextThreshold(int currentPoints)
        {
            var t = EnsureNormalized().TierThresholds;
            foreach (int threshold in t)
            {
                if (currentPoints < threshold)
                    return threshold;
            }
            return -1;
        }
    }
}
