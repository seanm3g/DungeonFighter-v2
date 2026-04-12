using System;

using System.Collections.Generic;

using System.Linq;



namespace RPGGame

{

    /// <summary>

    /// Attribute-driven **display** class title (Steps 1–7). Does not affect XP, level, or class points;

    /// class points are awarded on level-up from the equipped weapon (see <see cref="LevelUpManager"/>).

    /// Uses base stats + temporary bonuses, no equipment (stable build identity) for **which** solo/duo/trio/quad

    /// shape applies; **Scarred / Blooded / … / quad tier words** use the same <see cref="ClassPresentationConfig.TierThresholds"/>

    /// gates as weapon paths, compared to **primary weapon path class points** (highest path by points).

    /// </summary>

    public static class AttributeClassNameComposer

    {

        public static string ComposeDisplayClass(

            CharacterStats? stats,

            CharacterProgression? progression,

            ClassPresentationConfig presentation)

        {

            var cfg = presentation.EnsureNormalized();

            if (stats == null)

                return cfg.DefaultNoPointsClassName;



            int str = stats.GetEffectiveStrength(0, 0);

            int agi = stats.GetEffectiveAgility(0);

            int tec = stats.GetEffectiveTechnique(0);

            int intel = stats.GetEffectiveIntelligence(0);



            int minMeaningful = Math.Max(1, cfg.MeaningfulAttributeMinimum);

            var allRanked = BuildRankedStats(str, agi, tec, intel);

            var meaningful = allRanked.Where(x => x.Value >= minMeaningful).ToList();



            if (meaningful.Count == 0)

                return cfg.DefaultNoPointsClassName;



            int tierScore = GetPrimaryPathClassPointsForDisplayTier(progression);

            int band = cfg.GetTierBandIndex(tierScore);



            return meaningful.Count switch

            {

                1 => BuildSoloTitle(meaningful[0], allRanked, cfg, band),

                2 => BuildDuoTitle(meaningful, allRanked, cfg, band),

                3 => BuildTrioTitle(meaningful, cfg, band),

                _ => BuildQuadTitle(meaningful, cfg, band)

            };

        }



        /// <summary>Class points on the highest path (0 if none); drives tier band vs <see cref="ClassPresentationConfig.TierThresholds"/>.</summary>

        public static int GetPrimaryPathClassPointsForDisplayTier(CharacterProgression? progression)

        {

            if (progression == null) return 0;

            var sorted = progression.GetClassPathsSortedByPoints();

            return sorted[0].Points;

        }



        private static List<(WeaponType Path, int Value)> BuildRankedStats(int str, int agi, int tec, int intel)

        {

            var list = new List<(WeaponType Path, int Value)>

            {

                (WeaponType.Mace, str),

                (WeaponType.Sword, agi),

                (WeaponType.Dagger, tec),

                (WeaponType.Wand, intel)

            };

            return list

                .OrderByDescending(x => x.Value)

                .ThenBy(x => Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, x.Path))

                .ToList();

        }



        /// <summary>Solo/duo/trio prefix slot 1..4 from <see cref="ClassPresentationConfig.GetTierBandIndex"/> (shifted vs quad names).</summary>

        private static int SoloTrioPrefixSlot1To4FromBand(int band)

        {

            if (band < 0) return 1;

            if (band == 0) return 1;

            return Math.Min(band + 1, ClassPresentationConfig.TierSlotCount);

        }



        /// <summary>Quad tier slot 1..4: same 0-based band slots as weapon path evolved tiers (band 1..4 → slots 1..4).</summary>

        private static int QuadTierSlot1To4FromBand(int band)

        {

            if (band < 1) return 1;

            return Math.Min(band, ClassPresentationConfig.TierSlotCount);

        }



        private static string JoinTitle(string prefix, string core, string? modifierSuffix)

        {

            string m = string.IsNullOrWhiteSpace(modifierSuffix) ? "" : " " + modifierSuffix.Trim();

            return $"{prefix.Trim()} {core.Trim()}{m}".Trim();

        }



        private static string BuildSoloTitle(

            (WeaponType Path, int Value) sole,

            List<(WeaponType Path, int Value)> allRanked,

            ClassPresentationConfig cfg,

            int band)

        {

            int slot = SoloTrioPrefixSlot1To4FromBand(band);

            string prefix = cfg.GetAttributeSoloTrioTierPrefix(slot);

            string core = cfg.GetDisplayName(sole.Path);

            var second = allRanked.First(x => x.Path != sole.Path);

            string mod = cfg.GetAttributeDisciplineModifier(second.Path);

            return JoinTitle(prefix, core, mod);

        }



        private static string BuildDuoTitle(

            List<(WeaponType Path, int Value)> meaningfulCore,

            List<(WeaponType Path, int Value)> allRanked,

            ClassPresentationConfig cfg,

            int band)

        {

            var a = meaningfulCore[0].Path;

            var b = meaningfulCore[1].Path;

            int slot = SoloTrioPrefixSlot1To4FromBand(band);

            string prefix = cfg.GetAttributeSoloTrioTierPrefix(slot);

            string core = cfg.GetAttributeDuoCoreName(a, b);

            var coreSet = new HashSet<WeaponType> { a, b };

            var modPath = allRanked.Where(x => !coreSet.Contains(x.Path))

                .OrderByDescending(x => x.Value)

                .ThenBy(x => Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, x.Path))

                .First().Path;

            string mod = cfg.GetAttributeDisciplineModifier(modPath);

            return JoinTitle(prefix, core, mod);

        }



        private static string BuildTrioTitle(

            List<(WeaponType Path, int Value)> meaningfulThree,

            ClassPresentationConfig cfg,

            int band)

        {

            int slot = SoloTrioPrefixSlot1To4FromBand(band);

            string prefix = cfg.GetAttributeSoloTrioTierPrefix(slot);

            var set = meaningfulThree.Select(x => x.Path).ToHashSet();

            string? core = cfg.TryGetAttributeTrioCoreName(set);

            if (string.IsNullOrEmpty(core))

                core = string.Join(" / ", meaningfulThree.Select(x => cfg.GetDisplayName(x.Path)));

            var lowest = meaningfulThree

                .OrderBy(x => x.Value)

                .ThenBy(x => Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, x.Path))

                .First();

            string mod = cfg.GetAttributeDisciplineModifier(lowest.Path);

            return JoinTitle(prefix, core!, mod);

        }



        private static string BuildQuadTitle(

            List<(WeaponType Path, int Value)> meaningfulFour,

            ClassPresentationConfig cfg,

            int band)

        {

            int slot = QuadTierSlot1To4FromBand(band);

            return cfg.GetAttributeQuadTierName(slot);

        }

    }

}


