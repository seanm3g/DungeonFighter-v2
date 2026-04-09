namespace RPGGame.ActionInteractionLab
{
    /// <summary>One executed lab turn for deterministic replay and undo.</summary>
    public readonly struct LabStep
    {
        public LabStep(int d20, string forcedActionName)
        {
            D20 = d20;
            ForcedActionName = forcedActionName;
        }

        public int D20 { get; }
        public string ForcedActionName { get; }
    }
}
