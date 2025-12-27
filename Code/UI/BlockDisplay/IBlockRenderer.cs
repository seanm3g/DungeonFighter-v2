using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Interface for rendering message groups to different UI backends
    /// </summary>
    public interface IBlockRenderer
    {
        void RenderMessageGroups(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null);
        Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null);
    }
}

