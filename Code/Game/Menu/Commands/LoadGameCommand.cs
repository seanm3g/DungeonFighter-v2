using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for loading an existing game.
    /// Loads character from save file and transitions to game loop.
    /// </summary>
    public class LoadGameCommand : MenuCommand
    {
        protected override string CommandName => "LoadGame";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Loading game");
            
            if (context?.StateManager != null)
            {
                var savedCharacter = await Character.LoadCharacterAsync().ConfigureAwait(false);
                if (savedCharacter != null)
                {
                    context.StateManager.SetCurrentPlayer(savedCharacter);
                    
                    // Apply health multiplier if configured
                    var settings = GameSettings.Instance;
                    if (settings.PlayerHealthMultiplier != 1.0)
                    {
                        savedCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                    }
                    
                    LogStep("Game loaded successfully");
                }
                else
                {
                    LogError("No saved game found");
                }
            }
            
            await Task.CompletedTask;
        }
    }
}

