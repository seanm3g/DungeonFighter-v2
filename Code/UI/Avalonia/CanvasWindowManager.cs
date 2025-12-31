namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Manages window and game instance references for CanvasUICoordinator.
    /// Extracted from CanvasUICoordinator to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CanvasWindowManager
    {
        private MainWindow? mainWindow;
        private GameCoordinator? game;
        private System.Action? closeAction;
        
        /// <summary>
        /// Sets the main window reference for accessing UI controls
        /// </summary>
        public void SetMainWindow(MainWindow window)
        {
            this.mainWindow = window;
        }
        
        /// <summary>
        /// Gets the main window reference
        /// </summary>
        public MainWindow? GetMainWindow()
        {
            return this.mainWindow;
        }
        
        /// <summary>
        /// Sets the game instance reference for accessing handlers
        /// </summary>
        public void SetGame(GameCoordinator gameInstance)
        {
            this.game = gameInstance;
        }
        
        /// <summary>
        /// Gets the game instance reference
        /// </summary>
        public GameCoordinator? GetGame()
        {
            return this.game;
        }
        
        /// <summary>
        /// Sets the close action callback
        /// </summary>
        public void SetCloseAction(System.Action action)
        {
            closeAction = action;
        }
        
        /// <summary>
        /// Invokes the close action if set
        /// </summary>
        public void Close()
        {
            closeAction?.Invoke();
        }
    }
}
