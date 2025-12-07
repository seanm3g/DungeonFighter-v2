namespace RPGGame.UI.Avalonia.Display.Buffer
{
    using System.Collections.Generic;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Validates messages and checks for equality between segment lists.
    /// </summary>
    public class MessageValidator
    {
        /// <summary>
        /// Checks if two segment lists are equal (for duplicate detection)
        /// </summary>
        public bool AreSegmentsEqual(List<ColoredText> segments1, List<ColoredText> segments2)
        {
            if (segments1 == null && segments2 == null) return true;
            if (segments1 == null || segments2 == null) return false;
            if (segments1.Count != segments2.Count) return false;
            
            for (int i = 0; i < segments1.Count; i++)
            {
                if (segments1[i].Text != segments2[i].Text || 
                    segments1[i].Color != segments2[i].Color)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}

