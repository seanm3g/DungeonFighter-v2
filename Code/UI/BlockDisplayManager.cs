using System;
using System.Collections.Generic;
using System.Threading;

namespace RPGGame
{
    /// <summary>
    /// Manages block-based display system for consistent combat output formatting
    /// Each block type has its own delay and spacing rules
    /// </summary>
    public static class BlockDisplayManager
    {
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
        /// <summary>
        /// Displays an ACTION BLOCK with consistent formatting
        /// Format: [Entity] action [Target] for X damage
        ///         (roll: X | attack Y - Z armor | speed: X.Xs)
        ///         [Status effects if any]
        /// </summary>
        /// <param name="actionText">The main action text</param>
        /// <param name="rollInfo">The roll information (always shown)</param>
        /// <param name="statusEffects">Status effects (only shown if present)</param>
        public static void DisplayActionBlock(string actionText, string rollInfo, List<string>? statusEffects = null)
        {
            if (UIManager.UIConfig.DisableAllOutput) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("ActionBlock");
            
            // Extract entity name from action text (format: [EntityName] ...)
            string? currentEntity = ExtractEntityNameFromMessage(actionText);
            
            // Note: Entity switching spacing is now handled by centralized spacing rules
            // We only track the entity for potential future use, but don't add manual spacing
            
            // Display the action
            Console.WriteLine(actionText);
            
            // Always display roll info with 4-space indentation
            Console.WriteLine($"    {rollInfo}");
            
            // Display status effects if present
            var validEffects = statusEffects?.FindAll(e => !string.IsNullOrEmpty(e)) ?? new List<string>();
            foreach (var effect in validEffects)
            {
                Console.WriteLine($"    {effect}");
            }
            
            // Update the last acting entity
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
        /// Format: [Entity] is stunned and cannot act!
        ///         (X turns remaining)
        /// </summary>
        /// <param name="effectText">The main effect text</param>
        /// <param name="details">Additional details (like turn count)</param>
        public static void DisplayEffectBlock(string effectText, string? details = null)
        {
            if (UIManager.UIConfig.DisableAllOutput) return;
            
            // Manage spacing between block types
            ManageBlockSpacing("EffectBlock");
            
            // Display the effect
            Console.WriteLine(effectText);
            
            // Display details if present
            if (!string.IsNullOrEmpty(details))
            {
                Console.WriteLine($"    ({details})");
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
            
            // Display the narrative
            Console.WriteLine(narrativeText);
            
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
            
            // Display the environmental action
            Console.WriteLine(environmentalText);
            
            // Display effects if present
            var validEffects = effects?.FindAll(e => !string.IsNullOrEmpty(e)) ?? new List<string>();
            foreach (var effect in validEffects)
            {
                Console.WriteLine($"    {effect}");
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
            
            Console.WriteLine(systemText);
            
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
            
            Console.WriteLine(statsText);
            
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
            
            Console.WriteLine(menuText);
            
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
            
            // Get the delay multiplier for this block type
            double multiplier = UIManager.UIConfig.BeatTiming?.BlockDelays?.GetValueOrDefault(blockType, 0.0) ?? 0.0;
            
            if (multiplier > 0)
            {
                int delayMs = (int)((UIManager.UIConfig.BeatTiming?.CombatBeatLengthMs ?? 1500) * multiplier);
                Thread.Sleep(delayMs);
            }
        }
        
        /// <summary>
        /// Resets entity tracking for a new battle
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
                    Console.WriteLine();
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
                    Console.WriteLine();
                }
            }
        }
        
        /// <summary>
        /// Extracts entity name from a message in the format [EntityName] ...
        /// </summary>
        /// <param name="message">Message to extract entity name from</param>
        /// <returns>Entity name if found, null otherwise</returns>
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
