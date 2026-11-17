using Avalonia.Input;
using RPGGame;
using RPGGame.UI.Avalonia.Utils;
using RPGGame.Utils;
using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Handles keyboard input conversion and processing for MainWindow
    /// </summary>
    public class MainWindowInputHandler
    {
        private GameCoordinator? game;
        private string inputBuffer = "";
        private string currentTextInput = "";
        
        public MainWindowInputHandler(GameCoordinator? game)
        {
            this.game = game;
        }
        
        public void SetGame(GameCoordinator? game)
        {
            this.game = game;
        }
        
        /// <summary>
        /// Converts Avalonia Key to game input string
        /// </summary>
        public string? ConvertKeyToInput(Key key, KeyModifiers modifiers)
        {
            return KeyInputConverter.ConvertKeyToInput(key, modifiers);
        }

        /// <summary>
        /// Adds text to current text input (for TextInput event)
        /// This is the primary method for text input - TextInput event provides clean text
        /// </summary>
        public void AddTextInput(string text, System.Action updateTextInputDisplay)
        {
            ScrollDebugLogger.LogAlways($"AddTextInput: Called with text='{text}', state={game?.CurrentState}");
            
            if (game == null)
            {
                ScrollDebugLogger.LogAlways($"AddTextInput: ERROR - game is null!");
                return;
            }
            
            if (game.CurrentState != GameState.CreateAction && game.CurrentState != GameState.EditAction)
            {
                ScrollDebugLogger.LogAlways($"AddTextInput: Wrong state: {game.CurrentState}, expected CreateAction or EditAction");
                return;
            }
            
            // Append text to current input
            if (!string.IsNullOrEmpty(text))
            {
                currentTextInput += text;
                ScrollDebugLogger.LogAlways($"AddTextInput: Added text, currentTextInput now='{currentTextInput}'");
                updateTextInputDisplay();
            }
            else
            {
                ScrollDebugLogger.LogAlways($"AddTextInput: Text is null or empty, not adding");
            }
        }

    }
}

