using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.ColorSystem;
using RPGGame;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Interface for rendering message groups to different UI backends
    /// </summary>
    public interface IBlockRenderer
    {
        void RenderMessageGroups(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null);
        Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null);

        /// <summary>
        /// Two-beat attack headline: show setup (and reserved follow-up rows on canvas), then either
        /// play the sequence HUD or wait <paramref name="halfDelayMs"/>, then replace with the complete
        /// line (and commit action SFX / strip flash if the HUD did not), then fill follow-ups in place.
        /// </summary>
        Task RenderSetupPunchlineAsync(
            List<ColoredText> setup,
            List<ColoredText> completeHeadline,
            List<(List<ColoredText> segments, UIMessageType messageType)> followUps,
            int halfDelayMs,
            Character? character,
            UIMessageType headlineType,
            System.Func<System.Threading.Tasks.Task>? betweenBeats = null);
    }
}
