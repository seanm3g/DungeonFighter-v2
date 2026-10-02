namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// LibVLC custom RGB32 frame sizing. Pitches/lines must be multiples of 32 or
    /// avcodec <c>get_buffer()</c> fails during decode.
    /// </summary>
    public static class NarrativeVideoFrameLayout
    {
        public const int BytesPerPixel = 4;
        public const uint Alignment = 32;

        /// <summary>Round up to the next multiple of <see cref="Alignment"/>.</summary>
        public static uint Align32(uint size)
        {
            if (size == 0)
                return Alignment;
            uint rem = size % Alignment;
            return rem == 0 ? size : size + (Alignment - rem);
        }

        /// <summary>
        /// Pitch (bytes per row) and line count for an RV32 callback frame.
        /// Does not change the visible pixel width/height — only padded storage.
        /// </summary>
        public static void ComputeRv32Storage(uint width, uint height, out uint pitches, out uint lines, out int bufferBytes)
        {
            pitches = Align32(width * BytesPerPixel);
            lines = Align32(height);
            bufferBytes = checked((int)(pitches * lines));
        }
    }
}
