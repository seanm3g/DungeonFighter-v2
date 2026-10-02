using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Text
{

    /// <summary>
    /// Handles text wrapping logic for both plain text and colored text segments.
    /// </summary>
    public class TextWrappingHelper
    {
        /// <summary>
        /// Short words at the end of a line are pulled onto the next line with their continuation
        /// (avoids "Wolf" / "answers with…" and "The" / "exchange…" orphans).
        /// </summary>
        public const int MaxOrphanWordLength = 4;

        /// <summary>
        /// Wraps text to fit within a specified width
        /// </summary>
        public static List<string> WrapText(string text, int maxWidth)
        {
            var result = new List<string>();
            
            if (string.IsNullOrEmpty(text))
                return result;
            
            // Preserve leading spaces (important for indented text like roll info)
            string leadingSpaces = "";
            int leadingSpaceCount = 0;
            for (int i = 0; i < text.Length && char.IsWhiteSpace(text[i]); i++)
            {
                leadingSpaces += text[i];
                leadingSpaceCount++;
            }
            
            // Get the text without leading spaces for word splitting
            string textWithoutLeading = text.Substring(leadingSpaceCount);
            
            // If the entire text is just whitespace, return it as-is
            if (string.IsNullOrEmpty(textWithoutLeading))
            {
                result.Add(text);
                return result;
            }
            
            var words = textWithoutLeading.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = leadingSpaces; // Start with leading spaces
            
            foreach (var word in words)
            {
                var testLine = currentLine == leadingSpaces ? $"{currentLine}{word}" : $"{currentLine} {word}";
                
                if (testLine.Length <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    // Only add line if it has content beyond just leading spaces
                    if (!string.IsNullOrEmpty(currentLine.Trim()))
                    {
                        result.Add(currentLine);
                    }
                    // Start new line with leading spaces preserved
                    currentLine = leadingSpaces + word;
                }
            }
            
            // Add the last line if it has content
            if (!string.IsNullOrEmpty(currentLine.Trim()))
            {
                result.Add(currentLine);
            }
            
            return result;
        }
        
        /// <summary>
        /// Soft-wraps colored segments at word boundaries and rejoins them with a single
        /// <c>\n</c> between physical lines (kept in one segment list). Used by the combat-log
        /// buffer and by F7 narrative typewriter so wrap points are decided on the full text
        /// before characters are revealed — a word that will not fit starts on the next line
        /// from its first glyph instead of jumping mid-reveal.
        /// </summary>
        public static List<ColoredText> ApplySoftWraps(List<ColoredText> segments, int maxWidth)
        {
            if (segments == null || segments.Count == 0)
                return segments ?? new List<ColoredText>();

            if (maxWidth <= 0)
                maxWidth = 1;

            int displayLength = 0;
            bool hasBreak = false;
            foreach (var seg in segments)
            {
                string text = seg?.Text ?? string.Empty;
                displayLength += text.Length;
                if (!hasBreak && (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0))
                    hasBreak = true;
            }

            if (displayLength <= maxWidth && !hasBreak)
                return segments;

            var wrappedLines = WrapColoredSegments(segments, maxWidth);
            if (wrappedLines.Count == 0)
                return segments;
            if (wrappedLines.Count == 1)
                return wrappedLines[0];

            var result = new List<ColoredText>();
            for (int i = 0; i < wrappedLines.Count; i++)
            {
                if (i > 0)
                    result.Add(new ColoredText("\n", Colors.White));
                result.AddRange(wrappedLines[i]);
            }
            return result;
        }

        /// <summary>
        /// Wraps ColoredText segments while preserving their colors.
        /// Soft-wraps at word boundaries inside multi-word colored spans (never treats a short
        /// multi-word phrase as an atomic block). Glues orphan punctuation onto the preceding word
        /// so "," / "." cannot open a line alone. Short trailing words in the tight EOL zone that
        /// would sit alone before more content are kept with the next line (e.g. "Wolf answers…"
        /// stays together) — without pulling mid-line connectors early and leaving stubs like
        /// "Skill" alone before "and luck…".
        /// <para>
        /// Book-style first-line indent: a run of leading spaces on the input is kept only on the
        /// first wrapped line (and counts toward width). Continuation lines never lead with a space.
        /// </para>
        /// <para>
        /// Leading spaces between tokens are restored only when the original text had whitespace
        /// (or after glued punctuation). Per-character color/animation segments that form one word
        /// (e.g. undulating "Ancient Forest", template-colored "STUN") must not gain spaces between glyphs.
        /// </para>
        /// </summary>
        public static List<List<ColoredText>> WrapColoredSegments(List<ColoredText> segments, int maxWidth)
        {
            if (maxWidth <= 0)
                maxWidth = 1;

            ExtractLeadingIndent(segments, out List<ColoredText> indentSegments, out List<ColoredText> bodySegments);
            int indentWidth = 0;
            foreach (var seg in indentSegments)
                indentWidth += seg.Text?.Length ?? 0;

            var tokens = FlattenToTokens(bodySegments);
            var wrappedLines = new List<List<ColoredText>>();
            var currentLine = new List<ColoredText>();
            int currentLineWidth = 0;
            // After glued punctuation (e.g. "fortune,"), the next word still needs a separator.
            bool spaceAfterPunctuation = false;

            void SeedFirstLineIndent()
            {
                // Book style: indent only the first wrapped line, never continuations.
                if (indentWidth <= 0 || wrappedLines.Count > 0 || currentLine.Count > 0 || currentLineWidth > 0)
                    return;
                foreach (var seg in indentSegments)
                    currentLine.Add(CloneSegment(seg.Text, seg));
                currentLineWidth = indentWidth;
            }

            void FlushLine()
            {
                if (currentLine.Count == 0)
                    return;
                wrappedLines.Add(currentLine);
                currentLine = new List<ColoredText>();
                currentLineWidth = 0;
                spaceAfterPunctuation = false;
                // Continuation lines stay flush left (book style — no hanging indent).
            }

            void GluePunctuation(string punct, ColoredText source)
            {
                SeedFirstLineIndent();

                if (currentLine.Count > 0)
                {
                    var last = currentLine[currentLine.Count - 1];
                    last.Text += punct;
                    currentLineWidth += punct.Length;
                    spaceAfterPunctuation = true;
                    return;
                }

                currentLine.Add(CloneSegment(punct, source));
                currentLineWidth += punct.Length;
                spaceAfterPunctuation = true;
            }

            void AppendWord(string word, ColoredText source, bool needsLeadingSpace)
            {
                if (string.IsNullOrEmpty(word))
                    return;

                SeedFirstLineIndent();

                if (word.Length > maxWidth)
                {
                    int offset = 0;
                    while (offset < word.Length)
                    {
                        if (currentLineWidth > 0 && currentLineWidth >= maxWidth)
                            FlushLine();

                        int room = maxWidth - currentLineWidth;
                        if (room <= 0)
                        {
                            FlushLine();
                            room = maxWidth;
                        }

                        int take = Math.Min(room, word.Length - offset);
                        string chunk = word.Substring(offset, take);
                        currentLine.Add(CloneSegment(chunk, source));
                        currentLineWidth += take;
                        offset += take;
                    }

                    // Finished a whole word that was longer than the line; next word may still need a space.
                    spaceAfterPunctuation = false;
                    return;
                }

                bool OnIndentedFirstLine() =>
                    indentWidth > 0 && currentLineWidth == indentWidth && wrappedLines.Count == 0;

                bool insertSpace = currentLineWidth > 0 && needsLeadingSpace && !OnIndentedFirstLine();
                int needed = word.Length + (insertSpace ? 1 : 0);
                if (currentLineWidth > 0 && currentLineWidth + needed > maxWidth)
                    FlushLine();

                // Recompute after a possible flush (continuation lines never lead with a space;
                // first content word after book indent also has no separator).
                insertSpace = currentLineWidth > 0 && needsLeadingSpace && !OnIndentedFirstLine();
                if (insertSpace)
                {
                    currentLine.Add(CloneSegment(" ", source));
                    currentLineWidth += 1;
                }

                currentLine.Add(CloneSegment(word, source));
                currentLineWidth += word.Length;
                spaceAfterPunctuation = false;
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.IsHardBreak)
                {
                    FlushLine();
                    continue;
                }

                if (token.IsPunctuation)
                {
                    GluePunctuation(token.Text, token.Source);
                    continue;
                }

                bool needsLeadingSpace = token.NeedsLeadingSpace || spaceAfterPunctuation;

                // Look ahead: if this short word fits but the next *separate* word does not, keep them
                // together on the next line instead of stranding "Wolf" / "The" / "Fate" at EOL.
                // Only applies when the next token had real whitespace before it (NeedsLeadingSpace);
                // glued per-character glyphs must not trigger orphan pulls.
                //
                // Only fire in the tight EOL zone (remaining room for this word ≤ MaxOrphanWordLength).
                // Otherwise pulling "and"/"the" early leaves worse stubs ("    Skill" alone) when the
                // line still had room for a short connector but not the following content word.
                SeedFirstLineIndent();
                if (currentLineWidth > 0
                    && VisibleWordLength(token.Text) <= MaxOrphanWordLength)
                {
                    bool onIndentedFirstLine =
                        indentWidth > 0 && currentLineWidth == indentWidth && wrappedLines.Count == 0;
                    int spaceCost = currentLineWidth > 0 && needsLeadingSpace && !onIndentedFirstLine ? 1 : 0;
                    int remainBefore = maxWidth - currentLineWidth - spaceCost;
                    int widthIfPlaced = currentLineWidth + spaceCost + token.Text.Length;
                    if (remainBefore <= MaxOrphanWordLength
                        && widthIfPlaced <= maxWidth)
                    {
                        int nextWordIndex = IndexOfNextWord(tokens, i + 1);
                        if (nextWordIndex >= 0 && tokens[nextWordIndex].NeedsLeadingSpace)
                        {
                            int remainAfter = maxWidth - widthIfPlaced;
                            string nextWord = tokens[nextWordIndex].Text;
                            // Need room for a separating space + next word.
                            if (remainAfter < 1 + nextWord.Length)
                            {
                                FlushLine();
                                needsLeadingSpace = false; // new line never leads with a space
                            }
                        }
                    }
                }

                AppendWord(token.Text, token.Source, needsLeadingSpace);
            }

            FlushLine();

            if (wrappedLines.Count == 0)
            {
                // Indent-only / empty body: still emit the indent so callers see it.
                if (indentWidth > 0)
                {
                    var indentOnly = new List<ColoredText>();
                    foreach (var seg in indentSegments)
                        indentOnly.Add(CloneSegment(seg.Text, seg));
                    wrappedLines.Add(indentOnly);
                }
                else
                {
                    wrappedLines.Add(new List<ColoredText>());
                }
            }

            return wrappedLines;
        }

        /// <summary>
        /// Pulls a leading space run off the segment list for book-style first-line indent.
        /// Only the first wrapped line should receive these spaces.
        /// </summary>
        private static void ExtractLeadingIndent(
            List<ColoredText> segments,
            out List<ColoredText> indentSegments,
            out List<ColoredText> bodySegments)
        {
            indentSegments = new List<ColoredText>();
            bodySegments = new List<ColoredText>();
            if (segments == null || segments.Count == 0)
                return;

            bool inLeading = true;
            foreach (var segment in segments)
            {
                if (segment == null)
                    continue;

                string text = segment.Text ?? string.Empty;
                if (text.Length == 0)
                {
                    if (!inLeading)
                        bodySegments.Add(segment);
                    continue;
                }

                if (!inLeading)
                {
                    bodySegments.Add(segment);
                    continue;
                }

                // Newlines end the indent region immediately.
                if (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0)
                {
                    inLeading = false;
                    bodySegments.Add(segment);
                    continue;
                }

                int i = 0;
                while (i < text.Length && text[i] == ' ')
                    i++;

                if (i > 0)
                    indentSegments.Add(CloneSegment(text.Substring(0, i), segment));

                if (i < text.Length)
                {
                    inLeading = false;
                    if (i == 0)
                        bodySegments.Add(segment);
                    else
                        bodySegments.Add(CloneSegment(text.Substring(i), segment));
                }
            }
        }

        private static int IndexOfNextWord(List<WrapToken> tokens, int start)
        {
            for (int i = start; i < tokens.Count; i++)
            {
                if (tokens[i].IsHardBreak)
                    return -1;
                if (!tokens[i].IsPunctuation)
                    return i;
            }

            return -1;
        }

        private static int VisibleWordLength(string word)
        {
            if (string.IsNullOrEmpty(word))
                return 0;
            int len = word.Length;
            while (len > 0 && IsTrailingPunctuationChar(word[len - 1]))
                len--;
            return len;
        }

        private static bool IsTrailingPunctuationChar(char c) =>
            c is ',' or '.' or '!' or '?' or ';' or ':' or '\'' or '"' or ')' or ']' or '}';

        private static List<WrapToken> FlattenToTokens(List<ColoredText> segments)
        {
            var tokens = new List<WrapToken>();
            // Tracks whitespace that was present in the source text before the next word.
            // Critical: per-character template/animation segments have no inter-glyph spaces,
            // so this stays false across those boundaries and wrap must not invent spaces.
            bool pendingLeadingSpace = false;

            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;

                if (segment.Text.Contains('\n') || segment.Text.Contains('\r'))
                {
                    var parts = segment.Text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    for (int p = 0; p < parts.Length; p++)
                    {
                        if (p > 0)
                        {
                            tokens.Add(WrapToken.HardBreak());
                            pendingLeadingSpace = false;
                        }
                        AppendPartTokens(tokens, parts[p], segment, ref pendingLeadingSpace);
                    }
                    continue;
                }

                AppendPartTokens(tokens, segment.Text, segment, ref pendingLeadingSpace);
            }

            return tokens;
        }

        private static void AppendPartTokens(
            List<WrapToken> tokens,
            string part,
            ColoredText source,
            ref bool pendingLeadingSpace)
        {
            if (string.IsNullOrEmpty(part))
                return;

            int i = 0;
            while (i < part.Length)
            {
                if (part[i] == ' ')
                {
                    pendingLeadingSpace = true;
                    i++;
                    continue;
                }

                int start = i;
                while (i < part.Length && part[i] != ' ')
                    i++;

                string token = part.Substring(start, i - start);
                if (IsPunctuationOnly(token))
                {
                    tokens.Add(WrapToken.Punctuation(token, source));
                    // Punctuation does not consume the pending space; a following word still may need it
                    // if the source had "word , next" (unusual). More often "," is its own segment after
                    // a word with no trailing space, then " " + "next" sets pendingLeadingSpace again.
                }
                else
                {
                    tokens.Add(WrapToken.Word(token, source, pendingLeadingSpace));
                    pendingLeadingSpace = false;
                }
            }
        }

        private static ColoredText CloneSegment(string text, ColoredText source)
        {
            return new ColoredText(text, source.Color, source.SourceTemplate, source.ColorReadyForCanvas);
        }

        private static bool IsPunctuationOnly(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            for (int i = 0; i < token.Length; i++)
            {
                if (!IsTrailingPunctuationChar(token[i]))
                    return false;
            }

            return true;
        }

        private readonly struct WrapToken
        {
            public string Text { get; }
            public ColoredText Source { get; }
            public bool IsPunctuation { get; }
            public bool IsHardBreak { get; }
            /// <summary>True when the source text had whitespace immediately before this word.</summary>
            public bool NeedsLeadingSpace { get; }

            private WrapToken(string text, ColoredText source, bool isPunctuation, bool isHardBreak, bool needsLeadingSpace)
            {
                Text = text;
                Source = source;
                IsPunctuation = isPunctuation;
                IsHardBreak = isHardBreak;
                NeedsLeadingSpace = needsLeadingSpace;
            }

            public static WrapToken Word(string text, ColoredText source, bool needsLeadingSpace) =>
                new WrapToken(text, source, isPunctuation: false, isHardBreak: false, needsLeadingSpace);

            public static WrapToken Punctuation(string text, ColoredText source) =>
                new WrapToken(text, source, isPunctuation: true, isHardBreak: false, needsLeadingSpace: false);

            public static WrapToken HardBreak() =>
                new WrapToken(string.Empty, new ColoredText(string.Empty), isPunctuation: false, isHardBreak: true, needsLeadingSpace: false);
        }
    }
}
