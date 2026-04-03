using System.Collections.Generic;
using RPGGame;

namespace RPGGame.UI.Avalonia
{
    public partial class CanvasUICoordinator
    {
        #region Context Management

        public void SetDungeonContext(List<string> context)
        {
            contextCoordinator.SetDungeonContext(context);
        }

        public bool IsCharacterActive(Character? character)
        {
            return contextCoordinator.IsCharacterActive(character);
        }

        public void SetCurrentEnemy(Enemy enemy)
        {
            contextCoordinator.SetCurrentEnemy(enemy);
        }

        public void SetDungeonName(string? dungeonName)
        {
            contextCoordinator.SetDungeonName(dungeonName);
        }

        public void SetRoomName(string? roomName)
        {
            contextCoordinator.SetRoomName(roomName);
        }

        public void ClearCurrentEnemy()
        {
            contextCoordinator.ClearCurrentEnemy();
        }

        /// <summary>
        /// Current enemy in UI context (dungeon combat / encounter), or null when not in an encounter or in menus.
        /// </summary>
        public Enemy? GetCurrentEnemy()
        {
            return contextManager.GetCurrentEnemy();
        }

        #endregion
    }
}

