namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using RPGGame.UI;

    /// <summary>
    /// Factory for creating clickable buttons in inventory screens
    /// </summary>
    public static class InventoryButtonFactory
    {
        /// <summary>
        /// Creates a clickable button element
        /// </summary>
        public static ClickableElement CreateButton(int x, int y, int width, string value, string displayText)
        {
            return new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = 1,
                Type = ElementType.Button,
                Value = value,
                DisplayText = displayText
            };
        }
    }
}

