using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Data model representing a row from the Google Sheets action spreadsheet
    /// Maps spreadsheet columns to action properties
    /// </summary>
    public class SpreadsheetActionData
    {
        // Column A
        public string Action { get; set; } = "";
        
        // Column B
        public string Description { get; set; } = "";
        
        // Column C (empty in spreadsheet)
        public string ColumnC { get; set; } = "";
        
        // Column D
        public string Rarity { get; set; } = "";
        
        // Column E
        public string Category { get; set; } = "";
        
        // Column F
        public string DPS { get; set; } = "";
        
        // Column G
        public string NumberOfHits { get; set; } = "";
        
        // Column H
        public string Damage { get; set; } = "";
        
        // Column I
        public string Speed { get; set; } = "";
        
        // Column J
        public string Duration { get; set; } = "";
        
        // Column K
        public string Cadence { get; set; } = "";
        
        // Column L
        public string Opener { get; set; } = "";
        
        // Column M
        public string Finisher { get; set; } = "";
        
        // Columns N-Q: Hero bonuses (ACCURACY, HIT, COMBO, CRIT)
        public string HeroAccuracy { get; set; } = "";
        public string HeroHit { get; set; } = "";
        public string HeroCombo { get; set; } = "";
        public string HeroCrit { get; set; } = "";

        /// <summary>Crit miss threshold adjustment; JSON key heroCritMiss.</summary>
        public string HeroCritMiss { get; set; } = "";
        
        // Columns R-U: Enemy dice modifications
        public string EnemyAccuracy { get; set; } = "";
        public string EnemyHit { get; set; } = "";
        public string EnemyCombo { get; set; } = "";
        public string EnemyCrit { get; set; } = "";
        /// <summary>Crit miss threshold adjustment for enemy attacker; JSON key enemyCritMiss.</summary>
        public string EnemyCritMiss { get; set; } = "";
        
        // Columns V-Y: Hero stats (STR, AGI, TECH, INT)
        public string HeroSTR { get; set; } = "";
        public string HeroAGI { get; set; } = "";
        public string HeroTECH { get; set; } = "";
        public string HeroINT { get; set; } = "";
        
        // Columns Z-AC: Enemy stats (appear unused)
        public string EnemySTR { get; set; } = "";
        public string EnemyAGI { get; set; } = "";
        public string EnemyTECH { get; set; } = "";
        public string EnemyINT { get; set; } = "";
        
        /// <summary>Hero next-action modifiers (sheet columns AJ–AM; row-1 block "HERO BASE STATS").</summary>
        public string SpeedMod { get; set; } = "";
        public string DamageMod { get; set; } = "";
        public string MultiHitMod { get; set; } = "";
        public string AmpMod { get; set; } = "";

        /// <summary>Enemy next-action modifiers (sheet columns AD–AG; row-1 block "ENEMY BASE STATS").</summary>
        public string EnemySpeedMod { get; set; } = "";
        public string EnemyDamageMod { get; set; } = "";
        public string EnemyMultiHitMod { get; set; } = "";
        public string EnemyAmpMod { get; set; } = "";
        
        // Status effects columns (AH-AO approximately)
        public string Stun { get; set; } = "";
        public string Poison { get; set; } = "";
        public string Burn { get; set; } = "";
        public string Bleed { get; set; } = "";
        public string Weaken { get; set; } = "";
        public string Expose { get; set; } = "";
        public string Slow { get; set; } = "";
        public string Vulnerability { get; set; } = "";
        public string Harden { get; set; } = "";
        public string Silence { get; set; } = "";
        public string Pierce { get; set; } = "";
        public string StatDrain { get; set; } = "";
        public string Fortify { get; set; } = "";
        public string Consume { get; set; } = "";
        public string Focus { get; set; } = "";
        public string Cleanse { get; set; } = "";
        public string Lifesteal { get; set; } = "";
        public string Reflect { get; set; } = "";
        public string SelfDamage { get; set; } = "";
        
        // Heal columns
        public string HeroHeal { get; set; } = "";
        public string HeroHealMaxHealth { get; set; } = "";
        
        // Additional mechanics columns (various positions)
        public string ReplaceNextRoll { get; set; } = "";
        public string HighestLowestRoll { get; set; } = "";
        public string DiceRolls { get; set; } = "";
        public string ExplodingDiceThreshold { get; set; } = "";
        public string Curse { get; set; } = "";
        public string Skip { get; set; } = "";
        public string Jump { get; set; } = "";
        /// <summary>Jump (+slots); sheet column <c>SHIFT</c> (preferred) or legacy <c>JUMP RELATIVE</c>.</summary>
        public string JumpRelative { get; set; } = "";
        public string Disrupt { get; set; } = "";
        public string Grace { get; set; } = "";
        public string LoopChain { get; set; } = "";
        public string Shuffle { get; set; } = "";
        public string ReplaceAction { get; set; } = "";
        public string ChainLength { get; set; } = "";
        public string ChainPosition { get; set; } = "";
        public string ModifyBasedOnChainPosition { get; set; } = "";
        public string DistanceFromXSlot { get; set; } = "";
        
        // Trigger columns
        public string OnHit { get; set; } = "";
        public string OnMiss { get; set; } = "";
        public string OnCrit { get; set; } = "";
        public string OnKill { get; set; } = "";
        public string OnRoomsCleared { get; set; } = "";
        public string OnRollValue { get; set; } = "";
        
        /// <summary>Comma-separated: ONHIT, ONMISS, ONCOMBO, ONCRITICAL. When set, action effects apply only on these outcomes.</summary>
        public string TriggerConditions { get; set; } = "";

        /// <summary>JSON-serialized list of StatBonusEntry for round-trip with Actions settings form.</summary>
        public string StatBonusesJson { get; set; } = "";

        /// <summary>JSON-serialized list of ThresholdEntry for round-trip with Actions settings form.</summary>
        public string ThresholdsJson { get; set; } = "";

        /// <summary>JSON-serialized list of AccumulationEntry for round-trip with Actions settings form.</summary>
        public string AccumulationsJson { get; set; } = "";

        /// <summary>JSON-serialized list of <see cref="ChainPositionBonusEntry"/> for chain-position scaling.</summary>
        public string ChainPositionBonusesJson { get; set; } = "";

        // Threshold columns
        public string Target { get; set; } = "";
        public string ThresholdCategory { get; set; } = "";
        public string ThresholdAmount { get; set; } = "";
        public string Bonus { get; set; } = "";
        public string BonusAttribute { get; set; } = "";
        public string Value { get; set; } = "";
        public string Attribute { get; set; } = "";
        public string Reset { get; set; } = "";
        public string ResetBlockerBuffer { get; set; } = "";
        public string ModifyRoom { get; set; } = "";
        
        // Tags
        public string Tags { get; set; } = "";

        /// <summary>Default/starting action for round-trip (Actions settings). "1" or truthy = true.</summary>
        public string IsDefaultAction { get; set; } = "";

        /// <summary>Comma-separated weapon types (e.g. "Sword, Dagger") for round-trip from Actions settings.</summary>
        public string WeaponTypes { get; set; } = "";
        
        /// <summary>
        /// Parses a CSV row into SpreadsheetActionData. Delegates to SpreadsheetActionDataCsvParser.
        /// When header is provided, uses row 1 (context) and row 2 (labels) to resolve columns by section and label.
        /// </summary>
        public static SpreadsheetActionData FromCsvRow(string[] columns, SpreadsheetHeader? header = null)
        {
            return SpreadsheetActionDataCsvParser.FromCsvRow(columns, header);
        }
        
        /// <summary>
        /// Known spreadsheet sub-header / context labels that should not be ingested as actions.
        /// These appear as column-group headers (e.g. chain/sequence metadata) in the sheet.
        /// </summary>
        private static readonly HashSet<string> KnownContextLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RESET CHAIN", "ON FAIL NO RESET", "LOOP CHAIN", "SHUFFLE", "REPLACE ACTION",
            "CHAIN LENGTH", "CHAIN POSITION", "MODIFY BASED ON CHAIN POISITION", "DISTANCE FROM X SLOT",
            "ON HIT", "ON MISS", "ON CRIT", "ON KILL", "ON ROOMS CLEARED", "ON ROLL VALUE",
            "MODIFY BASED ON TAGS", "ADD TAG", "REMOVE TAG", "TARGET", "THRESHOLD CATEGORY",
            "THRESHOLD AMOUNT", "BONUS", "BONUS ATTRIBUTE", "CADENCE", "VALUE", "ATTRIBUTE",
            "RESET", "MODIFY ROOM", "GRACE", "CHAIN POSITION BONUSES JSON"
        };

        /// <summary>
        /// Returns true if the action name looks like spreadsheet context/header rather than a real action.
        /// Rows with parenthetical labels (e.g. "(RESET CHAIN)") or sub-header column names are skipped.
        /// </summary>
        public static bool IsHeaderOrContextRow(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return false;

            var trimmed = actionName.Trim();

            // Parenthetical context labels: "(RESET CHAIN)", "(ON FAIL NO RESET)"
            if (trimmed.StartsWith("(") && trimmed.Contains(")"))
                return true;

            // Comma-separated list that looks like column headers (e.g. "(ON FAIL NO RESET),LOOP CHAIN,SHUFFLE,...")
            if (trimmed.Contains(","))
            {
                var tokens = trimmed.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                int contextLike = 0;
                foreach (var t in tokens)
                {
                    if (t.StartsWith("(") && t.Contains(")"))
                        contextLike++;
                    else if (KnownContextLabels.Contains(t))
                        contextLike++;
                }
                if (contextLike >= 2 || (tokens.Count >= 2 && contextLike == tokens.Count))
                    return true;
            }

            // Single token that is a known sub-header label
            if (KnownContextLabels.Contains(trimmed))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if this row represents a valid action (has a name and is not spreadsheet context/header).
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Action))
                return false;
            return !IsHeaderOrContextRow(Action);
        }
        
        /// <summary>
        /// Parses a numeric value from a string, handling percentages and empty values
        /// </summary>
        public static double ParseNumericValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0.0;
            
            value = value.Trim();
            
            // Handle percentages
            if (value.EndsWith("%"))
            {
                if (double.TryParse(value.Replace("%", ""), out double percent))
                    return percent / 100.0;
            }
            
            // Handle regular numbers
            if (double.TryParse(value, out double result))
                return result;
            
            return 0.0;
        }
        
        /// <summary>
        /// Parses an integer value from a string
        /// </summary>
        public static int ParseIntValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;
            
            value = value.Trim();
            
            if (int.TryParse(value, out int result))
                return result;
            
            return 0;
        }
    }
}
