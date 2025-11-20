using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Generic command for selecting a menu option by index.
    /// Used in menus with numbered options.
    /// </summary>
    public class SelectOptionCommand : MenuCommand
    {
        private readonly int optionIndex;
        private readonly string optionName;

        public SelectOptionCommand(int index, string name = "")
        {
            optionIndex = index;
            optionName = name;
        }

        protected override string CommandName => 
            string.IsNullOrEmpty(optionName) 
                ? $"SelectOption({optionIndex})" 
                : $"SelectOption({optionName})";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep($"Selecting option: {CommandName}");
            
            // TODO: When integrating with Game.cs:
            // 1. Validate option index
            // 2. Get option data
            // 3. Execute option
            
            LogStep($"Option selected: {CommandName}");
            await Task.CompletedTask;
        }
    }
}


