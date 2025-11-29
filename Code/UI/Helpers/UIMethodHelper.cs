using System;
using RPGGame.UI;

namespace RPGGame.UI.Helpers
{
    /// <summary>
    /// Helper methods for UIManager to reduce code duplication
    /// </summary>
    internal static class UIMethodHelper
    {
        /// <summary>
        /// Executes an action if UI output is not disabled
        /// </summary>
        public static void ExecuteIfEnabled(bool disableFlag, System.Action action)
        {
            if (!disableFlag)
                action();
        }

        /// <summary>
        /// Executes an action if UI output is not disabled, returns default if disabled
        /// </summary>
        public static T ExecuteIfEnabled<T>(bool disableFlag, Func<T> action, T defaultValue = default!)
        {
            return disableFlag ? defaultValue : action();
        }
    }
}

