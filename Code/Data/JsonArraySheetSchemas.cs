using System;
using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>Canonical column orders and authorized JSON keys for tabular game-data sheets.</summary>
    public static class JsonArraySheetSchemas
    {
        /// <summary>
        /// Preferred column order for the weapons sheet / <c>Weapons.json</c> round-trip.
        /// <c>dps</c> and <c>balance</c> are sheet-only balancing helpers and are dropped when importing CSV → JSON.
        /// </summary>
        public static readonly string[] WeaponsCanonicalHeaders =
        {
            "type", "name", "dps", "balance", "baseDamage", "damageBonusMin", "damageBonusMax", "attackSpeed", "tier",
            "extraActionSlots", "extraActionSlotsMin", "extraActionSlotsMax",
            "attributeRequirements", "tags", "Compelled Action", "triggerName"
        };

        public static readonly string[] ModificationsCanonicalHeaders =
        {
            "DiceResult", "ItemRank", "Name", "Description", "Effect", "value", "prefixCategory",
            "ATTRIBUTE REQUIREMENT", "REQUIREMENT VALUE",
            "ATTRIBUTE REQUREMENT", "MaxValue", "MinValue", "RolledValue", "tags"
        };

        public static readonly string[] ArmorCanonicalHeaders =
        {
            "slot", "name", "armor", "tags",
            "STRENGTH", "AGILITY", "TECHNIQUE", "INTELLIGENCE", "HIT", "COMBO", "CRIT",
            "# OF ACTION SLOTS", "# OF BONUS ACTIONS",
            "tier", "attributeRequirements", "requirement value", "triggerName"
        };

        public static readonly string[] EnemiesCanonicalHeaders =
        {
            "region", "biome", "location", "rarity", "name", "tags", "archetype",
            "baseAttributes.strength", "baseAttributes.agility", "baseAttributes.technique", "baseAttributes.intelligence",
            "growthPerLevel.strength", "growthPerLevel.agility", "growthPerLevel.technique", "growthPerLevel.intelligence",
            "healthPercent", "healthGrowthPercent",
            "actions", "isLiving", "description", "colorOverride"
        };

        internal static readonly string[] EnemyNestedObjectNames = { "baseAttributes", "growthPerLevel" };

        public static int GetTabularSheetHeaderRowCount(GameDataTabularSheetKind kind) =>
            kind == GameDataTabularSheetKind.Enemies ? 2 : 1;

        public const string EnemySheetCategoryOverrides = "overrides";
        public const string EnemySheetCategoryBaseAttributes = "base attributes";
        public const string EnemySheetCategoryGrowth = "growth";
        public const string EnemySheetCategoryHealth = "HEALTH";

        public static readonly string[] EnvironmentsCanonicalHeaders =
            { "region", "biome", "location", "tags", "description", "actions", "enemies" };

        public static readonly string[] DungeonsCanonicalHeaders =
            { "name", "theme", "minLevel", "maxLevel", "possibleEnemies", "colorOverride" };

        public static readonly string[] StatBonusesCanonicalHeaders =
            { "Name", "Description", "Value", "Rarity", "StatType", "ItemRank", "Mechanics", "Requirements" };

        public static readonly string[] ConsumablesCanonicalHeaders =
            { "displayName", "internalKind", "effect", "potency" };

        public static readonly string[] TriggersCanonicalHeaders =
            { "id", "name", "when", "count", "scope", "mechanics", "value", "filters", "channel" };

        internal static readonly HashSet<string> ConsumablesAuthorizedJsonKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "displayName", "internalKind", "effect", "potency"
        };

        internal static readonly HashSet<string> TriggersAuthorizedJsonKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "id", "name", "when", "count", "scope", "mechanics", "value", "filters", "channel"
        };

        internal static readonly HashSet<string> StatBonusAuthorizedJsonKeys = new(StringComparer.Ordinal)
        {
            "Name", "Description", "Value", "Rarity", "StatType", "ItemRank", "Mechanics", "Requirements"
        };

        public static IReadOnlyList<string> GetCanonicalHeaders(GameDataTabularSheetKind kind) =>
            kind switch
            {
                GameDataTabularSheetKind.Weapons => WeaponsCanonicalHeaders,
                GameDataTabularSheetKind.Modifications => ModificationsCanonicalHeaders,
                GameDataTabularSheetKind.Armor => ArmorCanonicalHeaders,
                GameDataTabularSheetKind.Enemies => EnemiesCanonicalHeaders,
                GameDataTabularSheetKind.Environments => EnvironmentsCanonicalHeaders,
                GameDataTabularSheetKind.Dungeons => DungeonsCanonicalHeaders,
                GameDataTabularSheetKind.StatBonuses => StatBonusesCanonicalHeaders,
                GameDataTabularSheetKind.Consumables => ConsumablesCanonicalHeaders,
                GameDataTabularSheetKind.Triggers => TriggersCanonicalHeaders,
                _ => Array.Empty<string>()
            };
    }
}
