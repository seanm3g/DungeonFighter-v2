using RPGGame.ActionInteractionLab;

namespace RPGGame.Tests.Unit.ActionInteractionLab
{
    /// <summary>
    /// The base combo strip is 1 slot (lootSystem.comboSequenceBaseMax), so lab tests that build 2+ action strips
    /// add sandbox slots the way the left panel does: through the recorded delta, so undo/replay rebuilds keep them.
    /// </summary>
    internal static class LabTestSlots
    {
        public static void Give(ActionInteractionLabSession? lab, int slots = 4)
        {
            if (lab == null)
                return;
            int delta = slots - lab.LabPlayer.ActionLabActionSlotBonus;
            if (delta <= 0)
                return;
            lab.LabPlayer.ActionLabActionSlotBonus += delta;
            lab.RecordLabPanelStatDelta("actionslots", delta);
        }
    }
}
