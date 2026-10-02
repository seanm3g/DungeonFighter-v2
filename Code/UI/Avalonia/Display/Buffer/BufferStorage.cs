using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Renderers.Text;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display.Buffer
{

    /// <summary>
    /// Handles message storage, width fitting, and duplicate detection for the display buffer.
    /// </summary>
    public class BufferStorage
    {
        private readonly List<List<ColoredText>> messages;
        private readonly List<UIMessageType> lineMessageTypes;
        /// <summary>Parallel to messages: alternate dual-view lines (mechanical tip or prose) for F7 swap/hover.</summary>
        private readonly List<List<List<ColoredText>>?> lineHoverInfoLines;
        /// <summary>
        /// Parallel to messages: dual-view block span on the anchor line.
        /// Narrative prose anchors use 1; mechanical block anchors use N (line count); 0 = not an anchor.
        /// </summary>
        private readonly List<int> lineDualSpans;
        private readonly int maxLines;
        private readonly int maxLineWidth;
        private readonly MessageValidator validator;
        
        public BufferStorage(int maxLines, int maxLineWidth)
        {
            this.messages = new List<List<ColoredText>>();
            this.lineMessageTypes = new List<UIMessageType>();
            this.lineHoverInfoLines = new List<List<List<ColoredText>>?>();
            this.lineDualSpans = new List<int>();
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
            lineHoverInfoLines.Add(null);
            lineDualSpans.Add(0);
            
            // Keep only the last maxLines
            if (messages.Count > maxLines)
            {
                messages.RemoveAt(0);
                lineMessageTypes.RemoveAt(0);
                lineHoverInfoLines.RemoveAt(0);
                lineDualSpans.RemoveAt(0);
            }
            
            // Update scroll state with new message count
            scrollState.UpdateAfterAdd(wasAtTop, wasAtBottom, messages.Count);
        }

        /// <summary>
        /// Sets mechanical combat-log tip lines for a line counted from the end (0 = last). Used by F7 prose.
        /// Marks the line as a narrative dual-view anchor (span 1) when tip lines are present.
        /// </summary>
        public void SetHoverInfoLinesAtFromEnd(int offsetFromEnd, List<List<ColoredText>>? infoLines)
        {
            int index = messages.Count - 1 - offsetFromEnd;
            if (index < 0 || index >= lineHoverInfoLines.Count)
                return;
            var cloned = CombatLogProseHoverInfo.CloneLines(infoLines);
            lineHoverInfoLines[index] = cloned;
            lineDualSpans[index] = cloned != null && cloned.Count > 0 ? 1 : 0;
        }

        /// <summary>
        /// Binds a mechanical action block (last <paramref name="span"/> lines) to a single prose alternate
        /// so F7 can swap back to narrative. Marks the first line of the block as the dual anchor.
        /// </summary>
        public void BindMechanicalDualViewFromEnd(int span, List<ColoredText> proseParagraph)
        {
            if (span <= 0 || proseParagraph == null || messages.Count < span)
                return;

            int firstIndex = messages.Count - span;
            for (int i = 0; i < span; i++)
            {
                int index = firstIndex + i;
                if (i == 0)
                {
                    lineHoverInfoLines[index] = new List<List<ColoredText>>
                    {
                        new List<ColoredText>(proseParagraph)
                    };
                    lineDualSpans[index] = span;
                }
                else
                {
                    lineHoverInfoLines[index] = null;
                    lineDualSpans[index] = 0;
                }
            }
        }

        /// <summary>
        /// Replaces an existing line counted from the end (0 = last). Does not change line count.
        /// Preserves any existing hover-info binding unless <paramref name="setHoverInfoLines"/> is true.
        /// </summary>
        public void ReplaceAtFromEnd(
            int offsetFromEnd,
            List<ColoredText> segments,
            UIMessageType? messageType = null,
            List<List<ColoredText>>? hoverInfoLines = null,
            bool setHoverInfoLines = false)
        {
            int index = messages.Count - 1 - offsetFromEnd;
            if (index < 0 || index >= messages.Count)
                return;

            if (segments == null || segments.Count == 0)
            {
                messages[index] = new List<ColoredText>();
                if (messageType.HasValue)
                    lineMessageTypes[index] = messageType.Value;
                if (setHoverInfoLines)
                {
                    var cloned = CombatLogProseHoverInfo.CloneLines(hoverInfoLines);
                    lineHoverInfoLines[index] = cloned;
                    lineDualSpans[index] = cloned != null && cloned.Count > 0 ? 1 : 0;
                }
                return;
            }

            var processedSegments = FitToLineWidth(segments);

            messages[index] = new List<ColoredText>(processedSegments);
            if (messageType.HasValue)
                lineMessageTypes[index] = messageType.Value;
            if (setHoverInfoLines)
            {
                var cloned = CombatLogProseHoverInfo.CloneLines(hoverInfoLines);
                lineHoverInfoLines[index] = cloned;
                lineDualSpans[index] = cloned != null && cloned.Count > 0 ? 1 : 0;
            }
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
            lineHoverInfoLines.Add(null);
            lineDualSpans.Add(0);
            
            bool wasAtBottom = scrollState.WasAtBottom();
            bool wasAtTop = scrollState.WasAtTop();
            
            // Keep only the last maxLines
            if (messages.Count > maxLines)
            {
                messages.RemoveAt(0);
                lineMessageTypes.RemoveAt(0);
                lineHoverInfoLines.RemoveAt(0);
                lineDualSpans.RemoveAt(0);
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
                    lineHoverInfoLines.Add(null);
                    lineDualSpans.Add(0);
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
                lineHoverInfoLines.Add(null);
                lineDualSpans.Add(0);
            }
            
            // Keep only the last maxLines (batch removal)
            if (messages.Count > maxLines)
            {
                int removeCount = messages.Count - maxLines;
                messages.RemoveRange(0, removeCount);
                lineMessageTypes.RemoveRange(0, removeCount);
                lineHoverInfoLines.RemoveRange(0, removeCount);
                lineDualSpans.RemoveRange(0, removeCount);
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
            lineHoverInfoLines.Clear();
            lineDualSpans.Clear();
            scrollState.Reset();
        }

        /// <summary>
        /// Snapshot of every line including F7 dual-view alternate / span (for swap).
        /// </summary>
        public List<CombatLogDualView.Line> GetAllDualViewLines()
        {
            var result = new List<CombatLogDualView.Line>(messages.Count);
            for (int i = 0; i < messages.Count; i++)
            {
                int span = i < lineDualSpans.Count ? lineDualSpans[i] : 0;
                // Narrative prose with hover tip but no explicit span still swaps (legacy / live F7 write).
                var alternate = CombatLogProseHoverInfo.CloneLines(
                    i < lineHoverInfoLines.Count ? lineHoverInfoLines[i] : null);
                if (span <= 0 && alternate != null && alternate.Count > 0)
                    span = 1;

                result.Add(new CombatLogDualView.Line(
                    new List<ColoredText>(messages[i]),
                    lineMessageTypes[i],
                    alternate,
                    span));
            }

            return result;
        }

        /// <summary>
        /// Replaces the entire buffer with dual-view lines (F7 narrative ↔ mechanical swap).
        /// </summary>
        public void ReplaceAllDualViewLines(IReadOnlyList<CombatLogDualView.Line> lines, ScrollStateManager scrollState)
        {
            messages.Clear();
            lineMessageTypes.Clear();
            lineHoverInfoLines.Clear();
            lineDualSpans.Clear();

            if (lines != null)
            {
                foreach (var line in lines)
                {
                    var segments = line.Segments == null || line.Segments.Count == 0
                        ? new List<ColoredText>()
                        : FitToLineWidth(line.Segments);
                    messages.Add(new List<ColoredText>(segments));
                    lineMessageTypes.Add(line.MessageType);
                    lineHoverInfoLines.Add(CombatLogProseHoverInfo.CloneLines(line.AlternateLines));
                    lineDualSpans.Add(Math.Max(0, line.DualSpan));
                }
            }

            // Trim to max
            if (messages.Count > maxLines)
            {
                int removeCount = messages.Count - maxLines;
                messages.RemoveRange(0, removeCount);
                lineMessageTypes.RemoveRange(0, removeCount);
                lineHoverInfoLines.RemoveRange(0, removeCount);
                lineDualSpans.RemoveRange(0, removeCount);
            }

            scrollState.Reset();
            scrollState.UpdateAfterAdd(wasAtTop: false, wasAtBottom: true, messages.Count);
        }

        /// <summary>
        /// Gets the last N messages with the <see cref="UIMessageType"/> stored when each line was appended.
        /// </summary>
        public List<(List<ColoredText> Segments, UIMessageType MessageType, List<List<ColoredText>>? HoverInfoLines)> GetLastWithMessageTypes(int count)
        {
            int n = System.Math.Min(count, messages.Count);
            if (n <= 0)
                return new List<(List<ColoredText>, UIMessageType, List<List<ColoredText>>?)>();

            var sliceMessages = messages.TakeLast(n).ToList();
            var sliceTypes = lineMessageTypes.TakeLast(n).ToList();
            var sliceHover = lineHoverInfoLines.TakeLast(n).ToList();
            var result = new List<(List<ColoredText>, UIMessageType, List<List<ColoredText>>?)>(n);
            for (int i = 0; i < n; i++)
                result.Add((new List<ColoredText>(sliceMessages[i]), sliceTypes[i], CombatLogProseHoverInfo.CloneLines(sliceHover[i])));
            return result;
        }

        /// <summary>
        /// Fits content to the live center-panel text column by wrapping at word boundaries
        /// (newlines stay inside the same buffer entry). Uses
        /// <see cref="LayoutConstants.CenterPanelTextColumnWidth"/> so buffer pre-wrap matches
        /// display render width and does not leave short stub lines from a mismatched second wrap.
        /// Explicit narrower <c>maxLineWidth</c> (unit tests) still wins. Per-line wrapping keeps
        /// punchline reservations stable and prevents ellipsis.
        /// </summary>
        private List<ColoredText> FitToLineWidth(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
                return segments ?? new List<ColoredText>();

            return TextWrappingHelper.ApplySoftWraps(segments, ResolveWrapWidth());
        }

        /// <summary>
        /// Live text-column width, honoring an explicit narrower ctor budget (tests pass 40).
        /// </summary>
        private int ResolveWrapWidth()
        {
            int live = Math.Max(1, LayoutConstants.CenterPanelTextColumnWidth);
            if (maxLineWidth > 0 && maxLineWidth < live)
                return maxLineWidth;
            return live;
        }
    }
}

