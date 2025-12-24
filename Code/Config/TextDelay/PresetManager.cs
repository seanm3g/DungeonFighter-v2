namespace RPGGame.Config.TextDelay
{
    /// <summary>
    /// Handles preset management for text delay configuration
    /// Extracted from TextDelayConfiguration to separate preset logic
    /// </summary>
    public static class PresetManager
    {
        /// <summary>
        /// Gets a chunked text reveal preset configuration
        /// </summary>
        public static ChunkedTextRevealPreset? GetChunkedTextRevealPreset(string presetName, TextDelayLoader.TextDelayConfigData configData)
        {
            return configData.ChunkedTextRevealPresets.TryGetValue(presetName, out var preset) ? preset : null;
        }

        /// <summary>
        /// Gets the progressive menu delays configuration
        /// </summary>
        public static ProgressiveMenuDelaysConfig GetProgressiveMenuDelays(TextDelayLoader.TextDelayConfigData configData)
        {
            return configData.ProgressiveMenuDelays;
        }

        /// <summary>
        /// Sets a chunked text reveal preset
        /// </summary>
        public static void SetChunkedTextRevealPreset(string presetName, ChunkedTextRevealPreset preset, TextDelayLoader.TextDelayConfigData configData)
        {
            configData.ChunkedTextRevealPresets[presetName] = preset;
        }

        /// <summary>
        /// Sets the progressive menu delays
        /// </summary>
        public static void SetProgressiveMenuDelays(ProgressiveMenuDelaysConfig config, TextDelayLoader.TextDelayConfigData configData)
        {
            configData.ProgressiveMenuDelays = config;
        }
    }
}

