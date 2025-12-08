using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.World.Tags
{
    /// <summary>
    /// Combines tags from multiple sources (gear, actions, rooms, etc.)
    /// </summary>
    public static class TagAggregator
    {
        /// <summary>
        /// Aggregates tags from a character (gear + actions + character tags)
        /// </summary>
        public static List<string> AggregateCharacterTags(Character character)
        {
            var tags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            // Add weapon tags
            if (character.Weapon != null && character.Weapon.Tags != null)
            {
                foreach (var tag in character.Weapon.Tags)
                {
                    tags.Add(tag);
                }
            }

            // Add armor tags (Body, Head, Feet)
            if (character.Body != null && character.Body.Tags != null)
            {
                foreach (var tag in character.Body.Tags)
                {
                    tags.Add(tag);
                }
            }
            if (character.Head != null && character.Head.Tags != null)
            {
                foreach (var tag in character.Head.Tags)
                {
                    tags.Add(tag);
                }
            }
            if (character.Feet != null && character.Feet.Tags != null)
            {
                foreach (var tag in character.Feet.Tags)
                {
                    tags.Add(tag);
                }
            }

            // Add action tags (from equipped actions)
            var actions = character.Actions.GetAllActions(character);
            foreach (var action in actions)
            {
                if (action.Tags != null)
                {
                    foreach (var tag in action.Tags)
                    {
                        tags.Add(tag);
                    }
                }
            }

            return tags.ToList();
        }

        /// <summary>
        /// Aggregates tags from an action
        /// </summary>
        public static List<string> AggregateActionTags(Action action)
        {
            return action.Tags?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Aggregates tags from multiple sources
        /// </summary>
        public static List<string> AggregateTags(params IEnumerable<string>[] tagSources)
        {
            var tags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var source in tagSources)
            {
                foreach (var tag in source)
                {
                    tags.Add(tag);
                }
            }
            return tags.ToList();
        }
    }
}

