namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Global display mode for combo action strip cards (all panels use the same mode).
    /// Toggled by left-clicking any filled strip panel when strip reorder drag is not active.
    /// Both modes render as calculated flat damage and wall-clock seconds (not sheet %).
    /// </summary>
    public enum ActionStripDamageLineMode : byte
    {
        /// <summary>Intrinsic action factors (sheet damage/speed; no pending slot DAMAGE_MOD/SPEED_MOD; no combo amp), shown as flat damage and seconds.</summary>
        BaseIntrinsic = 0,
        /// <summary>After pending slot DAMAGE_MOD/SPEED_MOD; damage multiplied by combo-slot amplification (TECH curve and chain position), shown as flat damage and seconds.</summary>
        EffectiveWithComboAmp = 1,
    }
}
