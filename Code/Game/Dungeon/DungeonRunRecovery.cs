namespace RPGGame
{
    using System;

    /// <summary>
    /// Shared post-dungeon recovery: clear run-scoped effects and restore the hero to full HP
    /// when leaving a dungeon early or after a successful clear (rewards / completion screen).
    /// </summary>
    public static class DungeonRunRecovery
    {
        /// <summary>
        /// Clears combat/search buffs, refreshes room armor, and heals to effective max HP.
        /// </summary>
        /// <returns>HP restored (0 if already full or player is null).</returns>
        public static int RestoreAfterLeavingDungeon(Character? player)
        {
            if (player == null)
                return 0;

            player.ClearAllTempEffects();
            player.RefreshRoomArmor();
            player.ClearDungeonSearchBuffs();

            int maxHp = player.GetEffectiveMaxHealth();
            int missing = maxHp - player.CurrentHealth;
            if (missing > 0)
                player.Heal(missing);

            return Math.Max(0, missing);
        }
    }
}
