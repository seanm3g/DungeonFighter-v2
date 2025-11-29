using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Inventory Menu Handler using direct method calls.
    /// Handles 7 inventory actions: View, Equip, Unequip, Drop, Sort, Sell, etc.
    /// </summary>
    public class InventoryMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.Inventory;
        protected override string HandlerName => "Inventory";

        /// <summary>
        /// Handle input directly and return next game state.
        /// </summary>
        protected override async Task<GameState?> HandleInputDirect(string input)
        {
            string cleaned = input.Trim();

            if (cleaned.Length != 1)
                return null;

            return cleaned switch
            {
                "1" => await HandleInventoryAction("ViewInventory"),
                "2" => await HandleInventoryAction("EquipItem"),
                "3" => await HandleInventoryAction("UnequipItem"),
                "4" => await HandleInventoryAction("DropItem"),
                "5" => await HandleInventoryAction("SortInventory"),
                "6" => await HandleInventoryAction("SellItem"),
                "7" => await HandleInventoryAction("UseItem"),
                "0" => GameState.GameLoop,  // Return to game
                _ => null
            };
        }

        private Task<GameState?> HandleInventoryAction(string action)
        {
            LogStep($"Handling inventory action: {action}");
            // Inventory action logic would be handled by InventoryMenuHandler
            // This handler just marks the action
            return Task.FromResult<GameState?>(null); // Stay in inventory for most actions
        }
    }
}

