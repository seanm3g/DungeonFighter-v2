using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;

namespace RPGGame.GameCore.Display.Helpers
{
    /// <summary>
    /// Helper for applying spacing between display sections
    /// </summary>
    public static class SpacingApplier
    {
        /// <summary>
        /// Applies spacing before a block type using batch transaction
        /// </summary>
        public static void ApplySpacingBefore(DisplayBatchTransaction batch, TextSpacingSystem.BlockType blockType)
        {
            int spacing = TextSpacingSystem.GetSpacingBefore(blockType);
            for (int i = 0; i < spacing; i++)
                batch.Add("");
        }

        /// <summary>
        /// Applies spacing before a block type using UI manager
        /// </summary>
        public static void ApplySpacingBefore(IUIManager uiManager, TextSpacingSystem.BlockType blockType)
        {
            int spacing = TextSpacingSystem.GetSpacingBefore(blockType);
            for (int i = 0; i < spacing; i++)
                uiManager.WriteLine("", UIMessageType.System);
        }

        /// <summary>
        /// Adds content with spacing before it using batch transaction
        /// </summary>
        public static void AddWithSpacing(DisplayBatchTransaction batch, TextSpacingSystem.BlockType blockType, IEnumerable<string> content)
        {
            ApplySpacingBefore(batch, blockType);
            batch.AddRange(content);
            TextSpacingSystem.RecordBlockDisplayed(blockType);
        }

        /// <summary>
        /// Adds content with spacing before it using UI manager
        /// </summary>
        public static void AddWithSpacing(IUIManager uiManager, TextSpacingSystem.BlockType blockType, IEnumerable<string> content)
        {
            ApplySpacingBefore(uiManager, blockType);
            foreach (var line in content)
                uiManager.WriteLine(line, UIMessageType.System);
            TextSpacingSystem.RecordBlockDisplayed(blockType);
        }
    }
}

