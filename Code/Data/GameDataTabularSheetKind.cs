namespace RPGGame.Data
{
    /// <summary>Push/pull for JSON array game files on a single-header-row sheet tab.</summary>
    public enum GameDataTabularSheetKind
    {
        Weapons,
        Modifications,
        Armor,
        /// <summary><c>Enemies.json</c> — enemy archetypes, base attributes, and per-level growth.</summary>
        Enemies,
        /// <summary><c>Rooms.json</c> — room / environment definitions (sheet tab often named ENVIRONMENTS).</summary>
        Environments,
        /// <summary><c>Dungeons.json</c> — dungeon definitions: theme, levels, <c>possibleEnemies</c> (sheet tab often named DUNGEONS).</summary>
        Dungeons,
        /// <summary><c>StatBonuses.json</c> — rolled item suffix names / stat lines (sheet tab often named SUFFIXES).</summary>
        StatBonuses,
        /// <summary><c>Consumables.json</c> — room-search food and dungeon potions (sheet tab CONSUMABLES).</summary>
        Consumables,
        /// <summary>Unused since 2026-11-18: the item trigger catalog (Triggers.json) was removed; kept so saved sheet configs load.</summary>
        Triggers,
        /// <summary><c>MaterialBuilds.json</c> — material-set synthesis / convert / feed (sheet tab MATERIAL BUILDS).</summary>
        MaterialBuilds,
        /// <summary><c>Charms.json</c> — charm slot amplifiers (sheet tab CHARMS).</summary>
        Charms,
        /// <summary><c>Regions.json</c> — travel regions with fixed dungeon level bands and linked biomes (sheet tab REGIONS).</summary>
        Regions,
        /// <summary><c>ItemMods.json</c> — item mod families, one sheet row per tier (sheet tab ITEM MODS).</summary>
        ItemMods,
        /// <summary><c>RarityTable.json</c> — rarity weights, unlock level and line counts per pool (sheet tab RARITY).</summary>
        Rarity
    }
}
