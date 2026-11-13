namespace RPGGame.Tuning
{
    /// <summary>
    /// Heuristic menu/combat input policy for headless full-game playthroughs.
    /// </summary>
    public static class ClassPlaythroughPolicy
    {
        /// <summary>
        /// DUNGEONSIM: rotate the dungeon slot (1-3) per entry, starting at <see cref="DungeonMixOffset"/>, so runs visit all
        /// three of a region's dungeons (before 2026-11-12 sims always took slot 2).
        /// </summary>
        public static bool DungeonMix { get; set; }
        public static int DungeonMixOffset { get; set; }
        /// <summary>DUNGEONSIM --dungeon-slot N: always this menu slot (1-3). Null = rotate (DUNGEONSIM) or slot 2 (other tools).</summary>
        public static int? PinnedDungeonSlot { get; set; }
        private static int _dungeonMixPick;

        public static string PickAction(string currentState, int weaponMenuSlot)
        {
            if (currentState == "DungeonSelection" && DungeonMix)
                return (((_dungeonMixPick++ + DungeonMixOffset) % 3 + 3) % 3 + 1).ToString();
            if (currentState == "DungeonSelection" && PinnedDungeonSlot is int pinned and >= 1 and <= 3)
                return pinned.ToString();
            return currentState switch
            {
                // Digits on the main menu load fighter cards; the new-fighter key starts a fresh hero.
                "MainMenu" => MainMenuHandler.NewFighterKey,
                "TrainingGroundOffer" => "2",
                "PreWeaponPathIntro" => "1",
                "WeaponSelection" => weaponMenuSlot.ToString(),
                "CharacterCreation" => "1",
                // A new hero lands in the inventory after weapon selection; Back goes on to the game loop.
                "Inventory" => "0",
                "GameLoop" => "1",
                "DungeonCompletion" => "1",
                "DungeonSelection" => "2",
                "Death" => "1",
                _ => "1"
            };
        }

        public static bool IsDungeonEntry(string currentState, string action) =>
            currentState == "DungeonSelection" && action is "1" or "2" or "3";
    }
}
