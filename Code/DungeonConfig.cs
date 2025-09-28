namespace RPGGame
{
    public class DungeonConfig
    {
        public List<string> dungeonThemes { get; set; } = new();
        public List<string> roomThemes { get; set; } = new();
        public DungeonGenerationConfig dungeonGeneration { get; set; } = new();
    }

    public class DungeonGenerationConfig
    {
        public int minRooms { get; set; } = 2;
        public double roomCountScaling { get; set; } = 0.5;
        public double hostileRoomChance { get; set; } = 0.8;
        public string bossRoomName { get; set; } = "Boss";
    }
}
