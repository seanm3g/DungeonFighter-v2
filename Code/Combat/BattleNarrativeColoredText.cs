using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Facade for battle narrative formatting using the new ColoredText system.
    /// Delegates to specialized formatters to reduce code duplication and improve maintainability.
    /// 
    /// This class provides a unified interface for all battle narrative formatting needs,
    /// coordinating with focused formatter classes that handle specific narrative types.
    /// </summary>
    public static class BattleNarrativeColoredText
    {
        // ============================================================================
        // Single Entity Narratives
        // ============================================================================

        /// <summary>
        /// Formats first blood narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatFirstBloodColored(string narrativeText)
            => FirstBloodFormatter.Format(narrativeText);

        /// <summary>
        /// Formats critical hit narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCriticalHitColored(string actorName, string narrativeText)
            => CriticalHitFormatter.Format(actorName, narrativeText);

        /// <summary>
        /// Formats critical miss narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCriticalMissColored(string actorName, string narrativeText)
            => CriticalMissFormatter.Format(actorName, narrativeText);

        /// <summary>
        /// Formats environmental action narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnvironmentalActionColored(string effectDescription, string narrativeText)
            => EnvironmentalActionFormatter.Format(effectDescription, narrativeText);

        /// <summary>
        /// Formats health recovery narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthRecoveryColored(string targetName, string narrativeText)
            => HealthRecoveryFormatter.Format(targetName, narrativeText);

        /// <summary>
        /// Formats health lead change narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthLeadChangeColored(string leaderName, string narrativeText, bool isPlayer)
            => HealthLeadChangeFormatter.Format(leaderName, narrativeText, isPlayer);

        /// <summary>
        /// Formats below 50% health narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBelow50PercentColored(string entityName, string narrativeText, bool isPlayer)
            => Below50PercentFormatter.Format(entityName, narrativeText, isPlayer);

        /// <summary>
        /// Formats below 10% health narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBelow10PercentColored(string entityName, string narrativeText, bool isPlayer)
            => Below10PercentFormatter.Format(entityName, narrativeText, isPlayer);

        // ============================================================================
        // Dual Entity Narratives
        // ============================================================================

        /// <summary>
        /// Formats intense battle narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatIntenseBattleColored(string playerName, string enemyName, string narrativeText)
            => IntenseBattleFormatter.Format(playerName, enemyName, narrativeText);

        /// <summary>
        /// Formats good combo narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatGoodComboColored(string actorName, string targetName, bool isPlayerCombo)
            => ComboFormatter.Format(actorName, targetName, isPlayerCombo);

        /// <summary>
        /// Formats player defeated narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatPlayerDefeatedColored(string enemyName, string narrativeText)
            => PlayerDefeatedFormatter.Format(enemyName, narrativeText);

        /// <summary>
        /// Formats enemy defeated narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnemyDefeatedColored(string enemyName, string playerName, string narrativeText)
            => EnemyDefeatedFormatter.Format(enemyName, playerName, narrativeText);

        // ============================================================================
        // Taunt/Quote Narratives
        // ============================================================================

        /// <summary>
        /// Formats player taunt narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatPlayerTauntColored(string playerName, string enemyName, string tauntText)
            => PlayerTauntFormatter.Format(playerName, enemyName, tauntText);

        /// <summary>
        /// Formats enemy taunt narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnemyTauntColored(string enemyName, string playerName, string tauntText)
            => EnemyTauntFormatter.Format(enemyName, playerName, tauntText);

        // ============================================================================
        // Generic Narrative
        // ============================================================================

        /// <summary>
        /// Formats a generic narrative message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatGenericNarrativeColored(string narrativeText, ColorPalette primaryColor = ColorPalette.Info)
            => GenericNarrativeFormatter.Format(narrativeText, primaryColor);
    }
}
