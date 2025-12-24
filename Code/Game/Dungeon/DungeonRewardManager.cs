namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles reward calculation and distribution for dungeon completion
    /// Extracted from DungeonRunnerManager to separate reward logic
    /// </summary>
    public class DungeonRewardManager
    {
        private readonly GameStateManager stateManager;
        private List<Item>? startingInventory;

        public DungeonRewardManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        /// <summary>
        /// Capture the starting inventory to track items found during the run
        /// </summary>
        public void CaptureStartingInventory(Character player)
        {
            if (player != null)
            {
                startingInventory = new List<Item>(player.Inventory);
            }
        }

        /// <summary>
        /// Complete the dungeon run and calculate rewards
        /// </summary>
        public async Task<(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun)> CompleteDungeon()
        {
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null)
            {
                return (0, null, new List<LevelUpInfo>(), new List<Item>());
            }
            
            // Award rewards and get the data
            var dungeonLevel = stateManager.CurrentDungeon.MaxLevel;
            var dungeonManager = new DungeonManagerWithRegistry();
            var (xpGained, lootReceived, levelUpInfos) = dungeonManager.AwardLootAndXPWithReturns(
                stateManager.CurrentPlayer, 
                stateManager.CurrentInventory, 
                new List<Dungeon> { stateManager.CurrentDungeon }
            );
            
            // Calculate all items found during the dungeon run
            // Compare current inventory to starting inventory, excluding the final completion reward
            List<Item> itemsFoundDuringRun = new List<Item>();
            if (stateManager.CurrentPlayer != null && startingInventory != null)
            {
                var currentInventory = stateManager.CurrentPlayer.Inventory;
                // Find all items in current inventory that weren't in starting inventory
                foreach (var item in currentInventory)
                {
                    // Skip the final completion reward - it will be displayed separately
                    if (lootReceived != null && item.Name == lootReceived.Name && item.Type == lootReceived.Type)
                    {
                        continue;
                    }
                    
                    // Check if this item exists in starting inventory
                    // Use a simple comparison based on item identity (name + type should be unique enough)
                    bool foundInStarting = false;
                    foreach (var startingItem in startingInventory)
                    {
                        if (startingItem.Name == item.Name && startingItem.Type == item.Type)
                        {
                            foundInStarting = true;
                            break;
                        }
                    }
                    if (!foundInStarting)
                    {
                        itemsFoundDuringRun.Add(item);
                    }
                }
            }
            
            // Add a delay to let rewards display if in console
            if (!RPGGame.MCP.MCPMode.IsActive)
            {
                await Task.Delay(1500);
            }
            
            // Reset starting inventory for next run
            startingInventory = null;
            
            return (xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun);
        }
    }
}

