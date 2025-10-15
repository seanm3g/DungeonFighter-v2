# DungeonFighter v2 - Changelog

## Version 6.2 - GUI Polish & Feature Completion (October 11, 2025)

### Major Features Added

#### Title Screen Animation System
- **Animated Title Screen**: Smooth 30 FPS color transition animation
  - Phase 1: White → Warm white (DUNGEON) & Cool white (FIGHTER) - 1 second
  - Phase 2: Warm/Cool → Final colors (Gold & Red) - 1 second
  - Professional, polished intro experience
- **New Class**: `TitleScreenAnimator` for dedicated animation control
- **API Additions**: `CanvasUIManager.Clear()` and `CanvasUIManager.Refresh()` made public
- **Files**: `Code/UI/TitleScreenAnimator.cs` (new), `MainWindow.axaml.cs` (updated)

#### Item Color System
- **Rarity-Based Coloring**: Items display with fancy colors based on rarity
  - Common: Grey (solid)
  - Uncommon: Green (solid)
  - Rare: Blue (solid)
  - Epic: Purple (solid)
  - Legendary: Orange-White shimmer
  - Mythic: Purple-Cyan-White prismatic glow
  - Transcendent: White-Purple-Cyan-Blue ethereal radiance
- **Modifier Coloring**: Prefixes/suffixes colored by effect type
  - Damage: Fiery (Red/Orange/Yellow)
  - Speed: Electric (Cyan/Yellow/White)
  - Magic: Arcane (Purple/Magenta)
  - Life/Death: Bloodied (Dark Red/Red)
  - Divine: Holy (Gold/White)
- **Stat Bonus Coloring**: Color-coded by stat type
- **New Class**: `ItemColorSystem` for comprehensive item coloring
- **Files**: `Code/UI/ItemColorSystem.cs` (new), `ItemDisplayFormatter.cs` (updated)

#### Complete Inventory Implementation (GUI)
- **Full Feature Parity**: All 7 inventory actions from console UI now in GUI
  1. Equip Item - Full multi-step workflow
  2. Unequip Item - Slot selection with item return
  3. Discard Item - Permanent item removal
  4. Manage Combo Actions - Placeholder (future)
  5. Continue to Dungeon - Navigation to dungeon selection
  6. Return to Main Menu - Back to main menu
  7. Exit Game - Application exit
- **Multi-Step Actions**: Proper state management for item/slot selection
- **Visual Layout**: Two-column button layout with clear action labels
- **Files**: `CanvasUIManager.cs`, `Game.cs` (HandleInventoryInput enhanced)

#### Color Configuration System
- **Data-Driven Colors**: All color templates and keywords moved to JSON
  - `GameData/ColorTemplates.json` - 37 color templates
  - `GameData/KeywordColorGroups.json` - 27 keyword groups with 200+ keywords
- **Runtime Reloading**: Can reload configs without restarting game
- **Safe Fallback**: Gracefully falls back to hardcoded defaults if loading fails
- **New Classes**: `ColorTemplateLoader`, `KeywordColorLoader`
- **API Additions**: `ColorTemplateLibrary.Initialize()`, `KeywordColorSystem.Initialize()`

### Layout & UI Improvements

#### Resolution Update (1920x1080)
- **Window Size**: Updated from 880×750 to 1920×1080 pixels
- **Character Grid**: Expanded from 100×40 to 220×65 characters
- **Center Point**: Updated from 50 to 110 characters
- **Panel Widths**: Scaled proportionally to new dimensions
- **Files**: `MainWindow.axaml`, `GameCanvasControl.cs`, `CanvasUIManager.cs`, `PersistentLayoutManager.cs`

#### Layout Balance Optimization
- **Center Panel**: Increased from 146 to 162 characters (16 char wider)
- **Side Panels**: Reduced from 36 to 28 characters each (better balance)
- **Visual Balance**: Side panels now 13% each, center 74% (more symmetric)
- **Text Truncation**: Updated limits to fit narrower side panels
- **Files**: `PersistentLayoutManager.cs`

#### Main Menu Positioning
- **Menu Location**: Moved from near-bottom to near-top of screen
- **Better UX**: Options immediately visible below title
- **Positioning**: Menu starts at Y=12 (4 lines below "MAIN MENU" title)
- **Instructions**: Dynamically positioned 2 lines below menu
- **Files**: `CanvasUIManager.cs` (RenderMainMenu)

### Bug Fixes & Quality Improvements

#### Auto-Load Saved Character
- **Fixed**: Inventory access issue - pressing "2" at main menu now works for returning players
- **Auto-Load**: Saved characters automatically load when main menu displays
- **Better UX**: All menu options accessible immediately for returning players
- **Files**: `Game.cs` (ShowMainMenu method)

#### Combat Log Persistence
- **Fixed**: Combat log now stays visible when enemies defeated
- **Victory Messages**: Added to combat log instead of full-screen overlay
- **Room Cleared**: Shows in combat log with health info
- **New Methods**: `AddVictoryMessage()`, `AddDefeatMessage()`, `AddRoomClearedMessage()`
- **Files**: `CanvasUIManager.cs`, `Game.cs`

#### Combat Text Display Fixes
- **Color Bleeding Fix**: Keywords now reset to white after coloring
- **Text Wrapping Fix**: Combat text wraps properly within box boundaries
- **Template-Aware Wrapping**: Color templates preserved during text wrapping
- **Bracket Removal**: Removed all brackets from entity names (cleaner display)
- **Indentation Fix**: Continuation lines use minimal 2-space indent
- **New Method**: `WriteLineColoredWrapped()` for wrapped text rendering
- **Files**: `KeywordColorSystem.cs`, `CanvasUIManager.cs`, `CombatResults.cs`, `ActionExecutor.cs`

#### Console Output Leak Fix
- **Fixed**: Loot messages no longer appear in terminal during GUI mode
- **Proper Routing**: Changed `Console.WriteLine()` to `UIManager.WriteSystemLine()`
- **Clean Separation**: Terminal stays clean when running GUI
- **Files**: `RewardManager.cs`

### Technical Improvements

#### Text Display System
- **Accurate Width Calculations**: Uses `ColorParser.GetDisplayLength()` for wrapping
- **Template Preservation**: New `SplitPreservingMarkup()` method
- **Color Flow**: More natural color inheritance between keywords
- **Performance**: Optimized text wrapping for large combat logs

#### Color System Enhancements
- **129 New Templates**: Comprehensive coverage for modifiers, bonuses, environments
  - 29 item modifier templates (worn → transcendent)
  - 46 stat bonus templates (protection → lootmaster)
  - 25 environment templates (forest → divine)
  - 7 status effect templates (poisoned → slowed)
- **Quality Progression**: Dark/grey (bad) → Prismatic (transcendent)
- **Thematic Colors**: Environment and status effects use mixed color sequences
- **Files**: `GameData/ColorTemplates.json` (massive update)

### Documentation Created/Updated

#### New Documentation (Root Level)
- `IMPLEMENTATION_SUMMARY_TITLE_ANIMATION.md` - Complete animation implementation details
- `README_TITLE_SCREEN_ANIMATION.md` - User-facing animation guide
- `README_QUICK_START_ANIMATION.md` - Quick start for title animation
- `README_ITEM_COLOR_SYSTEM.md` - Item coloring implementation summary
- `README_INVENTORY_IMPLEMENTATION.md` - Complete inventory feature documentation
- `README_COMBAT_LOG_PERSISTENCE.md` - Combat log changes documentation
- `README_COLOR_CONFIG_UPDATE.md` - Color config system implementation
- `SUMMARY_COLOR_CONFIG.md` - Color config system summary
- `SUMMARY_COLOR_TEMPLATES_UPDATE.md` - Color template additions summary
- Various bug fix documentation (combat text, console output, etc.)

#### Updated Documentation
- `Documentation/05-Systems/OPENING_ANIMATION.md` - Added animated title screen style
- `Documentation/02-Development/TASKLIST.md` - Updated with completions
- `Documentation/05-Systems/PERSISTENT_LAYOUT_SYSTEM.md` - Updated layout specs
- `GameData/README_COLOR_CONFIG.md` - User guide for color customization

### Build & Testing
- ✅ All changes compile successfully (0 errors, 0 warnings)
- ✅ No linter errors introduced
- ✅ Feature parity achieved between console and GUI modes
- ✅ Backward compatibility maintained

---

## Version 6.1 - Persistent Layout System (October 2025)

### Major Features
- **Persistent Layout Manager**: Unified layout system for all game screens
- **Character Panel**: Always-visible left panel with stats, health, and equipment
- **Location/Enemy Panel**: Right panel with dungeon/room/enemy information
- **Dynamic Content**: Center panel switches between game phases
- **Consistent Experience**: Same layout across inventory, combat, exploration, menus

### Technical Details
- Screen: 220×65 characters (1920×1080 resolution)
- Left Panel: 28 chars (13%) - Character Info
- Center Panel: 162 chars (74%) - Dynamic Content
- Right Panel: 28 chars (13%) - Location/Enemy Info
- All render methods refactored to use `PersistentLayoutManager`

---

## Version 6.0 - GUI Implementation (October 2025)

### Major Features
- **Avalonia-Based GUI**: Modern ASCII canvas rendering
- **Mouse Support**: Clickable UI elements with hover effects
- **Keyboard Support**: Full keyboard navigation
- **Screen-Specific Rendering**: Dedicated render methods for each game phase
- **Color System Integration**: Caves of Qud-inspired color markup

### Color System
- Color templates for elemental, magical, status effects
- Keyword auto-coloring system
- Template-based color markup: `{{template|text}}`
- Direct color codes: `&X` for foreground, `^X` for background

### Text Animation
- **TextFadeAnimator**: Multiple fade patterns (alternating, sequential, etc.)
- Integration with color system
- Configurable timing and color progressions

---

## Earlier Versions

### Version 5.x - Balance Tuning
- Combat balance improvements
- Binary search tuning methodology
- Performance optimizations

### Version 4.x - Core Systems
- Character progression system
- Equipment and inventory system
- Dungeon generation and exploration

### Version 3.x - Combat System
- Turn-based combat implementation
- Action system with speed modifiers
- Status effects and conditions

### Version 2.x - Data Systems
- JSON-based data loading
- Configuration management
- Save/load system

### Version 1.x - Initial Implementation
- Console-based game engine
- Basic dungeon crawling mechanics
- Character classes and enemies

---

**Note**: This changelog focuses on major features and changes. For detailed commit history, see git logs. For implementation details, see individual documentation files in the Documentation/ directory.

