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
        
        string? GetDungeonName();
        void SetDungeonName(string? dungeonName);
        
        string? GetRoomName();
        void SetRoomName(string? roomName);
        
        List<string> GetDungeonContext();
        void SetDungeonContext(List<string> context);
        void RestoreDungeonContext();
        
        bool GetDeleteConfirmationPending();
        void SetDeleteConfirmationPending(bool pending);
        void ResetDeleteConfirmation();
        
        CanvasContext GetCurrentContext();
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
    }
}
