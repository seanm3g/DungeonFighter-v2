using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for decreasing a character stat during creation.
    /// </summary>
    public class DecreaseStatCommand : MenuCommand
    {
        private readonly string statName;

        public DecreaseStatCommand(string stat)
        {
            statName = stat;
        }

        protected override string CommandName => $"DecreaseStat({statName})";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep($"Decreasing {statName}");
            
            // TODO: When integrating with Game.cs:
            // 1. Validate stat can be decreased
            // 2. Decrease stat value
            // 3. Update UI display
            
            LogStep($"{statName} decreased");
            await Task.CompletedTask;
        }
    }
}


