namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// F7 narrative text effects triggered when a named action appears in prose.
    /// </summary>
    public enum ActionTextEffectKind
    {
        None = 0,
        /// <summary>Dump the whole word, then push adjacent glyphs outward briefly.</summary>
        DistortionPulse = 1
    }
}
