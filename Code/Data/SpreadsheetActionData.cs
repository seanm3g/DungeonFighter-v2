using System;
using System.Collections.Generic;

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
        
        // Columns R-U: Enemy bonuses (appear unused)
        public string EnemyAccuracy { get; set; } = "";
        public string EnemyHit { get; set; } = "";
        public string EnemyCombo { get; set; } = "";
        public string EnemyCrit { get; set; } = "";
        
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
        
        // Columns AD-AG: Modifiers
        public string SpeedMod { get; set; } = "";
        public string DamageMod { get; set; } = "";
        public string MultiHitMod { get; set; } = "";
        public string AmpMod { get; set; } = "";
        
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
        
        // Threshold columns
        public string Target { get; set; } = "";
        public string ThresholdCategory { get; set; } = "";
        public string ThresholdAmount { get; set; } = "";
        public string Bonus { get; set; } = "";
        public string BonusAttribute { get; set; } = "";
        public string Value { get; set; } = "";
        public string Attribute { get; set; } = "";
        public string Reset { get; set; } = "";
        public string ModifyRoom { get; set; } = "";
        
        // Tags
        public string Tags { get; set; } = "";
        
        /// <summary>
        /// Parses a CSV row into SpreadsheetActionData
        /// </summary>
        public static SpreadsheetActionData FromCsvRow(string[] columns)
        {
            var data = new SpreadsheetActionData();
            
            if (columns.Length > 0) data.Action = columns[0].Trim();
            if (columns.Length > 1) data.Description = columns[1].Trim();
            if (columns.Length > 3) data.Rarity = columns[3].Trim();
            if (columns.Length > 4) data.Category = columns[4].Trim();
            if (columns.Length > 4) data.DPS = columns[4].Trim(); // DPS(%) at index 4
            if (columns.Length > 5) data.NumberOfHits = columns[5].Trim(); // # OF HITS at index 5
            if (columns.Length > 6) data.Damage = columns[6].Trim(); // DAMAGE(%) at index 6
            // CSV data alignment: [7]=Speed, [8]=Duration, [9]=Cadence, [10]=Opener, [11]=Finisher
            if (columns.Length > 7) data.Speed = columns[7].Trim(); // SPEED(x) at index 7
            if (columns.Length > 8) data.Duration = columns[8].Trim(); // DURATION at index 8
            if (columns.Length > 9) data.Cadence = columns[9].Trim(); // CADENCE at index 9
            if (columns.Length > 10) data.Opener = columns[10].Trim(); // OPENER at index 10
            if (columns.Length > 11) data.Finisher = columns[11].Trim(); // FINISHER at index 11
            
            // Hero bonuses (N-Q, indices 12-15 based on header: ACCUARCY, HIT, COMBO, CRIT)
            // Actual CSV: [12]=ACCURACY, [13]=HIT, [14]=COMBO, [15]=CRIT
            if (columns.Length > 12) data.HeroAccuracy = columns[12].Trim();
            if (columns.Length > 13) data.HeroHit = columns[13].Trim();
            if (columns.Length > 14) data.HeroCombo = columns[14].Trim();
            if (columns.Length > 15) data.HeroCrit = columns[15].Trim();
            
            // Enemy bonuses (R-U, indices 16-19) - also check these as fallback for Hero bonuses
            // Actual CSV: [16]=Enemy ACCURACY, [17]=Enemy HIT, [18]=Enemy COMBO, [19]=Enemy CRIT
            if (columns.Length > 16) data.EnemyAccuracy = columns[16].Trim();
            if (columns.Length > 17) data.EnemyHit = columns[17].Trim();
            if (columns.Length > 18) data.EnemyCombo = columns[18].Trim();
            if (columns.Length > 19) data.EnemyCrit = columns[19].Trim();
            
            // Hero stats (V-Y, indices 21-24)
            if (columns.Length > 21) data.HeroSTR = columns[21].Trim();
            if (columns.Length > 22) data.HeroAGI = columns[22].Trim();
            if (columns.Length > 23) data.HeroTECH = columns[23].Trim();
            if (columns.Length > 24) data.HeroINT = columns[24].Trim();
            
            // Modifiers (AD-AG, indices 29-32)
            if (columns.Length > 29) data.SpeedMod = columns[29].Trim();
            if (columns.Length > 30) data.DamageMod = columns[30].Trim();
            if (columns.Length > 31) data.MultiHitMod = columns[31].Trim();
            if (columns.Length > 32) data.AmpMod = columns[32].Trim();
            
            // Status effects (approximately indices 33-50)
            if (columns.Length > 33) data.HeroHeal = columns[33].Trim();
            if (columns.Length > 34) data.HeroHealMaxHealth = columns[34].Trim();
            if (columns.Length > 35) data.Stun = columns[35].Trim();
            if (columns.Length > 36) data.Poison = columns[36].Trim();
            if (columns.Length > 37) data.Burn = columns[37].Trim();
            if (columns.Length > 38) data.Bleed = columns[38].Trim();
            if (columns.Length > 39) data.Weaken = columns[39].Trim();
            if (columns.Length > 40) data.Expose = columns[40].Trim();
            if (columns.Length > 41) data.Slow = columns[41].Trim();
            if (columns.Length > 42) data.Vulnerability = columns[42].Trim();
            if (columns.Length > 43) data.Harden = columns[43].Trim();
            if (columns.Length > 44) data.Silence = columns[44].Trim();
            if (columns.Length > 45) data.Pierce = columns[45].Trim();
            if (columns.Length > 46) data.StatDrain = columns[46].Trim();
            if (columns.Length > 47) data.Fortify = columns[47].Trim();
            if (columns.Length > 48) data.Consume = columns[48].Trim();
            if (columns.Length > 49) data.Focus = columns[49].Trim();
            if (columns.Length > 50) data.Cleanse = columns[50].Trim();
            if (columns.Length > 51) data.Lifesteal = columns[51].Trim();
            if (columns.Length > 52) data.Reflect = columns[52].Trim();
            if (columns.Length > 53) data.SelfDamage = columns[53].Trim();
            
            // Additional mechanics - these column indices are approximate and may need adjustment
            // Based on spreadsheet structure, these appear later in the row
            // We'll need to map these more carefully based on actual CSV structure
            
            return data;
        }
        
        /// <summary>
        /// Checks if this row represents a valid action (has a name)
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Action);
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
