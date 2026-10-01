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
            
            segments = FitToLineWidth(segments);
            
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
        /// Replaces an existing line counted from the end (0 = last). Does not change line count.
        /// </summary>
        public void ReplaceAtFromEnd(int offsetFromEnd, List<ColoredText> segments, UIMessageType? messageType = null)
        {
            int index = messages.Count - 1 - offsetFromEnd;
            if (index < 0 || index >= messages.Count)
                return;

            if (segments == null || segments.Count == 0)
            {
                messages[index] = new List<ColoredText>();
                if (messageType.HasValue)
                    lineMessageTypes[index] = messageType.Value;
                return;
            }

            var processedSegments = FitToLineWidth(segments);

            messages[index] = new List<ColoredText>(processedSegments);
            if (messageType.HasValue)
                lineMessageTypes[index] = messageType.Value;
        }

        /// <summary>
        /// Replaces the last line in place (setup → punchline). Does not change line count.
        /// </summary>
        public void ReplaceLast(List<ColoredText> segments, UIMessageType? messageType = null)
        {
            ReplaceAtFromEnd(0, segments, messageType);
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
                
                var processedSegments = FitToLineWidth(segments);
                
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

        /// <summary>
        /// Caps each visual line at <see cref="maxLineWidth"/>. Newlines stay inside the same buffer
        /// entry so punchline reservations keep their line count, but a later line is not eaten by
        /// the characters of the lines above it.
        /// </summary>
        private List<ColoredText> FitToLineWidth(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
                return segments ?? new List<ColoredText>();

            bool hasBreak = false;
            foreach (var seg in segments)
            {
                string text = seg?.Text ?? "";
                if (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0)
                {
                    hasBreak = true;
                    break;
                }
            }

            if (!hasBreak)
                return TruncateSingleLine(segments);

            var lines = SplitLogicalLines(segments);
            var result = new List<ColoredText>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                    result.Add(new ColoredText(global::System.Environment.NewLine, Colors.White));
                result.AddRange(TruncateSingleLine(lines[i]));
            }
            return result;
        }

        private List<ColoredText> TruncateSingleLine(List<ColoredText> line)
        {
            if (line == null || line.Count == 0)
                return line ?? new List<ColoredText>();
            if (ColoredTextRenderer.GetDisplayLength(line) <= maxLineWidth)
                return line;

            var truncated = ColoredTextRenderer.Truncate(line, System.Math.Max(0, maxLineWidth - 3));
            truncated.Add(new ColoredText("...", Colors.White));
            return truncated;
        }

        private static List<List<ColoredText>> SplitLogicalLines(List<ColoredText> segments)
        {
            var lines = new List<List<ColoredText>>();
            var current = new List<ColoredText>();
            foreach (var seg in segments)
            {
                string text = seg?.Text ?? "";
                if (text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0)
                {
                    if (text.Length > 0 && seg != null)
                        current.Add(seg);
                    continue;
                }

                var parts = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0)
                    {
                        lines.Add(current);
                        current = new List<ColoredText>();
                    }
                    if (parts[i].Length > 0 && seg != null)
                        current.Add(new ColoredText(parts[i], seg.Color, seg.SourceTemplate, seg.ColorReadyForCanvas));
                }
            }
            lines.Add(current);
            return lines;
        }
    }
}

