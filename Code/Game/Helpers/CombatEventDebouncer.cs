using System;
using System.Threading;
using Avalonia.Threading;
using RPGGame.UI.Avalonia;

namespace RPGGame.GameCore.Helpers
{
    /// <summary>
    /// Handles debouncing of combat event UI updates to prevent flickering
    /// </summary>
    public class CombatEventDebouncer
    {
        private readonly int minRefreshIntervalMs;
        private DateTime lastRefreshTime = DateTime.MinValue;
        private Timer? debounceTimer;
        private readonly object refreshLock = new object();
        private readonly System.Action refreshCallback;

        public CombatEventDebouncer(int minRefreshIntervalMs, System.Action refreshCallback)
        {
            this.minRefreshIntervalMs = minRefreshIntervalMs;
            this.refreshCallback = refreshCallback ?? throw new ArgumentNullException(nameof(refreshCallback));
        }

        /// <summary>
        /// Triggers a refresh with debouncing
        /// </summary>
        public void TriggerRefresh()
        {
            lock (refreshLock)
            {
                var now = DateTime.Now;
                var timeSinceLastRefresh = (now - lastRefreshTime).TotalMilliseconds;

                if (timeSinceLastRefresh >= minRefreshIntervalMs)
                {
                    lastRefreshTime = now;
                    debounceTimer?.Dispose();
                    debounceTimer = null;
                    Dispatcher.UIThread.Post(refreshCallback);
                }
                else
                {
                    var delayMs = minRefreshIntervalMs - (int)timeSinceLastRefresh;
                    debounceTimer?.Dispose();
                    debounceTimer = new Timer(_ =>
                    {
                        lock (refreshLock)
                        {
                            lastRefreshTime = DateTime.Now;
                            debounceTimer?.Dispose();
                            debounceTimer = null;
                            Dispatcher.UIThread.Post(refreshCallback);
                        }
                    }, null, delayMs, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public void Dispose()
        {
            lock (refreshLock)
            {
                debounceTimer?.Dispose();
                debounceTimer = null;
            }
        }
    }
}

