using System.Threading.Tasks;
using RPGGame;
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

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Starting new game flow");
            
            if (context?.StateManager != null)
            {
                // Create new character (null triggers random name generation)
                var newCharacter = new Character(null, 1);
                context.StateManager.SetCurrentPlayer(newCharacter);
                
                // Apply health multiplier if configured
                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    newCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
                
                LogStep("New character created, transitioning to weapon selection");
            }
            
            await Task.CompletedTask;
        }
    }
}

