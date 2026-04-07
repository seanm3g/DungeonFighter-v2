using System.Collections.Generic;
using RPGGame.Handlers.Inventory;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI
{
    /// <summary>
    /// Tracks which inventory right-panel sequence row or pool row is under the pointer (for tooltips).
    /// </summary>
    public static class RightPanelActionHoverState
    {
        private static readonly object Lock = new object();
        private static int _sequenceIndex = -1;
        private static int _poolIndex = -1;

        public static int HoveredSequenceIndex
        {
            get
            {
                lock (Lock) return _sequenceIndex;
            }
        }

        public static int HoveredPoolIndex
        {
            get
            {
                lock (Lock) return _poolIndex;
            }
        }

        /// <summary>
        /// Updates hover from clickable elements (after <see cref="RPGGame.UI.Avalonia.Managers.ICanvasInteractionManager.SetHoverPosition"/>).
        /// When <paramref name="inventoryActive"/> is false, clears state.
        /// </summary>
        /// <returns>True if the stored hover target changed.</returns>
        public static bool UpdateFromClickables(IReadOnlyList<ClickableElement> elements, bool inventoryActive)
        {
            if (!inventoryActive)
                return Set(-1, -1);

            int seq = -1;
            int pool = -1;
            for (int i = 0; i < elements.Count; i++)
            {
                var el = elements[i];
                if (!el.IsHovered || string.IsNullOrEmpty(el.Value))
                    continue;
                if (!el.Value.StartsWith(ComboPointerInput.Prefix, System.StringComparison.Ordinal))
                    continue;
                if (!ComboPointerInput.TryParse(el.Value, out var kind, out int idx))
                    continue;
                if (kind == ComboPointerInput.Kind.SequenceRemove)
                {
                    seq = idx;
                    break;
                }
                if (kind == ComboPointerInput.Kind.PoolAdd)
                {
                    pool = idx;
                    break;
                }
            }

            return Set(seq, pool);
        }

        private static bool Set(int sequenceIndex, int poolIndex)
        {
            lock (Lock)
            {
                if (_sequenceIndex == sequenceIndex && _poolIndex == poolIndex)
                    return false;
                _sequenceIndex = sequenceIndex;
                _poolIndex = poolIndex;
                return true;
            }
        }
    }
}
