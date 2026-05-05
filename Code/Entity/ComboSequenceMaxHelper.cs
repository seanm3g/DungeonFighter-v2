using System;

namespace RPGGame
{
    /// <summary>
    /// Effective max combo sequence length from <see cref="LootSystemConfig"/> plus equipped feet
    /// <see cref="Item.ExtraActionSlots"/> and StatBonuses with type <c>ExtraActionSlots</c>.
    /// </summary>
    public static class ComboSequenceMaxHelper
    {
        public static int GetEffectiveMax(Character? character)
        {
            var loot = GameConfiguration.Instance?.LootSystem;
            int baseMax = loot?.ComboSequenceBaseMax ?? 2;
            if (baseMax < 1)
                baseMax = 1;
            int absMax = loot?.ComboSequenceAbsoluteMax ?? 8;
            if (absMax < baseMax)
                absMax = baseMax;

            int feetSlots = SumFeetExtraActionSlots(character);
            return Math.Min(absMax, baseMax + Math.Max(0, feetSlots));
        }

        public static int SumFeetExtraActionSlots(Character? character)
        {
            if (character?.Equipment?.Feet is not FeetItem feet)
                return 0;
            int n = feet.ExtraActionSlots;
            if (feet.StatBonuses != null)
            {
                foreach (var sb in feet.StatBonuses)
                {
                    if (sb != null)
                        n += sb.SumContributionValuesForStatType("ExtraActionSlots");
                }
            }

            return Math.Max(0, n);
        }

        /// <summary>
        /// Removes non-required combo actions from the end of the ordered sequence until count ≤ <paramref name="maxAllowed"/>.
        /// </summary>
        public static void TrimComboSequenceToMax(Character character, int maxAllowed)
        {
            if (character == null || maxAllowed < 1)
                return;

            var combo = character.GetComboActions();
            while (combo.Count > maxAllowed)
            {
                bool removed = false;
                for (int i = combo.Count - 1; i >= 0; i--)
                {
                    Action? a = combo[i];
                    if (a == null)
                        continue;
                    if (!WeaponRequiredComboAction.CanRemoveFromCombo(character, a))
                        continue;
                    character.RemoveFromCombo(a, ignoreWeaponRequirement: false);
                    removed = true;
                    break;
                }

                if (!removed)
                    break;
                combo = character.GetComboActions();
            }
        }

        /// <summary>
        /// Drops removable actions while at or above max so one more <see cref="Character.AddToCombo"/> can succeed.
        /// </summary>
        public static void MakeRoomForOneComboAction(Character character, int maxAllowed)
        {
            while (character.GetComboActions().Count >= maxAllowed)
            {
                if (!TryRemoveOneRemovableFromComboEnd(character))
                    break;
            }
        }

        private static bool TryRemoveOneRemovableFromComboEnd(Character character)
        {
            var combo = character.GetComboActions();
            for (int i = combo.Count - 1; i >= 0; i--)
            {
                Action? a = combo[i];
                if (a == null)
                    continue;
                if (!WeaponRequiredComboAction.CanRemoveFromCombo(character, a))
                    continue;
                return character.RemoveFromCombo(a, ignoreWeaponRequirement: false);
            }

            return false;
        }
    }
}
