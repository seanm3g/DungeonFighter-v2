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

        /// <summary>Swing/whoosh on the action setup line (<c>{Actor} attacks {Target}...</c>), before the hit/miss punchline.</summary>
        Combat_ActionStart,
        Combat_CriticalMiss,
        Combat_Miss,
        Combat_Hit,
        /// <summary>Hero combo hit N of the current chain, rising a major scale (hits past 7 reuse step 7). Unbound steps fall back to <see cref="Combat_ComboComplete"/>.</summary>
        Combat_ComboStep1,
        Combat_ComboStep2,
        Combat_ComboStep3,
        Combat_ComboStep4,
        Combat_ComboStep5,
        Combat_ComboStep6,
        Combat_ComboStep7,
        /// <summary>Generic combo finisher: enemy combo hits, and the fallback for unbound step / finisher cues.</summary>
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

        /// <summary>
        /// Hero hit on the strip's last slot: a short jingle that starts on the note step N would play
        /// (a 1-action strip starts on note 1, a 3-action strip on note 3; capped at 7). Unbound jingles fall back to
        /// <see cref="Combat_ComboComplete"/>. Appended last so existing cue ordinals stay put.
        /// </summary>
        Combat_ComboFinish1,
        Combat_ComboFinish2,
        Combat_ComboFinish3,
        Combat_ComboFinish4,
        Combat_ComboFinish5,
        Combat_ComboFinish6,
        Combat_ComboFinish7,
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

        /// <summary>
        /// Cue to try when this one has no file bound, so finer-grained combat cues never go silent
        /// (combo step → combo complete → hit; critical hit → hit). <see cref="AudioCue.None"/> ends the chain.
        /// </summary>
        public static AudioCue GetFallback(this AudioCue cue) => cue switch
        {
            AudioCue.Combat_ComboStep1 or AudioCue.Combat_ComboStep2 or AudioCue.Combat_ComboStep3
                or AudioCue.Combat_ComboStep4 or AudioCue.Combat_ComboStep5
                or AudioCue.Combat_ComboStep6 or AudioCue.Combat_ComboStep7 => AudioCue.Combat_ComboComplete,
            AudioCue.Combat_ComboFinish1 or AudioCue.Combat_ComboFinish2 or AudioCue.Combat_ComboFinish3
                or AudioCue.Combat_ComboFinish4 or AudioCue.Combat_ComboFinish5
                or AudioCue.Combat_ComboFinish6 or AudioCue.Combat_ComboFinish7 => AudioCue.Combat_ComboComplete,
            AudioCue.Combat_ComboComplete => AudioCue.Combat_Hit,
            AudioCue.Combat_CriticalHit => AudioCue.Combat_Hit,
            _ => AudioCue.None
        };

        /// <summary>Step cue for a 0-based combo strip slot (slots past the seventh share step 7).</summary>
        public static AudioCue ComboStepCueForSlot(int slotIndex) => slotIndex switch
        {
            <= 0 => AudioCue.Combat_ComboStep1,
            1 => AudioCue.Combat_ComboStep2,
            2 => AudioCue.Combat_ComboStep3,
            3 => AudioCue.Combat_ComboStep4,
            4 => AudioCue.Combat_ComboStep5,
            5 => AudioCue.Combat_ComboStep6,
            _ => AudioCue.Combat_ComboStep7
        };

        /// <summary>Finisher jingle starting on the note of 0-based chain position <paramref name="noteIndex"/> (past the seventh share jingle 7).</summary>
        public static AudioCue ComboFinishCueForNote(int noteIndex) => noteIndex switch
        {
            <= 0 => AudioCue.Combat_ComboFinish1,
            1 => AudioCue.Combat_ComboFinish2,
            2 => AudioCue.Combat_ComboFinish3,
            3 => AudioCue.Combat_ComboFinish4,
            4 => AudioCue.Combat_ComboFinish5,
            5 => AudioCue.Combat_ComboFinish6,
            _ => AudioCue.Combat_ComboFinish7
        };
    }
}
