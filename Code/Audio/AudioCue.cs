namespace RPGGame.Audio
{
    /// <summary>
    /// Every named audio trigger in the game.
    /// </summary>
    /// <remarks>
    /// Each cue is mapped to an audio file by <see cref="AudioConfig"/> (loaded from
    /// <c>GameData/Audio/AudioConfig.json</c>). Cues are routed through
    /// <see cref="AudioCueDispatcher"/> which subscribes to <see cref="Combat.Events.CombatEventBus"/>
    /// for combat-driven cues; direct call sites (menus, progression, save) fire them
    /// through the static <see cref="AudioCues"/> facade.
    /// Music cues are routed by <see cref="MusicController"/> on
    /// <see cref="GameStateManager.StateChanged"/>.
    /// </remarks>
    public enum AudioCue
    {
        None = 0,

        Music_MainMenu,
        Music_WeaponSelection,
        Music_CharacterCreation,
        Music_Inventory,
        Music_DungeonSelection,
        Music_Dungeon,
        Music_Combat,
        Music_DungeonCompletion,
        Music_Death,
        Music_TrainingGround,

        Menu_Select,
        Menu_Back,
        Menu_Invalid,
        Menu_Confirm,
        Menu_Hover,

        Combat_CriticalMiss,
        Combat_Miss,
        Combat_Hit,
        Combat_ComboComplete,
        Combat_CriticalHit,
        Combat_EnemyDied,
        Combat_HeroHurt,
        Combat_HeroLowHealth,
        Combat_EnemyLowHealth,
        Combat_StatusApplied,

        Progression_LevelUp,
        Progression_XpGain,
        Loot_ItemPickup,
        Loot_Equip,
        Loot_Unequip,
        Loot_Save,

        Dungeon_Enter,
        Dungeon_Exit,
        Dungeon_RoomClear,
    }

    /// <summary>
    /// Buses the audio engine routes playback into so master, music, and SFX volume/mute can be controlled independently.
    /// </summary>
    public enum AudioBusKind
    {
        Sfx,
        Music
    }

    /// <summary>
    /// Helpers for classifying cues as music vs SFX without bespoke switches at every call site.
    /// </summary>
    public static class AudioCueExtensions
    {
        /// <summary>True if the cue routes through the music bus (streamed, crossfaded), false if SFX (cached, fire-and-forget).</summary>
        public static AudioBusKind GetBus(this AudioCue cue)
        {
            string name = cue.ToString();
            return name.StartsWith("Music_") ? AudioBusKind.Music : AudioBusKind.Sfx;
        }
    }
}
