using System;
using System.Collections.Generic;
using RPGGame.Data;
using RPGGame;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test data builders for creating test objects
    /// Simplifies test setup and improves test readability
    /// </summary>
    public static class TestDataBuilders
    {
        /// <summary>
        /// Builds a test character with configurable properties
        /// </summary>
        public class CharacterBuilder
        {
            private string _name = "TestCharacter";
            private int _level = 1;
            private int _strength = 10;
            private int _agility = 10;
            private int _technique = 10;
            private int _intelligence = 10;

            public CharacterBuilder WithName(string name)
            {
                _name = name;
                return this;
            }

            public CharacterBuilder WithLevel(int level)
            {
                _level = level;
                return this;
            }

            public CharacterBuilder WithStats(int strength, int agility, int technique, int intelligence)
            {
                _strength = strength;
                _agility = agility;
                _technique = technique;
                _intelligence = intelligence;
                return this;
            }

            public Character Build()
            {
                var character = new Character(_name, _level);
                // Always override stats with builder values to ensure test consistency
                character.Stats.Strength = _strength;
                character.Stats.Agility = _agility;
                character.Stats.Technique = _technique;
                character.Stats.Intelligence = _intelligence;
                return character;
            }
        }

        /// <summary>
        /// Builds a test enemy with configurable properties
        /// </summary>
        public class EnemyBuilder
        {
            private string _name = "TestEnemy";
            private int _level = 1;
            private int _health = 50;
            private int _strength = 5;
            private int _agility = 5;
            private int _technique = 5;
            private int _intelligence = 5;

            public EnemyBuilder WithName(string name)
            {
                _name = name;
                return this;
            }

            public EnemyBuilder WithLevel(int level)
            {
                _level = level;
                return this;
            }

            public EnemyBuilder WithHealth(int health)
            {
                _health = health;
                return this;
            }

            public EnemyBuilder WithStats(int strength, int agility, int technique, int intelligence)
            {
                _strength = strength;
                _agility = agility;
                _technique = technique;
                _intelligence = intelligence;
                return this;
            }

            public Enemy Build()
            {
                return new Enemy(_name, _level, _health, _strength, _agility, _technique, _intelligence);
            }
        }

        /// <summary>
        /// Builds a test item with configurable properties
        /// </summary>
        public class ItemBuilder
        {
            private ItemType _type = ItemType.Weapon;
            private string _name = "TestItem";
            private int _tier = 1;
            private int _baseDamage = 10;
            private string _rarity = "Common";

            public ItemBuilder WithType(ItemType type)
            {
                _type = type;
                return this;
            }

            public ItemBuilder WithName(string name)
            {
                _name = name;
                return this;
            }

            public ItemBuilder WithTier(int tier)
            {
                _tier = tier;
                return this;
            }

            public ItemBuilder WithBaseDamage(int damage)
            {
                _baseDamage = damage;
                return this;
            }

            public ItemBuilder WithRarity(string rarity)
            {
                _rarity = rarity;
                return this;
            }

            public Item Build()
            {
                return new Item(_type, _name, _tier, _baseDamage)
                {
                    Rarity = _rarity
                };
            }
        }

        /// <summary>
        /// Creates a new character builder
        /// </summary>
        public static CharacterBuilder Character()
        {
            return new CharacterBuilder();
        }

        /// <summary>
        /// Creates a new enemy builder
        /// </summary>
        public static EnemyBuilder Enemy()
        {
            return new EnemyBuilder();
        }

        /// <summary>
        /// Creates a new item builder
        /// </summary>
        public static ItemBuilder Item()
        {
            return new ItemBuilder();
        }

        /// <summary>
        /// Builds a test weapon with configurable properties
        /// </summary>
        public class WeaponBuilder
        {
            private string _name = "TestWeapon";
            private int _tier = 1;
            private int _baseDamage = 10;
            private double _baseAttackSpeed = 0.05;
            private WeaponType _weaponType = WeaponType.Sword;
            private List<StatBonus> _statBonuses = new List<StatBonus>();
            private List<Modification> _modifications = new List<Modification>();

            public WeaponBuilder WithName(string name)
            {
                _name = name;
                return this;
            }

            public WeaponBuilder WithTier(int tier)
            {
                _tier = tier;
                return this;
            }

            public WeaponBuilder WithBaseDamage(int damage)
            {
                _baseDamage = damage;
                return this;
            }

            public WeaponBuilder WithWeaponType(WeaponType weaponType)
            {
                _weaponType = weaponType;
                return this;
            }

            public WeaponBuilder WithStatBonus(string statType, double value)
            {
                _statBonuses.Add(new StatBonus { StatType = statType, Value = value });
                return this;
            }

            public WeaponBuilder WithModification(Modification modification)
            {
                _modifications.Add(modification);
                return this;
            }

            public WeaponItem Build()
            {
                var weapon = new WeaponItem(_name, _tier, _baseDamage, _baseAttackSpeed, _weaponType);
                weapon.StatBonuses = _statBonuses;
                weapon.Modifications = _modifications;
                return weapon;
            }
        }

        /// <summary>
        /// Builds a test armor item with configurable properties
        /// </summary>
        public class ArmorBuilder
        {
            private ItemType _armorType = ItemType.Head;
            private string _name = "TestArmor";
            private int _tier = 1;
            private int _armor = 5;
            private List<StatBonus> _statBonuses = new List<StatBonus>();
            private List<Modification> _modifications = new List<Modification>();

            public ArmorBuilder WithType(ItemType armorType)
            {
                _armorType = armorType;
                return this;
            }

            public ArmorBuilder WithName(string name)
            {
                _name = name;
                return this;
            }

            public ArmorBuilder WithTier(int tier)
            {
                _tier = tier;
                return this;
            }

            public ArmorBuilder WithArmor(int armor)
            {
                _armor = armor;
                return this;
            }

            public ArmorBuilder WithStatBonus(string statType, double value)
            {
                _statBonuses.Add(new StatBonus { StatType = statType, Value = value });
                return this;
            }

            public ArmorBuilder WithModification(Modification modification)
            {
                _modifications.Add(modification);
                return this;
            }

            public Item Build()
            {
                Item armor = _armorType switch
                {
                    ItemType.Head => new HeadItem(_name, _tier, _armor),
                    ItemType.Chest => new ChestItem(_name, _tier, _armor),
                    ItemType.Feet => new FeetItem(_name, _tier, _armor),
                    _ => new HeadItem(_name, _tier, _armor)
                };
                armor.StatBonuses = _statBonuses;
                armor.Modifications = _modifications;
                return armor;
            }
        }

        /// <summary>
        /// Creates a new weapon builder
        /// </summary>
        public static WeaponBuilder Weapon()
        {
            return new WeaponBuilder();
        }

        /// <summary>
        /// Creates a new armor builder
        /// </summary>
        public static ArmorBuilder Armor()
        {
            return new ArmorBuilder();
        }

        /// <summary>
        /// Creates a mock action for testing
        /// Wrapper method that delegates to MockFactories for backward compatibility
        /// </summary>
        public static RPGGame.Action CreateMockAction(string name = "MockAction", RPGGame.ActionType type = RPGGame.ActionType.Attack)
        {
            return MockFactories.CreateMockAction(name, type);
        }

        /// <summary>
        /// Creates a test character for testing
        /// </summary>
        public static Character CreateTestCharacter(string name = "TestCharacter", int level = 1)
        {
            return new Character(name, level);
        }

        /// <summary>
        /// Creates a test LootDataCache with minimal test data
        /// </summary>
        public static LootDataCache CreateLootDataCache()
        {
            var cache = LootDataCache.CreateEmpty();
            // Add a test tier distribution
            cache.TierDistributions.Add(new TierDistribution
            {
                Level = 5,
                Tier1 = 50.0,
                Tier2 = 30.0,
                Tier3 = 15.0,
                Tier4 = 4.0,
                Tier5 = 1.0
            });
            // Add a test rarity
            cache.RarityData.Add(new RarityData
            {
                Name = "Common",
                Weight = 500,
                StatBonuses = 1,
                ActionBonuses = 0,
                Modifications = 0
            });
            return cache;
        }
    }

    /// <summary>
    /// Mock object factories for testing
    /// </summary>
    public static class MockFactories
    {
        /// <summary>
        /// Creates a mock character for testing
        /// </summary>
        public static Character CreateMockCharacter(string name = "MockCharacter", int level = 1)
        {
            return new Character(name, level);
        }

        /// <summary>
        /// Creates a mock enemy for testing
        /// </summary>
        public static Enemy CreateMockEnemy(string name = "MockEnemy", int level = 1, int health = 50)
        {
            return new Enemy(name, level, health, 5, 5, 5, 5);
        }

        /// <summary>
        /// Creates a mock action for testing
        /// </summary>
        public static RPGGame.Action CreateMockAction(string name = "MockAction", RPGGame.ActionType type = RPGGame.ActionType.Attack)
        {
            return new RPGGame.Action
            {
                Name = name,
                Type = type,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
        }
    }
}
