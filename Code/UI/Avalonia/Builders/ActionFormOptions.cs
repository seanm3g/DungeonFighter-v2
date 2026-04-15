using System;
using System.Linq;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Canonical dropdown options and normalization for the action form.
    /// </summary>
    public static class ActionFormOptions
    {
        public const string NoneOption = "(None)";

        /// <summary>Category option meaning "can be anything" (no specific category). Stored as empty in data.</summary>
        public const string GeneralOption = "(general)";

        /// <summary>Canonical cadence values for strict list.</summary>
        public static readonly string[] CanonicalCadences = { "Action", "Ability", "Chain", "Fight", "Dungeon" };

        /// <summary>Rarity dropdown options (item/action context).</summary>
        public static readonly string[] RarityDropdownOptions = { NoneOption, "Common", "Uncommon", "Rare", "Legendary" };

        /// <summary>Category options: (general) is the default and means "can be anything"; plus classes and WEAPON, ENEMY, REFERENCE.</summary>
        public static readonly string[] CategoryDropdownOptions = { "Barbarian", "Warrior", "Rogue", "Wizard", "WEAPON", "ENEMY", "REFERENCE" };

        /// <summary>Target type options.</summary>
        public static readonly string[] TargetTypeDropdownOptions = { "Self", "SingleTarget", "AreaOfEffect", "Environment", "SelfAndTarget" };

        /// <summary>Stat bonus type options.</summary>
        public static readonly string[] StatBonusTypeDropdownOptions = { "", "Health Regen", "Max Health", "Heal", "Strength", "Agility", "Technique", "Intelligence" };

        /// <summary>Chain-position bonus targets (when Chain Position MOD is on; see Advanced Mechanics).</summary>
        public static readonly string[] ChainPositionBonusTargetOptions = { "", "Accuracy", "EnemyAccuracy", "Damage", "MultiHit" };

        /// <summary># = flat per position; % = Damage only (percent points × position).</summary>
        public static readonly string[] ChainPositionValueKindOptions = { "#", "%" };

        /// <summary>
        /// Chain position coefficient: blank = 1-based slot (opener ×1); ComboSlotIndex0 = 0-based (opener ×0);
        /// ComboSlotIndex1 = 1-based; AmpTier = opener/middle/finisher exponent.
        /// </summary>
        public static readonly string[] ChainPositionBasisOptions = { "", "ComboSlotIndex0", "ComboSlotIndex1", "AmpTier" };

        /// <summary>Threshold attribute options.</summary>
        public static readonly string[] ThresholdTypeDropdownOptions = { "Health", "Strength", "Agility", "Technique", "Intelligence" };

        /// <summary>Qualifier options for thresholds.</summary>
        public static readonly string[] ThresholdQualifierOptions = { "", "Enemy", "Hero", "Environment" };

        /// <summary>Operator options for threshold comparison.</summary>
        public static readonly string[] ThresholdOperatorOptions = { "", "<", ">", "=", "<=", ">=", "!=" };

        /// <summary>Value kind for thresholds: # = absolute, % = percent of max.</summary>
        public static readonly string[] ThresholdValueKindOptions = { "#", "%" };

        /// <summary>Accumulation type options: (Label, Type).</summary>
        public static readonly (string Label, string Type)[] AccumulationTypeOptions =
        {
            ("Actions passed", "CadenceAction"),
            ("Abilities passed", "CadenceAbility"),
            ("Chains passed", "CadenceChain"),
            ("Fights passed", "CadenceFight"),
            ("Dungeons passed", "CadenceDungeon"),
            ("Damage done", "SelfDamage"),
            ("Health restored", "HealthRestored"),
            ("Hits landed", "HitsLanded"),
            ("Blocks", "Blocks"),
            ("Dodges", "Dodges"),
            ("Kills", "Kills"),
            ("Damage taken", "DamageTaken"),
            ("Turns taken", "TurnsTaken"),
            ("Combos used", "CombosUsed")
        };

        /// <summary>Parameters that accumulations can modify.</summary>
        public static readonly string[] AccumulationModifiesOptions = { "Damage", "Max Health", "Heal", "Health Regen", "Strength", "Agility", "Technique", "Intelligence" };

        /// <summary>Normalizes legacy cadence values to canonical.</summary>
        public static string NormalizeCadence(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var u = raw.Trim();
            if (string.Equals(u, "Abilities", StringComparison.OrdinalIgnoreCase)) return "Ability";
            if (string.Equals(u, "ACTIONS", StringComparison.OrdinalIgnoreCase) || string.Equals(u, "Actions", StringComparison.OrdinalIgnoreCase)) return "Action";
            if (string.Equals(u, "COMBO", StringComparison.OrdinalIgnoreCase) || string.Equals(u, "Combo", StringComparison.OrdinalIgnoreCase)) return "Chain";
            if (string.Equals(u, "ATTACK", StringComparison.OrdinalIgnoreCase) || string.Equals(u, "ATTACKS", StringComparison.OrdinalIgnoreCase)) return "Action";
            var canonical = CanonicalCadences.FirstOrDefault(c => string.Equals(c, u, StringComparison.OrdinalIgnoreCase));
            return canonical ?? u;
        }
    }
}
