using System;
using System.Threading;
using Avalonia.Media;
using RPGGame.Config;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Runtime-selectable Avalonia typefaces for the ASCII game surface.
    /// F3 cycles VT323 → Sue Ellen Francisco → Bytesized → Courier New.
    /// Ctrl+/- adjusts UI zoom per preset (persisted in GeneralSettings.json) by resizing the main window.
    /// Bold is requested only when the preset ships a real bold face.
    /// </summary>
    public static class GameFonts
    {
        public enum Preset
        {
            Vt323 = 0,
            SueEllenFrancisco = 1,
            Bytesized = 2,
            CourierNew = 3,
        }

        public readonly record struct PresetInfo(
            Preset Id,
            string DisplayName,
            string FamilyName,
            FontWeight Weight);

        /// <summary>
        /// Embedded VT323 Regular (pixel monospace), with system VT323 / Courier New fallbacks.
        /// </summary>
        public const string Vt323FamilyName =
            "avares://DF/UI/Avalonia/Assets/Fonts/VT323-Regular.ttf#VT323, VT323, Courier New, monospace";

        /// <summary>
        /// Embedded Sue Ellen Francisco Regular. The TTF name table includes a trailing space on the family name.
        /// </summary>
        public const string SueEllenFranciscoFamilyName =
            "avares://DF/UI/Avalonia/Assets/Fonts/SueEllenFrancisco-Regular.ttf#Sue Ellen Francisco , Sue Ellen Francisco, Courier New, monospace";

        /// <summary>Embedded Bytesized Regular (pixel monospace).</summary>
        public const string BytesizedFamilyName =
            "avares://DF/UI/Avalonia/Assets/Fonts/Bytesized-Regular.ttf#Bytesized, Bytesized, Courier New, monospace";

        /// <summary>Previous ASCII default before VT323.</summary>
        public const string CourierNewFamilyName = "Courier New, monospace";

        /// <summary>Legacy alias for the default (VT323) family URI.</summary>
        public const string PrimaryFamilyName = Vt323FamilyName;

        public const double DefaultZoom = UiFontPreferences.DefaultZoom;
        public const double MinZoom = UiFontPreferences.MinZoom;
        public const double MaxZoom = UiFontPreferences.MaxZoom;
        public const double ZoomStep = 0.02;

        public static readonly PresetInfo[] Presets =
        {
            new(Preset.Vt323, "VT323", Vt323FamilyName, FontWeight.Normal),
            // Sue Ellen Francisco ships Regular only — no bold face in the file.
            new(Preset.SueEllenFrancisco, "Sue Ellen Francisco", SueEllenFranciscoFamilyName, FontWeight.Normal),
            // Bytesized ships Regular only.
            new(Preset.Bytesized, "Bytesized", BytesizedFamilyName, FontWeight.Normal),
            // Courier New has a real Bold face on Windows (courbd.ttf).
            new(Preset.CourierNew, "Courier New", CourierNewFamilyName, FontWeight.Bold),
        };

        private static int _presetIndex;
        private static readonly double[] ZoomByPreset = CreateDefaultZooms();

        /// <summary>Raised after <see cref="Cycle"/> / <see cref="SetPreset"/> changes the active face.</summary>
        public static event System.Action? ActiveFontChanged;

        /// <summary>Raised after the active preset's UI zoom changes.</summary>
        public static event System.Action? ActiveZoomChanged;

        public static Preset ActivePreset =>
            Presets[ClampIndex(Volatile.Read(ref _presetIndex))].Id;

        public static PresetInfo ActiveInfo =>
            Presets[ClampIndex(Volatile.Read(ref _presetIndex))];

        public static string ActiveFamilyName => ActiveInfo.FamilyName;

        public static FontFamily ActiveFamily => new(ActiveFamilyName);

        public static FontWeight ActiveWeight => ActiveInfo.Weight;

        public static Typeface ActiveTypeface =>
            new(ActiveFamily, FontStyle.Normal, ActiveWeight);

        /// <summary>UI zoom multiplier for the active font (1.0 = fill-height default).</summary>
        public static double ActiveZoom => GetZoom(ActivePreset);

        /// <summary>Legacy alias for the currently selected family (defaults to VT323).</summary>
        public static FontFamily Primary => ActiveFamily;

        public static double GetZoom(Preset preset)
        {
            int index = Array.FindIndex(Presets, p => p.Id == preset);
            if (index < 0)
                index = 0;
            return UiFontPreferences.ClampZoom(Volatile.Read(ref ZoomByPreset[index]));
        }

        public static void SetZoom(Preset preset, double zoom)
        {
            int index = Array.FindIndex(Presets, p => p.Id == preset);
            if (index < 0)
                return;

            double clamped = SnapZoom(zoom);
            double previous = Interlocked.Exchange(ref ZoomByPreset[index], clamped);
            if (Math.Abs(previous - clamped) < 1e-9)
                return;

            if (ActivePreset == preset)
                ActiveZoomChanged?.Invoke();
        }

        /// <summary>
        /// Steps the active font's UI zoom by <paramref name="direction"/> (±1 typical).
        /// Returns the resulting zoom (clamped).
        /// </summary>
        public static double AdjustActiveZoom(int direction)
        {
            if (direction == 0)
                return ActiveZoom;

            int index = ClampIndex(Volatile.Read(ref _presetIndex));
            double current = UiFontPreferences.ClampZoom(Volatile.Read(ref ZoomByPreset[index]));
            double next = SnapZoom(current + direction * ZoomStep);
            Volatile.Write(ref ZoomByPreset[index], next);
            ActiveZoomChanged?.Invoke();
            return next;
        }

        public static void SetPreset(Preset preset)
        {
            int index = Array.FindIndex(Presets, p => p.Id == preset);
            if (index < 0)
                index = 0;
            if (Interlocked.Exchange(ref _presetIndex, index) == index)
                return;
            ActiveFontChanged?.Invoke();
        }

        /// <summary>
        /// Advances VT323 → Sue Ellen Francisco → Bytesized → Courier New → VT323.
        /// Returns the newly active preset info.
        /// </summary>
        public static PresetInfo Cycle()
        {
            int next = (ClampIndex(Volatile.Read(ref _presetIndex)) + 1) % Presets.Length;
            Volatile.Write(ref _presetIndex, next);
            ActiveFontChanged?.Invoke();
            return Presets[next];
        }

        /// <summary>Loads active preset + per-font zoom from player-local general settings.</summary>
        public static void LoadFromStore()
        {
            ApplyPreferences(GeneralSettingsStore.Load().UiFontPreferences);
        }

        /// <summary>Writes the current preset + per-font zoom map to general settings.</summary>
        public static void SaveToStore()
        {
            GeneralSettingsStore.SaveUiFontPreferences(CapturePreferences());
        }

        public static void ApplyPreferences(UiFontPreferences prefs)
        {
            prefs ??= new UiFontPreferences();
            prefs.ValidateAndFix();

            if (Enum.TryParse(prefs.ActivePreset, ignoreCase: true, out Preset preset))
                SetPreset(preset);
            else
                SetPreset(Preset.Vt323);

            for (int i = 0; i < Presets.Length; i++)
            {
                double zoom = prefs.GetZoom(Presets[i].Id.ToString());
                Volatile.Write(ref ZoomByPreset[i], SnapZoom(zoom));
            }

            ActiveZoomChanged?.Invoke();
        }

        public static UiFontPreferences CapturePreferences()
        {
            var prefs = new UiFontPreferences
            {
                ActivePreset = ActivePreset.ToString()
            };

            for (int i = 0; i < Presets.Length; i++)
                prefs.SetZoom(Presets[i].Id.ToString(), Volatile.Read(ref ZoomByPreset[i]));

            prefs.ValidateAndFix();
            return prefs;
        }

        /// <summary>Test hook: restore default zoom map without touching disk.</summary>
        internal static void ResetZoomsForTests()
        {
            for (int i = 0; i < ZoomByPreset.Length; i++)
                Volatile.Write(ref ZoomByPreset[i], DefaultZoom);
        }

        private static double[] CreateDefaultZooms()
        {
            var zooms = new double[Presets.Length];
            for (int i = 0; i < zooms.Length; i++)
                zooms[i] = DefaultZoom;
            return zooms;
        }

        private static double SnapZoom(double zoom)
        {
            double clamped = UiFontPreferences.ClampZoom(zoom);
            double stepped = Math.Round(clamped / ZoomStep) * ZoomStep;
            return UiFontPreferences.ClampZoom(stepped);
        }

        private static int ClampIndex(int index)
        {
            if (index < 0 || index >= Presets.Length)
                return 0;
            return index;
        }
    }
}
