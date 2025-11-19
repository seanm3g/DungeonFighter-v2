using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Commands;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Dungeon Selection Menu Handler using the unified menu framework.
    /// Handles dungeon selection by index.
    /// 
    /// BEFORE: ~150 lines with scattered logic
    /// AFTER: ~85 lines with clean command pattern
    /// </summary>
    public class DungeonSelectionMenuHandler : MenuHandlerBase
    {
        private const int DungeonCount = 10;

        public override GameState TargetState => GameState.DungeonSelection;
        protected override string HandlerName => "DungeonSelection";

        /// <summary>
        /// Parse input into dungeon selection command.
        /// Supports: dungeon numbers (1-10) and action keys
        /// </summary>
        protected override IMenuCommand? ParseInput(string input)
        {
            string cleaned = input.Trim();

            // Try to parse as dungeon number
            if (int.TryParse(cleaned, out int dungeonNum))
            {
                if (dungeonNum >= 1 && dungeonNum <= DungeonCount)
                    return new SelectOptionCommand(dungeonNum, "Dungeon");
                else
                    return null;  // Invalid dungeon number
            }

            // Handle action keys
            return cleaned.ToLower() switch
            {
                "c" => new SelectOptionCommand(0, "ConfirmDungeon"),  // Confirm selection
                "0" => new CancelCommand("DungeonSelection"),
                _ => null
            };
        }

        /// <summary>
        /// Execute command and determine next state.
        /// </summary>
        protected override async Task<GameState?> ExecuteCommand(IMenuCommand command)
        {
            if (StateManager != null)
            {
                var context = new MenuContext(StateManager);
                await command.Execute(context);
            }
            else
            {
                DebugLogger.Log(HandlerName, "WARNING: StateManager is null, executing command with null context");
                await command.Execute(null);
            }

            return command switch
            {
                SelectOptionCommand => GameState.GameLoop,
                CancelCommand => GameState.MainMenu,
                _ => (GameState?)null  // Stay in dungeon selection
            };
        }
    }
}

