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
                
                Console.WriteLine("\nCombo Management:");
                Console.WriteLine("1. Add action to combo");
                Console.WriteLine("2. Remove action from combo");
                Console.WriteLine("3. Swap two combo actions");
                Console.WriteLine("4. Back to inventory");
                Console.Write("\nChoose an option: ");

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
                            SwapComboActions();
                            break;
                        case 4:
                            return;
                        default:
                            Console.WriteLine("Invalid option.");
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input.");
                    Console.WriteLine("Press any key to continue...");
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
                Console.WriteLine("\nNo actions available to add to combo.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("\nAvailable actions to add to combo:");
            for (int i = 0; i < availableActions.Count; i++)
            {
                var action = availableActions[i];
                int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                int timesAvailable = actionPool.Count(ap => ap.Name == action.Name);
                string usageInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}/{timesAvailable}]" : "";
                Console.WriteLine($"  {i + 1}. {action.Name}{usageInfo}");
                
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
                
                Console.WriteLine(statsLine);
            }
            
            Console.Write($"\nEnter action number to add (1-{availableActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= availableActions.Count)
            {
                var selectedAction = availableActions[choice - 1];
                player.AddToCombo(selectedAction);
                Console.WriteLine($"Added {selectedAction.Name} to combo sequence.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Invalid choice.");
                Console.WriteLine("Press any key to continue...");
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
                Console.WriteLine("\nNo actions in combo sequence to remove.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("\nCurrent combo sequence:");
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (player.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                Console.WriteLine($"  {i + 1}. {action.Name}{currentStep}");
                Console.WriteLine($"      {action.Description}");
            }
            
            Console.Write($"\nEnter action number to remove (1-{comboActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= comboActions.Count)
            {
                var selectedAction = comboActions[choice - 1];
                player.RemoveFromCombo(selectedAction);
                Console.WriteLine($"Removed {selectedAction.Name} from combo sequence.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Invalid choice.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Swaps two actions in the combo sequence
        /// </summary>
        private void SwapComboActions()
        {
            var comboActions = player.GetComboActions();
            
            if (comboActions.Count < 2)
            {
                Console.WriteLine("\nYou need at least 2 actions to swap them.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("\nCurrent combo sequence:");
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (player.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                Console.WriteLine($"  {i + 1}. {action.Name}{currentStep}");
            }
            
            Console.Write($"\nEnter first action number (1-{comboActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int firstIndex) && firstIndex >= 1 && firstIndex <= comboActions.Count)
            {
                Console.Write($"Enter second action number (1-{comboActions.Count}): ");
                if (int.TryParse(Console.ReadLine(), out int secondIndex) && secondIndex >= 1 && secondIndex <= comboActions.Count)
                {
                    if (firstIndex == secondIndex)
                    {
                        Console.WriteLine("Cannot swap an action with itself.");
                    }
                    else
                    {
                        // Swap the actions in the action pool
                        var firstAction = comboActions[firstIndex - 1];
                        var secondAction = comboActions[secondIndex - 1];
                        
                        // Find and swap in the ActionPool
                        var firstPoolEntry = player.ActionPool.FirstOrDefault(a => a.Item1.Name == firstAction.Name);
                        var secondPoolEntry = player.ActionPool.FirstOrDefault(a => a.Item1.Name == secondAction.Name);
                        
                        if (firstPoolEntry.Item1 != null && secondPoolEntry.Item1 != null)
                        {
                            // Swap the combo orders
                            int tempOrder = firstPoolEntry.Item1.ComboOrder;
                            firstPoolEntry.Item1.ComboOrder = secondPoolEntry.Item1.ComboOrder;
                            secondPoolEntry.Item1.ComboOrder = tempOrder;
                            
                            Console.WriteLine($"Swapped {firstAction.Name} and {secondAction.Name} in combo sequence.");
                        }
                        else
                        {
                            Console.WriteLine("Error: Could not find actions to swap.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Invalid second action number.");
                }
            }
            else
            {
                Console.WriteLine("Invalid first action number.");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Shows detailed combo management information
        /// </summary>
        private void ShowComboManagementInfo()
        {
            Console.WriteLine();
            Console.WriteLine("Combo Management Info:");
            Console.WriteLine($"Current combo step: {player.ComboStep + 1}");
            Console.WriteLine($"Combo sequence length: {player.ComboSequence.Count}");
            
            if (player.ComboSequence.Count > 0)
            {
                Console.WriteLine("Combo sequence:");
                for (int i = 0; i < player.ComboSequence.Count; i++)
                {
                    var action = player.ComboSequence[i];
                    string currentStep = (player.ComboStep % player.ComboSequence.Count == i) ? " ← NEXT" : "";
                    Console.WriteLine($"  {i + 1}. {action.Name}{currentStep}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("Action Pool Info:");
            ShowActionPoolInfo();
        }

        /// <summary>
        /// Shows action pool information
        /// </summary>
        private void ShowActionPoolInfo()
        {
            var actionPool = player.GetActionPool();
            var comboActions = player.GetComboActions();
            
            Console.WriteLine($"Total actions in pool: {actionPool.Count}");
            Console.WriteLine($"Actions in combo: {comboActions.Count}");
            
            if (actionPool.Count > 0)
            {
                Console.WriteLine("Available actions:");
                foreach (var action in actionPool)
                {
                    int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                    string comboInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}]" : "";
                    Console.WriteLine($"  - {action.Name}{comboInfo}");
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
