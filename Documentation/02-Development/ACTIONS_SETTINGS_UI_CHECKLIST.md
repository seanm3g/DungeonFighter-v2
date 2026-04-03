# Actions Settings Menu – UI and Verification Checklist

This checklist supports systematic verification of the **Actions** settings tab (Component 1: UI, Component 2: apply to game, Component 3: Testing menu).

---

## Component 1: Actions menu UI

Use these steps to verify each element works as intended. Open **Settings** → **Actions** tab before starting.

### Filters

| Step | Action | Expected result |
|------|--------|-----------------|
| 1.1 | Note the options in **Rarity** dropdown | Contains "(All)" plus distinct rarities from loaded actions. |
| 1.2 | Select a specific Rarity | Actions list shows only actions with that rarity. |
| 1.3 | Select "(All)" | Actions list shows all actions. |
| 1.4 | Repeat for **Category** dropdown | Same behavior; "General" and "(All)" available. |
| 1.5 | Repeat for **Cadence** dropdown | Filters by cadence. |
| 1.6 | Repeat for **Tag** dropdown | Filters by tag. |

### List and selection

| Step | Action | Expected result |
|------|--------|-----------------|
| 1.7 | Confirm **Actions list** (left) is populated | Action names appear, sorted. |
| 1.8 | Click an action name | Right panel shows the **action form** for that action with all fields populated. |
| 1.9 | Click another action | Form updates to the new action. |

### Create

| Step | Action | Expected result |
|------|--------|-----------------|
| 1.10 | Click **Create** | List selection clears; form shows blank/new action (e.g. Name empty, Type Attack, TargetType SingleTarget). |
| 1.11 | Fill required fields (e.g. Name, Description), click **Save** in form | New action is created; status message confirms; list refreshes and includes the new name. |
| 1.12 | Create again with duplicate name | Validation error (e.g. "already exists"). |

### Delete

| Step | Action | Expected result |
|------|--------|-----------------|
| 1.13 | Select an action, click **Delete** | Action is removed; list refreshes; form clears or shows no selection. |
| 1.14 | Click Delete with no selection (or while in Create mode) | Status message: "No action selected for deletion". |

### Action form – Save / Cancel

| Step | Action | Expected result |
|------|--------|-----------------|
| 1.15 | Select an action, change a field (e.g. Description), click **Save** in form | Changes persist; status confirms; list still shows selection. |
| 1.16 | Select an action, change a field, click **Cancel** | Form clears; selection clears; changes are discarded. |

### Action form – Default/Starting

| Step | Action | Expected result |
|------|--------|-----------------|
| 1.17 | Select an action; check **Default/Starting** (if present); click **Save** (main Settings Save, not form Save) | After save, that action is treated as default/starting (used when building player action pool). |
| 1.18 | Uncheck **Default/Starting**, Save from Settings | Action no longer default/starting. |

### Action form – field coverage

Verify the form includes (and persist when saved) at least:

- **Basic**: Name, Description, Rarity, Category, Tags, Target Type.
- **Combat**: MultiHitCount, DamageMultiplier, Speed (Length), Cadence, Duration (ComboBonusDuration).
- **Modifiers**: SpeedMod, DamageMod, MultiHitMod, Amp Mod, Chain Position, Chain Position Number, Chain Position MOD, Skip Next Turn, Repeat, Jump, Size (Chain Length), Reset.
- **Roll**: Crit Miss, Hit, Combo, Crit thresholds; Accuracy (RollBonus).
- **Triggers**: On Hit, On Miss, On Combo, On Crit.
- **Status effects**: CausesStun, CausesPoison, CausesBurn, CausesBleed, CausesWeaken, CausesExpose, etc.
- **Weapon types**: Sword, Dagger, Mace, Wand (Assign to Weapon Types).

---

## Component 2: Actions updates apply to the game

| Step | Action | Expected result |
|------|--------|-----------------|
| 2.1 | With a **game in progress** (player loaded), open Settings → Actions; change an action (e.g. description or Default/Starting); click main **Save** in Settings. | Settings close or stay open; no error. |
| 2.2 | Return to game (or start combat). | Player's action pool reflects the saved Actions.json (e.g. default/starting actions, weapon/class actions from same data). |
| 2.3 | Confirm **Actions.json** on disk (e.g. GameData/Actions.json) | File timestamp and content updated after Save; edited action and Default/Starting flags correct. |

---

## Component 3: Testing menu – run mechanics with action settings

After changing actions in the Actions tab and saving, use the **Testing** tab to confirm mechanics still work.

| Step | Action | Expected result |
|------|--------|-----------------|
| 3.1 | Open Settings → **Testing** tab. | Component tests and Integration tests sections visible. |
| 3.2 | Click **Actions Settings Integration** | Tests run (SpreadsheetImport, ActionMechanics, ActionExecutionFlow, DataSystem); output shows in Test Output; no failures. |
| 3.3 | Click **Action Mechanics (All)** | All action mechanics tests pass. |
| 3.4 | Click **Combat** | Combat integration tests pass. |
| 3.5 | (Optional) Click **Gameplay Flow** | Gameplay flow tests pass. |
| 3.6 | (Optional) Click **Run All** | Full integration suite runs; no failures. |

**Recommended flow:** Change something in Actions tab → Save → open Testing tab → run **Actions Settings Integration** (and optionally **Combat** or **Run All**) → all pass. This confirms that the adjustable action settings work correctly in a game setting.

---

## Quick reference – key files

- **UI**: `Code/UI/Avalonia/Settings/ActionsSettingsPanel.axaml`, `ActionsTabManager.cs`
- **Form**: `Code/UI/Avalonia/Builders/ActionFormBuilder.cs`
- **Persistence**: `ActionEditor.SaveActionsToFile()` → `Actions.json`
- **Apply**: `SettingsApplyService.ApplyAfterSave` → `ActionsTabManager.RefreshCurrentPlayerActionPool`
- **Testing**: `ComprehensiveTestRunner.RunActionsSettingsIntegrationTests`, `RunAllTests` (Integration)
