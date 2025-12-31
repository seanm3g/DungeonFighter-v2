using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Services;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Manages block-based display system for consistent combat output formatting.
    /// Uses TextSpacingSystem for context-aware spacing that maps directly to reference output.
    /// Refactored to use extracted components: renderers, message collector, entity name extractor, and delay manager.
    /// </summary>
    public static class BlockDisplayManager
    {
        private static string? lastActingEntity = null;
        private static GameStateManager? stateManager = null;
        private static readonly MessageFilterService filterService = new MessageFilterService();
        
        /// <summary>
        /// Sets the GameStateManager instance for checking active character
        /// </summary>
        public static void SetStateManager(GameStateManager? manager)
        {
            stateManager = manager;
        }
        
        /// <summary>
        /// Checks if combat logs should be displayed based on game state (menu states only)
        /// Note: Character filtering is no longer done here - messages are routed to per-character display managers
        /// and only the active character's display manager is rendered
        /// </summary>
        /// <param name="character">The character this combat action belongs to (unused, kept for compatibility)</param>
        /// <returns>True if combat logs should be displayed, false otherwise</returns>
        private static bool ShouldDisplayCombatLog(Character? character)
        {
            // Only check menu states - don't filter by character
            // Messages will be routed to the correct per-character display manager
            // Only the active character's display manager will be rendered
            if (stateManager == null)
            {
                return true; // Backward compatibility
            }
            
            var currentState = stateManager.CurrentState;
            bool isMenuState = filterService.IsMenuState(stateManager);
            
            // Don't add combat messages if we're in a menu state
            if (isMenuState)
            {
                return false;
            }
            
            return true;
        }
        
        // ===== COLORED TEXT OVERLOADS =====
        
        /// <summary>
        /// Displays an ACTION BLOCK using ColoredText for better color management
        /// This is the primary method - uses structured ColoredText for both action and roll info
        /// All narratives are included in the turn block to ensure each character's turn is displayed as a single unit
        /// Only displays if the character is currently active (for multi-character support)
        /// </summary>
        /// <param name="actionText">The action text to display</param>
        /// <param name="rollInfo">Roll information</param>
        /// <param name="statusEffects">Status effects</param>
        /// <param name="criticalMissNarrative">Critical miss narrative</param>
        /// <param name="narratives">Additional narratives</param>
        /// <param name="character">The character this combat action belongs to (null = always display)</param>
        public static void DisplayActionBlock(List<ColoredText> actionText, List<ColoredText> rollInfo, List<List<ColoredText>>? statusEffects = null, List<ColoredText>? criticalMissNarrative = null, List<List<ColoredText>>? narratives = null, Character? character = null)
        {
            try
            {
                // Check if we should display this combat action
                if (!ShouldDisplayCombatLog(character))
                {
                    return;
                }
                
                // Extract entity name from ColoredText for tracking
                string? currentEntity = null;
                if (actionText != null && actionText.Count > 0)
                {
                    string plainText = ColoredTextRenderer.RenderAsPlainText(actionText);
                    currentEntity = EntityNameExtractor.ExtractEntityNameFromMessage(plainText);
                }
                
                // Apply context-aware spacing based on what came before and actor changes
                // Note: Spacing system handles all spacing - no manual blank lines needed
                TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction, currentEntity);
                
                // Collect all messages for this combat action block
                var messageGroups = BlockMessageCollector.CollectActionBlockMessages(actionText, rollInfo, statusEffects, criticalMissNarrative, narratives);
                
                // Only render if we have messages to display
                if (messageGroups != null && messageGroups.Count > 0)
                {
                    // Calculate delay after batch (use ActionDelayMs for complete action blocks)
                    int delayAfterBatchMs = BlockDelayManager.CalculateActionBlockDelay();
                    
                    // Render using appropriate renderer
                    // Pass character to route to correct per-character display manager
                    var renderer = BlockRendererFactory.GetRenderer();
                    renderer.RenderMessageGroups(messageGroups, delayAfterBatchMs, character);
                }
                
                // Update the last acting Actor (for backward compatibility)
                if (currentEntity != null)
                {
                    lastActingEntity = currentEntity;
                }
                
                // Record that this block was displayed for spacing system (with entity tracking)
                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, currentEntity);
                
                // Delay and spacing are now handled by the batch method for GUI
                // For console, still apply delay and spacing
                var customUIManager = UIManager.GetCustomUIManager();
                if (customUIManager == null)
                {
                    BlockDelayManager.ApplyBlockDelay();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow combat to continue
                System.Diagnostics.Debug.WriteLine($"Error in BlockDisplayManager.DisplayActionBlock: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Displays an ACTION BLOCK using ColoredText (async version)
        /// This version waits for the display delay to complete, allowing the combat loop to wait for each action
        /// Only displays if the character is currently active (for multi-character support)
        /// </summary>
        /// <param name="actionText">The action text to display</param>
        /// <param name="rollInfo">Roll information</param>
        /// <param name="statusEffects">Status effects</param>
        /// <param name="criticalMissNarrative">Critical miss narrative</param>
        /// <param name="narratives">Additional narratives</param>
        /// <param name="character">The character this combat action belongs to (null = always display)</param>
        public static async Task DisplayActionBlockAsync(List<ColoredText> actionText, List<ColoredText> rollInfo, List<List<ColoredText>>? statusEffects = null, List<ColoredText>? criticalMissNarrative = null, List<List<ColoredText>>? narratives = null, Character? character = null)
        {
            try
            {
                // Check if we should display this combat action
                if (!ShouldDisplayCombatLog(character))
                {
                    return;
                }
                
                // Extract entity name from ColoredText for tracking
                string? currentEntity = null;
                if (actionText != null && actionText.Count > 0)
                {
                    string plainText = ColoredTextRenderer.RenderAsPlainText(actionText);
                    currentEntity = EntityNameExtractor.ExtractEntityNameFromMessage(plainText);
                }
                
                // Apply context-aware spacing based on what came before and actor changes
                // Note: Spacing system handles all spacing - no manual blank lines needed
                TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction, currentEntity);
                
                // Collect all messages for this combat action block
                var messageGroups = BlockMessageCollector.CollectActionBlockMessages(actionText, rollInfo, statusEffects, criticalMissNarrative, narratives);
                
                // Only render if we have messages to display
                if (messageGroups != null && messageGroups.Count > 0)
                {
                    // Calculate delay after batch (use ActionDelayMs for complete action blocks)
                    int delayAfterBatchMs = BlockDelayManager.CalculateActionBlockDelay();
                    
                    // Render using appropriate renderer (async)
                    // Pass character to route to correct per-character display manager
                    var renderer = BlockRendererFactory.GetRenderer();
                    await renderer.RenderMessageGroupsAsync(messageGroups, delayAfterBatchMs, character);
                }
                
                // Update the last acting Actor (for backward compatibility)
                if (currentEntity != null)
                {
                    lastActingEntity = currentEntity;
                }
                
                // Record that this block was displayed for spacing system (with entity tracking)
                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, currentEntity);
                
                // Delay and spacing are now handled by the batch method for GUI
                // For console, still apply delay and spacing
                var customUIManager = UIManager.GetCustomUIManager();
                if (customUIManager == null)
                {
                    BlockDelayManager.ApplyBlockDelay();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow combat to continue
                System.Diagnostics.Debug.WriteLine($"Error in BlockDisplayManager.DisplayActionBlockAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Core method for displaying simple blocks with consistent pattern
        /// </summary>
        private static void DisplayBlockCore(
            TextSpacingSystem.BlockType blockType,
            List<ColoredText> content,
            UIMessageType messageType,
            bool applyDelay = true)
        {
            if (UIManager.DisableAllUIOutput || content == null || content.Count == 0) return;
            
            // Apply context-aware spacing
            TextSpacingSystem.ApplySpacingBefore(blockType);
            
            // Display content
            UIManager.WriteColoredText(content);
            
            // Apply delay if requested
            if (applyDelay)
            {
                BlockDelayManager.ApplyBlockDelay();
            }
            
            // Record that this block was displayed
            TextSpacingSystem.RecordBlockDisplayed(blockType);
        }
        
        /// <summary>
        /// Displays a NARRATIVE BLOCK using ColoredText
        /// </summary>
        public static void DisplayNarrativeBlock(List<ColoredText> narrativeText)
        {
            DisplayBlockCore(TextSpacingSystem.BlockType.Narrative, narrativeText, UIMessageType.System);
        }
        
        /// <summary>
        /// Displays a SYSTEM BLOCK using ColoredText
        /// </summary>
        public static void DisplaySystemBlock(List<ColoredText> systemText)
        {
            DisplayBlockCore(TextSpacingSystem.BlockType.SystemMessage, systemText, UIMessageType.System);
        }
        
        /// <summary>
        /// Displays an EFFECT BLOCK using ColoredText
        /// Supports actor-based spacing (no blank line for same actor, blank line for different actor)
        /// All combat logs follow the same action block pattern: first line normal, subsequent lines indented
        /// </summary>
        public static void DisplayEffectBlock(List<ColoredText> effectText, List<ColoredText>? details = null)
        {
            if (UIManager.DisableAllUIOutput || effectText == null || effectText.Count == 0) return;
            
            // Extract entity name from effect text for tracking
            // Status effect messages may be in formats like "[Actor] takes X damage" or "Actor is affected by..."
            string? effectEntity = null;
            if (effectText != null && effectText.Count > 0)
            {
                string plainText = ColoredTextRenderer.RenderAsPlainText(effectText);
                effectEntity = EntityNameExtractor.ExtractEntityNameFromMessage(plainText);
            }
            
            // Apply context-aware spacing with entity tracking
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.StatusEffect, effectEntity);
            
            // Display the effect (first line - no indentation, as per action block pattern)
            UIManager.WriteColoredText(effectText!); // effectText is guaranteed non-null due to check at line 258
            
            // Display details if present (subsequent line - 5-space indentation to match action block pattern)
            if (details != null && details.Count > 0)
            {
                // Add delay between effect line and details line
                BlockDelayManager.ApplyBlockDelay();
                
                const string ACTION_BLOCK_INDENT = "     "; // 5 spaces
                var detailsBuilder = new ColoredTextBuilder();
                detailsBuilder.Add(ACTION_BLOCK_INDENT);
                detailsBuilder.Add("(");
                detailsBuilder.AddRange(details);
                detailsBuilder.Add(")");
                UIManager.WriteColoredText(detailsBuilder.Build());
            }
            
            // Record that this standalone status effect block was displayed (with entity tracking)
            // (DisplayEffectBlock is only used for standalone status effects like stun messages)
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.StatusEffect, effectEntity);
            
            // Apply block delay
            BlockDelayManager.ApplyBlockDelay();
        }
        
        /// <summary>
        /// Displays an ENVIRONMENTAL BLOCK using ColoredText
        /// All combat logs follow the same action block pattern: first line normal, subsequent lines indented
        /// </summary>
        public static void DisplayEnvironmentalBlock(List<ColoredText> environmentalText, List<List<ColoredText>>? effects = null)
        {
            if (UIManager.DisableAllUIOutput || environmentalText == null || environmentalText.Count == 0) return;
            
            // Extract environment name from environmental text for tracking
            // Environmental actions typically start with room/environment name
            string? environmentEntity = null;
            if (environmentalText != null && environmentalText.Count > 0)
            {
                string plainText = ColoredTextRenderer.RenderAsPlainText(environmentalText);
                // Remove emoji prefix if present (🌍 or similar) - emojis are typically single characters
                plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"^[^\w\s\[\]]+\s*", "");
                
                // Use EntityNameExtractor to extract environment name
                // It handles formats like "[EntityName] ..." or "EntityName uses ..."
                environmentEntity = EntityNameExtractor.ExtractEntityNameFromMessage(plainText);
                if (environmentEntity == null)
                {
                    // Fallback: use a generic identifier for environment
                    // We'll use a consistent identifier so all environmental actions are treated as the same "actor"
                    environmentEntity = "Environment";
                }
            }
            
            // Apply context-aware spacing (blank line before environmental actions, checking actor changes)
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.EnvironmentalAction, environmentEntity);
            
            // Display the environmental action (first line - no indentation, as per action block pattern)
            UIManager.WriteColoredText(environmentalText!);
            
            // Display effects if present (subsequent lines - 5-space indentation to match action block pattern)
            const string ACTION_BLOCK_INDENT = "     "; // 5 spaces
            if (effects != null)
            {
                // Add delay after first environmental line before effects start
                BlockDelayManager.ApplyBlockDelay();
                
                bool isFirstEffect = true;
                foreach (var effect in effects)
                {
                    if (effect != null && effect.Count > 0)
                    {
                        // Add delay between effects (including before the first one, which we already did above)
                        if (!isFirstEffect)
                        {
                            BlockDelayManager.ApplyBlockDelay();
                        }
                        isFirstEffect = false;
                        
                        // Remove any existing leading whitespace to avoid double indentation
                        // (effects may already have indentation from EnvironmentalActionExecutor)
                        var trimmedEffect = new List<ColoredText>(effect);
                        if (trimmedEffect.Count > 0)
                        {
                            string firstText = trimmedEffect[0].Text ?? "";
                            if (string.IsNullOrWhiteSpace(firstText))
                            {
                                // First segment is whitespace-only - remove it
                                trimmedEffect.RemoveAt(0);
                            }
                            else if (firstText.TrimStart() != firstText)
                            {
                                // First segment has leading whitespace - trim it
                                trimmedEffect[0] = new ColoredText(firstText.TrimStart(), trimmedEffect[0].Color);
                            }
                        }
                        
                        // Always use 5-space indentation for all effect lines (subsequent lines in action block)
                        var indentedEffect = new ColoredTextBuilder();
                        indentedEffect.Add(ACTION_BLOCK_INDENT);
                        indentedEffect.AddRange(trimmedEffect);
                        UIManager.WriteColoredText(indentedEffect.Build());
                    }
                }
            }
            
            // Record that this block was displayed (with entity tracking)
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnvironmentalAction, environmentEntity);
            
            // Apply block delay
            BlockDelayManager.ApplyBlockDelay();
        }
        
        /// <summary>
        /// Resets Actor tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            lastActingEntity = null;
            TextSpacingSystem.Reset();
        }
        
    }
}



