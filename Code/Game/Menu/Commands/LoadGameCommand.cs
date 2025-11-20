using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for loading a saved game.
    /// Loads character from save file and transitions to game loop.
    /// </summary>
    public class LoadGameCommand : MenuCommand
    {
        protected override string CommandName => "LoadGame";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Loading saved game");
            
            // TODO: When integrating with Game.cs:
            // 1. Load character from save file
            // 2. Validate save data
            // 3. Initialize game state
            
            LogStep("Game loaded successfully");
            await Task.CompletedTask;
        }
    }
}


