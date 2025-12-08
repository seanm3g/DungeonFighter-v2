using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.World.Tags
{
    /// <summary>
    /// Handles temporary tag addition/removal
    /// </summary>
    public class TagModifier
    {
        private readonly Dictionary<Actor, List<string>> _temporaryTags = new Dictionary<Actor, List<string>>();
        private readonly Dictionary<Actor, Dictionary<string, int>> _tagDurations = new Dictionary<Actor, Dictionary<string, int>>();

        /// <summary>
        /// Adds a temporary tag to an actor
        /// </summary>
        public void AddTemporaryTag(Actor actor, string tag, int duration)
        {
            if (!_temporaryTags.ContainsKey(actor))
            {
                _temporaryTags[actor] = new List<string>();
                _tagDurations[actor] = new Dictionary<string, int>();
            }

            if (!_temporaryTags[actor].Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                _temporaryTags[actor].Add(tag);
            }

            _tagDurations[actor][tag] = duration;
        }

        /// <summary>
        /// Removes a temporary tag from an actor
        /// </summary>
        public void RemoveTemporaryTag(Actor actor, string tag)
        {
            if (_temporaryTags.TryGetValue(actor, out var tags))
            {
                tags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            }

            if (_tagDurations.TryGetValue(actor, out var durations))
            {
                durations.Remove(tag);
            }
        }

        /// <summary>
        /// Gets all temporary tags for an actor
        /// </summary>
        public List<string> GetTemporaryTags(Actor actor)
        {
            return _temporaryTags.TryGetValue(actor, out var tags) ? tags.ToList() : new List<string>();
        }

        /// <summary>
        /// Updates tag durations (called each turn)
        /// </summary>
        public void UpdateTagDurations(double turnsPassed)
        {
            var actorsToClean = new List<Actor>();

            foreach (var actor in _tagDurations.Keys.ToList())
            {
                var durations = _tagDurations[actor];
                var tagsToRemove = new List<string>();

                foreach (var tag in durations.Keys.ToList())
                {
                    durations[tag] -= (int)Math.Ceiling(turnsPassed);
                    if (durations[tag] <= 0)
                    {
                        tagsToRemove.Add(tag);
                    }
                }

                foreach (var tag in tagsToRemove)
                {
                    RemoveTemporaryTag(actor, tag);
                }

                if (durations.Count == 0)
                {
                    actorsToClean.Add(actor);
                }
            }

            foreach (var actor in actorsToClean)
            {
                _temporaryTags.Remove(actor);
                _tagDurations.Remove(actor);
            }
        }

        /// <summary>
        /// Clears all temporary tags for an actor
        /// </summary>
        public void ClearActorTags(Actor actor)
        {
            _temporaryTags.Remove(actor);
            _tagDurations.Remove(actor);
        }
    }
}

