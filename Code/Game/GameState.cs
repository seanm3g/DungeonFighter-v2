namespace RPGGame
{
    public enum GameState
    {
        MainMenu,
        WeaponSelection,
        /// <summary>Offer pre-weapon Training Ground or skip before starter weapon is chosen.</summary>
        TrainingGroundOffer,
        /// <summary>Short interstitial after Training Ground completion or skip before starter weapon selection.</summary>
        PreWeaponPathIntro,
        CharacterCreation,
        GameLoop,
        RegionTravel,
        Inventory,
        CharacterInfo,
        Settings,
        Testing,
        DeveloperMenu,
        BattleStatistics,
        VariableEditor,
        TuningParameters,
        ActionEditor,
        CreateAction,
        EditAction,
        ViewAction,
        DeleteActionConfirmation,
        DungeonSelection,
        CharacterSelection,
        LoadCharacterSelection,
        Dungeon,
        Combat,
        /// <summary>Developer sandbox: stepped combat with forced actions and fixed d20 (Settings → Testing).</summary>
        ActionInteractionLab,
        DungeonCompletion,
        Death
    }
}

