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
            RegisterFromDefinitions();
        }

        public static TagRegistry Instance => _instance;

        public void RegisterTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                _registeredTags.Add(tag);
        }

        public void RegisterTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
                RegisterTag(tag);
        }

        public bool IsTagRegistered(string tag) => _registeredTags.Contains(tag);

        public IEnumerable<string> GetAllTags() => _registeredTags.ToList();

        private void RegisterFromDefinitions()
        {
            RegisterTags(TagDefinitions.AllRegistryTags);
        }
    }
}
