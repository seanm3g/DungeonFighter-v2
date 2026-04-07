namespace RPGGame.Handlers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame;
    using RPGGame.UI.Avalonia;
    using static RPGGame.Handlers.Inventory.ComboValidator;
    using static RPGGame.Handlers.Inventory.ComboReorderer;
    using ActionDelegate = System.Action;

    /// <summary>
    /// Manages combo sequence operations: add, remove, and reorder actions.
    /// </summary>
    public class InventoryComboManager
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly InventoryStateManager stateTracker;
        private readonly ComboInputHandler inputHandler;
        
        // Event delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        
        public InventoryComboManager(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            InventoryStateManager stateTracker)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
            this.inputHandler = new ComboInputHandler(
                stateManager,
                stateTracker,
                customUIManager,
                message => ShowMessageEvent?.Invoke(message),
                (ActionDelegate)(() => ShowInventoryEvent?.Invoke()),
                (ActionDelegate)RenderComboManagementScreen);
        }
        
        /// <summary>
        /// Show combo management menu
        /// </summary>
        public void ShowComboManagement()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            stateTracker.InComboManagement = true;
            RenderComboManagementScreen();
        }
        
        /// <summary>
        /// Helper method to render combo management screen
        /// </summary>
        private void RenderComboManagementScreen()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboManagement(stateManager.CurrentPlayer);
            }
        }
        
        /// <summary>
        /// Handles mouse-driven combo actions (values from <see cref="RPGGame.UI.Avalonia.Renderers.Inventory.ComboManagementRenderer"/>).
        /// </summary>
        public void HandleComboPointerInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;

            if (!ComboPointerInput.TryParse(input, out var kind, out int index))
            {
                ShowMessageEvent?.Invoke("Invalid action.");
                return;
            }

            var player = stateManager.CurrentPlayer;

            switch (kind)
            {
                case ComboPointerInput.Kind.Back:
                    if (!stateTracker.InComboManagement)
                        return;
                    stateTracker.InComboManagement = false;
                    ShowInventoryEvent?.Invoke();
                    return;

                case ComboPointerInput.Kind.AddAll:
                    if (!stateTracker.InComboManagement)
                        ShowComboManagement();
                    AddAllAvailableActionsToCombo();
                    return;

                case ComboPointerInput.Kind.Reorder:
                    if (!stateTracker.InComboManagement)
                        ShowComboManagement();
                    PromptReorderComboActions();
                    return;

                case ComboPointerInput.Kind.PoolAdd:
                {
                    var pool = player.GetActionPool();
                    if (index < 0 || index >= pool.Count)
                    {
                        ShowMessageEvent?.Invoke("Invalid pool selection.");
                        if (stateTracker.InComboManagement)
                            RenderComboManagementScreen();
                        else
                            ShowInventoryEvent?.Invoke();
                        return;
                    }

                    var action = pool[index];
                    player.AddToCombo(action);
                    ShowMessageEvent?.Invoke($"Added {action.Name} to sequence.");
                    if (stateTracker.InComboManagement)
                        RenderComboManagementScreen();
                    else
                        ShowInventoryEvent?.Invoke();
                    return;
                }

                case ComboPointerInput.Kind.SequenceRemove:
                {
                    var combo = player.GetComboActions();
                    if (index < 0 || index >= combo.Count)
                    {
                        ShowMessageEvent?.Invoke("Invalid sequence slot.");
                        if (stateTracker.InComboManagement)
                            RenderComboManagementScreen();
                        else
                            ShowInventoryEvent?.Invoke();
                        return;
                    }

                    var action = combo[index];
                    player.RemoveFromCombo(action);
                    ShowMessageEvent?.Invoke($"Removed {action.Name} from sequence.");
                    if (stateTracker.InComboManagement)
                        RenderComboManagementScreen();
                    else
                        ShowInventoryEvent?.Invoke();
                    return;
                }

                default:
                    ShowMessageEvent?.Invoke("Invalid action.");
                    return;
            }
        }

        /// <summary>
        /// Handle input for combo management menu
        /// </summary>
        public void HandleComboManagementInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            // Try to handle input through specialized input handler
            if (inputHandler.HandleAddActionSelection(input, stateManager.CurrentPlayer, 
                msg => ShowMessageEvent?.Invoke(msg)))
            {
                return;
            }
            
            if (inputHandler.HandleRemoveActionSelection(input, stateManager.CurrentPlayer,
                msg => ShowMessageEvent?.Invoke(msg)))
            {
                return;
            }
            
            if (inputHandler.HandleReorderInput(input, stateManager.CurrentPlayer,
                msg => ShowMessageEvent?.Invoke(msg),
                msg => ShowMessageEvent?.Invoke(msg)))
            {
                return;
            }
            
            // Main actions screen: 1/2 = keyboard pickers; 8/9 = reorder / add-all; 0/5 = back
            switch (input)
            {
                case "1":
                    PromptAddActionToCombo();
                    break;
                case "2":
                    PromptRemoveActionFromCombo();
                    break;
                case "8":
                    PromptReorderComboActions();
                    break;
                case "9":
                    AddAllAvailableActionsToCombo();
                    break;
                case "0":
                case "5":
                    stateTracker.InComboManagement = false;
                    ShowInventoryEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. 1=add list, 2=remove list, 8=reorder, 9=add all, 0/5=back. Or use the mouse.");
                    break;
            }
        }
        
        /// <summary>
        /// Prompt user to add an action to combo
        /// </summary>
        private void PromptAddActionToCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var actionPool = stateManager.CurrentPlayer.GetActionPool();
            if (actionPool.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions available to add to combo.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            stateTracker.WaitingForComboActionSelection = true;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboActionSelection(stateManager.CurrentPlayer, "add");
            }
        }
        
        /// <summary>
        /// Prompt user to remove an action from combo
        /// </summary>
        private void PromptRemoveActionFromCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            if (comboActions.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions in combo sequence to remove.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            stateTracker.WaitingForComboRemoveSelection = true;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboActionSelection(stateManager.CurrentPlayer, "remove");
            }
        }
        
        /// <summary>
        /// Prompt user to reorder combo actions
        /// </summary>
        private void PromptReorderComboActions()
        {
            if (stateManager.CurrentPlayer == null) return;

            if (stateManager.HasCurrentDungeon)
            {
                ShowMessageEvent?.Invoke("Cannot reorder combo during an active dungeon run.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            if (comboActions.Count < 2)
            {
                ShowMessageEvent?.Invoke("You need at least 2 actions to reorder them.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            stateTracker.WaitingForComboReorderInput = true;
            stateTracker.ReorderInputSequence = "";
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboReorderPrompt(stateManager.CurrentPlayer, "");
            }
        }
        
        /// <summary>
        /// Add all available actions to combo
        /// </summary>
        private void AddAllAvailableActionsToCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var actionPool = stateManager.CurrentPlayer.GetActionPool();
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            
            if (actionPool.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions available in action pool to add to combo.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            var actionsToAdd = actionPool.Where(action => 
                !comboActions.Any(comboAction => comboAction.Name == action.Name)).ToList();
            
            if (actionsToAdd.Count == 0)
            {
                ShowMessageEvent?.Invoke("All available actions are already in your combo sequence.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            int addedCount = 0;
            foreach (var action in actionsToAdd)
            {
                stateManager.CurrentPlayer.AddToCombo(action);
                addedCount++;
            }
            
            ShowMessageEvent?.Invoke($"Successfully added {addedCount} actions to combo sequence!");
            stateTracker.InComboManagement = true;
            RenderComboManagementScreen();
        }
        
    }
}

