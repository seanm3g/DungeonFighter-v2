using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat
{
    /// <summary>
    /// Text formatting for battle narratives
    /// Handles conversion of narrative text to ColoredText format
    /// </summary>
    public static class BattleNarrativeFormatter
    {
        /// <summary>
        /// Formats first blood narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatFirstBlood(string narrativeText)
        {
            return BattleNarrativeColoredText.FormatFirstBloodColored(narrativeText);
        }
        
        /// <summary>
        /// Formats critical hit narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCriticalHit(string actorName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatCriticalHitColored(actorName, narrativeText);
        }
        
        /// <summary>
        /// Formats critical miss narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCriticalMiss(string actorName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatCriticalMissColored(actorName, narrativeText);
        }
        
        /// <summary>
        /// Formats environmental action narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnvironmentalAction(string effectDescription, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatEnvironmentalActionColored(effectDescription, narrativeText);
        }
        
        /// <summary>
        /// Formats health recovery narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthRecovery(string targetName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatHealthRecoveryColored(targetName, narrativeText);
        }
        
        /// <summary>
        /// Formats health lead change narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthLeadChange(string leaderName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeColoredText.FormatHealthLeadChangeColored(leaderName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats below 50% health narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBelow50Percent(string entityName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeColoredText.FormatBelow50PercentColored(entityName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats below 10% health narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBelow10Percent(string entityName, string narrativeText, bool isPlayer)
        {
            return BattleNarrativeColoredText.FormatBelow10PercentColored(entityName, narrativeText, isPlayer);
        }
        
        /// <summary>
        /// Formats intense battle narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatIntenseBattle(string playerName, string enemyName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatIntenseBattleColored(playerName, enemyName, narrativeText);
        }
        
        /// <summary>
        /// Formats good combo narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatGoodCombo(string actorName, string targetName, bool isPlayerCombo)
        {
            return BattleNarrativeColoredText.FormatGoodComboColored(actorName, targetName, isPlayerCombo);
        }
        
        /// <summary>
        /// Formats player defeated narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatPlayerDefeated(string enemyName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatPlayerDefeatedColored(enemyName, narrativeText);
        }
        
        /// <summary>
        /// Formats enemy defeated narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnemyDefeated(string enemyName, string playerName, string narrativeText)
        {
            return BattleNarrativeColoredText.FormatEnemyDefeatedColored(enemyName, playerName, narrativeText);
        }
        
        /// <summary>
        /// Formats player taunt narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatPlayerTaunt(string playerName, string enemyName, string tauntText)
        {
            return BattleNarrativeColoredText.FormatPlayerTauntColored(playerName, enemyName, tauntText);
        }
        
        /// <summary>
        /// Formats enemy taunt narrative using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnemyTaunt(string enemyName, string playerName, string tauntText)
        {
            return BattleNarrativeColoredText.FormatEnemyTauntColored(enemyName, playerName, tauntText);
        }
        
        /// <summary>
        /// Formats a generic narrative message using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatGenericNarrative(string narrativeText, ColorPalette primaryColor = ColorPalette.Info)
        {
            return BattleNarrativeColoredText.FormatGenericNarrativeColored(narrativeText, primaryColor);
        }
    }
}

