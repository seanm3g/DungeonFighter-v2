using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Resolves which FlavorText location / room-context banks to use for a room.
    /// Room.Theme is the dungeon theme, which is the wrong key when a catalog room
    /// (e.g. Abyssal Depths) appears inside a differently themed dungeon.
    /// </summary>
    public static class FlavorLocationResolver
    {
        /// <summary>
        /// Room-type tokens checked against the display name, most specific first.
        /// Values are <c>roomContexts</c> keys (lowercase).
        /// </summary>
        private static readonly (string Needle, string RoomType)[] RoomTypeNameTokens =
        {
            ("boss", "boss"),
            ("treasure", "treasure"),
            ("library", "library"),
            ("study", "library"),
            ("archive", "library"),
            ("armory", "armory"),
            ("kitchen", "kitchen"),
            ("dining", "dining"),
            ("shrine", "shrine"),
            ("altar", "shrine"),
            ("sanctum", "sanctum"),
            ("observatory", "observatory"),
            ("laboratory", "laboratory"),
            ("vault", "vault"),
            ("guard", "guard"),
            ("trap", "trap"),
            ("puzzle", "puzzle"),
            ("rest", "rest"),
            ("storage", "storage"),
            ("hall", "hall"),
            ("grotto", "grotto"),
            ("catacomb", "catacomb"),
            ("throne", "throne"),
            ("chamber", "chamber")
        };

        /// <summary>
        /// Display-name aliases mapped to <c>locationDescriptions</c> keys.
        /// Same substring style as <see cref="TauntSystem.GetLocationType"/>, extended
        /// to the FlavorText bank vocabulary. No Ocean bank exists; water/ocean names
        /// use Swamp (the water-themed location bank).
        /// </summary>
        private static readonly (string Needle, string Theme)[] LocationNameAliases =
        {
            ("graveyard", "Graveyard"),
            ("frozen", "Ice"),
            ("glacial", "Ice"),
            ("ice", "Ice"),
            ("bog", "Swamp"),
            ("marsh", "Swamp"),
            ("swamp", "Swamp"),
            // Placeholder mapping: no Ocean/Underwater locationDescriptions bank exists yet.
            // These should route to that dedicated bank once authored — change only the
            // theme string on these five entries (e.g. "Swamp" → "Ocean"). No resolver logic changes.
            ("abyssal", "Swamp"),
            ("coral", "Swamp"),
            ("underwater", "Swamp"),
            ("ocean", "Swamp"),
            ("sea", "Swamp"),
            ("lava", "Lava"),
            ("volcano", "Lava"),
            ("volcanic", "Lava"),
            ("magma", "Lava"),
            ("fire", "Lava"),
            ("crypt", "Crypt"),
            ("tomb", "Crypt"),
            ("grave", "Crypt"),
            ("burial", "Crypt"),
            ("crystal", "Crystal"),
            ("geode", "Crystal"),
            ("forest", "Forest"),
            ("grove", "Forest"),
            ("meadow", "Forest"),
            ("desert", "Desert"),
            ("dune", "Desert"),
            ("oasis", "Desert"),
            ("cavern", "Cavern"),
            ("cave", "Cavern"),
            ("tunnel", "Cavern"),
            ("crumbling", "Ruins"),
            ("decayed", "Ruins"),
            ("ruin", "Ruins"),
            ("temple", "Temple"),
            ("sanctuary", "Temple"),
            ("altar", "Temple"),
            ("castle", "Castle")
        };

        /// <summary>
        /// Resolves the locationDescriptions / roomContexts theme for a room.
        /// Order: exact tag→bank key, then display-name match, then dungeon theme.
        /// </summary>
        public static string ResolveLocationTheme(Environment room)
        {
            if (room == null)
                return "Generic";
            return ResolveLocationTheme(room.Name, room.Tags, room.Theme);
        }

        public static string ResolveLocationTheme(string? roomName, IEnumerable<string>? tags, string? dungeonTheme)
        {
            var keys = GetLocationDescriptionKeys();

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    string? fromTag = FindKey(keys, tag);
                    if (fromTag != null)
                        return fromTag;
                }
            }

            string? fromName = ResolveThemeFromName(roomName, keys);
            if (fromName != null)
                return fromName;

            string? fromDungeon = FindKey(keys, dungeonTheme);
            if (fromDungeon != null)
                return fromDungeon;

            return string.IsNullOrWhiteSpace(dungeonTheme) ? "Generic" : dungeonTheme.Trim();
        }

        /// <summary>
        /// Resolves the roomContexts room-type key. Prefers <see cref="Environment.RoomType"/>
        /// when set; otherwise infers from the display name (boss before chamber, etc.).
        /// </summary>
        public static string ResolveRoomType(Environment room)
        {
            if (room == null)
                return "chamber";
            return ResolveRoomType(room.RoomType, room.Name);
        }

        public static string ResolveRoomType(string? authoredRoomType, string? roomName)
        {
            if (!string.IsNullOrWhiteSpace(authoredRoomType))
                return authoredRoomType.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(roomName))
                return "chamber";

            string lower = roomName.ToLowerInvariant();
            foreach (var (needle, roomType) in RoomTypeNameTokens)
            {
                if (lower.Contains(needle))
                    return roomType;
            }

            return "chamber";
        }

        private static string? ResolveThemeFromName(string? roomName, IReadOnlyList<string> keys)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                return null;

            string lower = roomName.ToLowerInvariant();

            foreach (var (needle, theme) in LocationNameAliases)
            {
                if (lower.Contains(needle))
                    return theme;
            }

            foreach (var key in keys.OrderByDescending(k => k.Length))
            {
                if (lower.Contains(key.ToLowerInvariant()))
                    return key;
            }

            return null;
        }

        private static IReadOnlyList<string> GetLocationDescriptionKeys()
        {
            var data = FlavorText.GetData();
            if (data?.Environments?.LocationDescriptions == null || data.Environments.LocationDescriptions.Count == 0)
                return Array.Empty<string>();
            return data.Environments.LocationDescriptions.Keys.ToList();
        }

        private static string? FindKey(IReadOnlyList<string> keys, string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate) || keys.Count == 0)
                return null;
            return keys.FirstOrDefault(k => string.Equals(k, candidate.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
