using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Services
{
    /// <summary>
    /// Primary routing service for all game messages
    /// Consolidates message entry points and routes to appropriate display managers
    /// Uses MessageFilterService internally for consistent filtering
    /// </summary>
    public class MessageRouter
    {
        private readonly MessageFilterService filterService;
        private readonly CenterPanelDisplayManager? displayManager;
        private readonly IUIManager? uiManager;
        private readonly GameStateManager? stateManager;
        private readonly ICanvasContextManager? contextManager;

        /// <summary>
        /// Creates a new MessageRouter
        /// </summary>
        /// <param name="displayManager">The center panel display manager to route messages to (optional, preferred)</param>
        /// <param name="uiManager">The UI manager to route messages to (optional, fallback)</param>
        /// <param name="stateManager">Game state manager for filtering (optional)</param>
        /// <param name="contextManager">Context manager for character tracking (optional)</param>
        public MessageRouter(
            CenterPanelDisplayManager? displayManager = null,
            IUIManager? uiManager = null,
            GameStateManager? stateManager = null,
            ICanvasContextManager? contextManager = null)
        {
            this.filterService = new MessageFilterService();
            this.displayManager = displayManager;
            this.uiManager = uiManager;
            this.stateManager = stateManager;
            this.contextManager = contextManager;
        }

        /// <summary>
        /// Routes a combat message (action block) to the display system
        /// </summary>
        /// <param name="actionText">The action text segments</param>
        /// <param name="rollInfo">Roll information segments</param>
        /// <param name="statusEffects">Status effect segments (optional)</param>
        /// <param name="criticalMissNarrative">Critical miss narrative (optional)</param>
        /// <param name="narratives">Additional narratives (optional)</param>
        /// <param name="character">The character this combat action belongs to (for filtering)</param>
        public void RouteCombatMessage(
            List<ColoredText> actionText,
            List<ColoredText> rollInfo,
            List<List<ColoredText>>? statusEffects = null,
            List<ColoredText>? criticalMissNarrative = null,
            List<List<ColoredText>>? narratives = null,
            Character? character = null)
        {
            // Check if message should be displayed
            if (!filterService.ShouldDisplayMessage(character, UIMessageType.Combat, stateManager, contextManager))
            {
                return;
            }

            // Route to display manager if available
            if (displayManager != null)
            {
                // Collect all messages for this combat action block
                var allMessages = new List<List<ColoredText>>();
                
                if (actionText != null && actionText.Count > 0)
                {
                    allMessages.Add(actionText);
                }
                
                if (rollInfo != null && rollInfo.Count > 0)
                {
                    allMessages.Add(rollInfo);
                }
                
                if (statusEffects != null)
                {
                    allMessages.AddRange(statusEffects);
                }
                
                if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
                {
                    allMessages.Add(criticalMissNarrative);
                }
                
                if (narratives != null)
                {
                    allMessages.AddRange(narratives);
                }

                if (allMessages.Count > 0)
                {
                    displayManager.AddMessages(allMessages);
                }
            }
        }

        /// <summary>
        /// Routes a system message to the display system
        /// </summary>
        /// <param name="message">The message text</param>
        /// <param name="messageType">The message type (defaults to System)</param>
        /// <param name="character">The character this message belongs to (for filtering, optional)</param>
        public void RouteSystemMessage(
            string message,
            UIMessageType messageType = UIMessageType.System,
            Character? character = null)
        {
            // Check if message should be displayed
            if (!filterService.ShouldDisplayMessage(character, messageType, stateManager, contextManager))
            {
                return;
            }

            // Route to display manager if available (preferred)
            if (displayManager != null)
            {
                displayManager.AddMessage(message, messageType);
            }
            // Fallback to UI manager
            else if (uiManager != null)
            {
                uiManager.WriteLine(message, messageType);
            }
        }

        /// <summary>
        /// Routes colored text segments to the display system
        /// </summary>
        /// <param name="segments">The colored text segments</param>
        /// <param name="messageType">The message type (defaults to System)</param>
        /// <param name="character">The character this message belongs to (for filtering, optional)</param>
        public void RouteColoredText(
            List<ColoredText> segments,
            UIMessageType messageType = UIMessageType.System,
            Character? character = null)
        {
            // Check if message should be displayed
            if (!filterService.ShouldDisplayMessage(character, messageType, stateManager, contextManager))
            {
                return;
            }

            // Route to display manager if available (preferred)
            if (displayManager != null)
            {
                displayManager.AddMessage(segments, messageType);
            }
            // Fallback to UI manager (convert segments to markup string)
            else if (uiManager != null)
            {
                string markup = ColoredTextRenderer.RenderAsMarkup(segments);
                uiManager.WriteLine(markup, messageType);
            }
        }

        /// <summary>
        /// Routes multiple messages as a batch
        /// </summary>
        /// <param name="messages">The messages to route</param>
        /// <param name="messageType">The message type (defaults to System)</param>
        /// <param name="delayAfterBatchMs">Delay after batch in milliseconds</param>
        /// <param name="character">The character these messages belong to (for filtering, optional)</param>
        public void RouteMessageBatch(
            IEnumerable<string> messages,
            UIMessageType messageType = UIMessageType.System,
            int delayAfterBatchMs = 0,
            Character? character = null)
        {
            // Check if messages should be displayed
            if (!filterService.ShouldDisplayMessage(character, messageType, stateManager, contextManager))
            {
                return;
            }

            // Route to display manager if available (preferred)
            if (displayManager != null)
            {
                displayManager.AddMessageBatch(messages, delayAfterBatchMs);
            }
            // Fallback: add messages individually through UI manager
            else if (uiManager != null)
            {
                foreach (var message in messages)
                {
                    uiManager.WriteLine(message, messageType);
                }
            }
        }

        /// <summary>
        /// Routes multiple colored text segments as a batch (async)
        /// </summary>
        /// <param name="segmentsList">The colored text segment lists</param>
        /// <param name="messageType">The message type (defaults to System)</param>
        /// <param name="delayAfterBatchMs">Delay after batch in milliseconds</param>
        /// <param name="character">The character these messages belong to (for filtering, optional)</param>
        public async Task RouteMessageBatchAsync(
            IEnumerable<List<ColoredText>> segmentsList,
            UIMessageType messageType = UIMessageType.System,
            int delayAfterBatchMs = 0,
            Character? character = null)
        {
            // Check if messages should be displayed
            if (!filterService.ShouldDisplayMessage(character, messageType, stateManager, contextManager))
            {
                return;
            }

            // Route to display manager if available (preferred)
            if (displayManager != null)
            {
                await displayManager.AddMessageBatchAsync(segmentsList, delayAfterBatchMs);
            }
            // Fallback: add messages individually through UI manager
            else if (uiManager != null)
            {
                foreach (var segments in segmentsList)
                {
                    string markup = ColoredTextRenderer.RenderAsMarkup(segments);
                    uiManager.WriteLine(markup, messageType);
                }
            }
        }
    }
}

