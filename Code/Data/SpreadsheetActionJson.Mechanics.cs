using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Additional mechanics columns (rolls, chain, etc.).
    /// </summary>
    public partial class SpreadsheetActionJson
    {
        [JsonPropertyName("replaceNextRoll")]
        public string ReplaceNextRoll { get; set; } = "";

        [JsonPropertyName("highestLowestRoll")]
        public string HighestLowestRoll { get; set; } = "";

        [JsonPropertyName("diceRolls")]
        public string DiceRolls { get; set; } = "";

        [JsonPropertyName("explodingDiceThreshold")]
        public string ExplodingDiceThreshold { get; set; } = "";

        [JsonPropertyName("curse")]
        public string Curse { get; set; } = "";

        [JsonPropertyName("skip")]
        public string Skip { get; set; } = "";

        [JsonPropertyName("jump")]
        public string Jump { get; set; } = "";

        [JsonPropertyName("disrupt")]
        public string Disrupt { get; set; } = "";

        [JsonPropertyName("grace")]
        public string Grace { get; set; } = "";

        [JsonPropertyName("loopChain")]
        public string LoopChain { get; set; } = "";

        [JsonPropertyName("shuffle")]
        public string Shuffle { get; set; } = "";

        [JsonPropertyName("replaceAction")]
        public string ReplaceAction { get; set; } = "";

        [JsonPropertyName("chainLength")]
        public string ChainLength { get; set; } = "";

        [JsonPropertyName("chainPosition")]
        public string ChainPosition { get; set; } = "";

        [JsonPropertyName("modifyBasedOnChainPosition")]
        public string ModifyBasedOnChainPosition { get; set; } = "";

        [JsonPropertyName("distanceFromXSlot")]
        public string DistanceFromXSlot { get; set; } = "";
    }
}
