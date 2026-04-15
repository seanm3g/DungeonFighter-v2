using System;
using System.Collections.Generic;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Centralized validation rules for all game data
    /// </summary>
    public static class ValidationRules
    {
        // Action validation rules
        public static class Actions
        {
            public const double MinDamageMultiplier = 0.1;
            public const double MaxDamageMultiplier = 10.0;
            public const double MinLength = 0.1;
            public const double MaxLength = 10.0;
            public const int MinCooldown = 0;
            public const int MaxCooldown = 100;
            public const int MinRollBonus = -20;
            public const int MaxRollBonus = 20;
            public const int MinMultiHitCount = 1;
            public const int MaxMultiHitCount = 10;
            public const int MinSelfDamagePercent = 0;
            public const int MaxSelfDamagePercent = 100;
            public const int MinThreshold = 0;
            public const int MaxThreshold = 20;
            public const int MinThresholdAdjustment = -20;
            public const int MaxThresholdAdjustment = 20;
            public const int MinMultipleDiceCount = 1;
            public const int MaxMultipleDiceCount = 10;

            public static readonly HashSet<string> ValidTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Attack", "Heal", "Buff", "Debuff", "Interact", "Move", "UseItem", "Spell"
            };

            public static readonly HashSet<string> ValidTargetTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Self", "SingleTarget", "AreaOfEffect", "Environment", "SelfAndTarget"
            };

            public static readonly HashSet<string> ValidMultipleDiceModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Sum", "TakeHighest", "TakeLowest", "Average"
            };

            public static readonly HashSet<string> ValidStatBonusTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "STR", "AGI", "TEC", "INT", "Strength", "Agility", "Technique", "Intelligence",
                "HealthRegen", "Health Regen", "MaxHealth", "Max Health", "Heal"
            };

            /// <summary>Valid attribute types for thresholds (Health, Strength, Agility, Technique, Intelligence).</summary>
            public static readonly HashSet<string> ValidThresholdStatTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Health", "Strength", "Agility", "Technique", "Intelligence"
            };

            /// <summary>Valid accumulation source types (cadences, damage done, hits landed, etc.).</summary>
            public static readonly HashSet<string> ValidAccumulationTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CadenceAction", "CadenceAbility", "CadenceChain", "CadenceFight", "CadenceDungeon",
                "SelfDamage", "HealthRestored",
                "HitsLanded", "Blocks", "Dodges", "Kills", "DamageTaken", "TurnsTaken", "CombosUsed"
            };

            /// <summary>Valid parameters that accumulations can modify (e.g. Damage, Max Health).</summary>
            public static readonly HashSet<string> ValidAccumulationModifiesParam = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Damage", "Max Health", "Heal", "Health Regen", "Strength", "Agility", "Technique", "Intelligence"
            };

            public static readonly HashSet<string> ValidChainPositionModifiesParam = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Accuracy", "EnemyAccuracy", "Damage", "MultiHit",
                "RollBonus", "EnemyRollBonus"
            };

            public static readonly HashSet<string> ValidChainPositionBasis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "", "ComboSlotIndex0", "ComboSlotIndex1", "AmpTier"
            };

            public static readonly HashSet<string> ValidChainPositionValueKind = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "#", "%"
            };
        }

        // Enemy validation rules
        public static class Enemies
        {
            public const double MinStatOverride = 0.1;
            public const double MaxStatOverride = 2.0;

            public static readonly HashSet<string> ValidArchetypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Assassin", "Berserker", "Tank", "Caster", "Support", "Brute", "Guardian"
            };
        }

        // Weapon validation rules
        public static class Weapons
        {
            public const int MinBaseDamage = 1;
            public const int MaxBaseDamage = 100;
            public const double MinAttackSpeed = 0.1;
            public const double MaxAttackSpeed = 5.0;
            public const int MinTier = 1;
            public const int MaxTier = 10;

            public static readonly HashSet<string> ValidTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Dagger", "Mace", "Sword", "Wand"
            };
        }

        // Armor validation rules
        public static class Armor
        {
            public const int MinArmorValue = 0;
            public const int MaxArmorValue = 100;
            public const int MinTier = 1;
            public const int MaxTier = 10;

            public static readonly HashSet<string> ValidSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Head", "Chest", "Legs", "Arms", "Feet", "Hands", "Neck", "Ring", "Trinket"
            };
        }

        // Dungeon validation rules
        public static class Dungeons
        {
            public const int MinLevel = 1;
            public const int MaxLevel = 100;

            public static readonly HashSet<string> ValidThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Forest", "Lava", "Crypt", "Crystal", "Temple", "Generic", "Ice", "Shadow",
                "Steampunk", "Swamp", "Astral", "Underground", "Storm", "Nature", "Arcane",
                "Desert", "Volcano", "Ruins", "Ocean", "Mountain", "Temporal", "Dream",
                "Dimensional", "Divine", "Void"
            };
        }

        // Helper methods for validation
        public static bool IsInRange(double value, double min, double max)
        {
            return value >= min && value <= max;
        }

        public static bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static string FormatRangeError(string field, double value, double min, double max)
        {
            return $"{field} value {value} is out of range. Expected: {min} to {max}";
        }

        public static string FormatRangeError(string field, int value, int min, int max)
        {
            return $"{field} value {value} is out of range. Expected: {min} to {max}";
        }
    }
}
