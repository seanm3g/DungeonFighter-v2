using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Setup/punchline combat headlines: identical telegraph, then result after the ellipsis.
    /// </summary>
    public static class ActionHeadlineFormatter
    {
        public const string SetupVerb = "Attacks";
        public const string Ellipsis = "...";

        /// <summary>
        /// Result-blind setup: <c>{Actor} Attacks {Target}...</c>
        /// </summary>
        public static List<ColoredText> FormatSetup(Actor attacker, Actor target)
        {
            var builder = new ColoredTextBuilder();
            EntityColorHelper.AppendActorNameColored(builder, attacker);
            builder.AddSpace();
            builder.Add(SetupVerb, Colors.White);
            builder.AddSpace();
            EntityColorHelper.AppendActorNameColored(builder, target);
            builder.Add(Ellipsis, Colors.White);
            return builder.Build();
        }

        /// <summary>
        /// Concatenate an already-built setup with a punchline (leading space + <c>and ...</c>).
        /// </summary>
        public static List<ColoredText> Combine(List<ColoredText> setup, List<ColoredText> punchline)
        {
            var combined = new List<ColoredText>(setup.Count + punchline.Count);
            combined.AddRange(setup);
            combined.AddRange(punchline);
            return combined;
        }

        /// <summary>
        /// Splits a completed headline at the setup ellipsis. Setup includes <c>...</c>; punchline is the rest.
        /// </summary>
        public static bool TrySplit(List<ColoredText>? headline, out List<ColoredText> setup, out List<ColoredText> punchline)
        {
            setup = new List<ColoredText>();
            punchline = new List<ColoredText>();
            if (headline == null || headline.Count == 0)
                return false;

            for (int i = 0; i < headline.Count; i++)
            {
                string text = headline[i]?.Text ?? string.Empty;
                int ellipsisAt = text.IndexOf(Ellipsis, StringComparison.Ordinal);
                if (ellipsisAt < 0)
                    continue;

                for (int j = 0; j < i; j++)
                    setup.Add(headline[j]);

                string before = text.Substring(0, ellipsisAt);
                if (!string.IsNullOrEmpty(before))
                    setup.Add(new ColoredText(before, headline[i].Color, headline[i].SourceTemplate, headline[i].ColorReadyForCanvas));
                setup.Add(new ColoredText(Ellipsis, headline[i].Color, headline[i].SourceTemplate, headline[i].ColorReadyForCanvas));

                string after = text.Substring(ellipsisAt + Ellipsis.Length);
                if (!string.IsNullOrEmpty(after))
                    punchline.Add(new ColoredText(after, headline[i].Color, headline[i].SourceTemplate, headline[i].ColorReadyForCanvas));
                for (int j = i + 1; j < headline.Count; j++)
                    punchline.Add(headline[j]);

                return punchline.Count > 0;
            }

            return false;
        }
    }
}
