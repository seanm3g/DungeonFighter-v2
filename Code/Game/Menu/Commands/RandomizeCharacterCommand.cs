using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for randomizing character stats during creation.
    /// Generates random stat allocation.
    /// </summary>
    public class RandomizeCharacterCommand : MenuCommand
    {
        protected override string CommandName => "RandomizeCharacter";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Randomizing character stats");
            
            // TODO: When integrating with Game.cs:
            // 1. Generate random stat values
            // 2. Apply to character
            // 3. Update UI display
            
            LogStep("Character stats randomized");
            await Task.CompletedTask;
        }
    }
}


