namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.GameCore.Helpers;

    /// <summary>
    /// Facade for dungeon execution, room processing, and enemy encounters.
    /// Coordinates specialized managers for dungeon orchestration.
    /// 
    /// Refactored from 709 lines to ~200 lines using Facade + Manager pattern.
    /// Delegates to:
    /// - DungeonOrchestrator: Main dungeon flow
    /// - RoomProcessor: Room processing logic
    /// - EnemyEncounterHandler: Enemy encounter logic
    /// - DungeonRewardManager: Reward calculation and distribution
    /// </summary>
    public class DungeonRunnerManager
    {
        private readonly GameStateManager stateManager;
        private readonly GameNarrativeManager narrativeManager;
        private readonly CombatManager? combatManager;
        private readonly IUIManager? customUIManager;
        private readonly DungeonDisplayManager displayManager;
        private readonly DungeonExitChoiceHandler? exitChoiceHandler;
        private readonly ExplorationManager? explorationManager;
        
        // Specialized managers
        private readonly DungeonOrchestrator orchestrator;
        private readonly RoomProcessor roomProcessor;
        private readonly EnemyEncounterHandler enemyEncounterHandler;
        private readonly DungeonRewardManager rewardManager;
        
        // Delegates for dungeon completion with reward data
        public delegate void DungeonCompletedHandler(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun);
        public delegate void ShowDeathScreenHandler(Character player);
        public delegate void DungeonExitedEarlyHandler();
        
        public event DungeonCompletedHandler? DungeonCompletedEvent;
        public event ShowDeathScreenHandler? ShowDeathScreenEvent;
        public event DungeonExitedEarlyHandler? DungeonExitedEarlyEvent;
        
        // Store last reward data for completion screen
        private int lastXPGained;
        private Item? lastLootReceived;
        private List<LevelUpInfo> lastLevelUpInfos = new List<LevelUpInfo>();

        public DungeonRunnerManager(
            GameStateManager stateManager,
            GameNarrativeManager narrativeManager,
            CombatManager? combatManager,
            IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.narrativeManager = narrativeManager ?? throw new ArgumentNullException(nameof(narrativeManager));
            this.combatManager = combatManager;
            this.customUIManager = customUIManager;
            this.displayManager = new DungeonDisplayManager(narrativeManager, customUIManager);
            this.exitChoiceHandler = new DungeonExitChoiceHandler(stateManager, customUIManager, displayManager);
            this.explorationManager = new ExplorationManager();
            
            // Initialize specialized managers
            this.rewardManager = new DungeonRewardManager(stateManager);
            this.enemyEncounterHandler = new EnemyEncounterHandler(
                stateManager,
                combatManager,
                customUIManager,
                displayManager,
                OnPlayerDeath);
            this.roomProcessor = new RoomProcessor(
                stateManager,
                combatManager,
                customUIManager,
                displayManager,
                explorationManager,
                enemyEncounterHandler);
            this.orchestrator = new DungeonOrchestrator(
                stateManager,
                customUIManager,
                displayManager,
                roomProcessor,
                rewardManager,
                exitChoiceHandler,
                (System.Action<int, Item?, List<LevelUpInfo>, List<Item>>)OnDungeonCompleted,
                (System.Action<Character>)OnPlayerDeath,
                (System.Action)OnDungeonExitedEarly);
        }
        
        /// <summary>
        /// Get the exit choice handler for input routing
        /// </summary>
        public DungeonExitChoiceHandler? GetExitChoiceHandler() => exitChoiceHandler;

        /// <summary>
        /// Run the entire dungeon
        /// </summary>
        public async Task RunDungeon()
        {
            DebugLogger.Log("DungeonRunnerManager", $"CombatManager: {(combatManager != null ? "initialized" : "null")}");
            
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null || combatManager == null)
            {
                DungeonErrorHandler.HandleMissingComponents(stateManager, customUIManager);
                return;
            }
            
            await orchestrator.RunDungeon();
        }
        
        /// <summary>
        /// Get the last reward data from dungeon completion
        /// </summary>
        public (int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos) GetLastRewardData()
        {
            return (lastXPGained, lastLootReceived, lastLevelUpInfos);
        }
        
        /// <summary>
        /// Event handler for dungeon completion
        /// </summary>
        private void OnDungeonCompleted(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun)
        {
            lastXPGained = xpGained;
            lastLootReceived = lootReceived;
            lastLevelUpInfos = levelUpInfos ?? new List<LevelUpInfo>();
            DungeonCompletedEvent?.Invoke(xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun);
        }
        
        /// <summary>
        /// Event handler for player death
        /// </summary>
        private void OnPlayerDeath(Character player)
        {
            ShowDeathScreenEvent?.Invoke(player);
        }
        
        /// <summary>
        /// Event handler for early dungeon exit
        /// </summary>
        private void OnDungeonExitedEarly()
        {
            DungeonExitedEarlyEvent?.Invoke();
        }
    }
}

