using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Builder pattern for creating Character instances with complex initialization
    /// Separates construction logic from the Character class itself
    /// </summary>
    public class CharacterBuilder
    {
        private string? _name;
        private int _level = 1;
        private List<Item>? _initialInventory;

        public CharacterBuilder WithName(string? name)
        {
            _name = name;
            return this;
        }

        public CharacterBuilder WithLevel(int level)
        {
            _level = level;
            return this;
        }

        public CharacterBuilder WithInventory(List<Item> inventory)
        {
            _initialInventory = inventory;
            return this;
        }

        public Character Build()
        {
            var character = new Character(_name, _level);
            
            // Initialize inventory if provided
            if (_initialInventory != null)
            {
                character.Equipment.Inventory = _initialInventory;
                character.Display = new GameDisplayManager(character, _initialInventory);
            }

            return character;
        }

        /// <summary>
        /// Creates a character with default settings
        /// </summary>
        public static Character CreateDefault(string? name = null, int level = 1)
        {
            return new CharacterBuilder()
                .WithName(name)
                .WithLevel(level)
                .Build();
        }

        /// <summary>
        /// Creates a character with specific inventory
        /// </summary>
        public static Character CreateWithInventory(string? name, int level, List<Item> inventory)
        {
            return new CharacterBuilder()
                .WithName(name)
                .WithLevel(level)
                .WithInventory(inventory)
                .Build();
        }
    }
}
