using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Helper class for building narrative text with colored segments.
    /// Provides common operations for highlighting names and text replacements.
    /// </summary>
    public class NarrativeTextBuilder
    {
        private readonly ColoredTextBuilder builder;

        public NarrativeTextBuilder()
        {
            builder = new ColoredTextBuilder();
        }

        /// <summary>
        /// Adds an emoji with a specific color to the narrative.
        /// </summary>
        public NarrativeTextBuilder AddEmoji(string emoji, ColorPalette color)
        {
            builder.Add(emoji, color);
            return this;
        }

        /// <summary>
        /// Adds plain text with a specific color.
        /// </summary>
        public NarrativeTextBuilder AddText(string text, ColorPalette color)
        {
            builder.Add(text, color);
            return this;
        }

        /// <summary>
        /// Highlights a name within text by coloring it differently.
        /// </summary>
        /// <param name="fullText">The complete text</param>
        /// <param name="nameToHighlight">The name to highlight</param>
        /// <param name="beforeColor">Color for text before the name</param>
        /// <param name="nameColor">Color for the name</param>
        /// <param name="afterColor">Color for text after the name</param>
        public NarrativeTextBuilder AddTextWithHighlight(
            string fullText, 
            string nameToHighlight, 
            ColorPalette beforeColor, 
            ColorPalette nameColor, 
            ColorPalette afterColor)
        {
            if (fullText.Contains(nameToHighlight))
            {
                int startIndex = fullText.IndexOf(nameToHighlight);
                
                if (startIndex > 0)
                    builder.Add(fullText.Substring(0, startIndex), beforeColor);
                
                builder.Add(nameToHighlight, nameColor);
                
                if (startIndex + nameToHighlight.Length < fullText.Length)
                    builder.Add(fullText.Substring(startIndex + nameToHighlight.Length), afterColor);
            }
            else
            {
                builder.Add(fullText, beforeColor);
            }

            return this;
        }

        /// <summary>
        /// Highlights two names within text by coloring them differently.
        /// </summary>
        public NarrativeTextBuilder AddTextWithDualHighlight(
            string fullText,
            string firstName,
            string secondName,
            ColorPalette baseColor,
            ColorPalette firstNameColor,
            ColorPalette secondNameColor)
        {
            int firstIndex = fullText.IndexOf(firstName);
            int secondIndex = fullText.IndexOf(secondName);

            if (firstIndex >= 0 && secondIndex >= 0)
            {
                if (firstIndex < secondIndex)
                {
                    // First name comes before second name
                    builder.Add(fullText.Substring(0, firstIndex), baseColor);
                    builder.Add(firstName, firstNameColor);
                    
                    int middleStart = firstIndex + firstName.Length;
                    int middleLength = secondIndex - middleStart;
                    builder.Add(fullText.Substring(middleStart, middleLength), baseColor);
                    
                    builder.Add(secondName, secondNameColor);
                    
                    int endStart = secondIndex + secondName.Length;
                    if (endStart < fullText.Length)
                        builder.Add(fullText.Substring(endStart), baseColor);
                }
                else
                {
                    // Second name comes before first name
                    builder.Add(fullText.Substring(0, secondIndex), baseColor);
                    builder.Add(secondName, secondNameColor);
                    
                    int middleStart = secondIndex + secondName.Length;
                    int middleLength = firstIndex - middleStart;
                    builder.Add(fullText.Substring(middleStart, middleLength), baseColor);
                    
                    builder.Add(firstName, firstNameColor);
                    
                    int endStart = firstIndex + firstName.Length;
                    if (endStart < fullText.Length)
                        builder.Add(fullText.Substring(endStart), baseColor);
                }
            }
            else if (firstIndex >= 0)
            {
                AddTextWithHighlight(fullText, firstName, baseColor, firstNameColor, baseColor);
            }
            else if (secondIndex >= 0)
            {
                AddTextWithHighlight(fullText, secondName, baseColor, secondNameColor, baseColor);
            }
            else
            {
                builder.Add(fullText, baseColor);
            }

            return this;
        }

        /// <summary>
        /// Handles quoted text with optional name highlighting after the quote.
        /// </summary>
        public NarrativeTextBuilder AddQuotedText(
            string fullText,
            string quoteColor,
            string nameToHighlight,
            ColorPalette afterQuoteColor,
            ColorPalette nameColor)
        {
            if (fullText.StartsWith("\""))
            {
                int quoteEnd = fullText.IndexOf("\"", 1);
                if (quoteEnd > 0)
                {
                    // Add the quoted part
                    builder.Add(fullText.Substring(0, quoteEnd + 1), ColorPalette.Cyan);
                    
                    // Add the remaining text with name highlighting
                    string remaining = fullText.Substring(quoteEnd + 1);
                    if (remaining.Contains(nameToHighlight))
                    {
                        int nameIndex = remaining.IndexOf(nameToHighlight);
                        builder.Add(remaining.Substring(0, nameIndex), afterQuoteColor);
                        builder.Add(nameToHighlight, nameColor);
                        builder.Add(remaining.Substring(nameIndex + nameToHighlight.Length), afterQuoteColor);
                    }
                    else
                    {
                        builder.Add(remaining, afterQuoteColor);
                    }
                }
                else
                {
                    builder.Add(fullText, ColorPalette.Cyan);
                }
            }
            else
            {
                // No quote, just highlight the name
                AddTextWithHighlight(fullText, nameToHighlight, afterQuoteColor, nameColor, afterQuoteColor);
            }

            return this;
        }

        /// <summary>
        /// Builds and returns the colored text list.
        /// </summary>
        public List<ColoredText> Build()
        {
            return builder.Build();
        }
    }
}

