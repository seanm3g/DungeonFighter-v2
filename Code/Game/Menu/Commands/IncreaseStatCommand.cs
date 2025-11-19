using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for increasing a character stat during creation.
    /// </summary>
    public class IncreaseStatCommand : MenuCommand
    {
        private readonly string statName;

        public IncreaseStatCommand(string stat)
        {
            statName = stat;
        }

        protected override string CommandName => $"IncreaseStat({statName})";

        protected override async Task ExecuteCommand(IMenuContext context)
        {
            LogStep($"Increasing {statName}");
            
            // TODO: When integrating with Game.cs:
            // 1. Validate stat can be increased
            // 2. Increase stat value
            // 3. Update UI display
            
            LogStep($"{statName} increased");
            await Task.CompletedTask;
        }
    }
}

