using RPGGame;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Interface for managing canvas animations
    /// </summary>
    public interface ICanvasAnimationManager
    {
        void StartDungeonSelectionAnimation(Character player, List<Dungeon> dungeons);
        void StopDungeonSelectionAnimation();
        void SetupAnimationManager(Renderers.DungeonRenderer dungeonRenderer, System.Action<Character, List<Dungeon>> reRenderCallback);
        void Dispose();
    }
}
