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
        /// Converts a key to its character representation for text input
        /// </summary>
        public string? ConvertKeyToChar(Key key, KeyModifiers modifiers)
        {
            bool isShift = modifiers.HasFlag(KeyModifiers.Shift);
            
            return key switch
            {
                Key.A => isShift ? "A" : "a",
                Key.B => isShift ? "B" : "b",
                Key.C => isShift ? "C" : "c",
                Key.D => isShift ? "D" : "d",
                Key.E => isShift ? "E" : "e",
                Key.F => isShift ? "F" : "f",
                Key.G => isShift ? "G" : "g",
                Key.H => isShift ? "H" : "h",
                Key.I => isShift ? "I" : "i",
                Key.J => isShift ? "J" : "j",
                Key.K => isShift ? "K" : "k",
                Key.L => isShift ? "L" : "l",
                Key.M => isShift ? "M" : "m",
                Key.N => isShift ? "N" : "n",
                Key.O => isShift ? "O" : "o",
                Key.P => isShift ? "P" : "p",
                Key.Q => isShift ? "Q" : "q",
                Key.R => isShift ? "R" : "r",
                Key.S => isShift ? "S" : "s",
                Key.T => isShift ? "T" : "t",
                Key.U => isShift ? "U" : "u",
                Key.V => isShift ? "V" : "v",
                Key.W => isShift ? "W" : "w",
                Key.X => isShift ? "X" : "x",
                Key.Y => isShift ? "Y" : "y",
                Key.Z => isShift ? "Z" : "z",
                Key.D0 or Key.NumPad0 => "0",
                Key.D1 or Key.NumPad1 => "1",
                Key.D2 or Key.NumPad2 => "2",
                Key.D3 or Key.NumPad3 => "3",
                Key.D4 or Key.NumPad4 => "4",
                Key.D5 or Key.NumPad5 => "5",
                Key.D6 or Key.NumPad6 => "6",
                Key.D7 or Key.NumPad7 => "7",
                Key.D8 or Key.NumPad8 => "8",
                Key.D9 or Key.NumPad9 => "9",
                Key.OemMinus => isShift ? "_" : "-",
                Key.OemPlus => isShift ? "+" : "=",
                Key.OemPeriod => isShift ? ">" : ".",
                Key.OemComma => isShift ? "<" : ",",
                Key.OemQuestion => isShift ? "?" : "/",
                Key.OemSemicolon => isShift ? ":" : ";",
                Key.OemQuotes => isShift ? "\"" : "'",
                Key.OemOpenBrackets => isShift ? "{" : "[",
                Key.OemCloseBrackets => isShift ? "}" : "]",
                Key.OemPipe => isShift ? "|" : "\\",
                Key.OemTilde => isShift ? "~" : "`",
                _ => null
            };
        }
        
        /// <summary>
        /// Handles special keys (Enter, Escape, Backspace) for CreateAction and EditAction states
        /// Actual text input is handled by the TextInput event
        /// </summary>
        public async Task<bool> HandleSpecialKeys(Key key, KeyModifiers modifiers, System.Action<string> updateStatus, System.Action updateTextInputDisplay)
        {
            if (game == null || (game.CurrentState != GameState.CreateAction && game.CurrentState != GameState.EditAction))
            {
                return false;
            }
            
            if (key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(currentTextInput))
                {
                    await game.HandleInput(currentTextInput);
                    currentTextInput = "";
                    updateTextInputDisplay();
                }
                else
                {
                    await game.HandleInput("enter");
                }
                return true;
            }
            else if (key == Key.Back)
            {
                if (currentTextInput.Length > 0)
                {
                    currentTextInput = currentTextInput.Substring(0, currentTextInput.Length - 1);
                    updateTextInputDisplay();
                }
                return true;
            }
            else if (key == Key.Escape)
            {
                // Escape is handled by game.HandleEscapeKey() in MainWindow, but we can clear input here if needed
                currentTextInput = "";
                return false; // Let the escape handler in MainWindow process it
            }
            
            return false;
        }
        
        /// <summary>
        /// Handles multi-digit input for testing menu
        /// </summary>
        public async Task<bool> HandleTestingMenuInput(Key key, KeyModifiers modifiers, System.Action<string> updateStatus)
        {
            if (game == null || game.CurrentState != GameState.Testing)
            {
                inputBuffer = "";
                return false;
            }
            
            string? input = ConvertKeyToInput(key, modifiers);
            if (input != null)
            {
                if (input.Length == 1 && char.IsDigit(input[0]))
                {
                    inputBuffer += input;
                    updateStatus($"Entered: {inputBuffer} (Press Enter to confirm, Esc to clear)");
                    return true;
                }
                else if (input == "enter" && !string.IsNullOrEmpty(inputBuffer))
                {
                    string bufferedInput = inputBuffer;
                    inputBuffer = "";
                    await game.HandleInput(bufferedInput);
                    return true;
                }
                else if (input == "enter")
                {
                    await game.HandleInput(input);
                    return true;
                }
                else
                {
                    inputBuffer = "";
                    await game.HandleInput(input);
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Handles regular input (non-special states)
        /// </summary>
        public async Task HandleRegularInput(Key key, KeyModifiers modifiers)
        {
            if (game == null) return;
            
            string? input = ConvertKeyToInput(key, modifiers);
            if (input != null)
            {
                await game.HandleInput(input);
            }
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
        
        /// <summary>
        /// Clears the input buffer (for testing menu)
        /// </summary>
        public void ClearInputBuffer()
        {
            inputBuffer = "";
        }
        
        /// <summary>
        /// Gets current text input
        /// </summary>
        public string GetCurrentTextInput()
        {
            return currentTextInput;
        }
        
        /// <summary>
        /// Clears the current text input (useful when starting a new form)
        /// </summary>
        public void ClearTextInput()
        {
            currentTextInput = "";
        }
    }
}

