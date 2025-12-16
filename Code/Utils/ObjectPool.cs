using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RPGGame.Utils
{
    /// <summary>
    /// Generic object pool for frequently created objects to reduce allocations
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentQueue<T> _pool = new ConcurrentQueue<T>();
        private readonly Func<T> _factory;
        private readonly Action<T>? _resetAction;
        private readonly int _maxSize;

        /// <summary>
        /// Creates a new object pool
        /// </summary>
        /// <param name="factory">Factory function to create new objects (default: new T())</param>
        /// <param name="resetAction">Action to reset objects before returning to pool</param>
        /// <param name="maxSize">Maximum pool size (default: 100)</param>
        public ObjectPool(Func<T>? factory = null, Action<T>? resetAction = null, int maxSize = 100)
        {
            _factory = factory ?? (() => new T());
            _resetAction = resetAction;
            _maxSize = maxSize;
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one
        /// </summary>
        public T Get()
        {
            if (_pool.TryDequeue(out var item))
            {
                return item;
            }

            return _factory();
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;

            // Reset the object if reset action is provided
            _resetAction?.Invoke(item);

            // Only add to pool if under max size
            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(item);
            }
        }

        /// <summary>
        /// Gets the current pool size
        /// </summary>
        public int Count => _pool.Count;

        /// <summary>
        /// Clears the pool
        /// </summary>
        public void Clear()
        {
            while (_pool.TryDequeue(out _)) { }
        }
    }

    /// <summary>
    /// Object pool for ColoredText segments
    /// </summary>
    public static class ColoredTextPool
    {
        private static readonly ObjectPool<List<RPGGame.UI.ColorSystem.ColoredText>> _segmentPool = 
            new ObjectPool<List<RPGGame.UI.ColorSystem.ColoredText>>(
                factory: () => new List<RPGGame.UI.ColorSystem.ColoredText>(),
                resetAction: list => list.Clear(),
                maxSize: 200
            );

        /// <summary>
        /// Gets a list of ColoredText segments from the pool
        /// </summary>
        public static List<RPGGame.UI.ColorSystem.ColoredText> Get()
        {
            return _segmentPool.Get();
        }

        /// <summary>
        /// Returns a list of ColoredText segments to the pool
        /// </summary>
        public static void Return(List<RPGGame.UI.ColorSystem.ColoredText> segments)
        {
            _segmentPool.Return(segments);
        }
    }

    /// <summary>
    /// Object pool for damage calculation results
    /// </summary>
    public static class DamageResultPool
    {
        private static readonly ObjectPool<Dictionary<(RPGGame.Actor, RPGGame.Action?, double, double, int), int>> _cachePool = 
            new ObjectPool<Dictionary<(RPGGame.Actor, RPGGame.Action?, double, double, int), int>>(
                factory: () => new Dictionary<(RPGGame.Actor, RPGGame.Action?, double, double, int), int>(),
                resetAction: dict => dict.Clear(),
                maxSize: 10
            );

        /// <summary>
        /// Gets a damage cache dictionary from the pool
        /// </summary>
        public static Dictionary<(RPGGame.Actor, RPGGame.Action?, double, double, int), int> Get()
        {
            return _cachePool.Get();
        }

        /// <summary>
        /// Returns a damage cache dictionary to the pool
        /// </summary>
        public static void Return(Dictionary<(RPGGame.Actor, RPGGame.Action?, double, double, int), int> cache)
        {
            _cachePool.Return(cache);
        }
    }
}
