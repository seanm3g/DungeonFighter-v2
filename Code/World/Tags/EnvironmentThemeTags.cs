using System;
using System.Collections.Generic;

namespace RPGGame.World.Tags
{
    /// <summary>Default environment match tags when room data has no authored <c>tags</c>.</summary>
    public static class EnvironmentThemeTags
    {
        public static IReadOnlyList<string> GetFallbackForTheme(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
                return Array.Empty<string>();

            return theme.Trim().ToLowerInvariant() switch
            {
                "lava" => new[] { "fire", "scorched" },
                "forest" => new[] { "overgrown" },
                "ice" => new[] { "exposed" },
                "crypt" => new[] { "exposed" },
                "cavern" => new[] { "earth", "exposed" },
                "desert" => new[] { "earth", "scorched" },
                "swamp" => new[] { "water", "flooded" },
                "ocean" => new[] { "water", "flooded" },
                _ => Array.Empty<string>()
            };
        }
    }
}
