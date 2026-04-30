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
        void SetupAnimationManager(Renderers.DungeonRenderer dungeonRenderer, System.Action<Character, List<Dungeon>>? reRenderCallback, GameStateManager? stateManager = null);
        /// <summary>When set, invoked on the undulation timer while <see cref="GameState.DungeonCompletion"/> is active.</summary>
        void SetDungeonCompletionReRenderCallback(System.Action? callback);
        void Dispose();
    }
}
