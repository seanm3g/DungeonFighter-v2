using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.World.Tags
{
    /// <summary>
    /// Central repository for all tags in the game
    /// </summary>
    public class TagRegistry
    {
        private static readonly TagRegistry _instance = new TagRegistry();
        private readonly HashSet<string> _registeredTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private TagRegistry()
        {
            // Initialize with common tags
            RegisterCommonTags();
        }

        public static TagRegistry Instance => _instance;

        /// <summary>
        /// Registers a tag
        /// </summary>
        public void RegisterTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                _registeredTags.Add(tag);
            }
        }

        /// <summary>
        /// Registers multiple tags
        /// </summary>
        public void RegisterTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                RegisterTag(tag);
            }
        }

        /// <summary>
        /// Checks if a tag is registered
        /// </summary>
        public bool IsTagRegistered(string tag)
        {
            return _registeredTags.Contains(tag);
        }

        /// <summary>
        /// Gets all registered tags
        /// </summary>
        public IEnumerable<string> GetAllTags()
        {
            return _registeredTags.ToList();
        }

        private void RegisterCommonTags()
        {
            var commonTags = new[]
            {
                "FIRE", "WATER", "ICE", "EARTH", "AIR", "LIGHTNING",
                "WIZARD", "WARRIOR", "ROGUE", "BARBARIAN",
                "CELESTIAL", "UNDERWORLD", "NATURE", "ARCANE",
                "SWORD", "MACE", "DAGGER", "WAND",
                "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY", "MYTHIC", "TRANSCENDENT"
            };
            RegisterTags(commonTags);
        }
    }
}

