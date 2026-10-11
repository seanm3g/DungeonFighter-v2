using System;
using System.Collections.Generic;

namespace RPGGame.World.DungeonMap
{
    /// <summary>
    /// Builds a compact walkable grid from an ordered content-room list.
    /// </summary>
    public static class DungeonMapGenerator
    {
        public const int MinSize = 5;
        public const int MaxSize = 11;
        public const int TrainingGroundSize = 3;

        public static DungeonTraversalState Generate(IReadOnlyList<Environment> rooms, Random? rng = null, bool trainingGround = false)
        {
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (rooms.Count == 0)
                throw new ArgumentException("At least one content room is required.", nameof(rooms));

            rng ??= new Random();

            if (trainingGround)
                return GenerateTrainingGround(rooms[0]);

            if (rooms.Count == 1)
                return GenerateSingleRoom(rooms[0]);

            return GenerateMultiRoom(rooms, rng);
        }

        /// <summary>Training Ground: 3×3 with center content; hero starts on the content cell.</summary>
        public static DungeonTraversalState GenerateTrainingGround(Environment room)
        {
            int size = TrainingGroundSize;
            var cells = CreateWallGrid(size, size);
            int cx = size / 2;
            int cy = size / 2;
            CarveFloor(cells, cx, cy);
            cells[cx, cy].Kind = DungeonMapCellKind.Content;
            cells[cx, cy].Room = room;
            cells[cx, cy].ContentRoomIndex = 0;

            // Small plus corridors so the map is not a lone cell
            CarveFloor(cells, cx - 1, cy);
            CarveFloor(cells, cx + 1, cy);
            CarveFloor(cells, cx, cy - 1);
            CarveFloor(cells, cx, cy + 1);

            var content = new List<(int X, int Y)> { (cx, cy) };
            return new DungeonTraversalState(cells, size, size, cx, cy, cx + 1, cy, content);
        }

        private static DungeonTraversalState GenerateSingleRoom(Environment room)
        {
            int size = MinSize;
            var cells = CreateWallGrid(size, size);
            var path = BuildSerpentinePath(size, size);
            if (path.Count < 3)
                throw new InvalidOperationException("Failed to carve a walkable path for a single-room map.");

            foreach (var (x, y) in path)
                CarveFloor(cells, x, y);

            int contentIdx = path.Count / 2;
            var (cx, cy) = path[contentIdx];
            cells[cx, cy].Kind = DungeonMapCellKind.Content;
            cells[cx, cy].Room = room;
            cells[cx, cy].ContentRoomIndex = 0;

            var (sx, sy) = path[0];
            var (ex, ey) = path[path.Count - 1];
            var content = new List<(int X, int Y)> { (cx, cy) };
            return new DungeonTraversalState(cells, size, size, sx, sy, ex, ey, content);
        }

        private static DungeonTraversalState GenerateMultiRoom(IReadOnlyList<Environment> rooms, Random rng)
        {
            int roomCount = rooms.Count;
            int size = Math.Clamp(2 * roomCount + 1, MinSize, MaxSize);
            // Prefer odd size so serpentine has a clean center
            if (size % 2 == 0)
                size = Math.Min(MaxSize, size + 1);

            var cells = CreateWallGrid(size, size);
            var path = BuildSerpentinePath(size, size);
            if (path.Count < roomCount + 2)
            {
                // Expand until path can host rooms + start/exit padding
                size = MaxSize;
                cells = CreateWallGrid(size, size);
                path = BuildSerpentinePath(size, size);
            }

            foreach (var (x, y) in path)
                CarveFloor(cells, x, y);

            // Optional spur dead-ends for visual interest (Random strategy may wander into them)
            AddRandomSpurs(cells, path, rng, spurCount: Math.Min(3, roomCount));

            var contentPositions = new List<(int X, int Y)>(roomCount);
            int usable = path.Count - 2; // leave path[0] start and path[^1] exit as floor
            if (usable < roomCount)
            {
                // Place on as many distinct path cells as possible after start
                for (int i = 0; i < roomCount; i++)
                {
                    int pi = Math.Min(path.Count - 1, 1 + i);
                    PlaceContent(cells, path[pi].X, path[pi].Y, rooms[i], i);
                    contentPositions.Add(path[pi]);
                }
            }
            else
            {
                for (int i = 0; i < roomCount; i++)
                {
                    int pi = 1 + (int)Math.Round(i * (usable - 1) / (double)Math.Max(1, roomCount - 1));
                    pi = Math.Clamp(pi, 1, path.Count - 2);
                    var (x, y) = path[pi];
                    // Avoid stacking two rooms on the same cell
                    if (cells[x, y].Kind == DungeonMapCellKind.Content)
                    {
                        for (int j = pi + 1; j <= path.Count - 2; j++)
                        {
                            if (cells[path[j].X, path[j].Y].Kind != DungeonMapCellKind.Content)
                            {
                                (x, y) = path[j];
                                break;
                            }
                        }
                    }

                    PlaceContent(cells, x, y, rooms[i], i);
                    contentPositions.Add((x, y));
                }
            }

            var (sx, sy) = path[0];
            var (ex, ey) = path[path.Count - 1];
            return new DungeonTraversalState(cells, size, size, sx, sy, ex, ey, contentPositions);
        }

        private static void PlaceContent(DungeonMapCell[,] cells, int x, int y, Environment room, int index)
        {
            cells[x, y].Kind = DungeonMapCellKind.Content;
            cells[x, y].Room = room;
            cells[x, y].ContentRoomIndex = index;
        }

        private static DungeonMapCell[,] CreateWallGrid(int width, int height)
        {
            var cells = new DungeonMapCell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    cells[x, y] = new DungeonMapCell { Kind = DungeonMapCellKind.Wall };
            }
            return cells;
        }

        private static void CarveFloor(DungeonMapCell[,] cells, int x, int y)
        {
            if (x < 0 || y < 0 || x >= cells.GetLength(0) || y >= cells.GetLength(1))
                return;
            if (cells[x, y].Kind == DungeonMapCellKind.Content)
                return;
            cells[x, y].Kind = DungeonMapCellKind.Floor;
        }

        /// <summary>
        /// Interior serpentine: odd rows left→right, even rows right→left, vertical links between rows.
        /// </summary>
        public static List<(int X, int Y)> BuildSerpentinePath(int width, int height)
        {
            var path = new List<(int X, int Y)>();
            int min = 1;
            int maxX = width - 2;
            int maxY = height - 2;
            if (maxX < min || maxY < min)
            {
                // Degenerate tiny grid: center only
                path.Add((width / 2, height / 2));
                return path;
            }

            for (int y = min; y <= maxY; y++)
            {
                bool leftToRight = ((y - min) % 2) == 0;
                if (leftToRight)
                {
                    for (int x = min; x <= maxX; x++)
                        path.Add((x, y));
                }
                else
                {
                    for (int x = maxX; x >= min; x--)
                        path.Add((x, y));
                }
            }

            return path;
        }

        private static void AddRandomSpurs(DungeonMapCell[,] cells, List<(int X, int Y)> path, Random rng, int spurCount)
        {
            int w = cells.GetLength(0);
            int h = cells.GetLength(1);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            for (int i = 0; i < spurCount && path.Count > 0; i++)
            {
                var (px, py) = path[rng.Next(path.Count)];
                int dir = rng.Next(4);
                int nx = px + dx[dir];
                int ny = py + dy[dir];
                if (nx <= 0 || ny <= 0 || nx >= w - 1 || ny >= h - 1)
                    continue;
                if (cells[nx, ny].Kind == DungeonMapCellKind.Wall)
                    CarveFloor(cells, nx, ny);
            }
        }
    }
}
