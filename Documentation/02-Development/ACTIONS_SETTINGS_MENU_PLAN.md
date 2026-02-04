# Actions Settings Menu Reimagined — Plan

This plan reimagines the Actions settings menu to align with the [Testing features doc](https://docs.google.com/document/d/e/2PACX-1vTGbqd7i56nTpfTa5g6moBajpkb9iq_ReCVWh1zoP1OA62YC9rK7IAB-vlccLO9iFIRJqqB0wE67Nfn/pub). It references [ActionsSettingsPanel.axaml](Code/UI/Avalonia/Settings/ActionsSettingsPanel.axaml), [ActionFormBuilder.cs](Code/UI/Avalonia/Builders/ActionFormBuilder.cs), [ActionsTabManager.cs](Code/UI/Avalonia/Managers/ActionsTabManager.cs), and related data/validation files.

---

## Decisions (confirmed)

1. **Default vs Starting**: Default and Starting are the same. One "Default/Starting" checkbox (IsDefaultAction); remove "Is Starting Action" from the form; sync IsStartingAction = IsDefaultAction on load/save.
2. **Cadence list**: Strict list only: Action, Ability, Chain, Fight, Dungeon. Migrate old values: Abilities → Ability, ACTIONS → Action, COMBO → Chain; update existing data to canonical values when loading/saving.
3. **Scope**: Two passes. **Phase 1**: Core (multihit/damage/length at top), Type (Rarity/Category/Cadence with strict cadence + migration), Action Assignment (single Default/Starting, weapon checkboxes readable), Modifiers (SpeedMod, DamageMod, MultiHitMod, AmpMod), full Status effects (all sheet status columns + note), Tags. **Phase 2**: Triggers, Chain/Combo, Roll mechanics, Heal, Threshold/conditional.

Implementation: Phase 1 first, then Phase 2; tests and manual checks after each.

---

## Phase 1 — Done

- **Form order**: Basic → Core (Damage/Speed) → Type → Action Assignment → Modifiers → Status → Advanced → Tags → Buttons.
- **Basic**: Removed "Is Starting Action"; only Name and Description.
- **Core**: MultiHitCount, DamageMultiplier, Length (section "Core (Damage / Speed)").
- **Default/Starting**: Single checkbox "Default/Starting" (IsDefaultAction); IsStartingAction synced on save and when assigning weapon types; removed "Is Starting Action" from form.
- **Weapon checkboxes**: Foreground = White, Background = dark panel color for readability.
- **Cadence**: Strict list (None, Action, Ability, Chain, Fight, Dungeon); `NormalizeCadence()` maps Abilities→Ability, ACTIONS→Action, COMBO→Chain, etc.; filter uses normalized cadence.
- **Modifiers**: New section with SpeedMod, DamageMod, MultiHitMod, AmpMod (ActionData + converter round-trip).
- **Status**: All ActionData status toggles (Stun, Poison, Burn, Bleed, Weaken, Expose, Slow, Vulnerability, Harden, Silence, Pierce, StatDrain, Fortify, Focus, Cleanse, Reflect) + note: "Ability adds STATUS component; items define effect mechanics."

---

## Current State (summary)

- **Panel**: Left column = Rarity + Cadence filters + ListBox; right = dynamic form + Create/Delete.
- **Form order**: Basic (Name, Description, Is Starting Action) → Type (Rarity, Category, Cadence) → Action Assignment (Is Default Action, Weapon Types) → Numeric → Status (5 toggles) → Advanced → Tags → Save/Cancel.
- **Data**: ActionData (in-game), SpreadsheetActionData / SpreadsheetActionJson (sheet round-trip). Spreadsheet has 20+ status columns, modifiers, Hero/Enemy roll and stat bonuses, triggers, chain/roll mechanics, heal, Tags.

---

## Implementation Plan

### 1. UI/UX fixes (Phase 1)

- **Weapon type checkboxes**: In ActionFormBuilder, set Foreground and Background so checkboxes are readable on dark panel (not black on dark blue).
- **Default/Starting action**: Single "Default/Starting" checkbox backed by IsDefaultAction. Remove "Is Starting Action" from form. On load: set form from IsDefaultAction; when loading action into form, set IsStartingAction = IsDefaultAction if needed for display. On save: write IsDefaultAction and set IsStartingAction = IsDefaultAction.
- **Form order**: Add "Core (Damage / Speed)" section immediately after Basic: MultiHitCount, DamageMultiplier, Length. Remove duplicate Numeric section.
- **Cadence**: Dropdown and filter use strict list: Action, Ability, Chain, Fight, Dungeon, (None). On load/display, migrate: Abilities → Ability, ACTIONS → Action, COMBO → Chain (and ATTACK/ATTACKS → Action if desired). On save, persist only canonical value.
- **Rarity/Category**: Populate from data + fixed set; fix filter and form binding so they save/load correctly.

### 2. Form sections (Phase 1 order)

1. Basic — Name, Description (no separate "Is Starting Action")
2. Core (Damage / Speed) — MultiHitCount, DamageMultiplier, Length
3. Type (Spreadsheet) — Rarity, Category, Cadence (strict + migration)
4. Action Assignment — Default/Starting (single checkbox), Weapon Types (readable checkboxes)
5. Modifiers — SpeedMod, DamageMod, MultiHitMod, AmpMod (Phase 1)
6. Status effects — All sheet status columns + note: "Ability adds STATUS component; items define effect mechanics."
7. Tags
8. Save/Cancel

Phase 2 adds: Roll/Stats (Hero & Enemy), Heal, Triggers/Conditional, Chain/Combo, Roll mechanics, Advanced.

### 3. Data and round-trip

- Ensure ActionData and SpreadsheetActionJson (and converter) support all fields edited in the form; cadence migration applied when reading/writing JSON/sheet format.
- Validation: allow only canonical cadence values (or migrate on validate); optional rules for new fields.

### 4. Status effect semantics

- Add note in Status section: "This action adds the selected status component(s). Effect strength and duration are determined by the item granting the action."

### 5. Testing

- Unit tests for create/update with new fields (cadence, rarity, category, status, modifier); cadence migration in load/save.
- Round-trip: load Actions.json (spreadsheet format), edit (e.g. cadence, one status field), save, reload and assert.
- Manual: filters (Rarity/Cadence), weapon checkboxes readable, Core at top, single Default/Starting, sections save correctly.

---

## Game integration

- **Modifiers (SpeedMod, DamageMod, MultiHitMod, AmpMod)**: Apply **only to the next** action or ability, based on **cadence** and **duration** (same consumption as ACTION/ABILITY keyword bonuses). **ACTION** cadence: consumed on the next roll (ability, hit, or miss). **ABILITY** cadence: consumed only when the next ability **succeeds**; then reset. Multiple sources accumulate until consumed. **AmpMod** is a % bonus (multiply). **SpeedMod** positive = faster (shorter time).
- **When changes take effect**: Saving an action in the Actions tab writes to file and reloads ActionLoader. If the player has a character in session, closing the Settings window refreshes that character’s action pool (default + weapon/armor/class actions) and invalidates the damage cache so edits apply without restarting.
- **Rarity**: Used when choosing actions for items (loot). Higher rarity = higher selection weight (Common=1, Uncommon=2, Rare=3, etc.).
- **Category**: Used to filter which actions can appear on which items (e.g. class abilities not on wrong items). When selecting actions for a weapon, only actions with an appropriate Category (or empty) are included. There is a 5% chance to bypass the Category filter so any category can appear.

---

## File touch summary

| Area | Files |
|------|--------|
| Form layout and fields | ActionFormBuilder.cs |
| Default/Starting logic | ActionFormBuilder.cs, ActionsTabManager.cs (if needed) |
| Filters and cadence options | ActionsTabManager.cs, ActionFormBuilder.cs |
| Cadence migration on load/save | ActionEditor, ActionLoader, SpreadsheetActionJsonConverter, SpreadsheetToActionDataConverter |
| Data model / JSON | ActionLoader.cs (ActionData), SpreadsheetActionJson.cs, SpreadsheetActionJsonConverter.cs, SpreadsheetToActionDataConverter.cs |
| Validation | ActionDataValidator.cs, ActionEditor.cs |
| Tests | ActionEditorTest*.cs, round-trip and cadence migration tests |
