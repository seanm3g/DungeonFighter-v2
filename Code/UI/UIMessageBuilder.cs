using System;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Builds formatted UI messages for various game events
    /// Specializes in combat, healing, and status effect messages
    /// </summary>
    public class UIMessageBuilder
    {
        private readonly UIColoredTextManager _coloredTextManager;

        public UIMessageBuilder(UIColoredTextManager coloredTextManager)
        {
            _coloredTextManager = coloredTextManager;
        }

        /// <summary>
        /// Creates and displays a combat message using the new color system
        /// </summary>
        public void WriteCombatMessage(string attacker, string action, string target, int? damage = null,
            bool isCritical = false, bool isMiss = false, bool isBlock = false, bool isDodge = false)
        {
            var builder = new ColoredTextBuilder();

            // Attacker name
            builder.Add(attacker, ColorPalette.Player);
            builder.AddSpace();

            // Action
            if (isMiss)
            {
                builder.AddWithPattern(action, "miss");
            }
            else if (isBlock)
            {
                builder.AddWithPattern(action, "block");
            }
            else if (isDodge)
            {
                builder.AddWithPattern(action, "dodge");
            }
            else
            {
                builder.AddWithPattern(action, "damage");
            }

            builder.AddSpace();

            // Target name
            builder.Add(target, ColorPalette.Enemy);

            // Damage amount
            if (damage.HasValue)
            {
                builder.AddSpace();
                builder.Add(damage.Value.ToString(), isCritical ? ColorPalette.Critical : ColorPalette.Damage);
                builder.AddSpace();
                builder.AddWithPattern("damage", "damage");
            }

            _coloredTextManager.WriteLineColoredTextBuilder(builder, UIMessageType.Combat);
        }

        /// <summary>
        /// Creates and displays a healing message using the new color system
        /// </summary>
        public void WriteHealingMessage(string healer, string target, int amount)
        {
            var builder = new ColoredTextBuilder();

            builder.Add(healer, ColorPalette.Player);
            builder.AddSpace();
            builder.AddWithPattern("heals", "healing");
            builder.AddSpace();
            builder.Add(target, ColorPalette.Player);
            builder.AddSpace();
            builder.Add(amount.ToString(), ColorPalette.Healing);
            builder.AddSpace();
            builder.AddWithPattern("health", "healing");

            _coloredTextManager.WriteLineColoredTextBuilder(builder, UIMessageType.Combat);
        }

        /// <summary>
        /// Creates and displays a status effect message using the new color system
        /// </summary>
        public void WriteStatusEffectMessage(string target, string effect, bool isApplied = true)
        {
            var builder = new ColoredTextBuilder();

            builder.Add(target, ColorPalette.Player);
            builder.AddSpace();

            if (isApplied)
            {
                builder.AddWithPattern("is affected by", "warning");
            }
            else
            {
                builder.AddWithPattern("is no longer affected by", "success");
            }

            builder.AddSpace();
            builder.AddWithPattern(effect, "warning");

            _coloredTextManager.WriteLineColoredTextBuilder(builder, UIMessageType.Combat);
        }
    }
}

