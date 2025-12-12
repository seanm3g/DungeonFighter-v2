using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// Data class for color override configuration
    /// </summary>
    public class ColorOverride
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = ""; // "template", "palette", "colorCode", "rgb"
        
        [JsonPropertyName("value")]
        public string? Value { get; set; } // Template name, palette name, color code, or hex
        
        [JsonPropertyName("rgb")]
        public int[]? Rgb { get; set; } // Direct RGB values [r, g, b] if type is "rgb"
    }
    
    /// <summary>
    /// Data class for dungeon information loaded from JSON
    /// </summary>
    public class DungeonData
    {
        [JsonPropertyName("name")]
        public string name { get; set; } = "";
        [JsonPropertyName("theme")]
        public string theme { get; set; } = "";
        [JsonPropertyName("minLevel")]
        public int minLevel { get; set; }
        [JsonPropertyName("maxLevel")]
        public int maxLevel { get; set; }
        [JsonPropertyName("possibleEnemies")]
        public List<string> possibleEnemies { get; set; } = new List<string>();
        [JsonPropertyName("colorOverride")]
        public ColorOverride? colorOverride { get; set; }
    }
}
