using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem.Applications
{
    /// <summary>
    /// Formats dungeon narration that mixes generic keyword coloring with structured game objects.
    /// </summary>
    public static class DungeonNarrativeColoredText
    {
        public static List<ColoredText> FormatSearchResult(global::RPGGame.SearchResult searchResult)
        {
            if (searchResult == null)
                throw new ArgumentNullException(nameof(searchResult));

            return FormatSearchMessage(
                searchResult.Message,
                searchResult.FoundLoot ? searchResult.LootItem : null);
        }

        public static List<ColoredText> FormatSearchMessage(string message, global::RPGGame.Item? lootItem)
        {
            if (string.IsNullOrEmpty(message))
                return new List<ColoredText>();

            string? itemName = lootItem?.Name;
            if (lootItem == null || string.IsNullOrWhiteSpace(itemName))
                return KeywordColorSystem.Colorize(message);

            int itemStart = message.LastIndexOf(itemName, StringComparison.Ordinal);
            if (itemStart < 0)
                return KeywordColorSystem.Colorize(message);

            var builder = new ColoredTextBuilder();
            AddKeywordColoredText(builder, message.Substring(0, itemStart));
            builder.AddRange(ItemDisplayColoredText.FormatFullItemName(lootItem));
            AddKeywordColoredText(builder, message.Substring(itemStart + itemName.Length));
            return builder.Build();
        }

        public static List<ColoredText> FormatHealingMessage(int healthRestored)
        {
            return new ColoredTextBuilder()
                .Add("You have been fully ", Colors.White)
                .Healing("healed")
                .Add("! (", Colors.White)
                .Healing($"+{healthRestored} health")
                .Add(")", Colors.White)
                .Build();
        }

        private static void AddKeywordColoredText(ColoredTextBuilder builder, string text)
        {
            if (!string.IsNullOrEmpty(text))
                builder.AddRange(KeywordColorSystem.Colorize(text));
        }
    }
}
