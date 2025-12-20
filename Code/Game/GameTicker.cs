using System;
using System.Threading;
using System.Threading.Tasks;

namespace RPGGame
{
    /// <summary>
    /// Handles game timing and ticker system
    /// </summary>
    public class GameTicker
    {
        private static GameTicker? _instance;
        private static readonly object _lock = new object();

        private double _gameTime = 0.0;
        private double _lastRealTime = 0.0;
        private bool _isRunning = false;
        private Task? _tickerTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public double GameTime => _gameTime;
        public bool IsRunning => _isRunning;
        
        public static GameTicker Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GameTicker();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private GameTicker()
        {
            _lastRealTime = DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
        }
        
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            // Use Task.Run for async execution instead of Thread
            _tickerTask = Task.Run(async () => await TickerLoopAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            if (_tickerTask != null)
            {
                try
                {
                    if (!_tickerTask.Wait(TimeSpan.FromSeconds(2)))
                    {
                        // Task didn't complete in time, but we've already cancelled it
                        Utils.ScrollDebugLogger.Log("GameTicker: Warning - ticker task did not stop within timeout period");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token is cancelled
                }
                catch (Exception ex)
                {
                    Utils.ScrollDebugLogger.Log($"GameTicker: Error stopping ticker: {ex.Message}");
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        
        public void Reset()
        {
            _gameTime = 0.0;
            _lastRealTime = DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
        }
        
        private async Task TickerLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    var config = GameConfiguration.Instance.GameSpeed;
                    var tickerInterval = config.GameTickerInterval;
                    var speedMultiplier = config.GameSpeedMultiplier;

                    // Calculate real time elapsed
                    double currentRealTime = DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
                    double realTimeElapsed = currentRealTime - _lastRealTime;

                    // Convert to game time using speed multiplier
                    double gameTimeElapsed = realTimeElapsed * speedMultiplier;
                    _gameTime += gameTimeElapsed;
                    _lastRealTime = currentRealTime;

                    // Delay for the ticker interval (non-blocking)
                    int delayMs = (int)(tickerInterval * 1000);
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
        
        /// <summary>
        /// Gets the current game time, updating it if needed
        /// </summary>
        public double GetCurrentGameTime()
        {
            if (_isRunning)
            {
                // Update game time based on real time elapsed
                double currentRealTime = DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
                double realTimeElapsed = currentRealTime - _lastRealTime;
                double gameTimeElapsed = realTimeElapsed * GameConfiguration.Instance.GameSpeed.GameSpeedMultiplier;
                _gameTime += gameTimeElapsed;
                _lastRealTime = currentRealTime;
            }
            
            return _gameTime;
        }
        
        /// <summary>
        /// Advances game time by a specific amount (for testing or manual control)
        /// </summary>
        public void AdvanceGameTime(double timeAmount)
        {
            _gameTime += timeAmount;
        }
    }
}
