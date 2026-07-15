using System;
using RPGGame.Combat.Calculators;

namespace RPGGame
{
    /// <summary>
    /// Dev-only resurrection on the death screen — restores the hero with no penalties.
    /// </summary>
    public static class CharacterResurrectionService
    {
        public static void ResurrectDevNoPenalty(Character character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            character.ClearAllTempEffects();
            character.CurrentHealth = character.GetEffectiveMaxHealth();
            character.SessionStats.SessionEndTime = null;
            DamageCalculator.InvalidateCache(character);
        }
    }
}
