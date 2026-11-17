namespace RPGGame
{
    /// <summary>
    /// Status-trigger skills (Blood Frenzy, Pinning Drill, Toxic Insight, Kindled Mind) are rule nodes since 2026-11-16:
    /// <c>ONSTATUSAPPLY:bleed -> BUFF STR 6 FOR dungeon</c> (SkillTrees.json crossWire). The count of attribute points they
    /// grant lives on <see cref="CrossWires.StatusBuffPointsGranted"/>.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        /// <summary>Attribute points ONSTATUSAPPLY rules have granted, never reset (sim fight logs diff it).</summary>
        public static long StatusSkillPointsGranted => CrossWires.StatusBuffPointsGranted;
    }
}
