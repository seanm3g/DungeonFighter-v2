using System;
using System.Collections.Generic;

namespace RPGGame.World.DungeonMap
{
    /// <summary>
    /// Runtime traversal state for one generated dungeon run.
    /// </summary>
    public sealed class DungeonTraversalState
    {
        public DungeonTraversalState(
            DungeonMapCell[,] cells,
            int width,
            int height,
            int startX,
            int startY,
            int exitX,
            int exitY,
            IReadOnlyList<(int X, int Y)> contentPositions)
        {
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            Width = width;
            Height = height;
            StartX = startX;
            StartY = startY;
            ExitX = exitX;
            ExitY = exitY;
            ContentPositions = contentPositions ?? throw new ArgumentNullException(nameof(contentPositions));
            HeroX = startX;
            HeroY = startY;
            RevealAround(HeroX, HeroY);
            MarkVisited(HeroX, HeroY);
        }

        public DungeonMapCell[,] Cells { get; }
        public int Width { get; }
        public int Height { get; }
        public int StartX { get; }
        public int StartY { get; }
        public int ExitX { get; }
        public int ExitY { get; }
        public IReadOnlyList<(int X, int Y)> ContentPositions { get; }

        public int HeroX { get; set; }
        public int HeroY { get; set; }

        /// <summary>Index of the next content room the navigator should walk to / process.</summary>
        public int NextContentIndex { get; set; }

        public TraversalStrategy CurrentStrategy { get; set; } = TraversalStrategy.Random;

        /// <summary>Cell marked on hostile/trap approach contact; cleared after room combat.</summary>
        public (int X, int Y)? InteractionCell { get; set; }

        /// <summary>Previous hero cell for Random non-backtrack preference.</summary>
        public int PreviousHeroX { get; set; } = -1;

        public int PreviousHeroY { get; set; } = -1;

        public bool HasMoreContent => NextContentIndex < ContentPositions.Count;

        public DungeonMapCell GetCell(int x, int y) => Cells[x, y];

        public bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height;

        public bool TryGetNextContentTarget(out int x, out int y)
        {
            if (!HasMoreContent)
            {
                x = 0;
                y = 0;
                return false;
            }

            (x, y) = ContentPositions[NextContentIndex];
            return true;
        }

        public void MoveHeroTo(int x, int y)
        {
            PreviousHeroX = HeroX;
            PreviousHeroY = HeroY;
            HeroX = x;
            HeroY = y;
            MarkVisited(x, y);
            RevealAround(x, y);
        }

        public void MarkVisited(int x, int y)
        {
            if (!InBounds(x, y)) return;
            var cell = Cells[x, y];
            cell.Visited = true;
            cell.Revealed = true;
        }

        public void RevealAround(int x, int y, int radius = 1)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (InBounds(nx, ny))
                        Cells[nx, ny].Revealed = true;
                }
            }
        }

        public void SetInteractionAtHero()
        {
            InteractionCell = (HeroX, HeroY);
        }

        public void ClearInteraction()
        {
            InteractionCell = null;
        }
    }
}
