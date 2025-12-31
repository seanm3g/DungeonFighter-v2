using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Services;

namespace RPGGame
{
    /// <summary>
    /// Handles message routing and filtering for dungeon display messages
    /// Determines character ownership and whether messages should be displayed
    /// </summary>
    public class DungeonMessageRouter
    {
        private readonly MessageFilterService filterService = new MessageFilterService();
        private readonly MessageRouter? messageRouter;
        private readonly IUIManager? uiManager;
        private readonly GameStateManager? stateManager;
        private readonly DungeonDisplayState state;

        public DungeonMessageRouter(
            DungeonDisplayState state,
            MessageRouter? messageRouter = null,
            IUIManager? uiManager = null,
            GameStateManager? stateManager = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.messageRouter = messageRouter;
            this.uiManager = uiManager;
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Routes a combat event message
        /// Determines character ownership, filters based on active character, and routes to UI
        /// </summary>
        /// <param name="message">The combat event message</param>
        /// <param name="sourceCharacter">The character this combat event belongs to (optional, will be inferred if not provided)</param>
        /// <param name="displayBuffer">The display buffer to add the message to (if character is active)</param>
        /// <param name="narrativeManager">The narrative manager to log the message to (always, for character context tracking)</param>
        /// <returns>True if the message was displayed, false otherwise</returns>
        public bool RouteCombatEvent(
            string message,
            Character? sourceCharacter,
            Display.Dungeon.DungeonDisplayBuffer displayBuffer,
            GameNarrativeManager narrativeManager)
        {
            // Allow empty strings (for blank lines) but filter out null or whitespace-only strings
            if (message == null)
                return false;

            // Determine the character that owns this message
            var messageOwner = state.DetermineMessageOwner(sourceCharacter);

            // Use MessageFilterService to determine if message should be displayed
            // This consolidates all filtering logic (menu states, character matching)
            bool shouldUpdateUI = filterService.ShouldDisplayMessage(
                messageOwner,
                UIMessageType.System, // Combat events use System type
                stateManager,
                null, // No context manager
                false); // No race condition check needed here

            // Only add to display buffer if character is active
            // This prevents inactive character combat messages from polluting the shared display buffer
            if (shouldUpdateUI && message != null)
            {
                displayBuffer.AddCombatEvent(message);
            }

            // Only add to narrative manager and UI if not empty (narrative doesn't need blank lines)
            if (!string.IsNullOrWhiteSpace(message))
            {
                // Add to narrative manager's dungeon log (always - for character context tracking)
                // This is per-character, so it's safe to always add
                narrativeManager.LogDungeonEvent(message);

                // Add to display buffer so it's visible immediately (only if character is active)
                // Use MessageRouter if available, otherwise fall back to direct uiManager call
                if (shouldUpdateUI)
                {
                    if (messageRouter != null)
                    {
                        messageRouter.RouteSystemMessage(message, UIMessageType.System, messageOwner);
                    }
                    else if (uiManager != null)
                    {
                        uiManager.WriteLine(message, UIMessageType.System);
                    }
                }
            }
            else if (message == "" && shouldUpdateUI && uiManager != null)
            {
                // Empty string - add blank line to UI for spacing (only if character is active)
                uiManager.WriteLine("", UIMessageType.System);
            }

            return shouldUpdateUI;
        }

        /// <summary>
        /// Routes a combat event message using colored text
        /// Determines character ownership, filters based on active character, and routes to UI
        /// </summary>
        /// <param name="builder">The colored text builder for the combat event</param>
        /// <param name="sourceCharacter">The character this combat event belongs to (optional, will be inferred if not provided)</param>
        /// <param name="displayBuffer">The display buffer to add the message to (if character is active)</param>
        /// <param name="narrativeManager">The narrative manager to log the message to (always, for character context tracking)</param>
        /// <returns>True if the message was displayed, false otherwise</returns>
        public bool RouteCombatEvent(
            ColoredTextBuilder builder,
            Character? sourceCharacter,
            Display.Dungeon.DungeonDisplayBuffer displayBuffer,
            GameNarrativeManager narrativeManager)
        {
            if (builder == null)
                return false;

            var segments = builder.Build();

            // Convert to markup string for display buffer and narrative manager
            string markupMessage = ColoredTextRenderer.RenderAsMarkup(segments);

            // Determine the character that owns this message
            var messageOwner = state.DetermineMessageOwner(sourceCharacter);

            // Use MessageFilterService to determine if message should be displayed
            // This consolidates all filtering logic (menu states, character matching)
            bool shouldUpdateUI = filterService.ShouldDisplayMessage(
                messageOwner,
                UIMessageType.System, // Combat events use System type
                stateManager,
                null, // No context manager
                false); // No race condition check needed here

            // Only add to display buffer if character is active
            // This prevents inactive character combat messages from polluting the shared display buffer
            if (shouldUpdateUI)
            {
                displayBuffer.AddCombatEvent(markupMessage);
            }

            // Add to narrative manager's dungeon log (always - for character context tracking)
            // This is per-character, so it's safe to always add
            narrativeManager.LogDungeonEvent(markupMessage);

            // Add to UI using colored text directly (only if character is active)
            if (shouldUpdateUI && uiManager != null)
            {
                uiManager.WriteLineColoredSegments(segments, UIMessageType.System);
            }

            return shouldUpdateUI;
        }
    }
}
