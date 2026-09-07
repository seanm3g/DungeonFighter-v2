using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Inherent animal tag ladder (no sheet): 2+ animal general, 2+ taxon mechanic, 3+ specific heighten / action unlock.
    /// Charm slot is excluded from counts (gear slots only).
    /// </summary>
    public static class AnimalSetController
    {
        public const int GeneralThreshold = 2;
        public const int TaxonThreshold = 2;
        public const int SpecificThreshold = 3;
        public const int BaseGeneralSpeedPct = 3;
        public const int BaseBirdSpeedPct = 8;
        public const int BaseMythicAmpPct = 5;
        public const double SpecificHeightenFactor = 1.5;

        public static IEnumerable<Item?> EnumerateGear(Character? hero)
        {
            if (hero?.Equipment == null)
                yield break;
            yield return hero.Equipment.Head;
            yield return hero.Equipment.Body;
            yield return hero.Equipment.Legs;
            yield return hero.Equipment.Feet;
            yield return hero.Equipment.Weapon;
        }

        public static int CountTag(Character? hero, string? tag)
        {
            if (hero == null || string.IsNullOrWhiteSpace(tag))
                return 0;
            int n = 0;
            foreach (var item in EnumerateGear(hero))
            {
                if (item != null && GameDataTagHelper.HasTag(item.Tags, tag))
                    n++;
            }
            return n;
        }

        public static int CountAnimals(Character? hero) => CountTag(hero, AnimalTagHelper.AnimalTag);

        public static int CountSpecific(Character? hero, string? specific)
        {
            if (hero == null || string.IsNullOrWhiteSpace(specific))
                return 0;
            int n = 0;
            foreach (var item in EnumerateGear(hero))
            {
                if (item?.StatBonuses == null)
                    continue;
                foreach (var bonus in item.StatBonuses)
                {
                    if (!AnimalTagHelper.IsAnimalSuffix(bonus))
                        continue;
                    if (string.Equals(AnimalTagHelper.NormalizeSpecificAnimalTag(bonus.Name), specific,
                            StringComparison.OrdinalIgnoreCase))
                        n++;
                }
            }
            return n;
        }

        /// <summary>Standing SPEED_MOD % from the general animal ladder (charm-amplified).</summary>
        public static int GetGeneralSpeedPct(Character? hero)
        {
            if (CountAnimals(hero) < GeneralThreshold)
                return 0;
            double mult = CharmBonusController.GetAnimalLadderMultiplier(hero);
            return Math.Max(0, (int)Math.Round(BaseGeneralSpeedPct * mult));
        }

        public static string? GetDominantTaxon(Character? hero)
        {
            string? best = null;
            int bestCount = 0;
            foreach (var taxon in AnimalTagHelper.TaxonTags)
            {
                int n = CountTag(hero, taxon);
                if (n > bestCount)
                {
                    bestCount = n;
                    best = taxon;
                }
            }
            return bestCount >= TaxonThreshold ? best : null;
        }

        public static bool HasTaxonSet(Character? hero, string taxon) =>
            CountTag(hero, taxon) >= TaxonThreshold;

        public static string? GetDominantSpecific(Character? hero)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in EnumerateGear(hero))
            {
                if (item?.StatBonuses == null)
                    continue;
                foreach (var bonus in item.StatBonuses)
                {
                    if (!AnimalTagHelper.IsAnimalSuffix(bonus))
                        continue;
                    string specific = AnimalTagHelper.NormalizeSpecificAnimalTag(bonus.Name);
                    if (specific.Length == 0)
                        continue;
                    counts.TryGetValue(specific, out int c);
                    counts[specific] = c + 1;
                }
            }

            string? best = null;
            int bestCount = 0;
            foreach (var kv in counts)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    best = kv.Key;
                }
            }
            return bestCount >= SpecificThreshold ? best : null;
        }

        public static bool HasSpecificHeighten(Character? hero) =>
            GetDominantSpecific(hero) != null;

        public static double GetTaxonMagnitudeMultiplier(Character? hero)
        {
            double mult = CharmBonusController.GetAnimalLadderMultiplier(hero);
            if (HasSpecificHeighten(hero))
                mult *= SpecificHeightenFactor;
            return mult;
        }

        public static int GetTaxonMechanicValue(Character? hero, string taxon, int baseValue)
        {
            if (!HasTaxonSet(hero, taxon))
                return 0;
            return Math.Max(0, (int)Math.Round(baseValue * GetTaxonMagnitudeMultiplier(hero)));
        }

        public static IReadOnlyList<(string Label, int Count)> GetFormingSets(Character? hero)
        {
            var list = new List<(string, int)>();
            if (hero == null)
                return list;

            int animals = CountAnimals(hero);
            if (animals >= GeneralThreshold)
                list.Add(($"Animals {animals}", animals));

            foreach (var taxon in AnimalTagHelper.TaxonTags)
            {
                int n = CountTag(hero, taxon);
                if (n >= TaxonThreshold)
                {
                    string label = char.ToUpperInvariant(taxon[0]) + taxon.Substring(1);
                    list.Add(($"{label} {n}", n));
                }
            }

            string? specific = GetDominantSpecific(hero);
            if (specific != null)
                list.Add(($"{specific} {CountSpecific(hero, specific)}", CountSpecific(hero, specific)));

            return list;
        }

        public static IReadOnlyList<string> GetGrantedAnimalActionNames(Character? hero)
        {
            var names = new List<string>();
            if (!CharmBonusController.UnlocksAnimalActions(hero))
                return names;
            string? specific = GetDominantSpecific(hero);
            if (specific == null)
                return names;
            names.Add(FormatAnimalActionName(specific));
            return names;
        }

        public static string FormatAnimalActionName(string specificTag)
        {
            string pretty = specificTag.Replace('_', ' ').ToUpperInvariant();
            return $"ANIMAL: {pretty}";
        }

        public static void SyncAnimalActionsToPool(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return;

            var granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in GetGrantedAnimalActionNames(hero))
            {
                if (!string.IsNullOrWhiteSpace(name))
                    granted.Add(name.Trim());
            }

            var stale = new List<Action>();
            foreach (var entry in hero.ActionPool)
            {
                if (entry.action?.Name == null)
                    continue;
                if (!entry.action.Name.StartsWith("ANIMAL:", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (granted.Contains(entry.action.Name))
                    continue;
                stale.Add(entry.action);
            }

            foreach (var action in stale)
            {
                var combo = hero.GetComboActions();
                for (int i = combo.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(combo[i].Name, action.Name, StringComparison.OrdinalIgnoreCase))
                        hero.RemoveFromCombo(combo[i], ignoreWeaponRequirement: true);
                }
                hero.RemoveAllActionsByName(action.Name);
            }

            foreach (string actionName in granted)
            {
                bool exists = false;
                foreach (var entry in hero.ActionPool)
                {
                    if (entry.action != null &&
                        string.Equals(entry.action.Name, actionName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists)
                    continue;

                var action = new Action(actionName, ActionType.Attack)
                {
                    DamageMultiplier = 1.0,
                    Length = 1.0,
                    Tags = new List<string> { AnimalTagHelper.AnimalTag }
                };
                hero.AddAction(action, 1.0);
            }
        }
    }
}
