using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>One CHARMS sheet / <c>Charms.json</c> row.</summary>
    public sealed class CharmData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary><c>class</c>, <c>material</c>, or <c>animal</c>.</summary>
        [JsonPropertyName("layer")]
        public string Layer { get; set; } = "";

        [JsonPropertyName("amplifyMint")]
        public double AmplifyMint { get; set; } = 1.0;

        [JsonPropertyName("amplifyConvert")]
        public double AmplifyConvert { get; set; } = 1.0;

        [JsonPropertyName("amplifyClassDefense")]
        public double AmplifyClassDefense { get; set; } = 1.0;

        [JsonPropertyName("amplifyClassTagDamage")]
        public double AmplifyClassTagDamage { get; set; } = 1.0;

        [JsonPropertyName("amplifyAnimalLadder")]
        public double AmplifyAnimalLadder { get; set; } = 1.0;

        [JsonPropertyName("unlockAnimalAction")]
        public bool UnlockAnimalAction { get; set; }

        [JsonPropertyName("rarity")]
        public string Rarity { get; set; } = "Rare";

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        public CharmItem ToItem()
        {
            var item = new CharmItem(Name, 1)
            {
                Layer = (Layer ?? "").Trim().ToLowerInvariant(),
                AmplifyMint = AmplifyMint <= 0 ? 1.0 : AmplifyMint,
                AmplifyConvert = AmplifyConvert <= 0 ? 1.0 : AmplifyConvert,
                AmplifyClassDefense = AmplifyClassDefense <= 0 ? 1.0 : AmplifyClassDefense,
                AmplifyClassTagDamage = AmplifyClassTagDamage <= 0 ? 1.0 : AmplifyClassTagDamage,
                AmplifyAnimalLadder = AmplifyAnimalLadder <= 0 ? 1.0 : AmplifyAnimalLadder,
                UnlockAnimalAction = UnlockAnimalAction,
                Rarity = string.IsNullOrWhiteSpace(Rarity) ? "Rare" : Rarity.Trim(),
                Tags = Tags == null || Tags.Count == 0
                    ? new List<string> { "charm" }
                    : new List<string>(Tags)
            };
            return item;
        }
    }
}
