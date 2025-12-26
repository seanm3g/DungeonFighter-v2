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
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CanvasContextManager.cs:SetCurrentEnemy", message = "Setting enemy in context", data = new { enemyName = enemy?.Name ?? "null", currentCharacterName = currentCharacter?.Name ?? "null" }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
            // #endregion
            currentEnemy = enemy;
        }

        public void ClearCurrentEnemy()
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CanvasContextManager.cs:ClearCurrentEnemy", message = "Clearing enemy from context", data = new { currentCharacterName = currentCharacter?.Name ?? "null", hadEnemy = currentEnemy != null, enemyName = currentEnemy?.Name ?? "null" }, sessionId = "debug-session", runId = "run1", hypothesisId = "H1" }) + "\n"); } catch { }
            // #endregion
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
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CanvasContextManager.cs:ClearDungeonContext", message = "Clearing dungeon context", data = new { currentCharacterName = currentCharacter?.Name ?? "null", contextCount = dungeonContext.Count }, sessionId = "debug-session", runId = "run1", hypothesisId = "H8" }) + "\n"); } catch { }
            // #endregion
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
