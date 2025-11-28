using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Coordinators
{
    /// <summary>
    /// Consolidated coordinator for handling message writing, text rendering, and combat message operations
    /// </summary>
    public class MessageWritingCoordinator
    {
        private readonly ICanvasTextManager textManager;
        private readonly CanvasRenderer renderer;
        private readonly ICanvasContextManager contextManager;
        
        public MessageWritingCoordinator(ICanvasTextManager textManager, CanvasRenderer renderer, ICanvasContextManager contextManager)
        {
            this.textManager = textManager;
            this.renderer = renderer;
            this.contextManager = contextManager;
        }
        
        /// <summary>
        /// Writes a line with specified message type
        /// Uses unified display system - rendering is handled automatically
        /// </summary>
        public void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            textManager.AddToDisplayBuffer(message, messageType);
            // Rendering is now handled automatically by the unified display system
        }
        
        /// <summary>
        /// Writes a message (alias for WriteLine)
        /// </summary>
        public void Write(string message)
        {
            WriteLine(message);
        }
        
        /// <summary>
        /// Writes a system message
        /// </summary>
        public void WriteSystemLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a menu message
        /// </summary>
        public void WriteMenuLine(string message)
        {
            textManager.AddToDisplayBuffer(message, UIMessageType.Menu);
            // Rendering is now handled automatically by the unified display system
        }
        
        /// <summary>
        /// Writes a title message
        /// </summary>
        public void WriteTitleLine(string message)
        {
            WriteLine(message, UIMessageType.Title);
        }
        
        /// <summary>
        /// Writes a dungeon message
        /// </summary>
        public void WriteDungeonLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a room message
        /// </summary>
        public void WriteRoomLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes an enemy message
        /// </summary>
        public void WriteEnemyLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a room cleared message
        /// </summary>
        public void WriteRoomClearedLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes an effect message
        /// </summary>
        public void WriteEffectLine(string message)
        {
            WriteLine(message, UIMessageType.EffectMessage);
        }
        
        /// <summary>
        /// Writes a blank line
        /// </summary>
        public void WriteBlankLine()
        {
            textManager.AddToDisplayBuffer("", UIMessageType.System);
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer as a single batch
        /// Schedules a single render after all messages are added, with an optional delay
        /// </summary>
        public void AddMessageBatch(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            textManager.AddMessageBatch(messages, delayAfterBatchMs);
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer as a single batch and waits for the delay
        /// This async version allows the combat loop to wait for each action's display to complete
        /// </summary>
        public async System.Threading.Tasks.Task AddMessageBatchAsync(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                await canvasTextManager.AddMessageBatchAsync(messages, delayAfterBatchMs);
            }
            else
            {
                // Fallback to synchronous version for non-CanvasTextManager implementations
                textManager.AddMessageBatch(messages, delayAfterBatchMs);
            }
        }
        
        /// <summary>
        /// Writes chunked text with reveal animation
        /// </summary>
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            textManager.WriteChunked(message, config);
        }
        
        /// <summary>
        /// Resets display buffer for new battle
        /// Preserves pre-combat information (dungeon, room, room number, enemy encounter) 
        /// so it remains visible during combat.
        /// NOTE: Does NOT clear the display buffer - only ensures dungeon context is preserved.
        /// The display should only be fully cleared when transitioning from dungeon completion
        /// to dungeon selection or inventory.
        /// </summary>
        public void ResetForNewBattle()
        {
            // Ensure dungeon context is available for rendering
            // Do NOT clear the display buffer - preserve all existing content
            contextManager.RestoreDungeonContext();
        }
        
        /// <summary>
        /// Resets menu delay counter
        /// </summary>
        public void ResetMenuDelayCounter()
        {
            // Handled by text manager
        }
        
        /// <summary>
        /// Gets consecutive menu line count
        /// </summary>
        public int GetConsecutiveMenuLineCount()
        {
            return 0; // Simplified for canvas UI
        }
        
        /// <summary>
        /// Gets base menu delay
        /// </summary>
        public int GetBaseMenuDelay()
        {
            return 0; // Simplified for canvas UI
        }

        // ===== TEXT RENDERING METHODS (merged from TextRenderingCoordinator) =====
        
        /// <summary>
        /// Writes colored text at specified position
        /// </summary>
        public void WriteLineColored(string message, int x, int y)
        {
            textManager.WriteLineColored(message, x, y);
        }
        
        /// <summary>
        /// Writes colored text with wrapping at specified position
        /// </summary>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            return textManager.WriteLineColoredWrapped(message, x, y, maxWidth);
        }
        
        /// <summary>
        /// Writes colored text segments at specified position
        /// </summary>
        public void WriteLineColoredSegments(List<ColoredText> segments, int x, int y)
        {
            textManager.WriteLineColoredSegments(segments, x, y);
        }

        // ===== COMBAT MESSAGE METHODS (merged from CombatMessageCoordinator) =====
        
        /// <summary>
        /// Adds victory message after defeating an enemy
        /// </summary>
        public void AddVictoryMessage(Enemy enemy, BattleNarrative? battleNarrative)
        {
            var messageHandler = new CombatMessageHandler(textManager);
            messageHandler.AddVictoryMessage(enemy, battleNarrative);
            // Rendering is now handled automatically by the unified display system
        }
        
        /// <summary>
        /// Adds defeat message when player is defeated
        /// </summary>
        public void AddDefeatMessage()
        {
            var messageHandler = new CombatMessageHandler(textManager);
            messageHandler.AddDefeatMessage();
            // Rendering is now handled automatically by the unified display system
        }
        
        /// <summary>
        /// Adds room cleared message
        /// </summary>
        public void AddRoomClearedMessage()
        {
            var messageHandler = new CombatMessageHandler(textManager);
            messageHandler.AddRoomClearedMessage(contextManager.GetCurrentCharacter());
            // Rendering is now handled automatically by the unified display system
        }
    }
}

