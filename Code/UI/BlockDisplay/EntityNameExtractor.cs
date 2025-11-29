namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Extracts Actor name from messages
    /// Supports formats: "[EntityName] ..." or "EntityName hits ..."
    /// </summary>
    public static class EntityNameExtractor
    {
        /// <summary>
        /// Extracts Actor name from a message
        /// Supports formats: "[EntityName] ..." or "EntityName hits ..."
        /// </summary>
        /// <param name="message">Message to extract Actor name from</param>
        /// <returns>Actor name if found, null otherwise</returns>
        public static string? ExtractEntityNameFromMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            
            // Try bracket format first: [EntityName] ...
            int startIndex = message.IndexOf('[');
            if (startIndex != -1)
            {
                int endIndex = message.IndexOf(']', startIndex);
                if (endIndex != -1)
                {
                    return message.Substring(startIndex + 1, endIndex - startIndex - 1);
                }
            }
            
            // Try format without brackets: "EntityName hits ..."
            int hitsIndex = message.IndexOf(" hits ");
            if (hitsIndex > 0)
            {
                return message.Substring(0, hitsIndex).Trim();
            }
            
            // Try format: "EntityName misses ..."
            int missesIndex = message.IndexOf(" misses ");
            if (missesIndex > 0)
            {
                return message.Substring(0, missesIndex).Trim();
            }
            
            // Try format: "EntityName uses ..."
            int usesIndex = message.IndexOf(" uses ");
            if (usesIndex > 0)
            {
                return message.Substring(0, usesIndex).Trim();
            }
            
            return null;
        }
    }
}

