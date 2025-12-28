namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Extracts Actor name from messages
    /// Supports formats: "[EntityName] ...", "EntityName hits ...", "EntityName takes ...", "EntityName is affected ..."
    /// </summary>
    public static class EntityNameExtractor
    {
        /// <summary>
        /// Extracts Actor name from a message
        /// Supports formats: "[EntityName] ...", "EntityName hits ...", "EntityName takes ...", "EntityName is affected ..."
        /// </summary>
        /// <param name="message">Message to extract Actor name from</param>
        /// <returns>Actor name if found, null otherwise</returns>
        public static string? ExtractEntityNameFromMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            
            // Remove leading whitespace/indentation (5 spaces for action blocks)
            message = message.TrimStart();
            
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
            
            // Try format: "EntityName takes ..." (for status effect damage messages)
            int takesIndex = message.IndexOf(" takes ");
            if (takesIndex > 0)
            {
                return message.Substring(0, takesIndex).Trim();
            }
            
            // Try format: "EntityName is affected ..." (for status effect application messages)
            int isAffectedIndex = message.IndexOf(" is affected ");
            if (isAffectedIndex > 0)
            {
                return message.Substring(0, isAffectedIndex).Trim();
            }
            
            // Try format: "EntityName is ..." (for status effect messages like "is stunned", "is bleeding")
            int isIndex = message.IndexOf(" is ");
            if (isIndex > 0)
            {
                // Check if it's a status effect message (not "is no longer")
                if (!message.Substring(isIndex).StartsWith(" is no longer "))
                {
                    return message.Substring(0, isIndex).Trim();
                }
            }
            
            return null;
        }
    }
}

