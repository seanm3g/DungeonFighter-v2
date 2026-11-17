namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Manages all game logging, narrative output, and event tracking.
    /// This manager centralizes narrative/logging that was previously scattered throughout Game.cs.
    /// 
    /// Responsibilities:
    /// - Build and maintain dungeon event log
    /// - Track room-specific information
    /// - Format narrative output for display
    /// - Manage event messages and descriptions
    /// - Support clearing logs and resetting narrative state
    /// 
    /// Design: Immutable list returns to prevent external modification
    /// Usage: LogDungeonEvent() to add events, GetFormattedLog() to retrieve
    /// </summary>
    public class GameNarrativeManager
    {
        private List<string> dungeonLog = new();
        private List<string> dungeonHeaderInfo = new();
        private List<string> currentRoomInfo = new();

        /// <summary>
        /// Gets a copy of the dungeon event log.
        /// Returns immutable copy to prevent external modifications.
        /// </summary>
        public List<string> DungeonLog => new List<string>(dungeonLog);

        /// <summary>
        /// Gets a copy of the dungeon header information.
        /// Contains header/title information for the current dungeon.
        /// </summary>
        public List<string> DungeonHeaderInfo => new List<string>(dungeonHeaderInfo);

        /// <summary>
        /// Gets a copy of the current room information.
        /// Contains details about the current room/encounter.
        /// </summary>
        public List<string> CurrentRoomInfo => new List<string>(currentRoomInfo);

        /// <summary>
        /// Gets the total number of events logged in current dungeon.
        /// </summary>
        public int EventCount => dungeonLog.Count;

        /// <summary>
        /// Gets whether the log has any events.
        /// </summary>
        public bool HasEvents => dungeonLog.Count > 0;

        /// <summary>
        /// Logs a single event message to the dungeon event log.
        /// Ignores null or empty messages.
        /// </summary>
        /// <param name="message">The event message to log.</param>
        public void LogDungeonEvent(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                dungeonLog.Add(message);
            }
        }

        /// <summary>
        /// Sets the dungeon header information (e.g., dungeon name, level, theme).
        /// This typically shows at the beginning of a dungeon run.
        /// </summary>
        /// <param name="info">List of header information lines.</param>
        public void SetDungeonHeaderInfo(List<string> info)
        {
            dungeonHeaderInfo = new List<string>(info ?? new List<string>());
        }

        /// <summary>
        /// Sets the current room information (e.g., room name, enemies, environment).
        /// This typically shows when entering a new room.
        /// </summary>
        /// <param name="info">List of room information lines.</param>
        public void SetRoomInfo(List<string> info)
        {
            currentRoomInfo = new List<string>(info ?? new List<string>());
        }

        /// <summary>
        /// Clears the dungeon event log but keeps header and room info.
        /// Used when transitioning between rooms.
        /// </summary>
        public void ClearDungeonLog()
        {
            dungeonLog.Clear();
        }

        /// <summary>
        /// Clears the current room information.
        /// Used when leaving a room.
        /// </summary>
        public void ClearRoomInfo()
        {
            currentRoomInfo.Clear();
        }

        /// <summary>
        /// Clears the dungeon header information.
        /// Used when leaving a dungeon.
        /// </summary>
        public void ClearDungeonHeaderInfo()
        {
            dungeonHeaderInfo.Clear();
        }

        /// <summary>
        /// Gets the formatted event log as a single string.
        /// Each event is on a separate line.
        /// </summary>
        /// <returns>Formatted log string, or empty string if no events.</returns>
        public string GetFormattedLog()
        {
            if (dungeonLog.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var entry in dungeonLog)
            {
                sb.AppendLine(entry);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Resets all narrative state (log, header, room info).
        /// Used when starting a new dungeon or new game.
        /// </summary>
        public void ResetNarrative()
        {
            ClearDungeonLog();
            ClearRoomInfo();
            ClearDungeonHeaderInfo();
        }

        /// <summary>
        /// Gets a summary of the current narrative state for debugging.
        /// </summary>
        /// <returns>Formatted string showing event count and info summary.</returns>
        public override string ToString()
        {
            return $"Narrative: {EventCount} events, " +
                   $"Header: {dungeonHeaderInfo.Count} lines, " +
                   $"Room: {currentRoomInfo.Count} lines";
        }
    }
}

