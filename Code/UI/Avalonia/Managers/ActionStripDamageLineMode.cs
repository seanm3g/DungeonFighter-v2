namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Display mode for combo action strip swing lines (flat damage and wall-clock seconds).
    /// UI always uses <see cref="EffectiveWithComboAmp"/>; <see cref="BaseIntrinsic"/> remains for comparisons/tests.
    /// </summary>
    public enum ActionStripDamageLineMode : byte
    {
        /// <summary>Intrinsic action factors (sheet damage/speed; no pending slot DAMAGE_MOD/SPEED_MOD; no combo amp), shown as flat damage and seconds.</summary>
        BaseIntrinsic = 0,
        /// <summary>After pending slot DAMAGE_MOD/SPEED_MOD; damage multiplied by combo-slot amplification (TECH curve and chain position), shown as flat damage and seconds.</summary>
        EffectiveWithComboAmp = 1,
    }
}
