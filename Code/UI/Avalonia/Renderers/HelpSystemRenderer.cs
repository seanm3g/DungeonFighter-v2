using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for help system and documentation
    /// </summary>
    public class HelpSystemRenderer
    {
        private readonly GameCanvasControl canvas;
        private bool showHelp = false;
        
        public HelpSystemRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        /// <summary>
        /// Toggles the help display state
        /// </summary>
        public bool ToggleHelp()
        {
            showHelp = !showHelp;
            return showHelp;
        }
        
        /// <summary>
        /// Gets the current help display state
        /// </summary>
        public bool IsHelpVisible => showHelp;
        
        /// <summary>
        /// Renders the help screen with controls and game features
        /// </summary>
        public void RenderHelp()
        {
            canvas.Clear();
            
            // Title
            canvas.AddTitle(2, "HELP - DUNGEON FIGHTER", AsciiArtAssets.Colors.White);
            
            // Help content
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 2, 6, "Controls:", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 8, "1-6: Select menu option", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 9, "H: Toggle this help screen", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 10, "ESC: Go back/Exit", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 11, "Arrow Keys: Navigate menus", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 12, "Enter: Confirm selection", AsciiArtAssets.Colors.White);
            
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 2, 15, "Game Features:", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 17, "• Enter Dungeon: Start exploring", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 18, "• View Inventory: Check items", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 19, "• Character Info: View stats", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 20, "• Tuning Console: Game settings", AsciiArtAssets.Colors.White);
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 4, 21, "• Save Game: Save progress", AsciiArtAssets.Colors.White);
            
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 2, 24, "Press H to return to main menu", AsciiArtAssets.Colors.White);
            
            canvas.Refresh();
        }
    }
}
