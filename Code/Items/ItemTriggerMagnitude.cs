using System;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Resolves item trigger bundle magnitudes, including optional <see cref="ActionTriggerBundle.ScaleFrom"/>.
    /// Rule: when scaleFrom is set, effective = (value ?? 0) × ScaleUnits; otherwise value as-is.
    /// </summary>
    public static class ItemTriggerMagnitude
    {
        public static double? Resolve(ActionTriggerBundle? bundle, Character? character)
        {
            if (bundle == null)
                return null;
            if (string.IsNullOrWhiteSpace(bundle.ScaleFrom) || character == null)
                return bundle.Value;
            double baseVal = bundle.Value ?? 0;
            int units = ScaleUnits(character, bundle.ScaleFrom);
            return baseVal * units;
        }

        public static double ResolveOrZero(ActionTriggerBundle? bundle, Character? character) =>
            Resolve(bundle, character) ?? 0;

        public static int ScaleUnits(Character character, string? scaleFrom)
        {
            if (character == null || string.IsNullOrWhiteSpace(scaleFrom))
                return 0;
            string key = scaleFrom.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return key switch
            {
                "STR" or "STRENGTH" => character.Stats?.Strength ?? 0,
                "AGI" or "AGILITY" => character.Stats?.Agility ?? 0,
                "TEC" or "TECH" or "TECHNIQUE" => character.Stats?.Technique ?? 0,
                "INT" or "INTELLIGENCE" => character.Stats?.Intelligence ?? 0,
                "PRIMARY" => ResolvePrimaryAttr(character),
                "LEVEL" => character.Level,
                "BARBARIAN" or "BARBARIANPOINTS" => character.BarbarianPoints,
                "WARRIOR" or "WARRIORPOINTS" => character.WarriorPoints,
                "ROGUE" or "ROGUEPOINTS" => character.RoguePoints,
                "WIZARD" or "WIZARDPOINTS" => character.WizardPoints,
                _ => 0
            };
        }

        private static int ResolvePrimaryAttr(Character character)
        {
            // Use base stats (not equipment-inclusive getters) to avoid recursion in equip filters.
            int str = character.Stats?.Strength ?? 0;
            int agi = character.Stats?.Agility ?? 0;
            int intel = character.Stats?.Intelligence ?? 0;
            if (character.Weapon is WeaponItem w)
            {
                return w.WeaponType switch
                {
                    WeaponType.Mace => str,
                    WeaponType.Sword => str,
                    WeaponType.Dagger => agi,
                    WeaponType.Wand => intel,
                    _ => str
                };
            }

            return str;
        }
    }
}
