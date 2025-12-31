using RPGGame;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Implementation of ICanvasContextManager for managing UI context state
    /// </summary>
    public class CanvasContextManager : ICanvasContextManager
    {
        private Character? currentCharacter;
        private Enemy? currentEnemy;
        private string? currentDungeonName;
        private string? currentRoomName;
        private List<string> dungeonContext = new List<string>();
        private bool deleteConfirmationPending = false;

        public Character? GetCurrentCharacter()
        {
            return currentCharacter;
        }

        public void SetCurrentCharacter(Character? character)
        {
            currentCharacter = character;
        }

        public Enemy? GetCurrentEnemy()
        {
            return currentEnemy;
        }

        public void SetCurrentEnemy(Enemy enemy)
        {
            currentEnemy = enemy;
        }

        public void ClearCurrentEnemy()
        {
            currentEnemy = null;
            // Note: Do NOT clear dungeonName and roomName here - location information should persist
            // even when there's no enemy, as the player is still in that dungeon/room.
            // Location should only be cleared when explicitly leaving the dungeon.
        }

        public string? GetDungeonName()
        {
            return currentDungeonName;
        }

        public void SetDungeonName(string? dungeonName)
        {
            currentDungeonName = dungeonName;
        }

        public string? GetRoomName()
        {
            return currentRoomName;
        }

        public void SetRoomName(string? roomName)
        {
            currentRoomName = roomName;
        }

        public List<string> GetDungeonContext()
        {
            return dungeonContext;
        }

        public void SetDungeonContext(List<string> context)
        {
            dungeonContext = new List<string>(context);
        }

        public void ClearDungeonContext()
        {
            dungeonContext.Clear();
        }

        public void RestoreDungeonContext()
        {
            // This method is called when we need to restore dungeon context
            // The actual restoration logic is handled by the caller
        }

        public bool GetDeleteConfirmationPending()
        {
            return deleteConfirmationPending;
        }

        public void SetDeleteConfirmationPending(bool pending)
        {
            deleteConfirmationPending = pending;
        }

        public void ResetDeleteConfirmation()
        {
            deleteConfirmationPending = false;
        }

        private bool isFirstCombatRender = true;
        
        public CanvasContext GetCurrentContext()
        {
            return new CanvasContext
            {
                Character = currentCharacter,
                Enemy = currentEnemy,
                DungeonName = currentDungeonName,
                RoomName = currentRoomName,
                DungeonContext = new List<string>(dungeonContext),
                DeleteConfirmationPending = deleteConfirmationPending,
                IsFirstCombatRender = isFirstCombatRender
            };
        }
        
        public void ResetForNewBattle()
        {
            isFirstCombatRender = true;
        }
        
        public void MarkCombatRenderComplete()
        {
            isFirstCombatRender = false;
        }
    }
}
