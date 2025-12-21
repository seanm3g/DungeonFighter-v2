using Avalonia;
using Avalonia.Input;
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
        /// Handles a mouse click at the given position.
        /// </summary>
        private void HandleMouseClick(Point position)
        {
            if (canvasUI == null) return;

            // Convert screen coordinates to character grid coordinates
            var gridPos = ScreenToGrid(position);

            // Check if click is on a clickable element
            var clickedElement = canvasUI.GetElementAt(gridPos.X, gridPos.Y);
            if (clickedElement != null)
            {
                // Process the click
                ProcessElementClick(clickedElement);
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
