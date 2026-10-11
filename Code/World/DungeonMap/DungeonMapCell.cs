namespace RPGGame.World.DungeonMap
{
    public enum DungeonMapCellKind
    {
        Wall,
        Floor,
        Content
    }

    /// <summary>
    /// One cell on the automated dungeon traversal grid.
    /// </summary>
    public sealed class DungeonMapCell
    {
        public DungeonMapCellKind Kind { get; set; } = DungeonMapCellKind.Wall;

        /// <summary>Attached content room when <see cref="Kind"/> is <see cref="DungeonMapCellKind.Content"/>.</summary>
        public Environment? Room { get; set; }

        /// <summary>Index into <see cref="Dungeon.Rooms"/> when this is a content cell; otherwise -1.</summary>
        public int ContentRoomIndex { get; set; } = -1;

        public bool Visited { get; set; }
        public bool Revealed { get; set; }

        public bool IsWalkable =>
            Kind == DungeonMapCellKind.Floor || Kind == DungeonMapCellKind.Content;
    }
}
