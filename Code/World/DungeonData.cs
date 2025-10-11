using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Data class for dungeon information loaded from JSON
    /// </summary>
    public class DungeonData
    {
        public string name { get; set; } = "";
        public string theme { get; set; } = "";
        public int minLevel { get; set; }
        public int maxLevel { get; set; }
        public List<string> possibleEnemies { get; set; } = new List<string>();
    }
}
