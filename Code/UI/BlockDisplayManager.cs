using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Manages block-based display system for consistent combat output formatting
    /// Each block type has its own delay and spacing rules
    /// </summary>
    public static class BlockDisplayManager
    {
        /// <summary>
        /// Applies keyword coloring to text if enabled
        /// Skips coloring if text already has explicit color codes to prevent conflicts
        /// </summary>
        private static string ApplyKeywordColoring(string text)
        {
            // Return text as-is. Keyword coloring will be applied at render time if needed.
            // This method is kept for compatibility but no longer modifies the text.
            return text;
        }
        private static string? lastActingEntity = null;
        private static string? lastBlockType = null;

        /// <summary>
        /// Defines spacing rules for each block type
        /// </summary>
        private struct SpacingRule
        {
            public int Before { get; set; }  // Blank lines before this block type
            public int After { get; set; }   // Blank lines after this block type
        }

        /// <summary>
        /// Centralized spacing rules for all block types
        /// Based on UI reference patterns:
        /// 
        /// Spacing Rules (matching UI reference):
        /// - ActionBlock: No spacing before, 1 line after (separates actions)
        /// - EnvironmentalBlock: No spacing before, 1 line after (follows action blocks)
        /// - EffectBlock: No spacing before, 1 line after (follows environmental blocks)
        /// - NarrativeBlock: No spacing before, 1 line after (follows action blocks)
        /// - SystemBlock: No spacing before, 1 line after (follows other blocks)
        /// - StatsBlock: No spacing (for consecutive stats display)
        /// - MenuBlock: No spacing (compact menu items)
        /// </summary>
        private static readonly Dictionary<string, SpacingRule> BlockSpacingRules = new()
        {
            ["ActionBlock"] = new SpacingRule { Before = 0, After = 1 },
            ["EnvironmentalBlock"] = new SpacingRule { Before = 0, After = 1 },
            ["EffectBlock"] = new SpacingRule { Before = 0, After = 1 },
            ["NarrativeBlock"] = new SpacingRule { Before = 0, After = 1 },
            ["SystemBlock"] = new SpacingRule { Before = 0, After = 1 },
            ["StatsBlock"] = new SpacingRule { Before = 0, After = 0 },
            ["MenuBlock"] = new SpacingRule { Before = 0, After = 0 }
        };
        // ===== COLORED TEXT OVERLOADS =====
        
        /// <summary>
        /// Displays an ACTION BLOCK using ColoredText for better color management
        /// </summary>
        public static void DisplayActionBlock(List<ColoredText> actionText, string rollInfo, List<string>? statusEffects = null)
        {
            // Convert ColoredText to markup string for now (future: render directly)
            UIManager.WriteColoredText(actionText);
            UIManager.WriteLine($"    {rollInfo}", UIMessageType.RollInfo);
            
            var validEffects = statusEffects?.FindAll(e => !string.IsNullOrEmpty(e)) ?? new List<string>();
            foreach (var effect in validEffects)
            {
                UIManager.WriteLine($"    {ApplyKeywordColoring(effect)}", UIMessageType.EffectMessage);
            }
            
            ApplyBlockDelay("ActionBlock");
            ApplyBlockSpacing("ActionBlock");
        }
        
        /// <summary>
        /// Displays a NARRATIVE BLOCK using ColoredText
        /// </summary>
        public static void DisplayNarrativeBlock(List<ColoredText> narrativeText)
        {
            if (UIManager.UIConfig.DisableAllOutput || narrativeText == null || narrativeText.Count == 0) return;
            
            ManageBlockSpacing("NarrativeBlock");
            UIManager.WriteColoredText(narrativeText);
            ApplyBlockDelay("NarrativeBlock");
            ApplyBlockSpacing("NarrativeBlock");
        }
        
        /// <summary>
        /// Displays a SYSTEM BLOCK using ColoredText
        /// </summary>
        public static void DisplaySystemBlock(List<ColoredText> systemText)
        {
            if (UIManager.UIConfig.DisableAllOutput || systemText == null || systemText.Count == 0) return;
            
            ManageBlockSpacing("SystemBlock");
            UIManager.WriteColoredText(systemText);
            ApplyBlockDelay("SystemBlock");
            ApplyBlockSpacing("SystemBlock");
        }
        
        // ===== STRING-BASED METHODS (Legacy Support) =====
        
        /// <summary>
        /// Displays an ACTION BLOCK with consistent formatting
        /// Format: [Actor] action [Target] for X damage
        ///         (roll: X | attack Y - Z armor | speed: X.Xs)
        ///         [Status effects if any]
        /// </summary>
        /// <param name="actionText">The main action text</param>
        /// <param name="rollInfo">The roll information (always shown)</param>
        /// <param name="statusEffects">Status effects (only shown if present)</param>
        public static void DisplayActionBlock(string actionText, string rollInfo, List<string>? statusEffects = null)
        {
            if (UIManager.UIConfig.DisableAllOutput) return;
            
            // Extract Actor name from action text (format: [EntityName] ...)
            string? currentEntity = ExtractEntityNameFromMessage(actionText);
            
            // Add blank line when switching between different actors
            if (lastActingEntity != null && currentEntity != null && lastActingEntity != currentEntity)
            {
                UIManager.WriteBlankLine();
            }
            
            // Manage spacing between block types
            ManageBlockSpacing("ActionBlock");
            
            // Display the action with keyword coloring
            UIManager.WriteLine(ApplyKeywordColoring(actionText), UIMessageType.Combat);
            
            // Always display roll info with 4-space indentation (NO COLORING - keep stats white)
            UIManager.WriteLine($"    {rollInfo}", UIMessageType.RollInfo);
            
            // Display status effects if present
            var validEffects = statusEffects?.FindAll(e => !string.IsNullOrEmpty(e)) ?? new List<string>();
            foreach (var effect in validEffects)
            {
                UIManager.WriteLine($"    {ApplyKeywordColoring(effect)}", UIMessageType.EffectMessage);
            }
            
            // Update the last acting Actor
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            // Apply block delay and spacing
            ApplyBlockDelay("ActionBlock");
            ApplyBlockSpacing("ActionBlock");
        }
        
        /// <summary>
        /// Displays an EFFECT BLOCK for status effects like stun
        /// Format: [Actor] is stunned and cannot act!
        ///         (X turns remaining)
        /// </summary>
        /// <param name="effectText">The main effect text</param>
        /// <param name="details">Additional details (like turn count)</param>
        public static void DisplayEffectBlock(string effectText, string? details = null)
        {
            if (UIManager.UIConfig.DisableAllOutput) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("EffectBlock");
            
            // Display the effect with keyword coloring
            UIManager.WriteLine(ApplyKeywordColoring(effectText), UIMessageType.EffectMessage);
            
            // Display details if present
            if (!string.IsNullOrEmpty(details))
            {
                UIManager.WriteLine($"    ({ApplyKeywordColoring(details)})", UIMessageType.EffectMessage);
            }
            
            // Apply block delay and spacing
            ApplyBlockDelay("EffectBlock");
            ApplyBlockSpacing("EffectBlock");
        }
        
        /// <summary>
        /// Displays a NARRATIVE BLOCK with proper spacing
        /// Format: [blank line]
        ///         Narrative text
        ///         [blank line]
        /// </summary>
        /// <param name="narrativeText">The narrative message</param>
        public static void DisplayNarrativeBlock(string narrativeText)
        {
            if (UIManager.UIConfig.DisableAllOutput || string.IsNullOrEmpty(narrativeText)) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("NarrativeBlock");
            
            // Display the narrative with keyword coloring
            UIManager.WriteLine(ApplyKeywordColoring(narrativeText), UIMessageType.System);
            
            // Apply block delay and spacing
            ApplyBlockDelay("NarrativeBlock");
            ApplyBlockSpacing("NarrativeBlock");
        }
        
        /// <summary>
        /// Displays an ENVIRONMENTAL BLOCK for room actions
        /// Format: [Room] uses [Action] on [Target]!
        ///         [Effects if any]
        /// </summary>
        /// <param name="environmentalText">The environmental action text</param>
        /// <param name="effects">Environmental effects</param>
        public static void DisplayEnvironmentalBlock(string environmentalText, List<string>? effects = null)
        {
            if (UIManager.UIConfig.DisableAllOutput) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("EnvironmentalBlock");
            
            // Display the environmental action with keyword coloring
            UIManager.WriteLine(ApplyKeywordColoring(environmentalText), UIMessageType.Environmental);
            
            // Display effects if present
            var validEffects = effects?.FindAll(e => !string.IsNullOrEmpty(e)) ?? new List<string>();
            foreach (var effect in validEffects)
            {
                UIManager.WriteLine($"    {ApplyKeywordColoring(effect)}", UIMessageType.EffectMessage);
            }
            
            // Apply block delay and spacing
            ApplyBlockDelay("EnvironmentalBlock");
            ApplyBlockSpacing("EnvironmentalBlock");
        }
        
        /// <summary>
        /// Displays a SYSTEM BLOCK for system messages
        /// </summary>
        /// <param name="systemText">The system message</param>
        public static void DisplaySystemBlock(string systemText)
        {
            if (UIManager.UIConfig.DisableAllOutput || string.IsNullOrEmpty(systemText)) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("SystemBlock");
            
            UIManager.WriteLine(ApplyKeywordColoring(systemText), UIMessageType.System);
            
            // Apply block delay and spacing
            ApplyBlockDelay("SystemBlock");
            ApplyBlockSpacing("SystemBlock");
        }
        
        /// <summary>
        /// Displays a STATS BLOCK for consecutive stats display (no spacing)
        /// </summary>
        /// <param name="statsText">The stats text</param>
        public static void DisplayStatsBlock(string statsText)
        {
            if (UIManager.UIConfig.DisableAllOutput || string.IsNullOrEmpty(statsText)) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("StatsBlock");
            
            UIManager.WriteLine(ApplyKeywordColoring(statsText), UIMessageType.System);
            
            // Apply block delay and spacing
            ApplyBlockDelay("StatsBlock");
            ApplyBlockSpacing("StatsBlock");
        }
        
        /// <summary>
        /// Displays a MENU BLOCK for menu items
        /// </summary>
        /// <param name="menuText">The menu text</param>
        public static void DisplayMenuBlock(string menuText)
        {
            if (UIManager.UIConfig.DisableAllOutput || string.IsNullOrEmpty(menuText)) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("MenuBlock");
            
            UIManager.WriteLine(ApplyKeywordColoring(menuText), UIMessageType.Menu);
            
            // Apply block delay and spacing
            ApplyBlockDelay("MenuBlock");
            ApplyBlockSpacing("MenuBlock");
        }
        
        /// <summary>
        /// Applies delay based on block type
        /// </summary>
        /// <param name="blockType">The type of block for delay calculation</param>
        private static void ApplyBlockDelay(string blockType)
        {
            if (!UIManager.UIConfig.EnableDelays) return;
            
            // Use centralized delay system for individual messages
            CombatDelayManager.DelayAfterMessage();
        }
        
        /// <summary>
        /// Resets Actor tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            lastActingEntity = null;
            lastBlockType = null;
        }
        
        /// <summary>
        /// Manages spacing between different block types using centralized rules
        /// </summary>
        /// <param name="currentBlockType">The type of block being displayed</param>
        private static void ManageBlockSpacing(string currentBlockType)
        {
            // Get spacing rule for current block type
            if (BlockSpacingRules.TryGetValue(currentBlockType, out SpacingRule currentRule))
            {
                // Add blank lines before this block type
                for (int i = 0; i < currentRule.Before; i++)
                {
                    UIManager.WriteBlankLine();
                }
            }
            
            lastBlockType = currentBlockType;
        }

        /// <summary>
        /// Applies spacing after a block type is displayed
        /// </summary>
        /// <param name="blockType">The type of block that was just displayed</param>
        private static void ApplyBlockSpacing(string blockType)
        {
            // Get spacing rule for this block type
            if (BlockSpacingRules.TryGetValue(blockType, out SpacingRule rule))
            {
                // Add blank lines after this block type
                for (int i = 0; i < rule.After; i++)
                {
                    UIManager.WriteBlankLine();
                }
            }
        }
        
        /// <summary>
        /// Extracts Actor name from a message in the format [EntityName] ...
        /// </summary>
        /// <param name="message">Message to extract Actor name from</param>
        /// <returns>Actor name if found, null otherwise</returns>
        private static string? ExtractEntityNameFromMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            
            int startIndex = message.IndexOf('[');
            if (startIndex == -1) return null;
            
            int endIndex = message.IndexOf(']', startIndex);
            if (endIndex == -1) return null;
            
            return message.Substring(startIndex + 1, endIndex - startIndex - 1);
        }
    }
}



