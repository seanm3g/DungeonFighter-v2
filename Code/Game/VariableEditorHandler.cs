namespace RPGGame
{
    using System;
    using RPGGame.Editors;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles variable editor menu display and input.
    /// Provides editing functionality for the old "Edit Game Variables" menu.
    /// </summary>
    public class VariableEditorHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private VariableEditor variableEditor;
        private EditableVariable? selectedVariable = null;
        private bool isEditingValue = false;
        private string currentInputBuffer = "";
        
        // Delegates
        public delegate void OnShowDeveloperMenu();
        
        public event OnShowDeveloperMenu? ShowDeveloperMenuEvent;

        public VariableEditorHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.variableEditor = new VariableEditor();
        }

        /// <summary>
        /// Display the variable editor menu
        /// </summary>
        public void ShowVariableEditor()
        {
            selectedVariable = null;
            isEditingValue = false;
            currentInputBuffer = "";
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderVariableEditor();
            }
            else
            {
                ScrollDebugLogger.Log($"VariableEditorHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
            stateManager.TransitionToState(GameState.VariableEditor);
        }

        /// <summary>
        /// Handle variable editor menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                if (isEditingValue && selectedVariable != null)
                {
                    // We're in value input mode - accumulate characters
                    if (input == "cancel" || input == "c" || input == "C")
                    {
                        // Cancel editing
                        isEditingValue = false;
                        selectedVariable = null;
                        currentInputBuffer = "";
                        canvasUI.RenderVariableEditor();
                    }
                    else if (input == "enter" || input == "Enter" || input == "\r" || input == "\n")
                    {
                        // Submit the value
                        if (string.IsNullOrWhiteSpace(currentInputBuffer))
                        {
                            canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer, "Please enter a value.");
                            return;
                        }
                        
                        // Try to parse and set the value
                        try
                        {
                            var valueType = selectedVariable.GetValueType();
                            object newValue;
                            
                            if (valueType == typeof(int))
                            {
                                if (int.TryParse(currentInputBuffer.Trim(), out int intValue))
                                {
                                    newValue = intValue;
                                }
                                else
                                {
                                    ScrollDebugLogger.Log($"VariableEditorHandler: Invalid integer value: {currentInputBuffer}");
                                    canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer, "Invalid integer value. Please enter a number.");
                                    return;
                                }
                            }
                            else if (valueType == typeof(double))
                            {
                                if (double.TryParse(currentInputBuffer.Trim(), out double doubleValue))
                                {
                                    newValue = doubleValue;
                                }
                                else
                                {
                                    ScrollDebugLogger.Log($"VariableEditorHandler: Invalid double value: {currentInputBuffer}");
                                    canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer, "Invalid number value. Please enter a number.");
                                    return;
                                }
                            }
                            else if (valueType == typeof(bool))
                            {
                                string trimmed = currentInputBuffer.Trim().ToLower();
                                if (bool.TryParse(trimmed, out bool boolValue))
                                {
                                    newValue = boolValue;
                                }
                                else if (trimmed == "1" || trimmed == "true" || trimmed == "t")
                                {
                                    newValue = true;
                                }
                                else if (trimmed == "0" || trimmed == "false" || trimmed == "f")
                                {
                                    newValue = false;
                                }
                                else
                                {
                                    ScrollDebugLogger.Log($"VariableEditorHandler: Invalid boolean value: {currentInputBuffer}");
                                    canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer, "Invalid boolean value. Enter true/false or 1/0.");
                                    return;
                                }
                            }
                            else
                            {
                                newValue = currentInputBuffer.Trim(); // String or other types
                            }
                            
                            selectedVariable.SetValue(newValue);
                            ScrollDebugLogger.Log($"VariableEditorHandler: Set {selectedVariable.Name} to {newValue}");
                            isEditingValue = false;
                            selectedVariable = null;
                            currentInputBuffer = "";
                            canvasUI.RenderVariableEditor();
                        }
                        catch (Exception ex)
                        {
                            ScrollDebugLogger.Log($"VariableEditorHandler: Error setting value: {ex.Message}");
                            canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer, $"Error: {ex.Message}");
                        }
                    }
                    else if (input == "backspace" || input == "Backspace" || input == "\b")
                    {
                        // Handle backspace
                        if (currentInputBuffer.Length > 0)
                        {
                            currentInputBuffer = currentInputBuffer.Substring(0, currentInputBuffer.Length - 1);
                        }
                        canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer);
                    }
                    else
                    {
                        // Accumulate character input
                        // Only allow valid characters based on type
                        var valueType = selectedVariable.GetValueType();
                        bool isValidChar = false;
                        
                        if (valueType == typeof(int))
                        {
                            // Allow digits, minus sign at start
                            isValidChar = char.IsDigit(input[0]) || (input == "-" && currentInputBuffer.Length == 0);
                        }
                        else if (valueType == typeof(double))
                        {
                            // Allow digits, decimal point, minus sign at start
                            isValidChar = char.IsDigit(input[0]) || 
                                        (input == "." && !currentInputBuffer.Contains(".")) ||
                                        (input == "-" && currentInputBuffer.Length == 0);
                        }
                        else if (valueType == typeof(bool))
                        {
                            // Allow letters and digits for true/false/1/0
                            isValidChar = char.IsLetterOrDigit(input[0]);
                        }
                        else
                        {
                            // String - allow any character
                            isValidChar = true;
                        }
                        
                        if (isValidChar && input.Length == 1)
                        {
                            currentInputBuffer += input;
                            canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer);
                        }
                        else if (input.Length > 1)
                        {
                            // Multi-character input (like "50") - add it all
                            currentInputBuffer += input;
                            canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer);
                        }
                    }
                }
                else if (input == "0")
                {
                    stateManager.TransitionToState(GameState.DeveloperMenu);
                    ShowDeveloperMenuEvent?.Invoke();
                }
                else if (input == "s" || input == "S")
                {
                    // Save changes
                    bool saved = variableEditor.SaveChanges();
                    if (saved)
                    {
                        ScrollDebugLogger.Log("VariableEditorHandler: Changes saved successfully");
                        canvasUI.RenderVariableEditor(null, false, "Changes saved successfully!");
                    }
                    else
                    {
                        ScrollDebugLogger.Log("VariableEditorHandler: Error saving changes");
                        canvasUI.RenderVariableEditor(null, false, "Error saving changes. Check logs.");
                    }
                }
                else if (int.TryParse(input, out int variableIndex))
                {
                    // Select a variable to edit
                    var variables = variableEditor.GetVariables();
                    if (variableIndex > 0 && variableIndex <= variables.Count)
                    {
                        selectedVariable = variables[variableIndex - 1];
                        isEditingValue = true;
                        currentInputBuffer = ""; // Reset input buffer
                        canvasUI.RenderVariableEditor(selectedVariable, true, currentInputBuffer);
                    }
                    else
                    {
                        canvasUI.ResetDeleteConfirmation();
                        canvasUI.RenderVariableEditor();
                    }
                }
                else
                {
                    canvasUI.ResetDeleteConfirmation();
                    canvasUI.RenderVariableEditor();
                }
            }
            else
            {
                ScrollDebugLogger.Log($"VariableEditorHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        /// <summary>
        /// Get the variable editor instance
        /// </summary>
        public VariableEditor GetVariableEditor()
        {
            return variableEditor;
        }
    }
}

