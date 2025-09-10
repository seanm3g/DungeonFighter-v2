namespace RPGGame
{
    public static class FlavorText
    {
        // Character and Enemy Names
        public static class Names
        {
            public static readonly string[] CharacterFirstNames = new[]
            {
                "Aric", "Bran", "Cael", "Dain", "Eldrin", "Fenris", "Galen", "Haldir",
                "Ivar", "Joren", "Kael", "Lorin", "Merek", "Nolan", "Orin", "Pax",
                "Quinn", "Roran", "Soren", "Taran", "Ulfric", "Varis", "Wren", "Xan",
                "Yorin", "Zarek"
            };

            public static readonly string[] CharacterLastNames = new[]
            {
                "Stormrider", "Ironheart", "Shadowbane", "Dawnbringer", "Frostborn",
                "Blackthorn", "Silverwind", "Firebrand", "Stormcaller", "Nightshade",
                "Brightblade", "Darkwood", "Swiftarrow", "Stoneheart", "Moonwhisper"
            };

            public static readonly string[] EnemyNames = new[]
            {
                "Goblin", "Orc", "Troll", "Skeleton", "Zombie", "Bandit", "Cultist",
                "Wraith", "Ghoul", "Harpy", "Minotaur", "Gargoyle", "Imp", "Specter",
                "Wight", "Revenant", "Banshee", "Wendigo", "Chimera", "Basilisk"
            };

            public static readonly string[] BossNames = new[]
            {
                "The Corrupted King", "The Shadow Queen", "The Lich Lord", "The Demon Prince",
                "The Dragon Tyrant", "The Necromancer", "The Archfiend", "The Void Walker",
                "The Blood Mage", "The Soul Reaper"
            };
        }

        // Item Names and Descriptions
        public static class Items
        {
            public static readonly string[] WeaponPrefixes = new[]
            {
                "Sharp", "Deadly", "Mighty", "Ancient", "Cursed", "Blessed", "Flaming",
                "Frost", "Thundering", "Venomous", "Radiant", "Shadow", "Bloody", "Soul",
                "Dragon"
            };

            public static readonly string[] WeaponTypes = new[]
            {
                "Sword", "Axe", "Mace", "Dagger", "Bow", "Staff", "Wand", "Spear",
                "Hammer", "Scythe", "Rapier", "Greatsword", "Battleaxe", "Warhammer"
            };

            public static readonly string[] ArmorPrefixes = new[]
            {
                "Sturdy", "Reinforced", "Enchanted", "Ancient", "Cursed", "Blessed",
                "Dragon", "Demon", "Angelic", "Shadow", "Radiant", "Soul", "Blood",
                "Frost", "Flame"
            };

            public static readonly string[] ArmorTypes = new[]
            {
                "Helmet", "Chestplate", "Gauntlets", "Greaves", "Boots", "Shield",
                "Pauldrons", "Bracers", "Belt", "Cloak"
            };

            public static readonly string[] ConsumableNames = new[]
            {
                "Health Potion", "Mana Potion", "Stamina Potion", "Antidote",
                "Elixir of Strength", "Elixir of Agility", "Elixir of Intelligence",
                "Scroll of Teleportation", "Scroll of Identification", "Bomb"
            };
        }

        // Action Names and Descriptions
        public static class Actions
        {
            public static readonly string[] AttackNames = new[]
            {
                "Slash", "Strike", "Thrust", "Cleave", "Smash", "Pierce", "Bash",
                "Hack", "Chop", "Lunge", "Swipe", "Crush", "Impale", "Rend"
            };

            public static readonly string[] SpellNames = new[]
            {
                "Fireball", "Frostbolt", "Lightning Strike", "Shadow Bolt", "Holy Light",
                "Arcane Missile", "Poison Cloud", "Healing Wave", "Mana Surge",
                "Soul Drain"
            };

            public static readonly string[] BuffNames = new[]
            {
                "Strength Boost", "Agility Boost", "Intelligence Boost", "Protection",
                "Haste", "Regeneration", "Mana Regeneration", "Stamina Regeneration",
                "Resistance", "Fortitude"
            };

            public static readonly string[] DebuffNames = new[]
            {
                "Weakness", "Slow", "Poison", "Curse", "Bleed", "Burn", "Freeze",
                "Stun", "Silence", "Blind"
            };
        }

        // Environment Descriptions
        public static class Environments
        {
            public static readonly string[] LocationNames = new[]
            {
                "Ancient Forest", "Dark Cavern", "Ruined Temple", "Frozen Wastes",
                "Desert Oasis", "Misty Swamp", "Volcanic Crater", "Crystal Caves",
                "Haunted Mansion", "Dragon's Lair"
            };

            public static readonly string[] LocationDescriptions = new[]
            {
                "A dense forest with ancient trees that seem to whisper secrets of old.",
                "A dark and damp cavern filled with the echoes of dripping water.",
                "A once-great temple now in ruins, its walls covered in mysterious runes.",
                "A vast expanse of ice and snow where the wind howls endlessly.",
                "A small oasis in the middle of a scorching desert, a rare sight of life.",
                "A murky swamp where the air is thick with mist and strange creatures lurk.",
                "A massive crater filled with bubbling lava and the stench of sulfur.",
                "Beautiful caves filled with glowing crystals that illuminate the darkness.",
                "An eerie mansion where shadows seem to move on their own.",
                "A massive cave filled with treasure and the bones of unfortunate adventurers."
            };
        }

        // Helper Methods
        public static string GetRandomName(string[] nameList)
        {
            return nameList[new Random().Next(nameList.Length)];
        }

        public static string GenerateWeaponName()
        {
            return $"{GetRandomName(Items.WeaponPrefixes)} {GetRandomName(Items.WeaponTypes)}";
        }

        public static string GenerateArmorName()
        {
            return $"{GetRandomName(Items.ArmorPrefixes)} {GetRandomName(Items.ArmorTypes)}";
        }

        public static string GenerateEnemyName()
        {
            return GetRandomName(Names.EnemyNames);
        }

        public static string GenerateCharacterName()
        {
            return $"{GetRandomName(Names.CharacterFirstNames)} {GetRandomName(Names.CharacterLastNames)}";
        }

        public static string GenerateLocationName()
        {
            return GetRandomName(Environments.LocationNames);
        }

        public static string GenerateLocationDescription()
        {
            return GetRandomName(Environments.LocationDescriptions);
        }
    }
} 