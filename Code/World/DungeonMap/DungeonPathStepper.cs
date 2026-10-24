using System;
using System.Collections.Generic;

namespace RPGGame.World.DungeonMap
{
    public interface IDungeonPathStepper
    {
        /// <summary>
        /// Chooses the next walkable cell toward the target (or a wander step for Random).
        /// Returns false if no move is possible.
        /// </summary>
        bool TryGetNextStep(DungeonTraversalState state, int targetX, int targetY, out int nextX, out int nextY);
    }

    public static class DungeonPathStepperFactory
    {
        public static IDungeonPathStepper Create(TraversalStrategy strategy, Random? rng = null) =>
            strategy == TraversalStrategy.Smart
                ? new SmartPathStepper()
                : new RandomPathStepper(rng ?? RandomUtility.Ambient);
    }

    /// <summary>
    /// Greedy BFS: pick the neighbor that minimizes distance to the target.
    /// </summary>
    public sealed class SmartPathStepper : IDungeonPathStepper
    {
        public bool TryGetNextStep(DungeonTraversalState state, int targetX, int targetY, out int nextX, out int nextY)
        {
            nextX = state.HeroX;
            nextY = state.HeroY;

            if (state.HeroX == targetX && state.HeroY == targetY)
                return false;

            var distances = BfsDistances(state, targetX, targetY);
            int bestDist = int.MaxValue;
            int bestX = -1, bestY = -1;

            foreach (var (nx, ny) in GetWalkableNeighbors(state, state.HeroX, state.HeroY))
            {
                int key = Pack(nx, ny, state.Width);
                if (!distances.TryGetValue(key, out int dist))
                    continue;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestX = nx;
                    bestY = ny;
                }
            }

            if (bestX < 0)
                return false;

            nextX = bestX;
            nextY = bestY;
            return true;
        }

        internal static Dictionary<int, int> BfsDistances(DungeonTraversalState state, int goalX, int goalY)
        {
            var dist = new Dictionary<int, int>();
            var q = new Queue<(int X, int Y)>();
            q.Enqueue((goalX, goalY));
            dist[Pack(goalX, goalY, state.Width)] = 0;

            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                int d = dist[Pack(x, y, state.Width)];
                foreach (var (nx, ny) in GetWalkableNeighbors(state, x, y))
                {
                    int key = Pack(nx, ny, state.Width);
                    if (dist.ContainsKey(key))
                        continue;
                    dist[key] = d + 1;
                    q.Enqueue((nx, ny));
                }
            }

            return dist;
        }

        internal static IEnumerable<(int X, int Y)> GetWalkableNeighbors(DungeonTraversalState state, int x, int y)
        {
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (state.InBounds(nx, ny) && state.GetCell(nx, ny).IsWalkable)
                    yield return (nx, ny);
            }
        }

        internal static int Pack(int x, int y, int width) => y * width + x;
    }

    /// <summary>
    /// Random walk preferring not to reverse the last step; still BFS-progresses if stuck in a spur.
    /// </summary>
    public sealed class RandomPathStepper : IDungeonPathStepper
    {
        private readonly Random _rng;

        public RandomPathStepper(Random? rng = null)
        {
            _rng = rng ?? RandomUtility.Ambient;
        }

        public bool TryGetNextStep(DungeonTraversalState state, int targetX, int targetY, out int nextX, out int nextY)
        {
            nextX = state.HeroX;
            nextY = state.HeroY;

            if (state.HeroX == targetX && state.HeroY == targetY)
                return false;

            var neighbors = new List<(int X, int Y)>(SmartPathStepper.GetWalkableNeighbors(state, state.HeroX, state.HeroY));
            if (neighbors.Count == 0)
                return false;

            // Prefer not reversing
            var preferred = new List<(int X, int Y)>();
            foreach (var n in neighbors)
            {
                if (n.X == state.PreviousHeroX && n.Y == state.PreviousHeroY)
                    continue;
                preferred.Add(n);
            }

            var pool = preferred.Count > 0 ? preferred : neighbors;

            // Bias slightly toward target so Random still eventually arrives (70% random, 30% smarter)
            if (_rng.NextDouble() < 0.30)
            {
                var distances = SmartPathStepper.BfsDistances(state, targetX, targetY);
                int bestDist = int.MaxValue;
                int bestX = -1, bestY = -1;
                foreach (var (nx, ny) in pool)
                {
                    int key = SmartPathStepper.Pack(nx, ny, state.Width);
                    if (!distances.TryGetValue(key, out int dist))
                        continue;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestX = nx;
                        bestY = ny;
                    }
                }

                if (bestX >= 0)
                {
                    nextX = bestX;
                    nextY = bestY;
                    return true;
                }
            }

            var pick = pool[_rng.Next(pool.Count)];
            nextX = pick.X;
            nextY = pick.Y;
            return true;
        }
    }
}
