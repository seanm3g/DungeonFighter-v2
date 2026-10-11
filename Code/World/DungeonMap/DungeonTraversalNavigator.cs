using System;
using System.Threading.Tasks;

namespace RPGGame.World.DungeonMap
{
    /// <summary>
    /// Auto-walks the hero from the current cell to the next content room using the active strategy.
    /// </summary>
    public static class DungeonTraversalNavigator
    {
        public const int DefaultStepDelayMs = 120;
        public const int MaxStepsPerApproach = 256;

        /// <summary>
        /// Steps until the hero stands on the next content room cell.
        /// Invokes optional <paramref name="onStep"/> after each move (pacing / diagnostics only).
        /// Callers should not full-repaint the dungeon HUD per step — there is no on-screen map,
        /// and repeated layout paints flicker the side panels. Paint once on room arrival instead.
        /// </summary>
        public static async Task<bool> WalkToNextContentAsync(
            DungeonTraversalState state,
            Func<Task>? onStep = null,
            int maxSteps = MaxStepsPerApproach)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.TryGetNextContentTarget(out int tx, out int ty))
                return false;

            // Already standing on the target content cell
            if (state.HeroX == tx && state.HeroY == ty)
            {
                state.SetInteractionAtHero();
                if (onStep != null)
                    await onStep();
                return true;
            }

            var stepper = DungeonPathStepperFactory.Create(state.CurrentStrategy);
            int steps = 0;
            while (steps < maxSteps)
            {
                if (state.HeroX == tx && state.HeroY == ty)
                {
                    state.SetInteractionAtHero();
                    if (onStep != null)
                        await onStep();
                    return true;
                }

                if (!stepper.TryGetNextStep(state, tx, ty, out int nx, out int ny))
                    break;

                state.MoveHeroTo(nx, ny);
                steps++;

                if (state.HeroX == tx && state.HeroY == ty)
                    state.SetInteractionAtHero();
                else
                    state.ClearInteraction();

                if (onStep != null)
                    await onStep();
            }

            // Failsafe: snap to target if Random wander exceeded budget
            if (state.HeroX != tx || state.HeroY != ty)
            {
                state.MoveHeroTo(tx, ty);
                state.SetInteractionAtHero();
                if (onStep != null)
                    await onStep();
            }

            return state.HeroX == tx && state.HeroY == ty;
        }
    }
}
