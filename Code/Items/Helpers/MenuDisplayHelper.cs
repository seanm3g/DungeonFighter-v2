using System;
using RPGGame.UI;

namespace RPGGame.Items.Helpers
{
    /// <summary>
    /// Helper methods for displaying menus and handling common menu patterns
    /// </summary>
    public static class MenuDisplayHelper
    {
        /// <summary>
        /// Shows a message and waits for user to press a key
        /// </summary>
        public static void ShowMessageAndWait(string message)
        {
            UIManager.WriteMenuLine(message);
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Shows an error message and waits for user to press a key
        /// </summary>
        public static void ShowErrorAndWait(string errorMessage)
        {
            UIManager.WriteMenuLine($"\n{errorMessage}");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Shows a success message and waits for user to press a key
        /// </summary>
        public static void ShowSuccessAndWait(string successMessage)
        {
            UIManager.WriteMenuLine($"\n{successMessage}");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}

