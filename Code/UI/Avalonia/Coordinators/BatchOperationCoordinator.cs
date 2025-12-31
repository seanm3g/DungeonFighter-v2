using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;
using RPGGame;

namespace RPGGame.UI.Avalonia.Coordinators
{
    /// <summary>
    /// Coordinates batch operations for the UI
    /// </summary>
    public class BatchOperationCoordinator
    {
        private readonly ICanvasTextManager textManager;
        private readonly MessageWritingCoordinator messageWritingCoordinator;

        public BatchOperationCoordinator(ICanvasTextManager textManager, MessageWritingCoordinator messageWritingCoordinator)
        {
            this.textManager = textManager;
            this.messageWritingCoordinator = messageWritingCoordinator;
        }

        /// <summary>
        /// Writes multiple ColoredText segment lists with delays between each message
        /// Messages are added one at a time with delays between them for better pacing
        /// Stores structured data directly to eliminate round-trip conversions
        /// </summary>
        /// <param name="messageGroups">List of tuples containing (segments, messageType) to write</param>
        /// <param name="delayAfterBatchMs">Optional delay in milliseconds after the batch is added (for combat timing)</param>
        public void WriteColoredSegmentsBatch(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0, Character? character = null)
        {
            if (messageGroups == null || messageGroups.Count == 0)
                return;
            
            // Extract segments (ignore messageType for now - all go to same buffer)
            // Empty segments are treated as blank lines for spacing
            var segmentsList = new List<List<ColoredText>>();
            foreach (var (segments, messageType) in messageGroups)
            {
                if (segments != null)
                {
                    // Add segment even if empty (for blank lines)
                    segmentsList.Add(segments);
                }
            }
            
            if (segmentsList.Count == 0)
                return;
            
            // Fire and forget - use async version but don't wait
            _ = WriteColoredSegmentsBatchAsync(messageGroups, delayAfterBatchMs, character);
        }
        
        /// <summary>
        /// Writes multiple ColoredText segment lists with delays between each message and waits for completion
        /// This async version allows the combat loop to wait for each action's display to complete
        /// Adds delays between each message group (line) within the action block
        /// </summary>
        /// <param name="messageGroups">List of tuples containing (segments, messageType) to write</param>
        /// <param name="delayAfterBatchMs">Optional delay in milliseconds after the batch is added (for combat timing)</param>
        public async Task WriteColoredSegmentsBatchAsync(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0, Character? character = null)
        {
            if (messageGroups == null || messageGroups.Count == 0)
                return;
            
            // Extract segments (ignore messageType for now - all go to same buffer)
            // Empty segments are treated as blank lines for spacing
            var segmentsList = new List<List<ColoredText>>();
            foreach (var (segments, messageType) in messageGroups)
            {
                if (segments != null)
                {
                    // Add segment even if empty (for blank lines)
                    segmentsList.Add(segments);
                }
            }
            
            if (segmentsList.Count == 0)
                return;
            
            // Store structured data directly - no conversion needed
            // Route to the correct per-character display manager if available
            if (textManager is CanvasTextManager canvasTextManager)
            {
                var targetDisplayManager = canvasTextManager.GetDisplayManagerForCharacter(character);
                
                // Add messages one at a time with delays between them
                // This creates a line-by-line reveal effect for action blocks
                for (int i = 0; i < segmentsList.Count; i++)
                {
                    var segment = segmentsList[i];
                    
                    // Add this single message
                    targetDisplayManager.AddMessage(segment);
                    
                    // Add delay between messages (but not after the last one - that's handled by delayAfterBatchMs)
                    if (i < segmentsList.Count - 1)
                    {
                        // Use MessageDelayMs for delays between lines within an action block
                        await CombatDelayManager.DelayAfterMessageAsync();
                    }
                }
                
                // Apply final delay after the entire batch (delay between action blocks)
                if (delayAfterBatchMs > 0)
                {
                    await Task.Delay(delayAfterBatchMs);
                }
            }
            else
            {
                // Fallback: convert to strings for non-CanvasTextManager implementations
                var markupMessages = segmentsList.Select(segments => ColoredTextRenderer.RenderAsMarkup(segments)).ToList();
                
                // Add messages one at a time with delays between them
                for (int i = 0; i < markupMessages.Count; i++)
                {
                    var message = markupMessages[i];
                    
                    // Add this single message (use WriteLine for individual messages)
                    if (string.IsNullOrEmpty(message))
                    {
                        messageWritingCoordinator.WriteBlankLine();
                    }
                    else
                    {
                        messageWritingCoordinator.WriteLine(message);
                    }
                    
                    // Add delay between messages (but not after the last one - that's handled by delayAfterBatchMs)
                    if (i < markupMessages.Count - 1)
                    {
                        // Use MessageDelayMs for delays between lines within an action block
                        await CombatDelayManager.DelayAfterMessageAsync();
                    }
                }
                
                // Apply final delay after the entire batch (delay between action blocks)
                if (delayAfterBatchMs > 0)
                {
                    await Task.Delay(delayAfterBatchMs);
                }
            }
        }
    }
}

