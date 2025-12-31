using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Services;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Helper class for display buffer operations.
    /// Extracted from CenterPanelDisplayManager to separate buffer management from rendering logic.
    /// </summary>
    public class DisplayBufferOperations
    {
        private readonly DisplayBuffer buffer;
        private readonly MessageFilterService filterService;
        private readonly GameStateManager? stateManager;
        private readonly ICanvasContextManager? contextManager;
        private readonly System.Action triggerRender;

        public DisplayBufferOperations(
            DisplayBuffer buffer,
            MessageFilterService filterService,
            GameStateManager? stateManager,
            ICanvasContextManager? contextManager,
            System.Action triggerRender)
        {
            this.buffer = buffer;
            this.filterService = filterService;
            this.stateManager = stateManager;
            this.contextManager = contextManager;
            this.triggerRender = triggerRender;
        }

        /// <summary>
        /// Adds a message to the buffer if it passes filtering
        /// </summary>
        public bool TryAddMessage(string message, UIMessageType messageType = UIMessageType.System)
        {
            bool shouldAddMessage = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                messageType,
                stateManager,
                contextManager,
                performRaceConditionCheck: true);

            if (!shouldAddMessage || message == null)
            {
                return false;
            }

            buffer.Add(message);
            return true;
        }

        /// <summary>
        /// Adds structured ColoredText segments to the buffer if they pass filtering
        /// </summary>
        public bool TryAddMessage(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            bool shouldAddMessage = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                messageType,
                stateManager,
                contextManager,
                performRaceConditionCheck: true);

            if (!shouldAddMessage)
            {
                return false;
            }

            buffer.Add(segments);
            return true;
        }

        /// <summary>
        /// Adds multiple messages to the buffer if they pass filtering
        /// </summary>
        public bool TryAddMessages(IEnumerable<string> messages)
        {
            bool shouldAddMessages = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                UIMessageType.System, // Default to System for batch operations
                stateManager,
                contextManager);

            if (!shouldAddMessages)
            {
                return false;
            }

            buffer.AddRange(messages);
            return true;
        }

        /// <summary>
        /// Adds multiple structured ColoredText segment lists to the buffer if they pass filtering
        /// Only checks menu states, not character (character filtering happens at routing level)
        /// </summary>
        public bool TryAddMessages(IEnumerable<List<ColoredText>> segmentsList)
        {
            // Use MessageFilterService to check menu states consistently
            // Don't perform race condition check for batch operations (already routed to correct character)
            bool shouldAddMessages = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                UIMessageType.System, // Default to System for batch operations
                stateManager,
                contextManager,
                performRaceConditionCheck: false);

            if (!shouldAddMessages || segmentsList == null)
            {
                return false;
            }

            buffer.AddRange(segmentsList);
            return true;
        }

        /// <summary>
        /// Clears the display buffer
        /// </summary>
        public void Clear()
        {
            buffer.Clear();
        }
    }
}
