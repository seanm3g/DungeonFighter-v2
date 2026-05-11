# Audio system — cues, music, and the Audio settings tab

The audio system plays music and sound effects in response to game events. It is fully cross-platform (Windows, macOS, Linux) using the [SoundFlow](https://github.com/LSXPrime/SoundFlow) NuGet package (MIT, MiniAudio backend bundled per-RID — no extra native installs).

## Architecture

```
Code/Audio/
├── AudioCue.cs              # Enum of every named trigger
├── AudioBusKind.cs          # Music / SFX bus
├── IAudioEngine.cs          # Backend abstraction
├── NullAudioEngine.cs       # No-op backend (tests / headless)
├── SoundFlowAudioEngine.cs  # Production backend (MiniAudioEngine + 2 mixers)
├── AudioConfig.cs           # POCO; load/save GameData/Audio/AudioConfig.json
├── AudioCueDispatcher.cs    # Subscribes to CombatEventBus; resolves cue->file
├── MusicController.cs       # Subscribes to GameStateManager.StateChanged
├── AudioCues.cs             # Static facade: AudioCues.Trigger(cue)
└── AudioBootstrap.cs        # One-time wire-up from GameInitializationHandler
```

The rest of the game depends only on `AudioCues.Trigger(...)` and the `IAudioEngine` abstraction. The hard dependency on `SoundFlow` lives only inside `Code/Audio/`.

## Trigger map

### Combat

| Source                                   | Cue                          |
|------------------------------------------|------------------------------|
| `ActionEventPublisher.PublishActionMiss` (`IsCriticalMiss=true`) | `Combat_CriticalMiss`        |
| `ActionEventPublisher.PublishActionMiss` (`IsCriticalMiss=false`) | `Combat_Miss`                |
| `ActionEventPublisher.PublishActionHit` (normal)                 | `Combat_Hit`                 |
| `ActionEventPublisher.PublishActionHit` (`IsCombo=true`)         | `Combat_ComboComplete`       |
| `ActionEventPublisher.PublishActionHit` (`IsCritical=true`)      | `Combat_CriticalHit`         |
| `EnemyDied`                              | `Combat_EnemyDied`           |
| `HeroLowHealth` (crosses ≤20% HP alive)  | `Combat_HeroLowHealth`       |
| `EnemyLowHealth` (crosses ≤20% HP alive) | `Combat_EnemyLowHealth`      |
| `StatusEffectApplied`                    | `Combat_StatusApplied`       |

The five direct combat outcome cues are mutually exclusive: critical miss, miss, normal hit, combo, critical hit. They fire directly from `ActionEventPublisher` after the matching hit/miss event is published so combat-thread sounds do not depend on `CombatEventBus` subscription lifetime. Critical hit takes precedence over combo if both flags are present on the same hit.

Low-health events are published when action damage or poison/burn/bleed ticks move the hero or enemy from above 20% HP to at-or-below 20% HP. They do not fire for already-low actors or for damage that kills the actor outright, so defeat cues remain distinct.

### Music (subscribed to `GameStateManager.StateChanged`)

Mapping is data-driven via `stateMusicMap` in `AudioConfig.json`. Default mapping:

| `GameState`            | Cue                       |
|------------------------|---------------------------|
| `MainMenu`             | `Music_MainMenu`          |
| `WeaponSelection`      | `Music_WeaponSelection`   |
| `CharacterCreation`    | `Music_CharacterCreation` |
| `Inventory`            | `Music_Inventory`         |
| `DungeonSelection`     | `Music_DungeonSelection`  |
| `Dungeon`              | `Music_Dungeon`           |
| `Combat`               | `Music_Combat`            |
| `DungeonCompletion`    | `Music_DungeonCompletion` |
| `Death`                | `Music_Death`             |
| `TrainingGroundOffer`  | `Music_TrainingGround`    |

Music crossfades when the track changes; default fade is **1000 ms**, adjustable in **Settings → Audio** (`musicCrossfadeMs` in `AudioConfig.json`, 0–10000 ms). The SoundFlow backend overlaps the outgoing and incoming `SoundPlayer`s and ramps their per-player volumes over that duration.

### Menu and direct-call cues

Fired by one-line `AudioCues.Trigger(...)` calls at these sites:

| Cue                      | Source                                                                  |
|--------------------------|-------------------------------------------------------------------------|
| `Menu_Select`            | `MenuCommand.Execute` success path                                      |
| `Menu_Back`              | `MainWindowInputHandler` Escape / Backspace                             |
| `Menu_Invalid`           | `MainMenuHandler` default switch case                                   |
| `Progression_LevelUp`    | `LevelUpManager` after granting level/points                            |
| `Loot_ItemPickup`        | `InventoryManager.AddItem`                                              |
| `Loot_Equip`             | `EquipmentManager.EquipItem` success                                    |
| `Loot_Unequip`           | `EquipmentManager.UnequipItem` success                                  |
| `Loot_Save`              | `CharacterSaveManager` after a successful save                          |
| `Dungeon_Enter`          | `DungeonDisplayManager.StartDungeon`                                    |
| `Dungeon_RoomClear`      | `DungeonRunnerManager` room cleared path                                |

## `AudioConfig.json` schema

Lives at [GameData/Audio/AudioConfig.json](../../GameData/Audio/AudioConfig.json). Default file ships with empty `file` entries; the game runs silently until you populate them from the **Settings → Audio** tab.

```json
{
  "masterVolume": 1.0,
  "musicVolume": 0.7,
  "sfxVolume": 0.9,
  "musicEnabled": true,
  "sfxEnabled": true,
  "musicCrossfadeMs": 1000,
  "cueMap": {
    "Menu_Select": { "file": "SFX/menu_select.wav", "volume": 0.8 },
    "Combat_Hit":  { "file": "SFX/combat_hit.wav",  "volume": 1.0, "rateLimitMs": 80 }
  },
  "stateMusicMap": {
    "MainMenu": "Music_MainMenu",
    "Combat":   "Music_Combat"
  }
}
```

Paths in `file` are relative to `GameData/Audio/`. Missing files do **not** crash — the dispatcher logs once and skips playback.

## Supported formats

The bundled MiniAudio decoder reads **WAV / MP3 / FLAC** out of the box on every platform. **OGG** requires installing `SoundFlow.Extensions.FFmpeg`; that package is **not** added by default to keep the install lightweight.

Recommended:
- **Music**: MP3 or FLAC (file streamed from disk).
- **SFX**: WAV (loaded once into memory; lowest latency).

## Engine abstraction

`IAudioEngine` exposes:

```csharp
void Play(string file, AudioBusKind bus, float volume);
void PlayMusic(string file, int crossfadeMs);
void StopMusic(int crossfadeMs);
void SetBusVolume(AudioBusKind bus, float volume);
void SetBusMute(AudioBusKind bus, bool muted);
void Shutdown();
```

- `SoundFlowAudioEngine`: one `MiniAudioEngine` at 48 kHz, two child mixers (`Music` and `SFX`) added to `Mixer.Master`. SFX players are cached by file path so each cue plays without re-decoding.
- `NullAudioEngine`: returns immediately on every call. Used by tests, the MCP server, automated tuning, and any headless launch.

The engine is gated by both the per-bus mute and the legacy `GameSettings.EnableSoundEffects` flag — if either is off, nothing plays.

## Settings → Audio tab

New `AudioSettingsPanel` (Avalonia `UserControl`) registered in `SettingsPanelCatalog` and `PanelHandlerRegistry`. Layout:

1. **Top**: master / music / SFX volume sliders, master mute, music mute, SFX mute, music crossfade ms. Live-applied to the engine on slider change (no Save required for instant feedback).
2. **Middle**: dropdowns for every `GameState` in `stateMusicMap` → choose any music cue (or "none").
3. **Bottom**: scrollable grid of cue rows, one row per `AudioCue`:
   - cue label
   - current file path (read-only text)
   - **Browse…** (Avalonia `OpenFileDialog`, initial folder = `GameData/Audio/`)
   - volume slider
   - **Test** button (calls `AudioCues.Trigger(cue, settingsPreview: true)` and uses an audible preview path even if master/SFX/music are muted)
   - **Clear** button

The panel persists changes to `GameData/Audio/AudioConfig.json`. Saved state is also reapplied to the running engine when the user clicks **Save**.

## Adding a new cue

1. Add a value to the `AudioCue` enum in `Code/Audio/AudioCue.cs`.
2. Add a default entry (with `file: ""`) to `cueMap` in `GameData/Audio/AudioConfig.json` if you want it to appear in the settings UI immediately. (`AudioConfig.EnsureDefaultEntriesForAllCues` does this automatically on load if the entry is missing.)
3. Fire the cue from one or more call sites — either `AudioCues.Trigger(AudioCue.MyNew)` for direct triggers, or subscribe `AudioCueDispatcher` to the appropriate `CombatEventType` for event-driven cues.
4. (Optional) Map a `GameState` to a music cue in `stateMusicMap` for state-driven music.

## Tests

`Code/Tests/Unit/Audio/AudioCueDispatcherTests.cs`:

- `AudioCueDispatcher_RoutesCombatHit_ToHitCue` — combat events route to the correct cue.
- Low-health routing and crossing tests — hero/enemy low-health events route to distinct cues and fire only when crossing the 20% threshold.
- `MusicController_ChangesTrack_OnStateChange` — state change triggers `PlayMusic`.
- `AudioCueDispatcher_RespectsRateLimit` — rapid-fire cues are throttled.
- `AudioConfig_LoadsAndValidates_MissingFile_NoCrash` — empty / missing `file` does not throw.
- `AudioCueDispatcher_RespectsGlobalMute` — `EnableSoundEffects=false` short-circuits all playback.

All tests use `NullAudioEngine`; no real audio runs in CI.
