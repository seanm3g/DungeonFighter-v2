using System;
using System.Threading.Tasks;
using RPGGame.Game;
using RPGGame.MCP.Models;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Runs one headless full-game playthrough until Death or max actions.
    /// </summary>
    public static class ClassPlaythroughRunner
    {
        public static async Task<PlaythroughRunResult> RunAsync(
            WeaponType weaponType,
            int weaponMenuSlot,
            string classDisplayName,
            int maxActionsPerRun = 500)
        {
            var result = new PlaythroughRunResult
            {
                WeaponType = weaponType,
                WeaponMenuSlot = weaponMenuSlot,
                ClassDisplayName = classDisplayName
            };

            GamePlaySession? session = null;
            try
            {
                session = new GamePlaySession();
                await session.Initialize();
                await session.StartNewGame();

                int dungeonsAttempted = 0;
                int dungeonsCompleted = 0;
                var levelTurnTracker = new PlaythroughLevelTurnTracker();

                for (int i = 0; i < maxActionsPerRun; i++)
                {
                    var state = session.CurrentState;
                    var current = state?.CurrentState ?? "";
                    if (current == "Death" || session.IsGameOver())
                        break;

                    int levelAtAction = state?.Player?.Level ?? 1;
                    levelTurnTracker.RecordAction(levelAtAction);

                    string action = ClassPlaythroughPolicy.PickAction(current, weaponMenuSlot);
                    bool enteringDungeon = ClassPlaythroughPolicy.IsDungeonEntry(current, action);
                    if (enteringDungeon)
                        dungeonsAttempted++;

                    await session.ExecuteAction(action);

                    var after = session.CurrentState?.CurrentState ?? "";
                    if (enteringDungeon && after == "DungeonCompletion")
                        dungeonsCompleted++;

                    if (after == "Death" || session.IsGameOver())
                        break;
                }

                var final = session.CurrentState;
                result.TurnCount = session.TurnCount;
                result.ActionsByLevel = levelTurnTracker.ToDictionary();
                result.DungeonsAttempted = dungeonsAttempted;
                result.DungeonsCompleted = dungeonsCompleted;
                result.FinalState = final?.CurrentState;
                result.ReachedDeath = final?.CurrentState == "Death";
                result.StopReason = result.ReachedDeath
                    ? PlaythroughStopReason.Death
                    : PlaythroughStopReason.MaxActions;

                if (final?.Player != null)
                {
                    var player = final.Player;
                    result.FinalLevel = player.Level;
                    result.FinalHealth = player.CurrentHealth;
                    result.FinalMaxHealth = player.MaxHealth;
                    result.InventoryCount = player.Inventory?.Count ?? 0;
                    result.TotalXp = player.XP;
                }
            }
            catch (Exception ex)
            {
                result.StopReason = PlaythroughStopReason.Error;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                session?.Dispose();
            }

            return result;
        }
    }
}
