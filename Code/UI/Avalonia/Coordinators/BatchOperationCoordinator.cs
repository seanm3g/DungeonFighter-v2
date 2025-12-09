using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Managers;

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
        /// Writes multiple ColoredText segment lists as a single batch
        /// All messages are added to the buffer together, then a single render is scheduled
        /// This ensures combat action blocks appear as a single unit
        /// Stores structured data directly to eliminate round-trip conversions
        /// </summary>
        /// <param name="messageGroups">List of tuples containing (segments, messageType) to write</param>
        /// <param name="delayAfterBatchMs">Optional delay in milliseconds after the batch is added (for combat timing)</param>
        public void WriteColoredSegmentsBatch(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0)
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
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.AddMessageBatch(segmentsList, delayAfterBatchMs);
            }
            else
            {
                // Fallback: convert to strings for non-CanvasTextManager implementations
                var markupMessages = segmentsList.Select(segments => ColoredTextRenderer.RenderAsMarkup(segments)).ToList();
                messageWritingCoordinator.AddMessageBatch(markupMessages, delayAfterBatchMs);
            }
        }
        
        /// <summary>
        /// Writes multiple ColoredText segment lists as a single batch and waits for the delay
        /// This async version allows the combat loop to wait for each action's display to complete
        /// </summary>
        /// <param name="messageGroups">List of tuples containing (segments, messageType) to write</param>
        /// <param name="delayAfterBatchMs">Optional delay in milliseconds after the batch is added (for combat timing)</param>
        public async Task WriteColoredSegmentsBatchAsync(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0)
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
            if (textManager is CanvasTextManager canvasTextManager)
            {
                await canvasTextManager.DisplayManager.AddMessageBatchAsync(segmentsList, delayAfterBatchMs);
            }
            else
            {
                // Fallback: convert to strings for non-CanvasTextManager implementations
                var markupMessages = segmentsList.Select(segments => ColoredTextRenderer.RenderAsMarkup(segments)).ToList();
                await messageWritingCoordinator.AddMessageBatchAsync(markupMessages, delayAfterBatchMs);
            }
        }
    }
}

