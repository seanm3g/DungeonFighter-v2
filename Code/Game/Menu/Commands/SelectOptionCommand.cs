using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for selecting a menu option.
    /// Used for generic option selection across different menus.
    /// </summary>
    public class SelectOptionCommand : MenuCommand
    {
        private readonly int optionIndex;
        private readonly string optionType;

        public SelectOptionCommand(int optionIndex, string optionType)
        {
            this.optionIndex = optionIndex;
            this.optionType = optionType;
        }

        protected override string CommandName => "SelectOption";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep($"Selecting {optionType} option: {optionIndex}");
            
            // Option selection logic is handled by the specific menu handler
            // This command just marks the selection
            
            await Task.CompletedTask;
        }
    }
}
