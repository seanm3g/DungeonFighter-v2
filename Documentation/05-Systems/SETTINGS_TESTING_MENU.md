# Settings Testing Menu (Game Mechanics & Reliability)

## Purpose

The **Testing** tab in Settings runs **game mechanics and reliability** tests only. It is intended to verify that combat, dice, status effects, progression, dungeon rewards, and related systems produce correct results in context—not to verify that code loads or that individual units pass in isolation.

## What the menu includes

The panel is organized under a single section: **Game mechanics & reliability**.

### Buttons

| Button | What it runs |
|--------|----------------|
| **Dice & rolls** | Dice roll mechanics (range, distribution, bonuses). |
| **Combat** | Combat system test suite: damage calculator, hit calculator, speed calculator, status effect calculator, threshold manager, battle narrative, etc. |
| **Status effects** | Status effect application and state (basic + advanced). |
| **Multi-hit** | Multi-hit action damage, early termination, counts. |
| **Combo** | Combo execution, amplifier scaling, interruption. |
| **Action mechanics** | Action mechanics discovery and tests (status effects, roll mods, triggers, combo routing, etc.). |
| **Progression (XP / level)** | Level-up, XP system, multi-source XP rewards. |
| **Dungeon & rewards** | Dungeon/enemy generation and loot/XP rewards. |
| **Save/Load** | Save/load persistence and round-trip state. |
| **Gameplay flow** | End-to-end flow: dungeon completion, progression, equipment, save/load, inventory. |
| **Run all mechanics** | All of the above in one run (no loaders, UI, or data-import tests). |
| **Clear** | Clears the output area. |

### Utilities (unchanged)

- **Resync Actions from Google Sheets** – fetches and reloads actions.
- **Optimize Actions.json** – removes empty fields and reloads.

## Run all mechanics

**Run all mechanics** invokes only:

1. Dice & rolls  
2. Combat (calculators & thresholds)  
3. Status effects  
4. Multi-hit  
5. Combo  
6. Action mechanics  
7. Progression (XP / level)  
8. Dungeon & rewards  
9. Save/Load  
10. Gameplay flow  

It does **not** run: Action Blocks, Spreadsheet Import, Actions Settings Integration, Error Handling, Game State Management, DataSystemTestRunner, UISystemTestRunner, ConfigSystemTestRunner, or other loader/UI suites.

## Full test suite

To run the **entire** test suite (including unit tests, loaders, UI tests, and system-specific runners), use:

- **Command line**: `Scripts\run-tests.bat` or `Scripts\run-tests.ps1`, or run the game with `--run-tests`.

The in-game Testing menu stays focused on mechanics and reliability; the full suite remains available outside the UI.
