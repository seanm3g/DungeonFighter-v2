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
        /// Updates the current character in UI context without clearing the center display buffer.
        /// Use for dungeon display sync after buffer content is already committed (e.g. <see cref="RPGGame.Display.Dungeon.DungeonDisplayContextSync.SetUIContext"/>).
        /// </summary>
        public void SetCharacterContextOnly(Character? character)
        {
            contextCoordinator.SetCharacterContextOnly(character);
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

