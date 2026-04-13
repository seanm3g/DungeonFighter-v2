using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// HUD / menu **display** class title from **weapon path class points** only (no STR/AGI/TEC/INT).
    /// Solo / duo **shape** = one or two paths with ≥1 class point, but the shown title stays
    /// <see cref="ClassPresentationConfig.DefaultNoPointsClassName"/> until class points on the **highest** path reach
    /// <see cref="ClassPresentationConfig.TierThresholds"/>[0] (first gate). Prefix tier words then use the same gates vs that path.
    /// Solo and duo titles are **prefix + core** only (no per-path discipline “of the …” suffix). When **three or
    /// four** paths have ≥1 point, the core is the **duo hybrid** for the **two highest** paths by points (ties →
    /// <see cref="ClassPresentationConfig.ClassWeaponOrder"/>), then—if that path’s points are ≥
    /// <see cref="ClassPresentationConfig.TierThresholds"/>[0]—<see cref="ClassPresentationConfig.GetAttributeDisciplineModifier"/>
    /// for the **third-highest** path is appended.
    /// </summary>
    public static class AttributeClassNameComposer
    {
        /// <summary>Builds the shown class string from progression and presentation config.</summary>
        public static string ComposeDisplayClass(
            CharacterProgression? progression,
            ClassPresentationConfig presentation)
        {
            var cfg = presentation.EnsureNormalized();
            if (progression == null)
                return cfg.DefaultNoPointsClassName;

            var ranked = progression.GetClassPathsSortedByPoints();
            var active = ranked.Where(x => x.Points >= 1).ToList();
            if (active.Count == 0)
                return cfg.DefaultNoPointsClassName;

            int highestPts = GetPrimaryPathClassPointsForDisplayTier(progression);
            if (highestPts < cfg.TierThresholds[0])
                return cfg.DefaultNoPointsClassName;

            int band = cfg.GetTierBandIndex(highestPts);

            return active.Count switch
            {
                1 => BuildSoloTitle(active[0].Path, cfg, band),
                2 => BuildDuoTitle(active[0].Path, active[1].Path, cfg, band),
                _ => BuildMultiPathTitle(ranked, cfg, band)
            };
        }

        /// <summary>Class points on the highest path (0 if none); drives tier band vs <see cref="ClassPresentationConfig.TierThresholds"/>.</summary>
        public static int GetPrimaryPathClassPointsForDisplayTier(CharacterProgression? progression)
        {
            if (progression == null) return 0;
            var sorted = progression.GetClassPathsSortedByPoints();
            return sorted[0].Points;
        }

        /// <summary>
        /// Maps <see cref="ClassPresentationConfig.GetTierBandIndex"/> to solo–trio prefix slot 1..4 (same meaning as
        /// Settings “Band 1..4” vs <see cref="ClassPresentationConfig.TierThresholds"/>). Band 0 (below first gate) uses band 1’s word.
        /// </summary>
        private static int SoloTrioPrefixSlot1To4FromBand(int band) =>
            Math.Clamp(band < 1 ? 1 : band, 1, ClassPresentationConfig.TierSlotCount);

        private static string JoinTitle(string prefix, string core, string? modifierSuffix)
        {
            string m = string.IsNullOrWhiteSpace(modifierSuffix) ? "" : " " + modifierSuffix.Trim();
            return $"{prefix.Trim()} {core.Trim()}{m}".Trim();
        }

        private static string BuildSoloTitle(
            WeaponType solePath,
            ClassPresentationConfig cfg,
            int band)
        {
            int slot = SoloTrioPrefixSlot1To4FromBand(band);
            string prefix = cfg.GetAttributeSoloTrioTierPrefix(slot);
            string core = cfg.GetDisplayName(solePath);
            return JoinTitle(prefix, core, null);
        }

        private static string BuildDuoTitle(
            WeaponType a,
            WeaponType b,
            ClassPresentationConfig cfg,
            int band)
        {
            int slot = SoloTrioPrefixSlot1To4FromBand(band);
            string prefix = cfg.GetAttributeSoloTrioTierPrefix(slot);
            string core = cfg.GetAttributeDuoCoreName(a, b);
            return JoinTitle(prefix, core, null);
        }

        /// <summary>Three or four paths: top-two duo core + optional third-path discipline suffix.</summary>
        private static string BuildMultiPathTitle(
            IReadOnlyList<(WeaponType Path, int Points)> ranked,
            ClassPresentationConfig cfg,
            int band) =>
            BuildMultiPathTopTwoTitle(ranked, cfg, band);

        /// <summary>Three or four paths with ≥1 point: duo core from the two highest point totals; third-highest path’s suffix only if its points ≥ first tier gate.</summary>
        private static string BuildMultiPathTopTwoTitle(
            IReadOnlyList<(WeaponType Path, int Points)> ranked,
            ClassPresentationConfig cfg,
            int band)
        {
            int slot = SoloTrioPrefixSlot1To4FromBand(band);
            string prefix = cfg.GetAttributeSoloTrioTierPrefix(slot);
            var a = ranked[0].Path;
            var b = ranked[1].Path;
            string core = cfg.GetAttributeDuoCoreName(a, b);
            int firstGate = cfg.TierThresholds[0];
            string? thirdPathSuffix = ranked[2].Points >= firstGate
                ? cfg.GetAttributeDisciplineModifier(ranked[2].Path)
                : null;
            return JoinTitle(prefix, core, thirdPathSuffix);
        }
    }
}
