using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Base interface for all screen renderers
    /// Provides core rendering capabilities for display
    /// </summary>
    public interface IScreenRenderer
    {
        /// <summary>
        /// Renders content to the canvas
        /// </summary>
        void Render();
        
        /// <summary>
        /// Clears the renderer state
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Gets the number of lines rendered
        /// </summary>
        int GetLineCount();
    }
    
    /// <summary>
    /// Extended interface for interactive screens
    /// Adds click handling and hover state management
    /// </summary>
    public interface IInteractiveRenderer : IScreenRenderer
    {
        /// <summary>
        /// Gets all clickable elements in the current render
        /// </summary>
        List<ClickableElement> GetClickableElements();
        
        /// <summary>
        /// Updates hover state based on mouse position
        /// </summary>
        void UpdateHoverState(int x, int y);
        
        /// <summary>
        /// Handles click events at the specified position
        /// Returns true if click was handled
        /// </summary>
        bool HandleClick(int x, int y);
    }
}

