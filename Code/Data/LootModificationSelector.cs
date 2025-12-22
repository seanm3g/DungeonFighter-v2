using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Selects modifications for items based on context (dungeon theme, enemy archetype)
    /// Implements 70/30 bias: 70% contextual modifications, 30% standard random
    /// </summary>
    public class LootModificationSelector
    {
        private readonly Random _random;
        private readonly ModificationTables _tables;

        public LootModificationSelector(Random random)
        {
            _random = random ?? new Random();
            _tables = LootTableLoader.GetModificationTables();
        }

        /// <summary>
        /// Gets "favored dice results" for modifications based on context
        /// Returns a list of dice values that should be favored for this context
        /// The bonus applier will use these 70% of the time, and roll normally 30% of the time
        /// </summary>
        public List<int> GetFavoredDiceResults(LootContext? context)
        {
            if (context == null)
                return new List<int>();

            var favoredResults = new List<int>();

            // Collect favored dice results from theme modifications
            if (!string.IsNullOrEmpty(context.DungeonTheme) &&
                _tables.ThemeModifications.TryGetValue(context.DungeonTheme, out var themeMods))
            {
                foreach (var mod in themeMods)
                {
                    favoredResults.AddRange(mod.DiceResults);
                }
            }

            // Collect favored dice results from archetype modifications
            if (!string.IsNullOrEmpty(context.EnemyArchetype) &&
                _tables.ArchetypeModifications.TryGetValue(context.EnemyArchetype, out var archetypeMods))
            {
                foreach (var mod in archetypeMods)
                {
                    favoredResults.AddRange(mod.DiceResults);
                }
            }

            return favoredResults.Distinct().ToList();
        }

        /// <summary>
        /// Selects a contextual modification based on the dice roll and context
        /// Looks for modifications whose diceResults match the rolled value
        /// </summary>
        public ModificationTableEntry? SelectContextualModification(LootContext? context, int diceRoll)
        {
            if (context == null)
                return null;

            var candidates = new List<(ModificationTableEntry mod, int weight)>();

            // Check theme modifications
            if (!string.IsNullOrEmpty(context.DungeonTheme) &&
                _tables.ThemeModifications.TryGetValue(context.DungeonTheme, out var themeMods))
            {
                foreach (var mod in themeMods)
                {
                    if (mod.DiceResults.Contains(diceRoll))
                    {
                        candidates.Add((mod, mod.Weight));
                    }
                }
            }

            // Check archetype modifications
            if (!string.IsNullOrEmpty(context.EnemyArchetype) &&
                _tables.ArchetypeModifications.TryGetValue(context.EnemyArchetype, out var archetypeMods))
            {
                foreach (var mod in archetypeMods)
                {
                    if (mod.DiceResults.Contains(diceRoll))
                    {
                        candidates.Add((mod, mod.Weight));
                    }
                }
            }

            // Return weighted random selection from candidates
            if (candidates.Count == 0)
                return null;

            return SelectByWeight(candidates);
        }

        /// <summary>
        /// Gets all available modifications from the provided context
        /// Used for listing available modifications in a theme/archetype
        /// </summary>
        public List<ModificationTableEntry> GetContextualModifications(LootContext? context)
        {
            if (context == null)
                return new List<ModificationTableEntry>();

            var mods = new List<ModificationTableEntry>();

            if (!string.IsNullOrEmpty(context.DungeonTheme) &&
                _tables.ThemeModifications.TryGetValue(context.DungeonTheme, out var themeMods))
            {
                mods.AddRange(themeMods);
            }

            if (!string.IsNullOrEmpty(context.EnemyArchetype) &&
                _tables.ArchetypeModifications.TryGetValue(context.EnemyArchetype, out var archetypeMods))
            {
                mods.AddRange(archetypeMods);
            }

            return mods;
        }

        /// <summary>
        /// Weighted random selection from a list of candidates
        /// Higher weight = higher probability of selection
        /// </summary>
        private ModificationTableEntry SelectByWeight(List<(ModificationTableEntry mod, int weight)> candidates)
        {
            int totalWeight = candidates.Sum(x => x.weight);
            int roll = _random.Next(totalWeight);

            int accumulated = 0;
            foreach (var (mod, weight) in candidates)
            {
                accumulated += weight;
                if (roll < accumulated)
                {
                    return mod;
                }
            }

            // Fallback (shouldn't happen)
            return candidates[0].mod;
        }
    }
}
