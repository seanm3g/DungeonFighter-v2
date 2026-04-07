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
            return CreateButton(x, y, width, 1, value, displayText, tooltipHoverValue: null);
        }

        /// <summary>Multi-line inventory row: optional <paramref name="tooltipHoverValue"/> (e.g. <c>lphover:inv:0</c>) while <paramref name="value"/> stays the input token.</summary>
        public static ClickableElement CreateButton(int x, int y, int width, int height, string value, string displayText, string? tooltipHoverValue)
        {
            return new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = height < 1 ? 1 : height,
                Type = ElementType.Button,
                Value = value,
                DisplayText = displayText,
                TooltipHoverValue = tooltipHoverValue
            };
        }
    }
}

