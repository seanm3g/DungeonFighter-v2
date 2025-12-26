namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Manages background dungeon runs for multiple characters.
    /// Allows dungeons to continue running while the player switches characters or returns to main menu.
    /// </summary>
    public class BackgroundDungeonTaskManager
    {
        private readonly Dictionary<string, Task> _activeDungeonTasks = new Dictionary<string, Task>();
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
            public string? CompletionMessage { get; set; }
        }

        /// <summary>
        /// Event fired when a background dungeon completes
        /// </summary>
        public event Action<string, DungeonRunInfo>? DungeonCompleted;

        /// <summary>
        /// Start a dungeon run in the background for a character
        /// </summary>
        public void StartBackgroundDungeon(
            string characterId,
            string characterName,
            string dungeonName,
            Func<Task> dungeonRunTask)
        {
            if (string.IsNullOrEmpty(characterId))
                throw new ArgumentException("Character ID cannot be null or empty", nameof(characterId));

            lock (_lockObject)
            {
                // Cancel any existing dungeon run for this character
                if (_activeDungeonTasks.ContainsKey(characterId))
                {
                    // Note: We can't cancel a running Task easily, but we can track it
                    // The old task will complete naturally
                }

                var runInfo = new DungeonRunInfo
                {
                    CharacterId = characterId,
                    CharacterName = characterName,
                    DungeonName = dungeonName,
                    StartTime = DateTime.UtcNow,
                    IsComplete = false
                };

                _dungeonRunInfo[characterId] = runInfo;

                // Start the dungeon run as a background task
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await dungeonRunTask();
                        runInfo.IsComplete = true;
                        runInfo.WasSuccessful = true;
                        runInfo.CompletionMessage = $"{characterName} completed {dungeonName}";
                    }
                    catch (Exception ex)
                    {
                        runInfo.IsComplete = true;
                        runInfo.WasSuccessful = false;
                        runInfo.CompletionMessage = $"{characterName} failed in {dungeonName}: {ex.Message}";
                    }
                    finally
                    {
                        // Fire completion event
                        DungeonCompleted?.Invoke(characterId, runInfo);
                        
                        // Clean up after a delay to allow UI to read the completion info
                        await Task.Delay(5000);
                        lock (_lockObject)
                        {
                            _activeDungeonTasks.Remove(characterId);
                            _dungeonRunInfo.Remove(characterId);
                        }
                    }
                });

                _activeDungeonTasks[characterId] = task;
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
            if (!_activeDungeonTasks.TryGetValue(characterId, out var task))
                return;

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

