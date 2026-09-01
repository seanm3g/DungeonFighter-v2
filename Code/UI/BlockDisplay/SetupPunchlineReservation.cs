using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Reserves blank follow-up rows with the setup headline so later lines fill in place
    /// instead of appending (which would scroll the block up near the bottom of the log).
    /// </summary>
    public static class SetupPunchlineReservation
    {
        /// <summary>
        /// Follow-ups that occupy a reserved row (null entries are skipped, matching batch write).
        /// </summary>
        public static List<(List<ColoredText> segments, UIMessageType messageType)> NonNullFollowUps(
            List<(List<ColoredText> segments, UIMessageType messageType)>? followUps)
        {
            var result = new List<(List<ColoredText> segments, UIMessageType messageType)>();
            if (followUps == null)
                return result;

            foreach (var item in followUps)
            {
                if (item.segments != null)
                    result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Setup line plus one empty placeholder per reserved follow-up, dumped together before the wait.
        /// </summary>
        public static List<(List<ColoredText> segments, UIMessageType messageType)> BuildInitialDump(
            List<ColoredText> setup,
            UIMessageType headlineType,
            List<(List<ColoredText> segments, UIMessageType messageType)> reservedFollowUps)
        {
            var dump = new List<(List<ColoredText> segments, UIMessageType messageType)>
            {
                (setup, headlineType)
            };

            if (reservedFollowUps == null)
                return dump;

            foreach (var (_, messageType) in reservedFollowUps)
                dump.Add((new List<ColoredText>(), messageType));

            return dump;
        }

        /// <summary>
        /// Offset from the end of the buffer for the headline after the initial dump (0 = last).
        /// </summary>
        public static int HeadlineOffsetFromEnd(int reservedFollowUpCount) => reservedFollowUpCount;

        /// <summary>
        /// Offset from the end for follow-up <paramref name="followUpIndex"/> (0 = first follow-up).
        /// </summary>
        public static int FollowUpOffsetFromEnd(int reservedFollowUpCount, int followUpIndex)
            => reservedFollowUpCount - 1 - followUpIndex;
    }
}
