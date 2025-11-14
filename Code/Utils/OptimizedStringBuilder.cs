using System;
using System.Text;

namespace RPGGame.Utils
{
    /// <summary>
    /// Optimized string builder for high-performance text operations
    /// Provides object pooling and efficient string concatenation
    /// </summary>
    public class OptimizedStringBuilder : IDisposable
    {
        private static readonly ObjectPool<StringBuilder> _stringBuilderPool = new ObjectPool<StringBuilder>(
            createFunc: () => new StringBuilder(256),
            resetAction: sb => sb.Clear()
        );
        
        private readonly StringBuilder _stringBuilder;
        private bool _disposed = false;
        
        /// <summary>
        /// Gets the current length of the string builder
        /// </summary>
        public int Length => _stringBuilder.Length;
        
        /// <summary>
        /// Gets the current capacity of the string builder
        /// </summary>
        public int Capacity => _stringBuilder.Capacity;
        
        /// <summary>
        /// Initializes a new instance of OptimizedStringBuilder
        /// </summary>
        public OptimizedStringBuilder()
        {
            _stringBuilder = _stringBuilderPool.Get();
        }
        
        /// <summary>
        /// Initializes a new instance with initial capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        public OptimizedStringBuilder(int capacity)
        {
            _stringBuilder = _stringBuilderPool.Get();
            if (_stringBuilder.Capacity < capacity)
            {
                _stringBuilder.Capacity = capacity;
            }
        }
        
        /// <summary>
        /// Appends a string to the builder
        /// </summary>
        /// <param name="value">String to append</param>
        /// <returns>This instance for chaining</returns>
        public OptimizedStringBuilder Append(string? value)
        {
            if (value != null)
            {
                _stringBuilder.Append(value);
            }
            return this;
        }
        
        /// <summary>
        /// Appends a character to the builder
        /// </summary>
        /// <param name="value">Character to append</param>
        /// <returns>This instance for chaining</returns>
        public OptimizedStringBuilder Append(char value)
        {
            _stringBuilder.Append(value);
            return this;
        }
        
        /// <summary>
        /// Appends an integer to the builder
        /// </summary>
        /// <param name="value">Integer to append</param>
        /// <returns>This instance for chaining</returns>
        public OptimizedStringBuilder Append(int value)
        {
            _stringBuilder.Append(value);
            return this;
        }
        
        /// <summary>
        /// Appends a formatted string to the builder
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Format arguments</param>
        /// <returns>This instance for chaining</returns>
        public OptimizedStringBuilder AppendFormat(string format, params object[] args)
        {
            _stringBuilder.AppendFormat(format, args);
            return this;
        }
        
        /// <summary>
        /// Appends a line to the builder
        /// </summary>
        /// <param name="value">String to append as a line</param>
        /// <returns>This instance for chaining</returns>
        public OptimizedStringBuilder AppendLine(string? value = null)
        {
            if (value != null)
            {
                _stringBuilder.AppendLine(value);
            }
            else
            {
                _stringBuilder.AppendLine();
            }
            return this;
        }
        
        /// <summary>
        /// Clears the string builder
        /// </summary>
        /// <returns>This instance for chaining</returns>
        public OptimizedStringBuilder Clear()
        {
            _stringBuilder.Clear();
            return this;
        }
        
        /// <summary>
        /// Converts the string builder to a string
        /// </summary>
        /// <returns>The built string</returns>
        public override string ToString()
        {
            return _stringBuilder.ToString();
        }
        
        /// <summary>
        /// Disposes the string builder and returns it to the pool
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _stringBuilderPool.Return(_stringBuilder);
                _disposed = true;
            }
        }
        
        /// <summary>
        /// Simple object pool implementation
        /// </summary>
        private class ObjectPool<T> where T : class
        {
            private readonly Queue<T> _objects = new Queue<T>();
            private readonly Func<T> _createFunc;
            private readonly Action<T> _resetAction;
            private readonly object _lock = new object();
            
            public ObjectPool(Func<T> createFunc, Action<T> resetAction)
            {
                _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
                _resetAction = resetAction ?? throw new ArgumentNullException(nameof(resetAction));
            }
            
            public T Get()
            {
                lock (_lock)
                {
                    if (_objects.Count > 0)
                    {
                        return _objects.Dequeue();
                    }
                }
                
                return _createFunc();
            }
            
            public void Return(T obj)
            {
                if (obj == null) return;
                
                _resetAction(obj);
                
                lock (_lock)
                {
                    _objects.Enqueue(obj);
                }
            }
        }
    }
}
