using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Commands;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Inventory Menu Handler using the unified menu framework.
    /// Handles 7 inventory actions: View, Equip, Unequip, Drop, Sort, Sell, etc.
    /// 
    /// BEFORE: ~200 lines with complex logic
    /// AFTER: ~90 lines with clean command pattern
    /// </summary>
    public class InventoryMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.Inventory;
        protected override string HandlerName => "Inventory";

        /// <summary>
        /// Parse input into inventory command.
        /// Supports: 7 inventory actions (1-7) and navigation keys
        /// </summary>
        protected override IMenuCommand? ParseInput(string input)
        {
            string cleaned = input.Trim();

            if (cleaned.Length != 1)
                return null;

            return cleaned switch
            {
                "1" => new SelectOptionCommand(1, "ViewInventory"),
                "2" => new SelectOptionCommand(2, "EquipItem"),
                "3" => new SelectOptionCommand(3, "UnequipItem"),
                "4" => new SelectOptionCommand(4, "DropItem"),
                "5" => new SelectOptionCommand(5, "SortInventory"),
                "6" => new SelectOptionCommand(6, "SellItem"),
                "7" => new SelectOptionCommand(7, "UseItem"),
                "0" => new CancelCommand("Inventory"),
                _ => null
            };
        }

        /// <summary>
        /// Execute command and determine next state.
        /// </summary>
        protected override async Task<GameState?> ExecuteCommand(IMenuCommand command)
        {
            if (StateManager != null)
            {
                var context = new MenuContext(StateManager);
                await command.Execute(context);
            }
            else
            {
                DebugLogger.Log(HandlerName, "WARNING: StateManager is null, executing command with null context");
                await command.Execute(null);
            }

            return command switch
            {
                CancelCommand => GameState.GameLoop,  // Return to game
                _ => (GameState?)null  // Stay in inventory for most actions
            };
        }
    }
}

