using System;
using RPGGame;

namespace RPGGame.UI.Avalonia.Transitions
{
    /// <summary>
    /// Context record for screen transitions.
    /// Makes screen transitions explicit and testable by carrying all required parameters.
    /// </summary>
    public record ScreenTransitionContext
    {
        /// <summary>
        /// The target game state for this transition
        /// </summary>
        public GameState TargetState { get; init; }
        
        /// <summary>
        /// The character to display (if any)
        /// </summary>
        public Character? Character { get; init; }
        
        /// <summary>
        /// Whether to clear enemy context when transitioning
        /// </summary>
        public bool ClearEnemyContext { get; init; } = true;
        
        /// <summary>
        /// Whether to clear dungeon/room context when transitioning
        /// </summary>
        public bool ClearDungeonContext { get; init; } = false;
        
        /// <summary>
        /// Action to perform the actual screen rendering
        /// </summary>
        public Action<CanvasUICoordinator> RenderAction { get; init; }
        
        /// <summary>
        /// Creates a new screen transition context
        /// </summary>
        public ScreenTransitionContext(
            GameState targetState,
            Action<CanvasUICoordinator> renderAction,
            Character? character = null,
            bool clearEnemyContext = true,
            bool clearDungeonContext = false)
        {
            TargetState = targetState;
            RenderAction = renderAction ?? throw new ArgumentNullException(nameof(renderAction));
            Character = character;
            ClearEnemyContext = clearEnemyContext;
            ClearDungeonContext = clearDungeonContext;
        }
    }
}

