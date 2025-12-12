namespace RPGGame
{
    using System;
    using RPGGame.Editors;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles tuning parameters menu display and input.
    /// </summary>
    public class TuningParametersHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private VariableEditor variableEditor;
        private string selectedCategory = "";
        private EditableVariable? selectedVariable = null;
        private bool isEditingValue = false;
        private string currentInputBuffer = "";
        
        // Delegates
        public delegate void OnShowDeveloperMenu();
        
        public event OnShowDeveloperMenu? ShowDeveloperMenuEvent;

        public TuningParametersHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.variableEditor = new VariableEditor();
        }

        /// <summary>
        /// Display the tuning parameters menu
        /// </summary>
        public void ShowTuningParametersMenu()
        {
            selectedCategory = ""; // Reset to category selection
            selectedVariable = null;
            isEditingValue = false;
            currentInputBuffer = "";
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderTuningParametersMenu();
            }
            else
            {
                ScrollDebugLogger.Log($"TuningParametersHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
            stateManager.TransitionToState(GameState.TuningParameters);
        }
        
        /// <summary>
        /// Hide the tuning parameters menu
        /// </summary>
        public void HideTuningParametersMenu()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.HideTuningParametersMenu();
            }
        }

        /// <summary>
        /// Handle tuning parameters menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                if (string.IsNullOrEmpty(selectedCategory))
                {
                    // Category selection mode
                    var categories = variableEditor.GetCategories();
                    if (input == "0")
                    {
                        canvasUI.ResetDeleteConfirmation();
                        HideTuningParametersMenu();
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        ShowDeveloperMenuEvent?.Invoke();
                    }
                    else if (int.TryParse(input, out int categoryIndex) && categoryIndex > 0 && categoryIndex <= categories.Count)
                    {
                        selectedCategory = categories[categoryIndex - 1];
                        canvasUI.RenderTuningParametersMenu(selectedCategory);
                    }
                    else
                    {
                        canvasUI.ResetDeleteConfirmation();
                        ShowTuningParametersMenu();
                    }
                }
                else
                {
                    // Variable viewing/editing mode
                    if (isEditingValue && selectedVariable != null)
                    {
                        // We're in value input mode - accumulate characters
                        if (input == "cancel" || input == "c" || input == "C")
                        {
                            // Cancel editing
                            isEditingValue = false;
                            selectedVariable = null;
                            currentInputBuffer = "";
                            canvasUI.RenderTuningParametersMenu(selectedCategory);
                        }
                        else if (input == "enter" || input == "Enter" || input == "\r" || input == "\n")
                        {
                            // Submit the value
                            if (string.IsNullOrWhiteSpace(currentInputBuffer))
                            {
                                canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer, "Please enter a value.");
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
                                        ScrollDebugLogger.Log($"TuningParametersHandler: Invalid integer value: {currentInputBuffer}");
                                        canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer, "Invalid integer value. Please enter a number.");
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
                                        ScrollDebugLogger.Log($"TuningParametersHandler: Invalid double value: {currentInputBuffer}");
                                        canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer, "Invalid number value. Please enter a number.");
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
                                        ScrollDebugLogger.Log($"TuningParametersHandler: Invalid boolean value: {currentInputBuffer}");
                                        canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer, "Invalid boolean value. Enter true/false or 1/0.");
                                        return;
                                    }
                                }
                                else
                                {
                                    newValue = currentInputBuffer.Trim(); // String or other types
                                }
                                
                                selectedVariable.SetValue(newValue);
                                ScrollDebugLogger.Log($"TuningParametersHandler: Set {selectedVariable.Name} to {newValue}");
                                isEditingValue = false;
                                selectedVariable = null;
                                currentInputBuffer = "";
                                canvasUI.RenderTuningParametersMenu(selectedCategory);
                            }
                            catch (Exception ex)
                            {
                                ScrollDebugLogger.Log($"TuningParametersHandler: Error setting value: {ex.Message}");
                                canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer, $"Error: {ex.Message}");
                            }
                        }
                        else if (input == "backspace" || input == "Backspace" || input == "\b")
                        {
                            // Handle backspace
                            if (currentInputBuffer.Length > 0)
                            {
                                currentInputBuffer = currentInputBuffer.Substring(0, currentInputBuffer.Length - 1);
                            }
                            canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer);
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
                                canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer);
                            }
                            else if (input.Length > 1)
                            {
                                // Multi-character input (like "50") - add it all
                                currentInputBuffer += input;
                                canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer);
                            }
                        }
                    }
                    else if (input == "0")
                    {
                        selectedCategory = "";
                        selectedVariable = null;
                        if (customUIManager is CanvasUICoordinator canvasUI2)
                        {
                            canvasUI2.HideTuningParametersMenu();
                        }
                        ShowTuningParametersMenu();
                    }
                    else if (input == "s" || input == "S")
                    {
                        // Save changes
                        bool saved = variableEditor.SaveChanges();
                        if (saved)
                        {
                            ScrollDebugLogger.Log("TuningParametersHandler: Changes saved successfully");
                            canvasUI.RenderTuningParametersMenu(selectedCategory, null, false, "Changes saved successfully!");
                        }
                        else
                        {
                            ScrollDebugLogger.Log("TuningParametersHandler: Error saving changes");
                            canvasUI.RenderTuningParametersMenu(selectedCategory, null, false, "Error saving changes. Check logs.");
                        }
                    }
                    else if (int.TryParse(input, out int variableIndex))
                    {
                        // Select a variable to edit
                        var variables = variableEditor.GetVariablesByCategory(selectedCategory);
                        if (variableIndex > 0 && variableIndex <= variables.Count)
                        {
                            selectedVariable = variables[variableIndex - 1];
                            isEditingValue = true;
                            currentInputBuffer = ""; // Reset input buffer
                            canvasUI.RenderTuningParametersMenu(selectedCategory, selectedVariable, true, currentInputBuffer);
                        }
                        else
                        {
                            canvasUI.ResetDeleteConfirmation();
                            canvasUI.RenderTuningParametersMenu(selectedCategory);
                        }
                    }
                    else
                    {
                        canvasUI.ResetDeleteConfirmation();
                        canvasUI.RenderTuningParametersMenu(selectedCategory);
                    }
                }
            }
            else
            {
                ScrollDebugLogger.Log($"TuningParametersHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        /// <summary>
        /// Get the variable editor instance
        /// </summary>
        public VariableEditor GetVariableEditor()
        {
            return variableEditor;
        }

        /// <summary>
        /// Get the selected category
        /// </summary>
        public string GetSelectedCategory()
        {
            return selectedCategory;
        }
    }
}

