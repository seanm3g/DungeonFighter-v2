namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Unified icon registry - single source of truth for all icons
    /// Replaces duplicate definitions in EquipmentIcons, StatusIcons, CombatIcons
    /// </summary>
    public static class IconRegistry
    {
        // Core icons (used in multiple contexts)
        public const string Shield = "🛡";
        public const string Sword = "⚔";
        public const string Heal = "💚";
        public const string Magic = "✨";
        public const string Speed = "💨";
        public const string Death = "💀";
        public const string Fire = "🔥";
        public const string Ice = "❄";

        // Equipment icons
        public const string Bow = "🏹";
        public const string Wand = "🔮";
        public const string Staff = "⛏";
        public const string Mace = "🔨";
        public const string Dagger = "🗡";
        public const string Armor = "🛡"; // Same as Shield
        public const string Helmet = "⛑";
        public const string Boots = "👢";
        public const string Ring = "💍";
        public const string Amulet = "📿";
        public const string Potion = "🧪";
        public const string Scroll = "📜";
        public const string Gem = "💎";

        // Status effect icons
        public const string Burn = "🔥"; // Same as Fire
        public const string Freeze = "❄"; // Same as Ice
        public const string Poison = "💀"; // Same as Death
        public const string Stun = "⚡";
        public const string Bleed = "🩸";
        public const string Strength = "💪";
        public const string Weak = "😵";
        public const string Confused = "😵‍💫";

        // Combat icons
        public const string Player = "👤";
        public const string Enemy = "👹";
        public const string Boss = "👑";
        public const string Damage = "💥";
        public const string Critical = "💢";
        public const string Miss = "💨"; // Same as Speed
        public const string Block = "🛡"; // Same as Shield
        public const string Dodge = "💨"; // Same as Speed
        public const string Parry = "⚔"; // Same as Sword
        public const string Combo = "⚡"; // Same as Stun
        public const string Victory = "🏆";
        public const string Defeat = "💔";

        // Dungeon icons
        public const string Room = "🏠";
        public const string Door = "🚪";
        public const string Chest = "📦";
        public const string Trap = "⚠";
        public const string Secret = "❓";
        public const string Exit = "🚪"; // Same as Door
        public const string Stairs = "🪜";
        public const string Portal = "🌀";
        public const string Altar = "⛩";
        public const string Fountain = "⛲";
        public const string Lava = "🌋";
        public const string Water = "💧";
        public const string Forest = "🌲";
        public const string Desert = "🏜";
        public const string Mountain = "⛰";
        public const string Cave = "🕳";

        /// <summary>
        /// Gets equipment icon by weapon type
        /// </summary>
        public static string GetWeaponIcon(string weaponType)
        {
            return weaponType.ToLower() switch
            {
                "sword" => Sword,
                "wand" => Wand,
                "mace" => Mace,
                "dagger" => Dagger,
                _ => Sword
            };
        }

        /// <summary>
        /// Gets armor icon by armor type
        /// </summary>
        public static string GetArmorIcon(string armorType)
        {
            return armorType.ToLower() switch
            {
                "helmet" or "head" => Helmet,
                "armor" or "body" or "chest" => Armor,
                "boots" or "feet" => Boots,
                "ring" => Ring,
                "amulet" or "necklace" => Amulet,
                _ => Armor
            };
        }

        /// <summary>
        /// Gets status effect icon
        /// </summary>
        public static string GetStatusIcon(string statusEffect)
        {
            return statusEffect.ToLower() switch
            {
                "burn" or "burning" => Burn,
                "freeze" or "frozen" => Freeze,
                "poison" or "poisoned" => Poison,
                "stun" or "stunned" => Stun,
                "bleed" or "bleeding" => Bleed,
                "heal" or "healing" => Heal,
                "shield" or "protected" => Shield,
                "speed" or "haste" => Speed,
                "strength" or "strong" => Strength,
                "magic" or "enchanted" => Magic,
                "weak" or "weakened" => Weak,
                "confused" or "confusion" => Confused,
                _ => "?"
            };
        }

        /// <summary>
        /// Gets combat icon by action type
        /// </summary>
        public static string GetCombatIcon(string actionType)
        {
            return actionType.ToLower() switch
            {
                "damage" => Damage,
                "critical" => Critical,
                "miss" => Miss,
                "block" => Block,
                "dodge" => Dodge,
                "parry" => Parry,
                "combo" => Combo,
                "heal" => Heal,
                "magic" => Magic,
                "victory" => Victory,
                "defeat" => Defeat,
                _ => "?"
            };
        }
    }
}

