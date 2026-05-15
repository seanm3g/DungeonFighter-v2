using RPGGame;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages UI context state including character, enemy, dungeon, and room information
    /// </summary>
    public interface ICanvasContextManager
    {
        Character? GetCurrentCharacter();
        void SetCurrentCharacter(Character? character);
        
        Enemy? GetCurrentEnemy();
        void SetCurrentEnemy(Enemy enemy);
        void ClearCurrentEnemy();

        /// <summary>
        /// Display name used for combat-log right-alignment: live enemy name when in combat, otherwise the last
        /// enemy name from <see cref="SetCurrentEnemy"/> until cleared (so post-fight lines keep alignment).
        /// </summary>
        string? GetCombatLogEnemyAlignmentName();

        /// <summary>
        /// Distinct enemy display names from <see cref="SetCurrentEnemy"/> in this session, sorted longest-first
        /// for prefix matching when multiple enemies appear in one combat log.
        /// </summary>
        IReadOnlyList<string> GetCombatLogEnemyAlignmentNames();

        /// <summary>
        /// Clears the sticky combat-log enemy name (e.g. when the center buffer is wiped or switching characters).
        /// </summary>
        void ClearCombatLogEnemyAlignmentSticky();
        
        string? GetDungeonName();
        void SetDungeonName(string? dungeonName);
        
        string? GetRoomName();
        void SetRoomName(string? roomName);
        
        List<string> GetDungeonContext();
        void SetDungeonContext(List<string> context);
        void ClearDungeonContext();
        void RestoreDungeonContext();
        
        bool GetDeleteConfirmationPending();
        void SetDeleteConfirmationPending(bool pending);
        void ResetDeleteConfirmation();
        
        CanvasContext GetCurrentContext();
        void ResetForNewBattle();
        void MarkCombatRenderComplete();
    }

    /// <summary>
    /// Context object containing all UI state information
    /// </summary>
    public class CanvasContext
    {
        public Character? Character { get; set; }
        public Enemy? Enemy { get; set; }
        public string? DungeonName { get; set; }
        public string? RoomName { get; set; }
        public List<string> DungeonContext { get; set; } = new List<string>();
        public bool DeleteConfirmationPending { get; set; }
        public bool IsFirstCombatRender { get; set; } = true;
    }
}
