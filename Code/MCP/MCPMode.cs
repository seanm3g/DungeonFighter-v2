namespace RPGGame.MCP
{
    /// <summary>
    /// Global flag to indicate MCP mode is active
    /// Used to disable delays and optimize for automated interactions
    /// </summary>
    public static class MCPMode
    {
        /// <summary>
        /// Gets or sets whether MCP mode is active
        /// When true, all delays are disabled for faster execution
        /// </summary>
        public static bool IsActive { get; set; } = false;
    }
}

