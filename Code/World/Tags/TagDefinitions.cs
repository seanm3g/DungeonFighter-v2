using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.World.Tags
{
    public enum TagLayer
    {
        PoolGate,
        Match,
        FieldDuplicate,
        Discouraged
    }

    [Flags]
    public enum TagEntityScope
    {
        None = 0,
        Action = 1,
        Item = 2,
        Enemy = 4,
        Environment = 8,
        Hero = 16,
        AllEntities = Action | Item | Enemy | Environment | Hero
    }

    /// <summary>Canonical tag registry: layers, entity scopes, enemy archetypes, and validation helpers.</summary>
    public static class TagDefinitions
    {
        public static readonly IReadOnlyList<string> ValidEnemyArchetypes = new[]
        {
            "Knight", "Assassin", "Berserker", "Acrobat", "Brute",
            "Warlord", "Sage", "Duelist", "Trickster"
        };

        private static readonly HashSet<string> ValidEnemyArchetypeSet =
            new(ValidEnemyArchetypes, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, TagDefinition> Catalog =
            BuildCatalog();

        private static readonly HashSet<string> AllRegistryTagNames =
            new(Catalog.Keys, StringComparer.OrdinalIgnoreCase);

        public static IEnumerable<string> AllRegistryTags => Catalog.Keys.OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

        public static bool IsKnownTag(string? tag) =>
            !string.IsNullOrWhiteSpace(tag) && AllRegistryTagNames.Contains(tag.Trim());

        public static bool IsValidEnemyArchetype(string? archetype) =>
            !string.IsNullOrWhiteSpace(archetype) && ValidEnemyArchetypeSet.Contains(archetype.Trim());

        public static string? CanonicalizeEnemyArchetype(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var trimmed = raw.Trim();
            foreach (var valid in ValidEnemyArchetypes)
            {
                if (string.Equals(valid, trimmed, StringComparison.OrdinalIgnoreCase))
                    return valid;
            }
            return trimmed;
        }

        public static TagLayer? GetLayer(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;
            return Catalog.TryGetValue(NormalizeKey(tag), out var def) ? def.Layer : null;
        }

        public static TagEntityScope GetScope(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return TagEntityScope.None;
            return Catalog.TryGetValue(NormalizeKey(tag), out var def) ? def.Scope : TagEntityScope.None;
        }

        public static bool IsAllowedOn(TagEntityScope scope, string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return true;
            if (!Catalog.TryGetValue(NormalizeKey(tag), out var def))
                return false;
            return (def.Scope & scope) != 0;
        }

        /// <summary>Returns human-readable validation messages (warnings).</summary>
        public static List<string> ValidateTagList(TagEntityScope scope, IEnumerable<string>? tags)
        {
            var messages = new List<string>();
            if (tags == null)
                return messages;

            foreach (var raw in tags)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    messages.Add("Tag list contains an empty entry.");
                    continue;
                }

                var tag = raw.Trim();
                if (!IsKnownTag(tag))
                {
                    messages.Add($"Unknown tag '{tag}'.");
                    continue;
                }

                if (!IsAllowedOn(scope, tag))
                    messages.Add($"Tag '{tag}' is not allowed on this entity type.");
            }

            return messages;
        }

        public static bool TryParseEnemyArchetype(string? raw, out EnemyArchetype archetype)
        {
            archetype = EnemyArchetype.Berserker;
            var canon = CanonicalizeEnemyArchetype(raw);
            if (canon == null || !IsValidEnemyArchetype(canon))
                return false;

            archetype = canon switch
            {
                "Knight" => EnemyArchetype.Knight,
                "Assassin" => EnemyArchetype.Assassin,
                "Berserker" => EnemyArchetype.Berserker,
                "Acrobat" => EnemyArchetype.Acrobat,
                "Brute" => EnemyArchetype.Brute,
                "Warlord" => EnemyArchetype.Warlord,
                "Sage" => EnemyArchetype.Sage,
                "Duelist" => EnemyArchetype.Duelist,
                "Trickster" => EnemyArchetype.Trickster,
                _ => EnemyArchetype.Berserker
            };
            return true;
        }

        private static string NormalizeKey(string tag) => tag.Trim().ToLowerInvariant();

        private static Dictionary<string, TagDefinition> BuildCatalog()
        {
            var list = new List<TagDefinition>();
            void Add(string tag, TagLayer layer, TagEntityScope scope) =>
                list.Add(new TagDefinition(tag, layer, scope));

            foreach (var t in new[] { "environment", "enemy", "weapon", "class", "item", "action", "unique", "starter" })
                Add(t, TagLayer.PoolGate, TagEntityScope.Action | TagEntityScope.Item);
            foreach (var t in new[] { "warrior", "barbarian", "rogue", "wizard" })
                Add(t, TagLayer.PoolGate, TagEntityScope.Action | TagEntityScope.Hero | TagEntityScope.Item);
            foreach (var t in new[] { "sword", "mace", "dagger", "wand" })
                Add(t, TagLayer.PoolGate, TagEntityScope.Action | TagEntityScope.Item);
            foreach (var t in new[] { "common", "uncommon", "rare", "epic", "legendary", "mythic" })
                Add(t, TagLayer.FieldDuplicate, TagEntityScope.Action);

            foreach (var t in new[] { "fire", "earth", "water", "air" })
                Add(t, TagLayer.Match, TagEntityScope.Action | TagEntityScope.Item | TagEntityScope.Enemy | TagEntityScope.Environment);
            foreach (var t in new[] { "scorched", "flooded", "overgrown", "exposed" })
                Add(t, TagLayer.Match, TagEntityScope.Environment | TagEntityScope.Action | TagEntityScope.Enemy);
            foreach (var t in new[] { "elegant", "dilapidated" })
                Add(t, TagLayer.Match, TagEntityScope.Environment | TagEntityScope.Action);
            foreach (var t in new[] { "dormant", "cycling", "active" })
                Add(t, TagLayer.Match, TagEntityScope.Environment);
            foreach (var t in new[] { "living", "undead", "plant", "elemental", "celestial" })
                Add(t, TagLayer.Match, TagEntityScope.Enemy | TagEntityScope.Item);
            foreach (var t in new[] { "giant", "young", "tiny", "frail", "has_hands", "has_feet", "has_legs", "has_head" })
                Add(t, TagLayer.Match, TagEntityScope.Enemy);
            foreach (var t in new[] { "boss", "minion" })
                Add(t, TagLayer.Match, TagEntityScope.Enemy);
            foreach (var t in new[]
                     {
                         "bone", "bronze", "glass", "willow", "steel", "gold", "obsidian", "silver",
                         "damascus", "mithril", "shadow", "crystal", "stone", "unknown", "strange"
                     })
                Add(t, TagLayer.Match, TagEntityScope.Item);
            foreach (var t in new[] { "required", "opener", "finisher" })
                Add(t, TagLayer.Match, TagEntityScope.Action);
            foreach (var t in new[] { "swift", "bludgeon", "focus", "insight" })
                Add(t, TagLayer.Match, TagEntityScope.Action);
            foreach (var t in new[] { "confidence", "footwork", "target", "aim" })
                Add(t, TagLayer.Match, TagEntityScope.Action);

            Add("modtrade", TagLayer.Discouraged, TagEntityScope.Action | TagEntityScope.Item);

            return list.ToDictionary(d => d.NormalizedName, d => d, StringComparer.OrdinalIgnoreCase);
        }

        public readonly struct TagDefinition
        {
            public TagDefinition(string name, TagLayer layer, TagEntityScope scope)
            {
                Name = name;
                NormalizedName = name.Trim().ToLowerInvariant();
                Layer = layer;
                Scope = scope;
            }

            public string Name { get; }
            public string NormalizedName { get; }
            public TagLayer Layer { get; }
            public TagEntityScope Scope { get; }
        }
    }
}
