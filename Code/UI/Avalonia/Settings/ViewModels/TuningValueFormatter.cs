using System;
using System.Globalization;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>Formats tuning slider values for text box display (shared by tuning ViewModels and tests).</summary>
    public static class TuningValueFormatter
    {
        public static string Format(double value, double minimum, double maximum, double tickFrequency)
        {
            bool asInteger = tickFrequency >= 1.0 && maximum - minimum >= 1.0;
            if (asInteger || maximum >= 100)
                return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        public static string ClampAndFormat(double value, double minimum, double maximum, double tickFrequency)
        {
            if (minimum > maximum)
                return string.Empty;

            double clamped = Math.Clamp(value, minimum, maximum);
            return Format(clamped, minimum, maximum, tickFrequency);
        }

        public static bool TryParse(string? text, out double value)
        {
            return double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
