using System;

namespace RPGGame.Actions.RollModification
{
    public enum MultiDiceLuckMode
    {
        None,
        Advantage,
        Disadvantage,
        Cancelled
    }

    /// <summary>
    /// Captures 2d20 luck/unluck dice for combat log roll footers.
    /// </summary>
    public readonly struct MultiDiceRollDetail
    {
        public static MultiDiceRollDetail None => default;

        public MultiDiceLuckMode Mode { get; }
        public int HighDie { get; }
        public int LowDie { get; }

        public bool HasLuckDetail => Mode != MultiDiceLuckMode.None;

        private MultiDiceRollDetail(MultiDiceLuckMode mode, int highDie, int lowDie)
        {
            Mode = mode;
            HighDie = highDie;
            LowDie = lowDie;
        }

        public static MultiDiceRollDetail FromTwoDice(MultiDiceLuckMode mode, int die1, int die2)
        {
            int high = Math.Max(die1, die2);
            int low = Math.Min(die1, die2);
            return new MultiDiceRollDetail(mode, high, low);
        }

        public static MultiDiceRollDetail Cancelled(int keptRoll) =>
            new MultiDiceRollDetail(MultiDiceLuckMode.Cancelled, keptRoll, keptRoll);
    }
}
