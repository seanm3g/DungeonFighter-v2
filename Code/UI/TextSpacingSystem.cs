using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Centralized text spacing system that maps directly to reference output structure.
    /// Makes it easy to adjust spacing to match the reference by defining rules based on
    /// what block type follows what (context-aware spacing).
    /// 
    /// 📖 FOR COMPLETE FORMATTING GUIDE: See Documentation/04-Reference/FORMATTING_SYSTEM_GUIDE.md
    /// 
    /// Quick Reference:
    /// - Edit spacing rules: Lines 77-131 (SpacingRules dictionary)
    /// - Add new block types: Add to BlockType enum (lines 16-44)
    /// - Usage pattern: ApplySpacingBefore() → Display → RecordBlockDisplayed()
    /// </summary>
    public static class TextSpacingSystem
    {
        /// <summary>
        /// Defines all block types in the display system
        /// </summary>
        public enum BlockType
        {
            // Section headers
            DungeonHeader,      // === ENTERING DUNGEON ===
            RoomHeader,         // === ENTERING ROOM ===
            RoomInfo,           // Room Number, Room name, description
            EnemyAppearance,    // "A Enemy appears."
            EnemyStats,         // Enemy Stats - Health: X/Y, Armor: Z
            HeroStats,          // Hero Stats - Health: X/Y, Armor: Z
            
            // Combat blocks
            CombatAction,       // "Actor hits Target for X damage" + roll info
            EnvironmentalAction, // "Room uses Action on Target!"
            StatusEffect,       // Status effect messages (part of action block)
            PoisonDamage,       // "[Actor] takes X poison damage"
            
            // Narrative blocks
            Narrative,          // Battle narrative text
            CriticalMissNarrative, // Critical miss description (part of action block)
            
            // System blocks
            RoomCleared,        // "Room cleared!" message
            SafeRoom,           // "It appears you are safe..."
            SystemMessage,      // Other system messages
            
            // UI blocks
            StatsBlock,         // Consecutive stats display
            MenuBlock           // Menu items
        }
        
        /// <summary>
        /// Tracks the last block type that was displayed
        /// </summary>
        private static BlockType? lastBlockType = null;
        
        /// <summary>
        /// Lazy-initialized dictionary to avoid static initialization issues
        /// </summary>
        private static Dictionary<(BlockType? previous, BlockType current), int>? _spacingRules = null;
        
        /// <summary>
        /// Defines spacing rules based on block transitions.
        /// Format: (PreviousBlockType, CurrentBlockType) => blank lines before current
        /// 
        /// This directly maps to the reference output structure:
        /// - DungeonHeader -> RoomHeader: 1 blank line
        /// - RoomInfo -> EnemyAppearance: 1 blank line
        /// - EnemyAppearance -> EnemyStats: 1 blank line
        /// - EnemyStats -> CombatAction: 1 blank line (first combat action)
        /// - CombatAction -> CombatAction: 0 blank lines (consecutive actions, no actor change spacing)
        /// - CombatAction -> EnvironmentalAction: 1 blank line
        /// - EnvironmentalAction -> CombatAction: 0 blank lines
        /// - CombatAction -> PoisonDamage: 1 blank line
        /// - Any -> Narrative: 1 blank line (except when part of action block)
        /// </summary>
        private static Dictionary<(BlockType? previous, BlockType current), int> SpacingRules
        {
            get
            {
                if (_spacingRules == null)
                {
                    _spacingRules = new Dictionary<(BlockType? previous, BlockType current), int>
                    {
            // First block (no previous) - DungeonHeader when starting
            { (null, BlockType.DungeonHeader), 0 },  // No blank line before first block
            
            // Section transitions (always 1 blank line)
            { (BlockType.DungeonHeader, BlockType.RoomHeader), 1 },
            { (BlockType.RoomHeader, BlockType.RoomInfo), 1 },  // Room header to room info
            { (BlockType.RoomInfo, BlockType.RoomHeader), 1 },  // Subsequent rooms after first room
            { (BlockType.RoomCleared, BlockType.RoomHeader), 1 },  // After room cleared, next room
            { (BlockType.RoomInfo, BlockType.EnemyAppearance), 1 },
            { (BlockType.EnemyAppearance, BlockType.EnemyStats), 1 },
            { (BlockType.EnemyStats, BlockType.CombatAction), 1 },  // First combat action
            { (BlockType.HeroStats, BlockType.CombatAction), 1 },   // First combat action (if hero stats shown)
            
            // Combat action transitions (no blank lines between consecutive actions)
            { (BlockType.CombatAction, BlockType.CombatAction), 0 },  // No blank line between actions
            { (BlockType.EnvironmentalAction, BlockType.CombatAction), 0 },
            { (BlockType.StatusEffect, BlockType.CombatAction), 0 },
            
            // Environmental action transitions
            { (BlockType.CombatAction, BlockType.EnvironmentalAction), 1 },  // Blank line before environmental
            { (BlockType.EnvironmentalAction, BlockType.EnvironmentalAction), 0 },
            
            // Status effects (part of action block, no spacing)
            { (BlockType.CombatAction, BlockType.StatusEffect), 0 },
            { (BlockType.EnvironmentalAction, BlockType.StatusEffect), 0 },
            { (BlockType.StatusEffect, BlockType.StatusEffect), 1 },  // Blank line between consecutive standalone status effect blocks
            
            // Poison damage (appears between combat actions)
            { (BlockType.CombatAction, BlockType.PoisonDamage), 1 },
            { (BlockType.EnvironmentalAction, BlockType.PoisonDamage), 1 },
            { (BlockType.PoisonDamage, BlockType.CombatAction), 0 },
            { (BlockType.PoisonDamage, BlockType.PoisonDamage), 0 },
            
            // Narrative blocks
            { (BlockType.CombatAction, BlockType.Narrative), 1 },
            { (BlockType.EnvironmentalAction, BlockType.Narrative), 1 },
            { (BlockType.Narrative, BlockType.CombatAction), 0 },
            
            // Critical miss narrative (part of action block, no spacing)
            { (BlockType.CombatAction, BlockType.CriticalMissNarrative), 0 },
            { (BlockType.CriticalMissNarrative, BlockType.StatusEffect), 0 },
            
            // Room cleared
            { (BlockType.CombatAction, BlockType.RoomCleared), 1 },
            // Note: (RoomCleared, RoomHeader) rule is defined above in section transitions (line 89)
            
            // Safe room
            { (BlockType.RoomInfo, BlockType.SafeRoom), 1 },
            
            // System messages
            { (BlockType.CombatAction, BlockType.SystemMessage), 0 },  // Default: no spacing, add explicitly if needed
            { (BlockType.EnvironmentalAction, BlockType.SystemMessage), 0 },
            
            // Stats blocks (no spacing)
            { (BlockType.EnemyStats, BlockType.HeroStats), 0 },
            
            // Menu blocks (no spacing)
            { (BlockType.MenuBlock, BlockType.MenuBlock), 0 },
                    };
                }
                return _spacingRules;
            }
        }
        
        /// <summary>
        /// Default spacing when no specific rule is found
        /// </summary>
        private const int DefaultSpacing = 0;
        
        /// <summary>
        /// Applies spacing before displaying a block based on what came before it.
        /// Call this before displaying any block to ensure correct spacing.
        /// This method now writes the blank lines directly to avoid circular dependency issues.
        /// </summary>
        /// <param name="currentBlockType">The type of block being displayed</param>
        public static void ApplySpacingBefore(BlockType currentBlockType)
        {
            int blankLines = GetSpacingBefore(currentBlockType);
            
            if (blankLines > 0)
            {
                // Write blank lines using UIManager, but catch any initialization exceptions
                // This prevents circular dependency issues during static initialization
                try
                {
                    for (int i = 0; i < blankLines; i++)
                    {
                        UIManager.WriteBlankLine();
                    }
                }
                catch (TypeInitializationException)
                {
                    // If UIManager fails to initialize, skip spacing silently
                    // Spacing is not critical - better to skip it than crash
                }
                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is TypeInitializationException)
                {
                    // TypeInitializationException wrapped in TargetInvocationException - skip spacing
                }
                catch
                {
                    // Any other exception - skip spacing silently
                    // Spacing is not critical
                }
            }
        }
        
        /// <summary>
        /// Gets the number of blank lines that should appear before the current block type
        /// based on what block was displayed previously.
        /// </summary>
        /// <param name="currentBlockType">The type of block being displayed</param>
        /// <returns>Number of blank lines to add before this block</returns>
        public static int GetSpacingBefore(BlockType currentBlockType)
        {
            // Check for specific transition rule
            if (SpacingRules.TryGetValue((lastBlockType, currentBlockType), out int spacing))
            {
                return spacing;
            }
            
            // Check for "any previous" rule (null means any)
            if (SpacingRules.TryGetValue((null, currentBlockType), out int anySpacing))
            {
                return anySpacing;
            }
            
            // Default: no spacing
            return DefaultSpacing;
        }
        
        /// <summary>
        /// Records that a block has been displayed. Call this after displaying a block
        /// so the next block knows what came before it.
        /// </summary>
        /// <param name="blockType">The type of block that was just displayed</param>
        public static void RecordBlockDisplayed(BlockType blockType)
        {
            // Don't record critical miss narratives as separate blocks
            // They're part of the action block and don't affect spacing
            // StatusEffect blocks are now recorded when they're standalone (like stun messages)
            // Status effects that are part of action blocks don't call this method
            if (blockType != BlockType.CriticalMissNarrative)
            {
                lastBlockType = blockType;
            }
        }
        
        /// <summary>
        /// Resets the spacing system for a new battle/dungeon.
        /// Call this when starting a new dungeon or battle.
        /// </summary>
        public static void Reset()
        {
            lastBlockType = null;
        }
        
        /// <summary>
        /// Gets the last block type that was displayed (for debugging)
        /// </summary>
        public static BlockType? GetLastBlockType()
        {
            return lastBlockType;
        }
        
        /// <summary>
        /// Validates that all expected spacing rules are defined.
        /// Returns a list of missing transitions that should have rules.
        /// </summary>
        public static List<string> ValidateSpacingRules()
        {
            var issues = new List<string>();
            
            // Get all block types
            var allBlockTypes = Enum.GetValues(typeof(BlockType)).Cast<BlockType>().ToList();
            
            // Check for common transitions that might be missing
            // We don't require ALL transitions, but check for important ones
            var importantTransitions = new List<(BlockType? previous, BlockType current)>
            {
                (null, BlockType.DungeonHeader), // First block
                (BlockType.DungeonHeader, BlockType.RoomHeader),
                (BlockType.RoomHeader, BlockType.RoomInfo),
                (BlockType.RoomInfo, BlockType.EnemyAppearance),
                (BlockType.EnemyAppearance, BlockType.EnemyStats),
                (BlockType.EnemyStats, BlockType.HeroStats),
                (BlockType.HeroStats, BlockType.CombatAction),
                (BlockType.CombatAction, BlockType.CombatAction), // Consecutive actions
                (BlockType.CombatAction, BlockType.RoomCleared),
                (BlockType.RoomCleared, BlockType.RoomHeader), // Next room
            };
            
            foreach (var transition in importantTransitions)
            {
                if (!SpacingRules.ContainsKey(transition))
                {
                    issues.Add($"Missing spacing rule for transition: {transition.previous} -> {transition.current}");
                }
            }
            
            return issues;
        }
        
        /// <summary>
        /// Gets all defined spacing rules (for debugging/validation)
        /// </summary>
        public static Dictionary<(BlockType? previous, BlockType current), int> GetAllSpacingRules()
        {
            return new Dictionary<(BlockType? previous, BlockType current), int>(SpacingRules);
        }
    }
}

