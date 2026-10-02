using System;
using System.Threading;
using Avalonia.Media;
using RPGGame.Config;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Runtime-selectable Avalonia typefaces for the ASCII game surface.
    /// F3 cycles VT323 → Noplato Mono → Pixelzone → Bytesized → Courier New.
    /// Ctrl+/- adjusts UI zoom per preset (persisted in GeneralSettings.json), scaling glyphs inside a fixed window.
    /// Ctrl+0 resets to the per-font default; Ctrl+Shift+0 saves the current size as that default.
    /// Bold is requested only when the preset ships a real bold face.
    /// </summary>
    public static class GameFonts
    {
        public enum Preset
        {
            Vt323 = 0,
            NoplatoMono = 1,
            Pixelzone = 2,
            Bytesized = 3,
            CourierNew = 4,
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
        /// Embedded Noplato Mono Regular. TTF family name is "Noplato Demo Mono".
        /// </summary>
        public const string NoplatoMonoFamilyName =
            "avares://DF/UI/Avalonia/Assets/Fonts/NoplatoMono.ttf#Noplato Demo Mono, Noplato Demo Mono, Courier New, monospace";

        /// <summary>Embedded Pixelzone Regular (pixel).</summary>
        public const string PixelzoneFamilyName =
            "avares://DF/UI/Avalonia/Assets/Fonts/Pixelzone.ttf#Pixelzone, Pixelzone, Courier New, monospace";

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
            // Noplato Mono ships Regular only.
            new(Preset.NoplatoMono, "Noplato Mono", NoplatoMonoFamilyName, FontWeight.Normal),
            // Pixelzone ships Regular only.
            new(Preset.Pixelzone, "Pixelzone", PixelzoneFamilyName, FontWeight.Normal),
            // Bytesized ships Regular only.
            new(Preset.Bytesized, "Bytesized", BytesizedFamilyName, FontWeight.Normal),
            // Courier New has a real Bold face on Windows (courbd.ttf).
            new(Preset.CourierNew, "Courier New", CourierNewFamilyName, FontWeight.Bold),
        };

        private static int _presetIndex;
        private static readonly double[] ZoomByPreset = CreateDefaultZooms();
        private static readonly double[] DefaultZoomByPreset = CreateDefaultZooms();

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

        /// <summary>UI zoom multiplier for the active font (1.0 = fill-height baseline inside the current window).</summary>
        public static double ActiveZoom => GetZoom(ActivePreset);

        /// <summary>Per-font default zoom for the active preset (Ctrl+0 reset target).</summary>
        public static double ActiveDefaultZoom => GetDefaultZoom(ActivePreset);

        /// <summary>Legacy alias for the currently selected family (defaults to VT323).</summary>
        public static FontFamily Primary => ActiveFamily;

        public static double GetZoom(Preset preset)
        {
            int index = Array.FindIndex(Presets, p => p.Id == preset);
            if (index < 0)
                index = 0;
            return UiFontPreferences.ClampZoom(Volatile.Read(ref ZoomByPreset[index]));
        }

        public static double GetDefaultZoom(Preset preset)
        {
            int index = Array.FindIndex(Presets, p => p.Id == preset);
            if (index < 0)
                index = 0;
            return UiFontPreferences.ClampZoom(Volatile.Read(ref DefaultZoomByPreset[index]));
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

        public static void SetDefaultZoom(Preset preset, double zoom)
        {
            int index = Array.FindIndex(Presets, p => p.Id == preset);
            if (index < 0)
                return;

            Volatile.Write(ref DefaultZoomByPreset[index], SnapZoom(zoom));
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

        /// <summary>
        /// Resets the active font's current zoom to its per-font default (Ctrl+0).
        /// Returns the resulting zoom.
        /// </summary>
        public static double ResetActiveZoomToDefault()
        {
            int index = ClampIndex(Volatile.Read(ref _presetIndex));
            double defaults = UiFontPreferences.ClampZoom(Volatile.Read(ref DefaultZoomByPreset[index]));
            double snapped = SnapZoom(defaults);
            double previous = Interlocked.Exchange(ref ZoomByPreset[index], snapped);
            if (Math.Abs(previous - snapped) >= 1e-9)
                ActiveZoomChanged?.Invoke();
            return snapped;
        }

        /// <summary>
        /// Saves the active font's current zoom as its per-font default (Ctrl+Shift+0).
        /// Returns the saved default zoom.
        /// </summary>
        public static double SetActiveZoomAsDefault()
        {
            int index = ClampIndex(Volatile.Read(ref _presetIndex));
            double current = SnapZoom(Volatile.Read(ref ZoomByPreset[index]));
            Volatile.Write(ref DefaultZoomByPreset[index], current);
            return current;
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
        /// Advances VT323 → Noplato Mono → Pixelzone → Bytesized → Courier New → VT323.
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
                string key = Presets[i].Id.ToString();
                Volatile.Write(ref ZoomByPreset[i], SnapZoom(prefs.GetZoom(key)));
                Volatile.Write(ref DefaultZoomByPreset[i], SnapZoom(prefs.GetDefaultZoom(key)));
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
            {
                string key = Presets[i].Id.ToString();
                prefs.SetZoom(key, Volatile.Read(ref ZoomByPreset[i]));
                prefs.SetDefaultZoom(key, Volatile.Read(ref DefaultZoomByPreset[i]));
            }

            prefs.ValidateAndFix();
            return prefs;
        }

        /// <summary>Test hook: restore default zoom map without touching disk.</summary>
        internal static void ResetZoomsForTests()
        {
            for (int i = 0; i < ZoomByPreset.Length; i++)
            {
                Volatile.Write(ref ZoomByPreset[i], DefaultZoom);
                Volatile.Write(ref DefaultZoomByPreset[i], DefaultZoom);
            }
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
