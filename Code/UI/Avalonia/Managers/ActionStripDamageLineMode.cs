namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Global display mode for combo action strip cards (all panels use the same mode).
    /// Toggled by left-clicking any filled strip panel when strip reorder drag is not active.
    /// </summary>
    public enum ActionStripDamageLineMode : byte
    {
        /// <summary>Intrinsic action damage % and speed % (sheet values; no pending slot DAMAGE_MOD/SPEED_MOD; no combo amp).</summary>
        BaseIntrinsic = 0,
        /// <summary>After pending slot DAMAGE_MOD/SPEED_MOD; damage % multiplied by combo-slot amplification (TECH curve and chain position).</summary>
        EffectiveWithComboAmp = 1,
    }
}
