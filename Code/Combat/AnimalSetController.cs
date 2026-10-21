using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Inherent animal tag ladder (no sheet): 2+ animal general, 2+ taxon mechanic, 3+ specific heighten / action unlock.
    /// Charm slot is excluded from counts (gear slots only).
    /// <para>Taxon set bonuses (2+ pieces of one taxon; scaled by the charm ladder and the 3+ specific heighten):
    /// Beast +6% damage, Bird +8% speed, Shell −8% damage taken, Reptile +4% damage / −4% taken,
    /// Bug +5% speed / +3% damage, Fish −6% damage taken, Mythic +5% AMP.</para>
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

        /// <summary>One taxon's set bonus at base strength (percent points).</summary>
        public readonly record struct TaxonBonus(string Taxon, string Name, double DamagePct, double SpeedPct, double AmpPct, double DamageTakenPct);

        private static readonly Dictionary<string, TaxonBonus> TaxonBonuses = new(StringComparer.OrdinalIgnoreCase)
        {
            ["beast"] = new TaxonBonus("beast", "Ferocity", 6, 0, 0, 0),
            ["bird"] = new TaxonBonus("bird", "Swiftness", 0, BaseBirdSpeedPct, 0, 0),
            ["shell"] = new TaxonBonus("shell", "Carapace", 0, 0, 0, 8),
            ["reptile"] = new TaxonBonus("reptile", "Scaled Hide", 4, 0, 0, 4),
            ["bug"] = new TaxonBonus("bug", "Swarm", 3, 5, 0, 0),
            ["fish"] = new TaxonBonus("fish", "Slippery", 0, 0, 0, 6),
            ["mythic"] = new TaxonBonus("mythic", "Legend", 0, 0, BaseMythicAmpPct, 0),
        };

        /// <summary>Active taxon bonuses (2+ pieces), already scaled by <see cref="GetTaxonMagnitudeMultiplier"/>.</summary>
        public static List<(TaxonBonus Bonus, int Count)> GetActiveTaxonBonuses(Character? hero)
        {
            var list = new List<(TaxonBonus, int)>();
            if (hero == null || hero is Enemy)
                return list;
            double mult = -1;
            foreach (var taxon in AnimalTagHelper.TaxonTags)
            {
                int n = CountTag(hero, taxon);
                if (n < TaxonThreshold || !TaxonBonuses.TryGetValue(taxon, out var b))
                    continue;
                if (mult < 0)
                    mult = GetTaxonMagnitudeMultiplier(hero);
                list.Add((b with
                {
                    DamagePct = b.DamagePct * mult,
                    SpeedPct = b.SpeedPct * mult,
                    AmpPct = b.AmpPct * mult,
                    DamageTakenPct = b.DamageTakenPct * mult
                }, n));
            }
            return list;
        }

        /// <summary>Hero damage dealt × this (beast / reptile / bug sets).</summary>
        public static double GetOutgoingDamageMultiplier(Actor? actor)
        {
            if (actor is not Character hero || actor is Enemy)
                return 1.0;
            double pct = 0;
            foreach (var (b, _) in GetActiveTaxonBonuses(hero))
                pct += b.DamagePct;
            return 1.0 + pct / 100.0;
        }

        /// <summary>Hero damage taken × this (shell / reptile / fish sets), never below 50%.</summary>
        public static double GetIncomingDamageMultiplier(Actor? actor)
        {
            if (actor is not Character hero || actor is Enemy)
                return 1.0;
            double pct = 0;
            foreach (var (b, _) in GetActiveTaxonBonuses(hero))
                pct += b.DamageTakenPct;
            return Math.Max(0.5, 1.0 - pct / 100.0);
        }

        /// <summary>Standing speed % from bird / bug sets (added to the general animal speed %).</summary>
        public static double GetTaxonSpeedPct(Character? hero)
        {
            double pct = 0;
            foreach (var (b, _) in GetActiveTaxonBonuses(hero))
                pct += b.SpeedPct;
            return pct;
        }

        /// <summary>Base AMP × (1 + this/100) from the mythic set.</summary>
        public static double GetAmpBonusPct(Character? hero)
        {
            double pct = 0;
            foreach (var (b, _) in GetActiveTaxonBonuses(hero))
                pct += b.AmpPct;
            return pct;
        }

        /// <summary>"Beast 2 - Ferocity: +6% damage" style lines for HUD tooltips.</summary>
        public static string DescribeBonus(TaxonBonus b)
        {
            var parts = new List<string>();
            if (b.DamagePct > 0) parts.Add($"+{b.DamagePct:0.#}% damage");
            if (b.SpeedPct > 0) parts.Add($"+{b.SpeedPct:0.#}% speed");
            if (b.AmpPct > 0) parts.Add($"+{b.AmpPct:0.#}% AMP");
            if (b.DamageTakenPct > 0) parts.Add($"-{b.DamageTakenPct:0.#}% damage taken");
            return $"{b.Name}: {string.Join(", ", parts)}";
        }

        /// <summary>Tooltip lines for an "animal:Label" HUD row: what every animal tier does and what is active now.</summary>
        public static List<string> DescribeSetBonuses(Character? hero)
        {
            var lines = new List<string>();
            int animals = CountAnimals(hero);
            lines.Add($"Animal gear: {animals} piece(s) (charm excluded).");
            lines.Add(animals >= GeneralThreshold
                ? $"Active - {GeneralThreshold}+ animals: +{GetGeneralSpeedPct(hero)}% speed."
                : $"{GeneralThreshold}+ animal pieces: +{BaseGeneralSpeedPct}% speed.");
            var active = GetActiveTaxonBonuses(hero);
            foreach (var (b, n) in active)
                lines.Add($"Active - {char.ToUpperInvariant(b.Taxon[0]) + b.Taxon.Substring(1)} {n}: {DescribeBonus(b)}");
            foreach (var kv in TaxonBonuses)
            {
                if (active.Exists(a => string.Equals(a.Bonus.Taxon, kv.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;
                int have = CountTag(hero, kv.Key);
                lines.Add($"{char.ToUpperInvariant(kv.Key[0]) + kv.Key.Substring(1)} {have}/{TaxonThreshold}: {DescribeBonus(kv.Value)}");
            }
            string? specific = GetDominantSpecific(hero);
            lines.Add(specific != null
                ? $"Active - 3x {specific.Replace('_', ' ')}: taxon bonuses x{SpecificHeightenFactor:0.#}."
                : $"{SpecificThreshold}+ of the same animal: taxon bonuses x{SpecificHeightenFactor:0.#}.");
            return lines;
        }

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
                    string bonus = TaxonBonuses.TryGetValue(taxon, out var b) ? $" {b.Name}" : "";
                    list.Add(($"{label} {n}{bonus}", n));
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
