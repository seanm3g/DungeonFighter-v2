using System;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// JSON model that matches Google Sheets column structure exactly
    /// All values are preserved as strings to match spreadsheet format
    /// </summary>
    public class SpreadsheetActionJson
    {
        // Column A
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";
        
        // Column B
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        
        // Column C (empty in spreadsheet)
        [JsonPropertyName("columnC")]
        public string ColumnC { get; set; } = "";
        
        // Column D
        [JsonPropertyName("rarity")]
        public string Rarity { get; set; } = "";
        
        // Column E
        [JsonPropertyName("category")]
        public string Category { get; set; } = "";
        
        // Column F
        [JsonPropertyName("dps")]
        public string DPS { get; set; } = "";
        
        // Column G
        [JsonPropertyName("numberOfHits")]
        public string NumberOfHits { get; set; } = "";
        
        // Column H
        [JsonPropertyName("damage")]
        public string Damage { get; set; } = "";
        
        // Column I
        [JsonPropertyName("speed")]
        public string Speed { get; set; } = "";
        
        // Column J
        [JsonPropertyName("duration")]
        public string Duration { get; set; } = "";
        
        // Column K
        [JsonPropertyName("cadence")]
        public string Cadence { get; set; } = "";
        
        // Column L
        [JsonPropertyName("opener")]
        public string Opener { get; set; } = "";
        
        // Column M
        [JsonPropertyName("finisher")]
        public string Finisher { get; set; } = "";
        
        // Columns N-Q: Hero bonuses (ACCURACY, HIT, COMBO, CRIT)
        [JsonPropertyName("heroAccuracy")]
        public string HeroAccuracy { get; set; } = "";
        
        [JsonPropertyName("heroHit")]
        public string HeroHit { get; set; } = "";
        
        [JsonPropertyName("heroCombo")]
        public string HeroCombo { get; set; } = "";
        
        [JsonPropertyName("heroCrit")]
        public string HeroCrit { get; set; } = "";
        
        // Columns R-U: Enemy bonuses
        [JsonPropertyName("enemyAccuracy")]
        public string EnemyAccuracy { get; set; } = "";
        
        [JsonPropertyName("enemyHit")]
        public string EnemyHit { get; set; } = "";
        
        [JsonPropertyName("enemyCombo")]
        public string EnemyCombo { get; set; } = "";
        
        [JsonPropertyName("enemyCrit")]
        public string EnemyCrit { get; set; } = "";
        
        // Columns V-Y: Hero stats (STR, AGI, TECH, INT)
        [JsonPropertyName("heroSTR")]
        public string HeroSTR { get; set; } = "";
        
        [JsonPropertyName("heroAGI")]
        public string HeroAGI { get; set; } = "";
        
        [JsonPropertyName("heroTECH")]
        public string HeroTECH { get; set; } = "";
        
        [JsonPropertyName("heroINT")]
        public string HeroINT { get; set; } = "";
        
        // Columns Z-AC: Enemy stats
        [JsonPropertyName("enemySTR")]
        public string EnemySTR { get; set; } = "";
        
        [JsonPropertyName("enemyAGI")]
        public string EnemyAGI { get; set; } = "";
        
        [JsonPropertyName("enemyTECH")]
        public string EnemyTECH { get; set; } = "";
        
        [JsonPropertyName("enemyINT")]
        public string EnemyINT { get; set; } = "";
        
        // Columns AD-AG: Modifiers
        [JsonPropertyName("speedMod")]
        public string SpeedMod { get; set; } = "";
        
        [JsonPropertyName("damageMod")]
        public string DamageMod { get; set; } = "";
        
        [JsonPropertyName("multiHitMod")]
        public string MultiHitMod { get; set; } = "";
        
        [JsonPropertyName("ampMod")]
        public string AmpMod { get; set; } = "";
        
        // Status effects columns
        [JsonPropertyName("stun")]
        public string Stun { get; set; } = "";
        
        [JsonPropertyName("poison")]
        public string Poison { get; set; } = "";
        
        [JsonPropertyName("burn")]
        public string Burn { get; set; } = "";
        
        [JsonPropertyName("bleed")]
        public string Bleed { get; set; } = "";
        
        [JsonPropertyName("weaken")]
        public string Weaken { get; set; } = "";
        
        [JsonPropertyName("expose")]
        public string Expose { get; set; } = "";
        
        [JsonPropertyName("slow")]
        public string Slow { get; set; } = "";
        
        [JsonPropertyName("vulnerability")]
        public string Vulnerability { get; set; } = "";
        
        [JsonPropertyName("harden")]
        public string Harden { get; set; } = "";
        
        [JsonPropertyName("silence")]
        public string Silence { get; set; } = "";
        
        [JsonPropertyName("pierce")]
        public string Pierce { get; set; } = "";
        
        [JsonPropertyName("statDrain")]
        public string StatDrain { get; set; } = "";
        
        [JsonPropertyName("fortify")]
        public string Fortify { get; set; } = "";
        
        [JsonPropertyName("consume")]
        public string Consume { get; set; } = "";
        
        [JsonPropertyName("focus")]
        public string Focus { get; set; } = "";
        
        [JsonPropertyName("cleanse")]
        public string Cleanse { get; set; } = "";
        
        [JsonPropertyName("lifesteal")]
        public string Lifesteal { get; set; } = "";
        
        [JsonPropertyName("reflect")]
        public string Reflect { get; set; } = "";
        
        [JsonPropertyName("selfDamage")]
        public string SelfDamage { get; set; } = "";
        
        // Heal columns
        [JsonPropertyName("heroHeal")]
        public string HeroHeal { get; set; } = "";
        
        [JsonPropertyName("heroHealMaxHealth")]
        public string HeroHealMaxHealth { get; set; } = "";
        
        // Additional mechanics columns
        [JsonPropertyName("replaceNextRoll")]
        public string ReplaceNextRoll { get; set; } = "";
        
        [JsonPropertyName("highestLowestRoll")]
        public string HighestLowestRoll { get; set; } = "";
        
        [JsonPropertyName("diceRolls")]
        public string DiceRolls { get; set; } = "";
        
        [JsonPropertyName("explodingDiceThreshold")]
        public string ExplodingDiceThreshold { get; set; } = "";
        
        [JsonPropertyName("curse")]
        public string Curse { get; set; } = "";
        
        [JsonPropertyName("skip")]
        public string Skip { get; set; } = "";
        
        [JsonPropertyName("jump")]
        public string Jump { get; set; } = "";
        
        [JsonPropertyName("disrupt")]
        public string Disrupt { get; set; } = "";
        
        [JsonPropertyName("grace")]
        public string Grace { get; set; } = "";
        
        [JsonPropertyName("loopChain")]
        public string LoopChain { get; set; } = "";
        
        [JsonPropertyName("shuffle")]
        public string Shuffle { get; set; } = "";
        
        [JsonPropertyName("replaceAction")]
        public string ReplaceAction { get; set; } = "";
        
        [JsonPropertyName("chainLength")]
        public string ChainLength { get; set; } = "";
        
        [JsonPropertyName("chainPosition")]
        public string ChainPosition { get; set; } = "";
        
        [JsonPropertyName("modifyBasedOnChainPosition")]
        public string ModifyBasedOnChainPosition { get; set; } = "";
        
        [JsonPropertyName("distanceFromXSlot")]
        public string DistanceFromXSlot { get; set; } = "";
        
        // Trigger columns
        [JsonPropertyName("onHit")]
        public string OnHit { get; set; } = "";
        
        [JsonPropertyName("onMiss")]
        public string OnMiss { get; set; } = "";
        
        [JsonPropertyName("onCrit")]
        public string OnCrit { get; set; } = "";
        
        [JsonPropertyName("onKill")]
        public string OnKill { get; set; } = "";
        
        [JsonPropertyName("onRoomsCleared")]
        public string OnRoomsCleared { get; set; } = "";
        
        [JsonPropertyName("onRollValue")]
        public string OnRollValue { get; set; } = "";
        
        // Threshold columns
        [JsonPropertyName("target")]
        public string Target { get; set; } = "";
        
        [JsonPropertyName("thresholdCategory")]
        public string ThresholdCategory { get; set; } = "";
        
        [JsonPropertyName("thresholdAmount")]
        public string ThresholdAmount { get; set; } = "";
        
        [JsonPropertyName("bonus")]
        public string Bonus { get; set; } = "";
        
        [JsonPropertyName("bonusAttribute")]
        public string BonusAttribute { get; set; } = "";
        
        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
        
        [JsonPropertyName("attribute")]
        public string Attribute { get; set; } = "";
        
        [JsonPropertyName("reset")]
        public string Reset { get; set; } = "";
        
        [JsonPropertyName("modifyRoom")]
        public string ModifyRoom { get; set; } = "";
        
        // Tags
        [JsonPropertyName("tags")]
        public string Tags { get; set; } = "";
        
        /// <summary>
        /// Converts SpreadsheetActionData to SpreadsheetActionJson
        /// </summary>
        public static SpreadsheetActionJson FromSpreadsheetActionData(SpreadsheetActionData data)
        {
            return new SpreadsheetActionJson
            {
                Action = data.Action,
                Description = data.Description,
                ColumnC = data.ColumnC,
                Rarity = data.Rarity,
                Category = data.Category,
                DPS = data.DPS,
                NumberOfHits = data.NumberOfHits,
                Damage = data.Damage,
                Speed = data.Speed,
                Duration = data.Duration,
                Cadence = data.Cadence,
                Opener = data.Opener,
                Finisher = data.Finisher,
                HeroAccuracy = data.HeroAccuracy,
                HeroHit = data.HeroHit,
                HeroCombo = data.HeroCombo,
                HeroCrit = data.HeroCrit,
                EnemyAccuracy = data.EnemyAccuracy,
                EnemyHit = data.EnemyHit,
                EnemyCombo = data.EnemyCombo,
                EnemyCrit = data.EnemyCrit,
                HeroSTR = data.HeroSTR,
                HeroAGI = data.HeroAGI,
                HeroTECH = data.HeroTECH,
                HeroINT = data.HeroINT,
                EnemySTR = data.EnemySTR,
                EnemyAGI = data.EnemyAGI,
                EnemyTECH = data.EnemyTECH,
                EnemyINT = data.EnemyINT,
                SpeedMod = data.SpeedMod,
                DamageMod = data.DamageMod,
                MultiHitMod = data.MultiHitMod,
                AmpMod = data.AmpMod,
                Stun = data.Stun,
                Poison = data.Poison,
                Burn = data.Burn,
                Bleed = data.Bleed,
                Weaken = data.Weaken,
                Expose = data.Expose,
                Slow = data.Slow,
                Vulnerability = data.Vulnerability,
                Harden = data.Harden,
                Silence = data.Silence,
                Pierce = data.Pierce,
                StatDrain = data.StatDrain,
                Fortify = data.Fortify,
                Consume = data.Consume,
                Focus = data.Focus,
                Cleanse = data.Cleanse,
                Lifesteal = data.Lifesteal,
                Reflect = data.Reflect,
                SelfDamage = data.SelfDamage,
                HeroHeal = data.HeroHeal,
                HeroHealMaxHealth = data.HeroHealMaxHealth,
                ReplaceNextRoll = data.ReplaceNextRoll,
                HighestLowestRoll = data.HighestLowestRoll,
                DiceRolls = data.DiceRolls,
                ExplodingDiceThreshold = data.ExplodingDiceThreshold,
                Curse = data.Curse,
                Skip = data.Skip,
                Jump = data.Jump,
                Disrupt = data.Disrupt,
                Grace = data.Grace,
                LoopChain = data.LoopChain,
                Shuffle = data.Shuffle,
                ReplaceAction = data.ReplaceAction,
                ChainLength = data.ChainLength,
                ChainPosition = data.ChainPosition,
                ModifyBasedOnChainPosition = data.ModifyBasedOnChainPosition,
                DistanceFromXSlot = data.DistanceFromXSlot,
                OnHit = data.OnHit,
                OnMiss = data.OnMiss,
                OnCrit = data.OnCrit,
                OnKill = data.OnKill,
                OnRoomsCleared = data.OnRoomsCleared,
                OnRollValue = data.OnRollValue,
                Target = data.Target,
                ThresholdCategory = data.ThresholdCategory,
                ThresholdAmount = data.ThresholdAmount,
                Bonus = data.Bonus,
                BonusAttribute = data.BonusAttribute,
                Value = data.Value,
                Attribute = data.Attribute,
                Reset = data.Reset,
                ModifyRoom = data.ModifyRoom,
                Tags = data.Tags
            };
        }
        
        /// <summary>
        /// Converts SpreadsheetActionJson to SpreadsheetActionData
        /// </summary>
        public SpreadsheetActionData ToSpreadsheetActionData()
        {
            return new SpreadsheetActionData
            {
                Action = this.Action,
                Description = this.Description,
                ColumnC = this.ColumnC,
                Rarity = this.Rarity,
                Category = this.Category,
                DPS = this.DPS,
                NumberOfHits = this.NumberOfHits,
                Damage = this.Damage,
                Speed = this.Speed,
                Duration = this.Duration,
                Cadence = this.Cadence,
                Opener = this.Opener,
                Finisher = this.Finisher,
                HeroAccuracy = this.HeroAccuracy,
                HeroHit = this.HeroHit,
                HeroCombo = this.HeroCombo,
                HeroCrit = this.HeroCrit,
                EnemyAccuracy = this.EnemyAccuracy,
                EnemyHit = this.EnemyHit,
                EnemyCombo = this.EnemyCombo,
                EnemyCrit = this.EnemyCrit,
                HeroSTR = this.HeroSTR,
                HeroAGI = this.HeroAGI,
                HeroTECH = this.HeroTECH,
                HeroINT = this.HeroINT,
                EnemySTR = this.EnemySTR,
                EnemyAGI = this.EnemyAGI,
                EnemyTECH = this.EnemyTECH,
                EnemyINT = this.EnemyINT,
                SpeedMod = this.SpeedMod,
                DamageMod = this.DamageMod,
                MultiHitMod = this.MultiHitMod,
                AmpMod = this.AmpMod,
                Stun = this.Stun,
                Poison = this.Poison,
                Burn = this.Burn,
                Bleed = this.Bleed,
                Weaken = this.Weaken,
                Expose = this.Expose,
                Slow = this.Slow,
                Vulnerability = this.Vulnerability,
                Harden = this.Harden,
                Silence = this.Silence,
                Pierce = this.Pierce,
                StatDrain = this.StatDrain,
                Fortify = this.Fortify,
                Consume = this.Consume,
                Focus = this.Focus,
                Cleanse = this.Cleanse,
                Lifesteal = this.Lifesteal,
                Reflect = this.Reflect,
                SelfDamage = this.SelfDamage,
                HeroHeal = this.HeroHeal,
                HeroHealMaxHealth = this.HeroHealMaxHealth,
                ReplaceNextRoll = this.ReplaceNextRoll,
                HighestLowestRoll = this.HighestLowestRoll,
                DiceRolls = this.DiceRolls,
                ExplodingDiceThreshold = this.ExplodingDiceThreshold,
                Curse = this.Curse,
                Skip = this.Skip,
                Jump = this.Jump,
                Disrupt = this.Disrupt,
                Grace = this.Grace,
                LoopChain = this.LoopChain,
                Shuffle = this.Shuffle,
                ReplaceAction = this.ReplaceAction,
                ChainLength = this.ChainLength,
                ChainPosition = this.ChainPosition,
                ModifyBasedOnChainPosition = this.ModifyBasedOnChainPosition,
                DistanceFromXSlot = this.DistanceFromXSlot,
                OnHit = this.OnHit,
                OnMiss = this.OnMiss,
                OnCrit = this.OnCrit,
                OnKill = this.OnKill,
                OnRoomsCleared = this.OnRoomsCleared,
                OnRollValue = this.OnRollValue,
                Target = this.Target,
                ThresholdCategory = this.ThresholdCategory,
                ThresholdAmount = this.ThresholdAmount,
                Bonus = this.Bonus,
                BonusAttribute = this.BonusAttribute,
                Value = this.Value,
                Attribute = this.Attribute,
                Reset = this.Reset,
                ModifyRoom = this.ModifyRoom,
                Tags = this.Tags
            };
        }
    }
}
