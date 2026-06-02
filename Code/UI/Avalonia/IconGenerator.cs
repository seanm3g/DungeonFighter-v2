using System;
using System.IO;

namespace DungeonFighter.UI.Avalonia
{
    /// <summary>
    /// Generates a dungeon-themed icon for the application window.
    /// Supports creating and loading custom icons.
    /// </summary>
    public static class IconGenerator
    {
        /// <summary>
        /// Gets the path to the dungeon icon SVG file.
        /// Creates it if it doesn't exist.
        /// </summary>
        public static string? GetOrCreateIconPath()
        {
            string[] possiblePaths = new[]
            {
                "Code/UI/Avalonia/Assets/dungeon_icon.svg",
                "../Code/UI/Avalonia/Assets/dungeon_icon.svg",
                "UI/Avalonia/Assets/dungeon_icon.svg",
                "./Assets/dungeon_icon.svg"
            };

            // Check if any existing path works
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            // If not found, try to create it in the most likely location
            try
            {
                string assetDir = Path.Combine("Code", "UI", "Avalonia", "Assets");
                Directory.CreateDirectory(assetDir);
                
                string iconPath = Path.Combine(assetDir, "dungeon_icon.svg");
                if (!File.Exists(iconPath))
                {
                    File.WriteAllText(iconPath, GetDefaultSVGIcon());
                }
                
                return Path.GetFullPath(iconPath);
            }
            catch
            {
                // If we can't create it, just return null
                return null;
            }
        }

        /// <summary>
        /// Returns the default SVG icon as a string.
        /// </summary>
        private static string GetDefaultSVGIcon()
        {
            return @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 256 256"" width=""256"" height=""256"">
  <rect width=""256"" height=""256"" fill=""#3c3c3c""/>
  <rect x=""20"" y=""20"" width=""216"" height=""216"" fill=""#512bd4""/>
  <text x=""128"" y=""148"" font-family=""Segoe UI, Arial, sans-serif"" font-size=""96"" font-weight=""700"" fill=""#ffffff"" text-anchor=""middle"">DF</text>
</svg>";
        }
    }
}
