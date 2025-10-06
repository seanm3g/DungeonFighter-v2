using System;

namespace RPGGame
{
    /// <summary>
    /// Centralized random number generation utility
    /// Eliminates duplication of Random instances across the codebase
    /// </summary>
    public static class RandomUtility
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Gets a random integer between min (inclusive) and max (exclusive)
        /// </summary>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>A random integer</returns>
        public static int Next(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Gets a random integer between 0 (inclusive) and max (exclusive)
        /// </summary>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>A random integer</returns>
        public static int Next(int max)
        {
            return _random.Next(max);
        }

        /// <summary>
        /// Gets a random double between 0.0 and 1.0
        /// </summary>
        /// <returns>A random double</returns>
        public static double NextDouble()
        {
            return _random.NextDouble();
        }

        /// <summary>
        /// Gets a random double between min and max
        /// </summary>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>A random double</returns>
        public static double NextDouble(double min, double max)
        {
            return min + (_random.NextDouble() * (max - min));
        }

        /// <summary>
        /// Selects a random element from an array
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="array">The array to select from</param>
        /// <returns>A random element from the array</returns>
        public static T NextElement<T>(T[] array)
        {
            if (array == null || array.Length == 0)
                throw new ArgumentException("Array cannot be null or empty");
            
            return array[_random.Next(array.Length)];
        }

        /// <summary>
        /// Selects a random element from a list
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="list">The list to select from</param>
        /// <returns>A random element from the list</returns>
        public static T NextElement<T>(System.Collections.Generic.List<T> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List cannot be null or empty");
            
            return list[_random.Next(list.Count)];
        }

        /// <summary>
        /// Selects a random element from a collection
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="collection">The collection to select from</param>
        /// <returns>A random element from the collection</returns>
        public static T NextElement<T>(System.Collections.Generic.IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentException("Collection cannot be null");
            
            var list = collection.ToList();
            if (list.Count == 0)
                throw new ArgumentException("Collection cannot be empty");
            
            return list[_random.Next(list.Count)];
        }

        /// <summary>
        /// Generates a random boolean value
        /// </summary>
        /// <returns>A random boolean</returns>
        public static bool NextBool()
        {
            return _random.Next(2) == 1;
        }

        /// <summary>
        /// Generates a random boolean with a specified probability
        /// </summary>
        /// <param name="probability">Probability of returning true (0.0 to 1.0)</param>
        /// <returns>A random boolean</returns>
        public static bool NextBool(double probability)
        {
            return _random.NextDouble() < probability;
        }

        /// <summary>
        /// Shuffles an array in place using Fisher-Yates algorithm
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="array">The array to shuffle</param>
        public static void Shuffle<T>(T[] array)
        {
            if (array == null || array.Length <= 1)
                return;
            
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        /// <summary>
        /// Shuffles a list in place using Fisher-Yates algorithm
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="list">The list to shuffle</param>
        public static void Shuffle<T>(System.Collections.Generic.List<T> list)
        {
            if (list == null || list.Count <= 1)
                return;
            
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Gets the shared Random instance for advanced usage
        /// </summary>
        /// <returns>The shared Random instance</returns>
        public static Random GetSharedRandom()
        {
            return _random;
        }
    }
}
