using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for starting a new game.
    /// Creates a new character and transitions to weapon selection.
    /// </summary>
    public class StartNewGameCommand : MenuCommand
    {
        protected override string CommandName => "StartNewGame";

        protected override async Task ExecuteCommand(IMenuContext context)
        {
            LogStep("Starting new game flow");
            
            // TODO: When integrating with Game.cs:
            // 1. Create new character
            // 2. Initialize character data
            // 3. Prepare for weapon selection
            
            LogStep("New game initialized");
            await Task.CompletedTask;
        }
    }
}

