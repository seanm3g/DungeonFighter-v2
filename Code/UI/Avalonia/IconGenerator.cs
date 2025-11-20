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
  <!-- Background -->
  <rect width=""256"" height=""256"" fill=""#1a1a2e""/>
  
  <!-- Stone brick pattern background -->
  <rect x=""10"" y=""10"" width=""236"" height=""236"" fill=""none"" stroke=""#444444"" stroke-width=""2""/>
  <line x1=""80"" y1=""10"" x2=""80"" y2=""246"" stroke=""#444444"" stroke-width=""1"" opacity=""0.5""/>
  <line x1=""150"" y1=""10"" x2=""150"" y2=""246"" stroke=""#444444"" stroke-width=""1"" opacity=""0.5""/>
  <line x1=""10"" y1=""80"" x2=""246"" y2=""80"" stroke=""#444444"" stroke-width=""1"" opacity=""0.5""/>
  <line x1=""10"" y1=""150"" x2=""246"" y2=""150"" stroke=""#444444"" stroke-width=""1"" opacity=""0.5""/>
  
  <!-- Sword -->
  <g>
    <!-- Blade (sharp point up) -->
    <polygon points=""128,40 145,100 111,100"" fill=""#e0e0e0"" stroke=""#ffffff"" stroke-width=""2""/>
    
    <!-- Blade shine -->
    <polygon points=""128,50 138,95 118,95"" fill=""#ffffff"" opacity=""0.6""/>
    
    <!-- Guard -->
    <rect x=""105"" y=""105"" width=""46"" height=""15"" fill=""#ffd700"" stroke=""#ffed4e"" stroke-width=""1""/>
    
    <!-- Handle -->
    <rect x=""118"" y=""120"" width=""20"" height=""50"" fill=""#8b4513"" stroke=""#654321"" stroke-width=""1""/>
    
    <!-- Handle pattern (leather wrapping) -->
    <line x1=""118"" y1=""127"" x2=""138"" y2=""127"" stroke=""#654321"" stroke-width=""1""/>
    <line x1=""118"" y1=""135"" x2=""138"" y2=""135"" stroke=""#654321"" stroke-width=""1""/>
    <line x1=""118"" y1=""143"" x2=""138"" y2=""143"" stroke=""#654321"" stroke-width=""1""/>
    <line x1=""118"" y1=""151"" x2=""138"" y2=""151"" stroke=""#654321"" stroke-width=""1""/>
    
    <!-- Pommel -->
    <circle cx=""128"" cy=""175"" r=""10"" fill=""#ffd700"" stroke=""#ffed4e"" stroke-width=""1""/>
  </g>
  
  <!-- Shield (background) -->
  <g opacity=""0.7"">
    <path d=""M 60 100 L 60 160 Q 60 190 85 210 L 85 100 Z"" fill=""#c41e3a"" stroke=""#8b0000"" stroke-width=""2""/>
    <path d=""M 70 110 L 78 150"" stroke=""#8b0000"" stroke-width=""1"" opacity=""0.5""/>
  </g>
  
  <!-- Dungeon Stone accent -->
  <g opacity=""0.6"">
    <path d=""M 195 120 L 225 140 L 205 170 L 175 150 Z"" fill=""#666666"" stroke=""#888888"" stroke-width=""1""/>
  </g>
  
  <!-- Glow effect around sword -->
  <circle cx=""128"" cy=""100"" r=""45"" fill=""none"" stroke=""#00ff00"" stroke-width=""1"" opacity=""0.3""/>
</svg>";
        }
    }
}
