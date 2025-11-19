using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for selecting a weapon during character creation.
    /// </summary>
    public class SelectWeaponCommand : MenuCommand
    {
        private readonly int weaponIndex;

        public SelectWeaponCommand(int index)
        {
            weaponIndex = index;
        }

        protected override string CommandName => $"SelectWeapon({weaponIndex})";

        protected override async Task ExecuteCommand(IMenuContext context)
        {
            LogStep($"Selecting weapon at index {weaponIndex}");
            
            // TODO: When integrating with Game.cs:
            // 1. Validate weapon index
            // 2. Get weapon from list
            // 3. Equip weapon on character
            
            LogStep($"Weapon selected: index {weaponIndex}");
            await Task.CompletedTask;
        }
    }
}

