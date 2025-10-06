using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages combo action operations including adding, removing, and swapping actions
    /// </summary>
    public class ComboManager
    {
        private Character player;
        private InventoryDisplayManager displayManager;

        public ComboManager(Character player, InventoryDisplayManager displayManager)
        {
            this.player = player;
            this.displayManager = displayManager;
        }

        /// <summary>
        /// Shows the combo management menu and handles user input
        /// </summary>
        public void ManageComboActions()
        {
            while (true)
            {
                Console.Clear();
                displayManager.ShowCharacterStats();
                displayManager.ShowCurrentEquipment();
                ShowComboManagementInfo();
                
                UIManager.WriteMenuLine("\nCombo Management:");
                UIManager.WriteMenuLine("1. Add action to combo");
                UIManager.WriteMenuLine("2. Remove action from combo");
                UIManager.WriteMenuLine("3. Re-order combo actions");
                UIManager.WriteMenuLine("4. Add all available actions to combo");
                UIManager.WriteMenuLine("5. Back to inventory");
                UIManager.Write("\nChoose an option: ");

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 1:
                            AddActionToCombo();
                            break;
                        case 2:
                            RemoveActionFromCombo();
                            break;
                        case 3:
                            ReorderComboActions();
                            break;
                        case 4:
                            AddAllAvailableActionsToCombo();
                            break;
                        case 5:
                            return;
                        default:
                            UIManager.WriteMenuLine("Invalid option.");
                            UIManager.WriteMenuLine("Press any key to continue...");
                            Console.ReadKey();
                            break;
                    }
                }
                else
                {
                    UIManager.WriteMenuLine("Invalid input.");
                    UIManager.WriteMenuLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// Adds an action to the combo sequence
        /// </summary>
        private void AddActionToCombo()
        {
            var actionPool = player.GetActionPool();
            var comboActions = player.GetComboActions();
            
            // Show all available actions (allow duplicates)
            var availableActions = actionPool.ToList();
            
            if (availableActions.Count == 0)
            {
                UIManager.WriteMenuLine("\nNo actions available to add to combo.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            UIManager.WriteMenuLine("\nAvailable actions to add to combo:");
            for (int i = 0; i < availableActions.Count; i++)
            {
                var action = availableActions[i];
                int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                int timesAvailable = actionPool.Count(ap => ap.Name == action.Name);
                string usageInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}/{timesAvailable}]" : "";
                UIManager.WriteMenuLine($"  {i + 1}. {action.Name}{usageInfo}");
                
                // Calculate speed percentage
                double speedPercentage = CalculateActionSpeedPercentage(action);
                string speedText = GetSpeedDescription(speedPercentage);
                
                // Build action stats line
                string statsLine = $"      {action.Description} | Damage: {action.DamageMultiplier:F1}x | Speed: {speedPercentage:F0}% ({speedText})";
                
                // Add any special effects
                if (action.CausesBleed) statsLine += ", Causes Bleed";
                if (action.CausesWeaken) statsLine += ", Causes Weaken";
                if (action.CausesSlow) statsLine += ", Causes Slow";
                if (action.CausesPoison) statsLine += ", Causes Poison";
                if (action.CausesStun) statsLine += ", Causes Stun";
                
                UIManager.WriteMenuLine(statsLine);
            }
            
            UIManager.Write($"\nEnter action number to add (1-{availableActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= availableActions.Count)
            {
                var selectedAction = availableActions[choice - 1];
                player.AddToCombo(selectedAction);
                UIManager.WriteMenuLine($"Added {selectedAction.Name} to combo sequence.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                UIManager.WriteMenuLine("Invalid choice.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
        
        /// <summary>
        /// Removes an action from the combo sequence
        /// </summary>
        private void RemoveActionFromCombo()
        {
            var comboActions = player.GetComboActions();
            
            if (comboActions.Count == 0)
            {
                UIManager.WriteMenuLine("\nNo actions in combo sequence to remove.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            UIManager.WriteMenuLine("\nCurrent combo sequence:");
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (player.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                UIManager.WriteMenuLine($"  {i + 1}. {action.Name}{currentStep}");
                UIManager.WriteMenuLine($"      {action.Description}");
            }
            
            UIManager.Write($"\nEnter action number to remove (1-{comboActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= comboActions.Count)
            {
                var selectedAction = comboActions[choice - 1];
                player.RemoveFromCombo(selectedAction);
                UIManager.WriteMenuLine($"Removed {selectedAction.Name} from combo sequence.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                UIManager.WriteMenuLine("Invalid choice.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Adds all available actions from the action pool to the combo sequence
        /// </summary>
        private void AddAllAvailableActionsToCombo()
        {
            var actionPool = player.GetActionPool();
            
            if (actionPool.Count == 0)
            {
                UIManager.WriteMenuLine("\nNo actions available in action pool to add to combo.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            // Show confirmation dialog
            UIManager.WriteMenuLine($"\nThis will add all {actionPool.Count} available actions to your combo sequence.");
            UIManager.WriteMenuLine("Available actions:");
            
            foreach (var action in actionPool)
            {
                UIManager.WriteMenuLine($"  - {action.Name}");
            }
            
            UIManager.WriteMenuLine("\nAre you sure you want to add all these actions? (y/n): ");
            string confirmation = Console.ReadLine()?.Trim().ToLower() ?? "";
            
            if (confirmation == "y" || confirmation == "yes")
            {
                int addedCount = 0;
                foreach (var action in actionPool)
                {
                    player.AddToCombo(action);
                    addedCount++;
                }
                
                UIManager.WriteMenuLine($"\nSuccessfully added {addedCount} actions to combo sequence!");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                UIManager.WriteMenuLine("Operation cancelled.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Re-orders actions in the combo sequence based on user input
        /// </summary>
        private void ReorderComboActions()
        {
            var comboActions = player.GetComboActions();
            
            if (comboActions.Count < 2)
            {
                UIManager.WriteMenuLine("\nYou need at least 2 actions to reorder them.");
                UIManager.WriteMenuLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            UIManager.WriteMenuLine("\nCurrent combo sequence:");
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (player.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                UIManager.WriteMenuLine($"  {i + 1}. {action.Name}{currentStep}");
            }
            
            UIManager.WriteMenuLine($"\nEnter the new order using numbers 1-{comboActions.Count} (e.g., 15324):");
            UIManager.Write("New order: ");
            
            string input = Console.ReadLine()?.Trim() ?? "";
            
            if (ValidateReorderInput(input, comboActions.Count))
            {
                if (ApplyReorder(input, comboActions))
                {
                    UIManager.WriteMenuLine("\nCombo sequence reordered successfully!");
                    
                    // Show the new sequence
                    var newComboActions = player.GetComboActions();
                    UIManager.WriteMenuLine("\nNew combo sequence:");
                    for (int i = 0; i < newComboActions.Count; i++)
                    {
                        var action = newComboActions[i];
                        string currentStep = (player.ComboStep % newComboActions.Count == i) ? " ← NEXT" : "";
                        UIManager.WriteMenuLine($"  {i + 1}. {action.Name}{currentStep}");
                    }
                }
                else
                {
                    UIManager.WriteMenuLine("Error: Failed to reorder combo sequence.");
                }
            }
            else
            {
                UIManager.WriteMenuLine("Invalid input. Please enter numbers 1-" + comboActions.Count + " in any order (e.g., 15324).");
            }
            
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// Validates the reorder input string
        /// </summary>
        private bool ValidateReorderInput(string input, int actionCount)
        {
            if (string.IsNullOrEmpty(input))
                return false;
                
            // Check if input contains only digits
            if (!input.All(char.IsDigit))
                return false;
                
            // Check if all numbers 1 to actionCount are present exactly once
            var numbers = input.Select(c => int.Parse(c.ToString())).ToList();
            
            if (numbers.Count != actionCount)
                return false;
                
            // Check if all numbers from 1 to actionCount are present
            for (int i = 1; i <= actionCount; i++)
            {
                if (!numbers.Contains(i))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Applies the reorder to the combo sequence
        /// </summary>
        private bool ApplyReorder(string input, List<Action> currentComboActions)
        {
            try
            {
                var newOrder = input.Select(c => int.Parse(c.ToString())).ToList();
                
                // Create a new list with actions in the specified order
                var reorderedActions = new List<Action>();
                for (int i = 0; i < newOrder.Count; i++)
                {
                    int actionIndex = newOrder[i] - 1; // Convert to 0-based index
                    reorderedActions.Add(currentComboActions[actionIndex]);
                }
                
                // Update the combo orders in the action pool
                for (int i = 0; i < reorderedActions.Count; i++)
                {
                    var action = reorderedActions[i];
                    var poolEntry = player.ActionPool.FirstOrDefault(a => a.Item1.Name == action.Name);
                    if (poolEntry.Item1 != null)
                    {
                        poolEntry.Item1.ComboOrder = i + 1;
                    }
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Shows detailed combo management information
        /// </summary>
        private void ShowComboManagementInfo()
        {
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Combo Management Info:");
            UIManager.WriteMenuLine($"Current combo step: {player.ComboStep + 1}");
            UIManager.WriteMenuLine($"Combo sequence length: {player.ComboSequence.Count}");
            
            if (player.ComboSequence.Count > 0)
            {
                UIManager.WriteMenuLine("Combo sequence:");
                for (int i = 0; i < player.ComboSequence.Count; i++)
                {
                    var action = player.ComboSequence[i];
                    string currentStep = (player.ComboStep % player.ComboSequence.Count == i) ? " ← NEXT" : "";
                    UIManager.WriteMenuLine($"  {i + 1}. {action.Name}{currentStep}");
                }
            }
            
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Action Pool Info:");
            ShowActionPoolInfo();
        }

        /// <summary>
        /// Shows action pool information
        /// </summary>
        private void ShowActionPoolInfo()
        {
            var actionPool = player.GetActionPool();
            var comboActions = player.GetComboActions();
            
            UIManager.WriteMenuLine($"Total actions in pool: {actionPool.Count}");
            UIManager.WriteMenuLine($"Actions in combo: {comboActions.Count}");
            
            if (actionPool.Count > 0)
            {
                UIManager.WriteMenuLine("Available actions:");
                foreach (var action in actionPool)
                {
                    int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                    string comboInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}]" : "";
                    UIManager.WriteMenuLine($"  - {action.Name}{comboInfo}");
                }
            }
        }

        /// <summary>
        /// Calculates the speed percentage for an action
        /// </summary>
        private double CalculateActionSpeedPercentage(Action action)
        {
            // This is a simplified calculation - the actual implementation would need to be moved from the original class
            return 100.0 / action.Length;
        }

        /// <summary>
        /// Gets a description of the action speed
        /// </summary>
        private string GetSpeedDescription(double speedPercentage)
        {
            if (speedPercentage >= 150) return "Very Fast";
            if (speedPercentage >= 120) return "Fast";
            if (speedPercentage >= 100) return "Normal";
            if (speedPercentage >= 80) return "Slow";
            return "Very Slow";
        }
    }
}
