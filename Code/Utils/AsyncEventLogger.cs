using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RPGGame.Utils
{
    /// <summary>
    /// Asynchronous event logger with background queue and batched writes
    /// Replaces synchronous file I/O to prevent blocking operations
    /// </summary>
    public class AsyncEventLogger : IDisposable
    {
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _backgroundTask;
        private readonly object _lockObject = new object();
        private readonly string _logFilePath;
        private readonly int _batchSize;
        private readonly int _batchTimeoutMs;
        private DateTime _lastWriteTime = DateTime.Now;
        private bool _disposed = false;

        /// <summary>
        /// Creates a new AsyncEventLogger
        /// </summary>
        /// <param name="logFilePath">Path to the log file</param>
        /// <param name="batchSize">Number of log entries to batch before writing (default: 10)</param>
        /// <param name="batchTimeoutMs">Maximum time to wait before flushing batch (default: 1000ms)</param>
        public AsyncEventLogger(string logFilePath, int batchSize = 10, int batchTimeoutMs = 1000)
        {
            _logFilePath = logFilePath;
            _batchSize = batchSize;
            _batchTimeoutMs = batchTimeoutMs;
            
            // Ensure directory exists
            string? directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Start background processing task
            _backgroundTask = Task.Run(async () => await ProcessLogQueueAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Logs a message asynchronously (non-blocking)
        /// </summary>
        public void LogAsync(string message)
        {
            if (_disposed)
                return;
                
            _logQueue.Enqueue(message);
        }

        /// <summary>
        /// Logs a message synchronously for backwards compatibility (still uses async queue)
        /// </summary>
        public void Log(string message)
        {
            LogAsync(message);
        }

        /// <summary>
        /// Processes the log queue in the background
        /// </summary>
        private async Task ProcessLogQueueAsync(CancellationToken cancellationToken)
        {
            var batch = new System.Collections.Generic.List<string>();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Collect messages from queue
                    while (_logQueue.TryDequeue(out string? message) && batch.Count < _batchSize)
                    {
                        if (message != null)
                        {
                            batch.Add(message);
                        }
                    }
                    
                    // Check if we should flush the batch
                    bool shouldFlush = false;
                    if (batch.Count >= _batchSize)
                    {
                        shouldFlush = true;
                    }
                    else if (batch.Count > 0 && (DateTime.Now - _lastWriteTime).TotalMilliseconds >= _batchTimeoutMs)
                    {
                        shouldFlush = true;
                    }
                    
                    // Write batch if needed
                    if (shouldFlush)
                    {
                        await WriteBatchAsync(batch);
                        batch.Clear();
                        _lastWriteTime = DateTime.Now;
                    }
                    
                    // Small delay to prevent tight loop when queue is empty
                    if (batch.Count == 0 && _logQueue.IsEmpty)
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(1, cancellationToken); // Small delay to allow batching
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception)
                {
                    // Ignore errors - logging should not crash the application
                    // Small delay before retrying
                    await Task.Delay(100, cancellationToken);
                }
            }
            
            // Flush remaining messages on shutdown
            if (batch.Count > 0)
            {
                await WriteBatchAsync(batch);
            }
        }

        /// <summary>
        /// Writes a batch of log messages to the file
        /// </summary>
        private async Task WriteBatchAsync(System.Collections.Generic.List<string> batch)
        {
            if (batch.Count == 0)
                return;
                
            try
            {
                // Use async file I/O
                using (var writer = new StreamWriter(_logFilePath, append: true))
                {
                    foreach (var message in batch)
                    {
                        await writer.WriteLineAsync(message);
                    }
                    await writer.FlushAsync();
                }
            }
            catch
            {
                // Ignore file write errors - logging should not crash the application
            }
        }

        /// <summary>
        /// Flushes all pending log messages (waits for queue to empty)
        /// </summary>
        public async Task FlushAsync()
        {
            // Wait for queue to empty (with timeout)
            int waitCount = 0;
            while (!_logQueue.IsEmpty && waitCount < 100)
            {
                await Task.Delay(10);
                waitCount++;
            }
            
            // Give background task a moment to process
            await Task.Delay(100);
        }

        /// <summary>
        /// Synchronous flush for backwards compatibility
        /// </summary>
        public void Flush()
        {
            Task.Run(async () => await FlushAsync()).Wait(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Disposes the logger and flushes remaining messages
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            // Signal cancellation
            _cancellationTokenSource.Cancel();
            
            // Wait for background task to complete (with timeout)
            try
            {
                _backgroundTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore timeout errors
            }
            
            // Flush any remaining messages
            Flush();
            
            _cancellationTokenSource.Dispose();
        }
    }
}

