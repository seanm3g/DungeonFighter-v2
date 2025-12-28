using Avalonia;
using Avalonia.Input;
using RPGGame;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Handles mouse interactions for the game canvas.
    /// Extracted from MainWindow to separate concerns.
    /// </summary>
    public class MouseInteractionHandler
    {
        private readonly GameCanvasControl gameCanvas;
        private readonly CanvasUICoordinator? canvasUI;
        private readonly GameCoordinator? game;

        public MouseInteractionHandler(
            GameCanvasControl gameCanvas,
            CanvasUICoordinator? canvasUI,
            GameCoordinator? game)
        {
            this.gameCanvas = gameCanvas;
            this.canvasUI = canvasUI;
            this.game = game;
        }

        /// <summary>
        /// Handles pointer pressed events (mouse clicks).
        /// </summary>
        public void HandlePointerPressed(PointerPressedEventArgs e)
        {
            if (game == null) return;

            var point = e.GetCurrentPoint(gameCanvas);
            if (point.Properties.IsLeftButtonPressed)
            {
                HandleMouseClick(point.Position);
            }
        }

        /// <summary>
        /// Handles pointer moved events (mouse hover).
        /// </summary>
        public void HandlePointerMoved(PointerEventArgs e)
        {
            var point = e.GetCurrentPoint(gameCanvas);
            HandleMouseHover(point.Position);
        }

        /// <summary>
        /// Handles pointer released events.
        /// </summary>
        public void HandlePointerReleased(PointerReleasedEventArgs e)
        {
            // Handle mouse release if needed
        }

        /// <summary>
        /// Handles pointer wheel events (mouse wheel scrolling).
        /// </summary>
        public void HandlePointerWheelChanged(PointerWheelEventArgs e)
        {
            if (canvasUI == null) return;

            // Get wheel delta (positive = scroll up, negative = scroll down)
            var delta = e.Delta.Y;
            
            // Only handle scrolling if there's a significant delta
            if (Math.Abs(delta) < 0.1) return;

            // Scroll the center panel display
            if (delta > 0)
            {
                // Scroll up (show earlier content)
                canvasUI.ScrollUp(3);
            }
            else
            {
                // Scroll down (show later content)
                canvasUI.ScrollDown(3);
            }
            
            // Mark event as handled to prevent default scrolling behavior
            e.Handled = true;
        }

        /// <summary>
        /// Handles a mouse click at the given position.
        /// </summary>
        private void HandleMouseClick(Point position)
        {
            if (canvasUI == null || game == null) return;

            // Convert screen coordinates to character grid coordinates
            var gridPos = ScreenToGrid(position);

            // Check if click is on a clickable element
            var clickedElement = canvasUI.GetElementAt(gridPos.X, gridPos.Y);
            if (clickedElement != null)
            {
                // Process the click
                ProcessElementClick(clickedElement);
            }
            else
            {
                // If no clickable element was clicked, check if we're in Settings state
                // and prevent accidental settings menu opening after tests
                if (game.StateManager != null && game.StateManager.CurrentState == GameState.Settings)
                {
                    // If clicking on empty space while in Settings state, don't do anything
                    // This prevents the settings menu from opening again
                    return;
                }
            }
        }

        /// <summary>
        /// Handles mouse hover at the given position.
        /// </summary>
        private void HandleMouseHover(Point position)
        {
            if (canvasUI == null) return;

            // Convert screen coordinates to character grid coordinates
            var gridPos = ScreenToGrid(position);

            // Update hover state
            canvasUI.SetHoverPosition(gridPos.X, gridPos.Y);
        }

        /// <summary>
        /// Converts screen coordinates to character grid coordinates.
        /// </summary>
        private (int X, int Y) ScreenToGrid(Point screenPosition)
        {
            // Use actual measured character dimensions from the canvas
            double charWidth = gameCanvas.GetCharWidth();
            double charHeight = gameCanvas.GetCharHeight();

            // Account for the margin (10px) in the XAML
            double adjustedX = screenPosition.X - 10;
            double adjustedY = screenPosition.Y - 10;

            int gridX = (int)(adjustedX / charWidth);
            int gridY = (int)(adjustedY / charHeight);

            return (gridX, gridY);
        }

        /// <summary>
        /// Processes a click on a clickable element.
        /// </summary>
        private async void ProcessElementClick(ClickableElement element)
        {
            if (game == null) return;

            try
            {
                switch (element.Type)
                {
                    case ElementType.MenuOption:
                        await game.HandleInput(element.Value);
                        break;
                    case ElementType.Item:
                        // Handle item selection
                        if (canvasUI != null)
                        {
                            canvasUI.UpdateStatus($"Selected item: {element.Value}");
                        }
                        break;
                    case ElementType.Button:
                        // Handle button click
                        await game.HandleInput(element.Value);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log error and show message to user
                System.Diagnostics.Debug.WriteLine($"Error processing element click: {ex.Message}\n{ex.StackTrace}");
                if (canvasUI != null)
                {
                    canvasUI.UpdateStatus($"Error: {ex.Message}");
                }
            }
        }
    }
}
