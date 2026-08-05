using System;

namespace RPGGame.Data
{
    /// <summary>
    /// Obsolete: use <see cref="RPGGame.MaterialTriggerStamp"/> (--stamp-material-triggers).
    /// </summary>
    [Obsolete("Use MaterialTriggerStamp.StampGameDataFiles.")]
    public static class AnimalSuffixTriggerStamp
    {
        public static (int Triggers, int Suffixes) StampGameDataFiles(string? gameDataDirectory = null)
        {
            int n = MaterialTriggerStamp.StampGameDataFiles(gameDataDirectory);
            return (n, 0);
        }
    }
}
