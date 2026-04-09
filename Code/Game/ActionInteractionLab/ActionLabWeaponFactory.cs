using System;
using System.Collections.Generic;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Builds a <see cref="WeaponItem"/> for the Action Interaction Lab from <see cref="WeaponData"/> plus optional prefix/suffix picks.
    /// </summary>
    public static class ActionLabWeaponFactory
    {
        private static readonly string[] RarityOrder = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };

        /// <summary>
        /// Creates a weapon with starter actions, optional rolled prefix modification, and optional stat-bonus suffix.
        /// </summary>
        public static WeaponItem CreateWeapon(
            WeaponData weaponData,
            Modification? prefixTemplate,
            StatBonus? suffixTemplate)
        {
            var weapon = ItemGenerator.GenerateWeaponItem(weaponData);
            weapon.Modifications.Clear();
            weapon.StatBonuses.Clear();
            weapon.ActionBonuses.Clear();
            weapon.GearAction = null;

            if (prefixTemplate != null)
                weapon.Modifications.Add(CloneModificationWithRoll(prefixTemplate));

            if (suffixTemplate != null)
                weapon.StatBonuses.Add(CloneStatBonus(suffixTemplate));

            ApplyMinimumRarity(weapon);
            weapon.Name = ItemGenerator.GenerateItemNameWithBonuses(weapon);
            AttachStarterWeaponActions(weapon);
            return weapon;
        }

        private static void AttachStarterWeaponActions(WeaponItem starterWeapon)
        {
            var actionSelector = new LootActionSelector(Random.Shared);
            var startingActions = actionSelector.GetStartingWeaponActions(starterWeapon.WeaponType.ToString());

            if (startingActions.Count > 0)
            {
                if (startingActions.Count > 1)
                {
                    foreach (var actionName in startingActions)
                        starterWeapon.ActionBonuses.Add(new ActionBonus { Name = actionName });
                }
                else
                    starterWeapon.GearAction = startingActions[0];
            }
            else
            {
                var fallbackAction = actionSelector.SelectWeaponActionForStarter(starterWeapon.WeaponType.ToString());
                if (!string.IsNullOrEmpty(fallbackAction))
                    starterWeapon.GearAction = fallbackAction;
            }
        }

        private static void ApplyMinimumRarity(WeaponItem weapon)
        {
            string? required = null;

            foreach (var mod in weapon.Modifications)
            {
                if (string.IsNullOrEmpty(mod.ItemRank))
                    continue;
                if (required == null || IsRarityStrictlyHigher(mod.ItemRank, required))
                    required = mod.ItemRank;
            }

            foreach (var sb in weapon.StatBonuses)
            {
                if (sb.Name.Equals("of the Sage", StringComparison.OrdinalIgnoreCase))
                {
                    if (required == null || IsRarityStrictlyHigher("Rare", required))
                        required = "Rare";
                }
            }

            if (required != null)
                weapon.Rarity = required;
        }

        private static bool IsRarityStrictlyHigher(string a, string b)
        {
            int ia = Array.IndexOf(RarityOrder, a);
            int ib = Array.IndexOf(RarityOrder, b);
            if (ia < 0) ia = 0;
            if (ib < 0) ib = 0;
            return ia > ib;
        }

        private static Modification CloneModificationWithRoll(Modification template)
        {
            var m = new Modification
            {
                DiceResult = template.DiceResult,
                ItemRank = template.ItemRank,
                Name = template.Name,
                Description = template.Description,
                Effect = template.Effect,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                RolledValue = RollValueBetween(template.MinValue, template.MaxValue),
            };
            if (template.StatusEffects != null && template.StatusEffects.Count > 0)
                m.StatusEffects = new List<string>(template.StatusEffects);
            return m;
        }

        private static double RollValueBetween(double minValue, double maxValue)
        {
            if (Math.Abs(minValue - maxValue) < 0.001)
                return minValue;
            return minValue + Random.Shared.NextDouble() * (maxValue - minValue);
        }

        private static StatBonus CloneStatBonus(StatBonus s) => new()
        {
            Name = s.Name,
            Description = s.Description,
            Value = s.Value,
            Weight = s.Weight,
            StatType = s.StatType,
        };

        /// <summary>
        /// Finds a reasonable default <see cref="WeaponData"/> row for the current lab weapon (match type, then name).
        /// </summary>
        public static int FindBestWeaponDataIndex(IReadOnlyList<WeaponData> weapons, WeaponItem? equipped)
        {
            if (weapons.Count == 0)
                return -1;
            if (equipped == null)
                return 0;

            string type = equipped.WeaponType.ToString();
            int tier = equipped.Tier;
            for (int i = 0; i < weapons.Count; i++)
            {
                if (string.Equals(weapons[i].Type, type, StringComparison.OrdinalIgnoreCase)
                    && weapons[i].Tier == tier)
                    return i;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                if (string.Equals(weapons[i].Type, type, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }
    }
}
