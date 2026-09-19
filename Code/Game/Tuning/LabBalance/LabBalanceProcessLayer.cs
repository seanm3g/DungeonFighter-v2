namespace RPGGame.Tuning.LabBalance
{
    /// <summary>
    /// Foundational balance ladder used by Action Lab Balance window.
    /// Distinct from <see cref="CombatTuningLayer"/> (registry grouping).
    /// </summary>
    public enum LabBalanceProcessLayer
    {
        CombatEquation,
        Feel,
        LevelCurve,
        WeaponParity,
        EnemyRoster,
        GearInjection,
        DungeonAttrition,
        PlaythroughCheck
    }
}
