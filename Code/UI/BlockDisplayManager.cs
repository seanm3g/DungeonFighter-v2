using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Manages block-based display system for consistent combat output formatting.
    /// Uses TextSpacingSystem for context-aware spacing that maps directly to reference output.
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
        // ===== COLORED TEXT OVERLOADS =====
        
        /// <summary>
        /// Displays an ACTION BLOCK using ColoredText for better color management
        /// This is the primary method - uses structured ColoredText for both action and roll info
        /// All narratives are included in the turn block to ensure each character's turn is displayed as a single unit
        /// </summary>
        public static void DisplayActionBlock(List<ColoredText> actionText, List<ColoredText> rollInfo, List<List<ColoredText>>? statusEffects = null, List<ColoredText>? criticalMissNarrative = null, List<List<ColoredText>>? narratives = null)
        {
            // Extract entity name from ColoredText for tracking (but don't add blank lines between actor changes)
            string? currentEntity = null;
            if (actionText != null && actionText.Count > 0)
            {
                string plainText = ColoredTextRenderer.RenderAsPlainText(actionText);
                currentEntity = ExtractEntityNameFromMessage(plainText);
            }
            
            // Apply context-aware spacing based on what came before
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction);
            
            // Render both actionText and rollInfo together as a single batch
            // All lines of a combat action block are added together, then rendered as one unit
            var customUIManager = UIManager.GetCustomUIManager();
            if (customUIManager != null && customUIManager is UI.Avalonia.CanvasUICoordinator canvasCoordinator)
            {
                // Collect all messages for this combat action block
                var messageGroups = new List<(List<ColoredText> segments, UIMessageType messageType)>();
                
                // Add action text
                if (actionText != null && actionText.Count > 0)
                {
                    messageGroups.Add((actionText, UIMessageType.Combat));
                }
                
                // Add roll info
                if (rollInfo != null && rollInfo.Count > 0)
                {
                    messageGroups.Add((rollInfo, UIMessageType.RollInfo));
                }
                
                // Add critical miss narrative
                if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
                {
                    messageGroups.Add((criticalMissNarrative, UIMessageType.System));
                }
                
                // Add status effects
                if (statusEffects != null)
                {
                    foreach (var effect in statusEffects)
                    {
                        if (effect != null && effect.Count > 0)
                        {
                            messageGroups.Add((effect, UIMessageType.EffectMessage));
                        }
                    }
                }
                
                // Add all narratives (all part of the same turn block)
                if (narratives != null)
                {
                    foreach (var narrative in narratives)
                    {
                        if (narrative != null && narrative.Count > 0)
                        {
                            messageGroups.Add((narrative, UIMessageType.System));
                        }
                    }
                }
                
                // Calculate delay after batch (use ActionDelayMs for complete action blocks)
                int delayAfterBatchMs = 0;
                if (UIManager.EnableDelays)
                {
                    delayAfterBatchMs = CombatDelayManager.Config.ActionDelayMs;
                }
                
                // Write all messages as a single batch (synchronous - for backward compatibility)
                if (messageGroups.Count > 0)
                {
                    canvasCoordinator.WriteColoredSegmentsBatch(messageGroups, delayAfterBatchMs);
                }
            }
            else if (customUIManager != null)
            {
                // Fallback for non-CanvasUICoordinator UI managers
                // Write actionText directly to bypass delay
                if (actionText != null && actionText.Count > 0)
                {
                    customUIManager.WriteColoredSegments(actionText, UIMessageType.Combat);
                }
                // Write rollInfo immediately after (on next line) without delay
                if (rollInfo != null && rollInfo.Count > 0)
                {
                    customUIManager.WriteColoredSegments(rollInfo, UIMessageType.RollInfo);
                }
                // Write critical miss narrative immediately after roll info (on next line) without delay
                if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
                {
                    customUIManager.WriteColoredSegments(criticalMissNarrative, UIMessageType.System);
                }
                // Render status effects if present
                if (statusEffects != null)
                {
                    foreach (var effect in statusEffects)
                    {
                        if (effect != null && effect.Count > 0)
                        {
                            customUIManager.WriteColoredSegments(effect, UIMessageType.EffectMessage);
                        }
                    }
                }
                // Write all narratives (all part of the same turn block)
                if (narratives != null)
                {
                    foreach (var narrative in narratives)
                    {
                        if (narrative != null && narrative.Count > 0)
                        {
                            customUIManager.WriteColoredSegments(narrative, UIMessageType.System);
                        }
                    }
                }
                // Apply delay after all lines are written
                if (UIManager.EnableDelays)
                {
                    CombatDelayManager.DelayAfterMessage();
                }
            }
            else
            {
                // For console, write both lines directly to bypass delays
                if (actionText != null && actionText.Count > 0)
                {
                    ColoredConsoleWriter.WriteSegments(actionText);
                    Console.WriteLine();
                }
                if (rollInfo != null && rollInfo.Count > 0)
                {
                    ColoredConsoleWriter.WriteSegments(rollInfo);
                    Console.WriteLine();
                }
                // Write critical miss narrative immediately after roll info (on next line) without delay
                if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
                {
                    ColoredConsoleWriter.WriteSegments(criticalMissNarrative);
                    Console.WriteLine();
                }
                // Render status effects if present
                if (statusEffects != null)
                {
                    foreach (var effect in statusEffects)
                    {
                        if (effect != null && effect.Count > 0)
                        {
                            ColoredConsoleWriter.WriteSegments(effect);
                            Console.WriteLine();
                        }
                    }
                }
                // Write all narratives (all part of the same turn block)
                if (narratives != null)
                {
                    foreach (var narrative in narratives)
                    {
                        if (narrative != null && narrative.Count > 0)
                        {
                            ColoredConsoleWriter.WriteSegments(narrative);
                            Console.WriteLine();
                        }
                    }
                }
                // Apply delay after all lines are written
                if (UIManager.EnableDelays)
                {
                    CombatDelayManager.DelayAfterMessage();
                }
            }
            
            // Update the last acting Actor
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            // Record that this block was displayed for spacing system
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction);
            
            // Delay and spacing are now handled by the batch method for GUI
            // For console, still apply delay and spacing
            if (customUIManager == null)
            {
                ApplyBlockDelay("ActionBlock");
            }
        }
        
        /// <summary>
        /// Displays an ACTION BLOCK using ColoredText (async version)
        /// This version waits for the display delay to complete, allowing the combat loop to wait for each action
        /// </summary>
        public static async System.Threading.Tasks.Task DisplayActionBlockAsync(List<ColoredText> actionText, List<ColoredText> rollInfo, List<List<ColoredText>>? statusEffects = null, List<ColoredText>? criticalMissNarrative = null, List<List<ColoredText>>? narratives = null)
        {
            // Extract entity name from ColoredText for tracking (but don't add blank lines between actor changes)
            string? currentEntity = null;
            if (actionText != null && actionText.Count > 0)
            {
                string plainText = ColoredTextRenderer.RenderAsPlainText(actionText);
                currentEntity = ExtractEntityNameFromMessage(plainText);
            }
            
            // Apply context-aware spacing based on what came before
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction);
            
            // Render both actionText and rollInfo together as a single batch
            // All lines of a combat action block are added together, then rendered as one unit
            var customUIManager = UIManager.GetCustomUIManager();
            if (customUIManager != null && customUIManager is UI.Avalonia.CanvasUICoordinator canvasCoordinator)
            {
                // Collect all messages for this combat action block
                var messageGroups = new List<(List<ColoredText> segments, UIMessageType messageType)>();
                
                // Add action text
                if (actionText != null && actionText.Count > 0)
                {
                    messageGroups.Add((actionText, UIMessageType.Combat));
                }
                
                // Add roll info
                if (rollInfo != null && rollInfo.Count > 0)
                {
                    messageGroups.Add((rollInfo, UIMessageType.RollInfo));
                }
                
                // Add critical miss narrative
                if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
                {
                    messageGroups.Add((criticalMissNarrative, UIMessageType.System));
                }
                
                // Add status effects
                if (statusEffects != null)
                {
                    foreach (var effect in statusEffects)
                    {
                        if (effect != null && effect.Count > 0)
                        {
                            messageGroups.Add((effect, UIMessageType.EffectMessage));
                        }
                    }
                }
                
                // Add all narratives (all part of the same turn block)
                if (narratives != null)
                {
                    foreach (var narrative in narratives)
                    {
                        if (narrative != null && narrative.Count > 0)
                        {
                            messageGroups.Add((narrative, UIMessageType.System));
                        }
                    }
                }
                
                // Calculate delay after batch (use ActionDelayMs for complete action blocks)
                int delayAfterBatchMs = 0;
                if (UIManager.EnableDelays)
                {
                    delayAfterBatchMs = CombatDelayManager.Config.ActionDelayMs;
                }
                
                // Write all messages as a single batch and wait for delay
                if (messageGroups.Count > 0)
                {
                    await canvasCoordinator.WriteColoredSegmentsBatchAsync(messageGroups, delayAfterBatchMs);
                }
            }
            else
            {
                // For non-CanvasUICoordinator or console, use synchronous version
                if (actionText != null && rollInfo != null)
                {
                    DisplayActionBlock(actionText, rollInfo, statusEffects, criticalMissNarrative, narratives);
                }
            }
            
            // Update the last acting Actor
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            // Record that this block was displayed for spacing system
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction);
            
            // Delay and spacing are now handled by the batch method for GUI
            // For console, still apply delay and spacing
            if (customUIManager == null)
            {
                ApplyBlockDelay("ActionBlock");
            }
        }
        
        /// <summary>
        /// Displays a NARRATIVE BLOCK using ColoredText
        /// </summary>
        public static void DisplayNarrativeBlock(List<ColoredText> narrativeText)
        {
            if (UIManager.DisableAllUIOutput || narrativeText == null || narrativeText.Count == 0) return;
            
            // Apply context-aware spacing
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.Narrative);
            
            UIManager.WriteColoredText(narrativeText);
            ApplyBlockDelay("NarrativeBlock");
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.Narrative);
        }
        
        /// <summary>
        /// Displays a SYSTEM BLOCK using ColoredText
        /// </summary>
        public static void DisplaySystemBlock(List<ColoredText> systemText)
        {
            if (UIManager.DisableAllUIOutput || systemText == null || systemText.Count == 0) return;
            
            // Apply context-aware spacing (defaults to 0, can be overridden for specific cases)
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.SystemMessage);
            
            UIManager.WriteColoredText(systemText);
            ApplyBlockDelay("SystemBlock");
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.SystemMessage);
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
            if (UIManager.DisableAllUIOutput) return;
            
            // Extract Actor name from action text (supports: [EntityName] ... or EntityName hits ...)
            string? currentEntity = ExtractEntityNameFromMessage(actionText);
            
            // Apply context-aware spacing based on what came before
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction);
            
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
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction);
            
            // Apply block delay
            ApplyBlockDelay("ActionBlock");
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
            if (UIManager.DisableAllUIOutput) return;
            
            // Apply context-aware spacing based on what came before
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.StatusEffect);
            
            // Display the effect with keyword coloring
            UIManager.WriteLine(ApplyKeywordColoring(effectText), UIMessageType.EffectMessage);
            
            // Display details if present
            if (!string.IsNullOrEmpty(details))
            {
                UIManager.WriteLine($"    ({ApplyKeywordColoring(details)})", UIMessageType.EffectMessage);
            }
            
            // Record that this block was displayed (StatusEffect doesn't affect spacing, so it's not recorded)
            // Apply block delay
            ApplyBlockDelay("EffectBlock");
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
            if (UIManager.DisableAllUIOutput || string.IsNullOrEmpty(narrativeText)) return;
            
            // Apply context-aware spacing
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.Narrative);
            
            // Display the narrative with keyword coloring
            UIManager.WriteLine(ApplyKeywordColoring(narrativeText), UIMessageType.System);
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.Narrative);
            
            // Apply block delay
            ApplyBlockDelay("NarrativeBlock");
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
            if (UIManager.DisableAllUIOutput) return;
            
            // Apply context-aware spacing (blank line before environmental actions)
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.EnvironmentalAction);
            
            // Display the environmental action with keyword coloring
            UIManager.WriteLine(ApplyKeywordColoring(environmentalText), UIMessageType.Environmental);
            
            // Display effects if present (part of environmental block, no spacing)
            var validEffects = effects?.FindAll(e => !string.IsNullOrEmpty(e)) ?? new List<string>();
            foreach (var effect in validEffects)
            {
                UIManager.WriteLine($"    {ApplyKeywordColoring(effect)}", UIMessageType.EffectMessage);
                // Status effects are part of the environmental block, don't record separately
            }
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnvironmentalAction);
            
            // Apply block delay
            ApplyBlockDelay("EnvironmentalBlock");
        }
        
        /// <summary>
        /// Displays a SYSTEM BLOCK for system messages
        /// </summary>
        /// <param name="systemText">The system message (supports multi-line with \n)</param>
        public static void DisplaySystemBlock(string systemText)
        {
            if (UIManager.DisableAllUIOutput || string.IsNullOrEmpty(systemText)) return;
            
            // Apply context-aware spacing (defaults to 0, can be overridden for specific cases)
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.SystemMessage);
            
            // Split multi-line strings and write each line separately to ensure proper rendering
            string[] lines = systemText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    UIManager.WriteLine(ApplyKeywordColoring(line), UIMessageType.System);
                }
            }
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.SystemMessage);
            
            // Apply block delay
            ApplyBlockDelay("SystemBlock");
        }
        
        /// <summary>
        /// Displays a STATS BLOCK for consecutive stats display (no spacing)
        /// </summary>
        /// <param name="statsText">The stats text</param>
        public static void DisplayStatsBlock(string statsText)
        {
            if (UIManager.DisableAllUIOutput || string.IsNullOrEmpty(statsText)) return;
            
            // Stats blocks have no spacing (consecutive display)
            // Apply context-aware spacing (defaults to 0 for stats)
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.StatsBlock);
            
            UIManager.WriteLine(ApplyKeywordColoring(statsText), UIMessageType.System);
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.StatsBlock);
            
            // Apply block delay
            ApplyBlockDelay("StatsBlock");
        }
        
        /// <summary>
        /// Displays a MENU BLOCK for menu items
        /// </summary>
        /// <param name="menuText">The menu text</param>
        public static void DisplayMenuBlock(string menuText)
        {
            if (UIManager.DisableAllUIOutput || string.IsNullOrEmpty(menuText)) return;
            
            // Menu blocks have no spacing (compact display)
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.MenuBlock);
            
            UIManager.WriteLine(ApplyKeywordColoring(menuText), UIMessageType.Menu);
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.MenuBlock);
            
            // Apply block delay
            ApplyBlockDelay("MenuBlock");
        }
        
        /// <summary>
        /// Applies delay based on block type
        /// </summary>
        /// <param name="blockType">The type of block for delay calculation</param>
        private static void ApplyBlockDelay(string blockType)
        {
            if (!UIManager.EnableDelays) return;
            
            // Use centralized delay system for individual messages
            CombatDelayManager.DelayAfterMessage();
        }
        
        /// <summary>
        /// Resets Actor tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            lastActingEntity = null;
            TextSpacingSystem.Reset();
        }
        
        /// <summary>
        /// Extracts Actor name from a message
        /// Supports formats: "[EntityName] ..." or "EntityName hits ..."
        /// </summary>
        /// <param name="message">Message to extract Actor name from</param>
        /// <returns>Actor name if found, null otherwise</returns>
        private static string? ExtractEntityNameFromMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            
            // Try bracket format first: [EntityName] ...
            int startIndex = message.IndexOf('[');
            if (startIndex != -1)
            {
                int endIndex = message.IndexOf(']', startIndex);
                if (endIndex != -1)
                {
                    return message.Substring(startIndex + 1, endIndex - startIndex - 1);
                }
            }
            
            // Try format without brackets: "EntityName hits ..."
            int hitsIndex = message.IndexOf(" hits ");
            if (hitsIndex > 0)
            {
                return message.Substring(0, hitsIndex).Trim();
            }
            
            // Try format: "EntityName misses ..."
            int missesIndex = message.IndexOf(" misses ");
            if (missesIndex > 0)
            {
                return message.Substring(0, missesIndex).Trim();
            }
            
            // Try format: "EntityName uses ..."
            int usesIndex = message.IndexOf(" uses ");
            if (usesIndex > 0)
            {
                return message.Substring(0, usesIndex).Trim();
            }
            
            return null;
        }
    }
}



