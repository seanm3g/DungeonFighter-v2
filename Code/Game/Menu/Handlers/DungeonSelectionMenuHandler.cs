using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Dungeon Selection Menu Handler using direct method calls.
    /// Handles dungeon selection by index.
    /// </summary>
    public class DungeonSelectionMenuHandler : MenuHandlerBase
    {
        private const int DungeonCount = 10;

        public override GameState TargetState => GameState.DungeonSelection;
        protected override string HandlerName => "DungeonSelection";

        /// <summary>
        /// Handle input directly and return next game state.
        /// </summary>
        protected override async Task<GameState?> HandleInputDirect(string input)
        {
            string cleaned = input.Trim();

            // Try to parse as dungeon number
            if (int.TryParse(cleaned, out int dungeonNum))
            {
                if (dungeonNum >= 1 && dungeonNum <= DungeonCount)
                    return await SelectDungeon(dungeonNum);
                else
                    return null;  // Invalid dungeon number
            }

            // Handle action keys
            return cleaned.ToLower() switch
            {
                "c" => await SelectDungeon(0),  // Confirm selection
                "0" => GameState.MainMenu,
                _ => null
            };
        }

        private Task<GameState?> SelectDungeon(int dungeonIndex)
        {
            LogStep($"Selecting dungeon index: {dungeonIndex}");
            // Dungeon selection logic would be handled by DungeonSelectionHandler
            // This handler just marks the selection and transitions
            return Task.FromResult<GameState?>(GameState.GameLoop);
        }
    }
}

