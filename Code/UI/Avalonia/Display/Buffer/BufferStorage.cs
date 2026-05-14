using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display.Buffer
{

    /// <summary>
    /// Handles message storage, truncation, and duplicate detection for the display buffer.
    /// </summary>
    public class BufferStorage
    {
        private readonly List<List<ColoredText>> messages;
        private readonly List<UIMessageType> lineMessageTypes;
        private readonly int maxLines;
        private readonly int maxLineWidth;
        private readonly MessageValidator validator;
        
        public BufferStorage(int maxLines, int maxLineWidth)
        {
            this.messages = new List<List<ColoredText>>();
            this.lineMessageTypes = new List<UIMessageType>();
            this.maxLines = maxLines;
            this.maxLineWidth = maxLineWidth;
            this.validator = new MessageValidator();
        }
        
        /// <summary>
        /// Gets all messages in the buffer as structured ColoredText segments
        /// </summary>
        public IReadOnlyList<List<ColoredText>> Messages => messages;
        
        /// <summary>
        /// Gets the current number of messages
        /// </summary>
        public int Count => messages.Count;
        
        /// <summary>
        /// Gets the maximum number of lines
        /// </summary>
        public int MaxLines => maxLines;
        
        /// <summary>
        /// Adds a ColoredText segment list to the buffer (preferred method)
        /// Stores structured data directly to eliminate round-trip conversions
        /// </summary>
        public void Add(List<ColoredText> segments, ScrollStateManager scrollState, UIMessageType messageType = UIMessageType.System)
        {
            if (segments == null || segments.Count == 0)
            {
                AddEmpty(scrollState, messageType);
                return;
            }
            
            // Truncate if too long
            var displayLength = ColoredTextRenderer.GetDisplayLength(segments);
            if (displayLength > maxLineWidth)
            {
                segments = ColoredTextRenderer.Truncate(segments, maxLineWidth - 3);
                // Add "..." as a final segment
                segments.Add(new ColoredText("...", Colors.White));
            }
            
            // Prevent consecutive duplicate messages
            // BUT: Allow blank lines to be added even if previous was blank (spacing needs multiple blanks)
            if (messages.Count > 0 && validator.AreSegmentsEqual(messages[messages.Count - 1], segments) && segments.Count > 0)
            {
                return; // Skip duplicate (but not blank lines)
            }
            
            bool wasAtBottom = scrollState.WasAtBottom();
            bool wasAtTop = scrollState.WasAtTop();
            
            messages.Add(new List<ColoredText>(segments));
            lineMessageTypes.Add(messageType);
            
            // Keep only the last maxLines
            if (messages.Count > maxLines)
            {
                messages.RemoveAt(0);
                lineMessageTypes.RemoveAt(0);
            }
            
            // Update scroll state with new message count
            scrollState.UpdateAfterAdd(wasAtTop, wasAtBottom, messages.Count);
        }
        
        /// <summary>
        /// Adds an empty line to the buffer
        /// </summary>
        private void AddEmpty(ScrollStateManager scrollState, UIMessageType messageType = UIMessageType.System)
        {
            messages.Add(new List<ColoredText>());
            lineMessageTypes.Add(messageType);
            
            bool wasAtBottom = scrollState.WasAtBottom();
            bool wasAtTop = scrollState.WasAtTop();
            
            // Keep only the last maxLines
            if (messages.Count > maxLines)
            {
                messages.RemoveAt(0);
                lineMessageTypes.RemoveAt(0);
            }
            
            // Update scroll state
            scrollState.UpdateAfterAdd(wasAtTop, wasAtBottom);
        }
        
        /// <summary>
        /// Adds multiple ColoredText segment lists to the buffer (preferred method)
        /// Stores structured data directly
        /// </summary>
        public void AddRange(IEnumerable<List<ColoredText>> segmentsList, ScrollStateManager scrollState, UIMessageType messageType = UIMessageType.System)
        {
            if (segmentsList == null)
                return;
            
            var segmentsListToAdd = segmentsList.ToList();
            if (segmentsListToAdd.Count == 0) return;
            
            // Check if we were at the bottom or top before adding
            bool wasAtBottom = scrollState.WasAtBottom();
            bool wasAtTop = scrollState.WasAtTop();
            
            // Process all messages in batch
            foreach (var segments in segmentsListToAdd)
            {
                if (segments == null || segments.Count == 0)
                {
                    // Always allow blank lines - they're used for spacing between sections
                    messages.Add(new List<ColoredText>());
                    lineMessageTypes.Add(messageType);
                    continue;
                }
                
                // Truncate if too long
                var displayLength = ColoredTextRenderer.GetDisplayLength(segments);
                var processedSegments = segments;
                if (displayLength > maxLineWidth)
                {
                    processedSegments = ColoredTextRenderer.Truncate(segments, maxLineWidth - 3);
                    // Add "..." as a final segment
                    processedSegments.Add(new ColoredText("...", Colors.White));
                }
                
                // Prevent consecutive duplicate messages (only check against last message in buffer)
                // BUT: Allow blank lines to be added even if previous was blank (spacing needs multiple blanks)
                if (messages.Count > 0 && validator.AreSegmentsEqual(messages[messages.Count - 1], processedSegments) && processedSegments.Count > 0)
                {
                    continue; // Skip duplicate (but not blank lines)
                }
                
                messages.Add(new List<ColoredText>(processedSegments));
                lineMessageTypes.Add(messageType);
            }
            
            // Keep only the last maxLines (batch removal)
            if (messages.Count > maxLines)
            {
                int removeCount = messages.Count - maxLines;
                messages.RemoveRange(0, removeCount);
                lineMessageTypes.RemoveRange(0, removeCount);
            }
            
            // Update scroll state with new message count
            scrollState.UpdateAfterAdd(wasAtTop, wasAtBottom, messages.Count);
        }
        
        /// <summary>
        /// Clears all messages from the buffer
        /// </summary>
        public void Clear(ScrollStateManager scrollState)
        {
            messages.Clear();
            lineMessageTypes.Clear();
            scrollState.Reset();
        }

        /// <summary>
        /// Gets the last N messages with the <see cref="UIMessageType"/> stored when each line was appended.
        /// </summary>
        public List<(List<ColoredText> Segments, UIMessageType MessageType)> GetLastWithMessageTypes(int count)
        {
            int n = System.Math.Min(count, messages.Count);
            if (n <= 0)
                return new List<(List<ColoredText>, UIMessageType)>();

            var sliceMessages = messages.TakeLast(n).ToList();
            var sliceTypes = lineMessageTypes.TakeLast(n).ToList();
            var result = new List<(List<ColoredText>, UIMessageType)>(n);
            for (int i = 0; i < n; i++)
                result.Add((new List<ColoredText>(sliceMessages[i]), sliceTypes[i]));
            return result;
        }
    }
}

