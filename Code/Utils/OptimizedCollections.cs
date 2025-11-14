using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Utils
{
    /// <summary>
    /// Optimized collection operations to reduce allocations and improve performance
    /// </summary>
    public static class OptimizedCollections
    {
        /// <summary>
        /// Efficiently filters a list without creating intermediate collections
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="predicate">Filter predicate</param>
        /// <returns>Filtered list</returns>
        public static List<T> FilterToList<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            var result = new List<T>();
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Efficiently maps a list without creating intermediate collections
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <typeparam name="U">Target type</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="selector">Mapping function</param>
        /// <returns>Mapped list</returns>
        public static List<U> MapToList<T, U>(IEnumerable<T> source, Func<T, U> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            
            var result = new List<U>();
            foreach (var item in source)
            {
                result.Add(selector(item));
            }
            return result;
        }
        
        /// <summary>
        /// Efficiently finds the first item matching a predicate
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="predicate">Search predicate</param>
        /// <returns>First matching item or default</returns>
        public static T? FindFirst<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return item;
                }
            }
            return default;
        }
        
        /// <summary>
        /// Efficiently counts items matching a predicate
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="predicate">Count predicate</param>
        /// <returns>Count of matching items</returns>
        public static int CountWhere<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            int count = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Efficiently checks if any item matches a predicate
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="predicate">Check predicate</param>
        /// <returns>True if any item matches</returns>
        public static bool Any<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Efficiently checks if all items match a predicate
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="predicate">Check predicate</param>
        /// <returns>True if all items match</returns>
        public static bool All<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            foreach (var item in source)
            {
                if (!predicate(item))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Efficiently sums values from a collection
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="selector">Value selector</param>
        /// <returns>Sum of selected values</returns>
        public static int Sum<T>(IEnumerable<T> source, Func<T, int> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            
            int sum = 0;
            foreach (var item in source)
            {
                sum += selector(item);
            }
            return sum;
        }
        
        /// <summary>
        /// Efficiently sums double values from a collection
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="selector">Value selector</param>
        /// <returns>Sum of selected values</returns>
        public static double Sum<T>(IEnumerable<T> source, Func<T, double> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            
            double sum = 0.0;
            foreach (var item in source)
            {
                sum += selector(item);
            }
            return sum;
        }
        
        /// <summary>
        /// Efficiently finds the maximum value from a collection
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="selector">Value selector</param>
        /// <returns>Maximum value</returns>
        public static int Max<T>(IEnumerable<T> source, Func<T, int> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            
            bool hasValue = false;
            int max = 0;
            
            foreach (var item in source)
            {
                int value = selector(item);
                if (!hasValue || value > max)
                {
                    max = value;
                    hasValue = true;
                }
            }
            
            return hasValue ? max : throw new InvalidOperationException("Sequence contains no elements");
        }
        
        /// <summary>
        /// Efficiently finds the minimum value from a collection
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="selector">Value selector</param>
        /// <returns>Minimum value</returns>
        public static int Min<T>(IEnumerable<T> source, Func<T, int> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            
            bool hasValue = false;
            int min = 0;
            
            foreach (var item in source)
            {
                int value = selector(item);
                if (!hasValue || value < min)
                {
                    min = value;
                    hasValue = true;
                }
            }
            
            return hasValue ? min : throw new InvalidOperationException("Sequence contains no elements");
        }
        
        /// <summary>
        /// Efficiently creates a dictionary from a collection
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <typeparam name="TKey">Type of keys</typeparam>
        /// <typeparam name="TValue">Type of values</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="valueSelector">Value selector</param>
        /// <returns>Dictionary</returns>
        public static Dictionary<TKey, TValue> ToDictionary<T, TKey, TValue>(
            IEnumerable<T> source, 
            Func<T, TKey> keySelector, 
            Func<T, TValue> valueSelector) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
            
            var dictionary = new Dictionary<TKey, TValue>();
            foreach (var item in source)
            {
                dictionary[keySelector(item)] = valueSelector(item);
            }
            return dictionary;
        }
        
        /// <summary>
        /// Efficiently groups items by a key
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <typeparam name="TKey">Type of keys</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="keySelector">Key selector</param>
        /// <returns>Grouped dictionary</returns>
        public static Dictionary<TKey, List<T>> GroupBy<T, TKey>(
            IEnumerable<T> source, 
            Func<T, TKey> keySelector) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            
            var groups = new Dictionary<TKey, List<T>>();
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new List<T>();
                    groups[key] = group;
                }
                group.Add(item);
            }
            return groups;
        }
    }
}
