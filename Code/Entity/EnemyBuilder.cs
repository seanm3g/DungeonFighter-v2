using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Builder pattern for creating Enemy instances with complex initialization
    /// Separates construction logic from the Enemy class itself
    /// </summary>
    public class EnemyBuilder
    {
        private string? _name;
        private int _level = 1;
        private int _maxHealth = 50;
        private int _strength = 8;
        private int _agility = 6;
        private int _technique = 4;
        private int _intelligence = 4;
        private int _armor = 0;
        private PrimaryAttribute _primaryAttribute = PrimaryAttribute.Strength;
        private bool _isLiving = true;
        private EnemyArchetype? _archetype;
        private bool _useDirectStats = false;
        private int _damage = 8;
        private double _attackSpeed = 1.0;

        public EnemyBuilder WithName(string? name)
        {
            _name = name;
            return this;
        }

        public EnemyBuilder WithLevel(int level)
        {
            _level = level;
            return this;
        }

        public EnemyBuilder WithMaxHealth(int maxHealth)
        {
            _maxHealth = maxHealth;
            return this;
        }

        public EnemyBuilder WithStats(int strength, int agility, int technique, int intelligence)
        {
            _strength = strength;
            _agility = agility;
            _technique = technique;
            _intelligence = intelligence;
            _useDirectStats = false;
            return this;
        }

        public EnemyBuilder WithDirectStats(int damage, double attackSpeed)
        {
            _damage = damage;
            _attackSpeed = attackSpeed;
            _useDirectStats = true;
            return this;
        }

        public EnemyBuilder WithArmor(int armor)
        {
            _armor = armor;
            return this;
        }

        public EnemyBuilder WithPrimaryAttribute(PrimaryAttribute primaryAttribute)
        {
            _primaryAttribute = primaryAttribute;
            return this;
        }

        public EnemyBuilder WithLivingStatus(bool isLiving)
        {
            _isLiving = isLiving;
            return this;
        }

        public EnemyBuilder WithArchetype(EnemyArchetype archetype)
        {
            _archetype = archetype;
            return this;
        }

        public Enemy Build()
        {
            if (_useDirectStats)
            {
                return new Enemy(_name, _level, _maxHealth, _damage, _armor, _attackSpeed, _primaryAttribute, _isLiving, _archetype, true);
            }
            else
            {
                return new Enemy(_name, _level, _maxHealth, _strength, _agility, _technique, _intelligence, _armor, _primaryAttribute, _isLiving, _archetype);
            }
        }

        /// <summary>
        /// Creates an enemy with default settings
        /// </summary>
        public static Enemy CreateDefault(string? name = null, int level = 1)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .Build();
        }

        /// <summary>
        /// Creates an enemy with direct stats
        /// </summary>
        public static Enemy CreateWithDirectStats(string? name, int level, int maxHealth, int damage, double attackSpeed, int armor = 0)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(maxHealth)
                .WithDirectStats(damage, attackSpeed)
                .WithArmor(armor)
                .Build();
        }

        /// <summary>
        /// Creates an enemy with traditional stats
        /// </summary>
        public static Enemy CreateWithStats(string? name, int level, int maxHealth, int strength, int agility, int technique, int intelligence, int armor = 0)
        {
            return new EnemyBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithMaxHealth(maxHealth)
                .WithStats(strength, agility, technique, intelligence)
                .WithArmor(armor)
                .Build();
        }
    }
}
