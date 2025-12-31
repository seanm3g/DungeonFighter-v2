using Avalonia.Controls;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Helper class for binding Slider and TextBox controls together.
    /// Eliminates repetitive event wiring code in SettingsEventWiring.
    /// </summary>
    public static class SliderTextBoxBinder
    {
        /// <summary>
        /// Binds a slider to a textbox so that slider value changes update the textbox.
        /// </summary>
        /// <param name="slider">The slider control</param>
        /// <param name="textBox">The textbox to update</param>
        /// <param name="format">Format string for the value (e.g., "F2" for 2 decimal places)</param>
        public static void BindSliderToTextBox(Slider slider, TextBox textBox, string format = "F2")
        {
            if (slider == null || textBox == null)
                return;

            slider.ValueChanged += (s, e) =>
            {
                textBox.Text = slider.Value.ToString(format);
            };
        }

        /// <summary>
        /// Binds a textbox to a slider so that textbox lost focus updates the slider with validation.
        /// </summary>
        /// <param name="textBox">The textbox control</param>
        /// <param name="slider">The slider to update</param>
        /// <param name="minValue">Minimum allowed value</param>
        /// <param name="maxValue">Maximum allowed value</param>
        /// <param name="format">Format string for the value (e.g., "F2" for 2 decimal places)</param>
        public static void BindTextBoxToSlider(TextBox textBox, Slider slider, double minValue, double maxValue, string format = "F2")
        {
            if (textBox == null || slider == null)
                return;

            textBox.LostFocus += (s, e) =>
            {
                if (double.TryParse(textBox.Text, out double value))
                {
                    value = Math.Clamp(value, minValue, maxValue);
                    slider.Value = value;
                    textBox.Text = value.ToString(format);
                }
            };
        }

        /// <summary>
        /// Binds a textbox to a slider with float validation (for animation settings).
        /// </summary>
        /// <param name="textBox">The textbox control</param>
        /// <param name="slider">The slider to update</param>
        /// <param name="minValue">Minimum allowed value</param>
        /// <param name="maxValue">Maximum allowed value</param>
        /// <param name="format">Format string for the value (e.g., "F1" for 1 decimal place)</param>
        public static void BindTextBoxToSliderFloat(TextBox textBox, Slider slider, float minValue, float maxValue, string format = "F1")
        {
            if (textBox == null || slider == null)
                return;

            textBox.LostFocus += (s, e) =>
            {
                if (float.TryParse(textBox.Text, out float value))
                {
                    value = Math.Clamp(value, minValue, maxValue);
                    slider.Value = value;
                    textBox.Text = value.ToString(format);
                }
            };
        }
    }
}
