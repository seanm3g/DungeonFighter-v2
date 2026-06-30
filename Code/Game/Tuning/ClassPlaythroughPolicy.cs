namespace RPGGame.Tuning
{
    /// <summary>
    /// Heuristic menu/combat input policy for headless full-game playthroughs.
    /// </summary>
    public static class ClassPlaythroughPolicy
    {
        public static string PickAction(string currentState, int weaponMenuSlot)
        {
            return currentState switch
            {
                "MainMenu" => "1",
                "TrainingGroundOffer" => "2",
                "PreWeaponPathIntro" => "1",
                "WeaponSelection" => weaponMenuSlot.ToString(),
                "CharacterCreation" => "1",
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
