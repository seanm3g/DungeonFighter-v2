using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using ActionDelegate = System.Action;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Helper class for wiring button click events.
    /// Eliminates repetitive event wiring code in SettingsEventWiring.
    /// </summary>
    public static class ButtonEventHandler
    {
        /// <summary>
        /// Wires up a button to invoke an action when clicked.
        /// </summary>
        /// <param name="button">The button control</param>
        /// <param name="action">The action to invoke</param>
        public static void WireAction(Button? button, ActionDelegate? action)
        {
            if (button == null || action == null)
                return;

            button.Click += (s, e) => action();
        }

        /// <summary>
        /// Wires up a button to invoke an async function when clicked.
        /// </summary>
        /// <param name="button">The button control</param>
        /// <param name="asyncAction">The async function to invoke</param>
        public static void WireAsyncAction(Button? button, Func<Task>? asyncAction)
        {
            if (button == null || asyncAction == null)
                return;

            button.Click += async (s, e) => await asyncAction();
        }

        /// <summary>
        /// Wires up a button to invoke an async function with a parameter when clicked.
        /// </summary>
        /// <param name="button">The button control</param>
        /// <param name="asyncAction">The async function to invoke</param>
        /// <param name="parameter">The parameter to pass to the function</param>
        public static void WireAsyncAction<T>(Button? button, Func<T, Task>? asyncAction, T parameter)
        {
            if (button == null || asyncAction == null)
                return;

            button.Click += async (s, e) => await asyncAction(parameter);
        }
    }
}
