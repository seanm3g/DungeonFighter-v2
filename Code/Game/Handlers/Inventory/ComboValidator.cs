using System;
using System.Linq;

namespace RPGGame.Handlers.Inventory
{
    /// <summary>
    /// Validates combo reorder input and action operations
    /// Extracted from InventoryComboManager.cs validation logic
    /// </summary>
    public static class ComboValidator
    {
        /// <summary>
        /// Validates the reorder input string
        /// </summary>
        public static bool ValidateReorderInput(string input, int actionCount)
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
    }
}

