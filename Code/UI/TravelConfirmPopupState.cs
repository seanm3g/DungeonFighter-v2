namespace RPGGame.UI
{
    /// <summary>What the "Travel to X?" popup on the world map shows.</summary>
    public sealed class TravelConfirmView
    {
        public string DestinationName { get; init; } = "";
        public string DestinationLevels { get; init; } = "";
        /// <summary>Estimated in-world travel time, already formatted (e.g. "2 hr 10 min").</summary>
        public string TravelTime { get; init; } = "";
        /// <summary>Chance that at least one stretch of the road goes badly (a damaging critical miss), 0-100.</summary>
        public int DangerPercent { get; init; }
        public int RoadEvents { get; init; }
    }

    /// <summary>
    /// The pending travel confirmation on the world map. Set by <see cref="RegionTravelHandler"/> when a region is picked;
    /// the travel renderer draws it over the map while set.
    /// </summary>
    public static class TravelConfirmPopupState
    {
        public const string ConfirmKey = "travel:confirm";
        public const string CancelKey = "travel:cancel";

        public static TravelConfirmView? Current { get; set; }
    }
}
