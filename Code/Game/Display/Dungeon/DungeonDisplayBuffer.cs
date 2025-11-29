using System.Collections.Generic;

namespace RPGGame.Display.Dungeon
{
    /// <summary>
    /// Manages display buffer for dungeon information
    /// Tracks dungeon header, room info, enemy info, and combat log
    /// </summary>
    public class DungeonDisplayBuffer
    {
        // Display sections (in order of display)
        private readonly List<string> dungeonHeader = new List<string>();
        private readonly List<string> roomInfo = new List<string>();
        private readonly List<string> enemyInfo = new List<string>();
        private readonly List<string> combatLog = new List<string>();

        /// <summary>
        /// Gets the complete display log (all sections combined in order)
        /// </summary>
        public List<string> CompleteDisplayLog
        {
            get
            {
                var log = new List<string>();
                log.AddRange(dungeonHeader);
                log.AddRange(roomInfo);
                log.AddRange(enemyInfo);
                log.AddRange(combatLog);
                return log;
            }
        }

        /// <summary>
        /// Gets just the combat log (for combat screen rendering)
        /// </summary>
        public List<string> CombatLog => new List<string>(combatLog);

        /// <summary>
        /// Gets dungeon header
        /// </summary>
        public List<string> DungeonHeader => new List<string>(dungeonHeader);

        /// <summary>
        /// Gets room info
        /// </summary>
        public List<string> RoomInfo => new List<string>(roomInfo);

        /// <summary>
        /// Gets enemy info
        /// </summary>
        public List<string> EnemyInfo => new List<string>(enemyInfo);

        /// <summary>
        /// Sets dungeon header
        /// </summary>
        public void SetDungeonHeader(List<string> header)
        {
            dungeonHeader.Clear();
            dungeonHeader.AddRange(header);
        }

        /// <summary>
        /// Sets room info
        /// </summary>
        public void SetRoomInfo(List<string> info)
        {
            roomInfo.Clear();
            roomInfo.AddRange(info);
        }

        /// <summary>
        /// Sets enemy info
        /// </summary>
        public void SetEnemyInfo(List<string> info)
        {
            enemyInfo.Clear();
            enemyInfo.AddRange(info);
        }

        /// <summary>
        /// Adds a combat event to the combat log
        /// </summary>
        public void AddCombatEvent(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                combatLog.Add(message);
            }
        }

        /// <summary>
        /// Adds multiple combat events to the combat log
        /// </summary>
        public void AddCombatEvents(IEnumerable<string> messages)
        {
            if (messages == null) return;

            foreach (var message in messages)
            {
                AddCombatEvent(message);
            }
        }

        /// <summary>
        /// Clears the combat log (but keeps dungeon header, room info, and enemy info)
        /// </summary>
        public void ClearCombatLog()
        {
            combatLog.Clear();
        }

        /// <summary>
        /// Clears enemy info
        /// </summary>
        public void ClearEnemyInfo()
        {
            enemyInfo.Clear();
        }

        /// <summary>
        /// Clears room-specific info (room info, enemy info, combat log)
        /// </summary>
        public void ClearRoomInfo()
        {
            roomInfo.Clear();
            enemyInfo.Clear();
            combatLog.Clear();
        }

        /// <summary>
        /// Clears all information
        /// </summary>
        public void ClearAll()
        {
            dungeonHeader.Clear();
            roomInfo.Clear();
            enemyInfo.Clear();
            combatLog.Clear();
        }
    }
}

