namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages background dungeon runs for multiple characters.
    /// Allows dungeons to continue running while the player switches characters or returns to main menu.
    /// </summary>
    public class BackgroundDungeonTaskManager
    {
        private readonly Dictionary<string, Task> _activeDungeonTasks = new Dictionary<string, Task>();
        private readonly Dictionary<string, CancellationTokenSource> _activeCancellation = new Dictionary<string, CancellationTokenSource>();
        private readonly Dictionary<string, DungeonRunInfo> _dungeonRunInfo = new Dictionary<string, DungeonRunInfo>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Information about an active dungeon run
        /// </summary>
        public class DungeonRunInfo
        {
            public string CharacterId { get; set; } = string.Empty;
            public string CharacterName { get; set; } = string.Empty;
            public string DungeonName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public bool IsComplete { get; set; }
            public bool WasSuccessful { get; set; }
            public bool WasCancelled { get; set; }
            public string? CompletionMessage { get; set; }
        }

        /// <summary>
        /// Event fired when a background dungeon completes
        /// </summary>
        public event Action<string, DungeonRunInfo>? DungeonCompleted;

        /// <summary>
        /// Start a dungeon run in the background for a character.
        /// The factory receives a cancellation token that is cancelled if another run starts for the same character
        /// or when <see cref="CancelDungeon"/> is called.
        /// </summary>
        public void StartBackgroundDungeon(
            string characterId,
            string characterName,
            string dungeonName,
            Func<CancellationToken, Task> dungeonRunTask)
        {
            if (string.IsNullOrEmpty(characterId))
                throw new ArgumentException("Character ID cannot be null or empty", nameof(characterId));
            if (dungeonRunTask == null)
                throw new ArgumentNullException(nameof(dungeonRunTask));

            CancellationTokenSource cts;
            DungeonRunInfo runInfo;
            lock (_lockObject)
            {
                CancelExistingLocked(characterId);

                cts = new CancellationTokenSource();
                _activeCancellation[characterId] = cts;

                runInfo = new DungeonRunInfo
                {
                    CharacterId = characterId,
                    CharacterName = characterName,
                    DungeonName = dungeonName,
                    StartTime = DateTime.UtcNow,
                    IsComplete = false
                };

                _dungeonRunInfo[characterId] = runInfo;

                var token = cts.Token;
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await dungeonRunTask(token);
                        token.ThrowIfCancellationRequested();
                        runInfo.IsComplete = true;
                        runInfo.WasSuccessful = true;
                        runInfo.CompletionMessage = $"{characterName} completed {dungeonName}";
                    }
                    catch (OperationCanceledException)
                    {
                        runInfo.IsComplete = true;
                        runInfo.WasSuccessful = false;
                        runInfo.WasCancelled = true;
                        runInfo.CompletionMessage = $"{characterName}'s run in {dungeonName} was cancelled";
                    }
                    catch (Exception ex)
                    {
                        runInfo.IsComplete = true;
                        runInfo.WasSuccessful = false;
                        runInfo.CompletionMessage = $"{characterName} failed in {dungeonName}: {ex.Message}";
                    }
                    finally
                    {
                        DungeonCompleted?.Invoke(characterId, runInfo);

                        await Task.Delay(5000);
                        lock (_lockObject)
                        {
                            if (_dungeonRunInfo.TryGetValue(characterId, out var info) && ReferenceEquals(info, runInfo))
                            {
                                _activeDungeonTasks.Remove(characterId);
                                _dungeonRunInfo.Remove(characterId);
                                if (_activeCancellation.TryGetValue(characterId, out var storedCts) && ReferenceEquals(storedCts, cts))
                                {
                                    _activeCancellation.Remove(characterId);
                                    storedCts.Dispose();
                                }
                            }
                        }
                    }
                }, CancellationToken.None);

                _activeDungeonTasks[characterId] = task;
            }
        }

        /// <summary>
        /// Backward-compatible overload without cancellation. Prefer the token-aware overload.
        /// </summary>
        public void StartBackgroundDungeon(
            string characterId,
            string characterName,
            string dungeonName,
            Func<Task> dungeonRunTask)
        {
            StartBackgroundDungeon(characterId, characterName, dungeonName, _ => dungeonRunTask());
        }

        /// <summary>
        /// Cancel an active dungeon run for a character (if any).
        /// </summary>
        public void CancelDungeon(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            lock (_lockObject)
            {
                CancelExistingLocked(characterId);
            }
        }

        private void CancelExistingLocked(string characterId)
        {
            if (_activeCancellation.TryGetValue(characterId, out var existingCts))
            {
                try
                {
                    existingCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed
                }

                _activeCancellation.Remove(characterId);
                existingCts.Dispose();
            }
        }

        /// <summary>
        /// Check if a character has an active dungeon run
        /// </summary>
        public bool HasActiveDungeon(string characterId)
        {
            lock (_lockObject)
            {
                return _activeDungeonTasks.ContainsKey(characterId) &&
                       _dungeonRunInfo.ContainsKey(characterId) &&
                       !_dungeonRunInfo[characterId].IsComplete;
            }
        }

        /// <summary>
        /// Get information about an active dungeon run
        /// </summary>
        public DungeonRunInfo? GetDungeonRunInfo(string characterId)
        {
            lock (_lockObject)
            {
                return _dungeonRunInfo.TryGetValue(characterId, out var info) ? info : null;
            }
        }

        /// <summary>
        /// Get all active dungeon runs
        /// </summary>
        public List<DungeonRunInfo> GetAllActiveDungeonRuns()
        {
            lock (_lockObject)
            {
                return _dungeonRunInfo.Values
                    .Where(info => !info.IsComplete)
                    .ToList();
            }
        }

        /// <summary>
        /// Get all characters with active dungeon runs
        /// </summary>
        public List<string> GetCharactersWithActiveDungeons()
        {
            lock (_lockObject)
            {
                return _dungeonRunInfo.Values
                    .Where(info => !info.IsComplete)
                    .Select(info => info.CharacterId)
                    .ToList();
            }
        }

        /// <summary>
        /// Wait for a character's dungeon to complete (for testing/debugging)
        /// </summary>
        public async Task WaitForDungeonCompletion(string characterId, int timeoutMs = 300000)
        {
            Task? task;
            lock (_lockObject)
            {
                if (!_activeDungeonTasks.TryGetValue(characterId, out task))
                    return;
            }

            try
            {
                await Task.WhenAny(task, Task.Delay(timeoutMs));
            }
            catch
            {
                // Ignore exceptions
            }
        }
    }
}
