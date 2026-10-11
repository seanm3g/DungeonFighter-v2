using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RPGGame.Combat.Narrative
{
    /// <summary>Writes combat-scene JSON under <c>GameData/CombatLogs/</c>.</summary>
    public static class CombatSceneLogWriter
    {
        public const string FolderName = "CombatLogs";

        public static string GetDefaultDirectory()
        {
            string? gameData = GameConstants.GetSettingsDirectory();
            if (string.IsNullOrWhiteSpace(gameData))
            {
                string flavor = GameConstants.GetGameDataFilePath("FlavorText.json");
                gameData = Path.GetDirectoryName(flavor);
            }

            if (string.IsNullOrWhiteSpace(gameData))
                gameData = Path.Combine(Directory.GetCurrentDirectory(), GameConstants.GameDataDirectory);

            return Path.Combine(gameData, FolderName);
        }

        public static string Write(
            IReadOnlyList<CombatScenePacket> scenes,
            string? directory = null,
            DateTime? utcNow = null)
        {
            if (scenes == null || scenes.Count == 0)
                throw new ArgumentException("No combat scenes to write.", nameof(scenes));

            directory ??= GetDefaultDirectory();
            Directory.CreateDirectory(directory);

            var stamp = (utcNow ?? DateTime.UtcNow).ToString("yyyyMMdd-HHmmss");
            string hero = SanitizeFileToken(ResolveFileCharacter(scenes));
            string path = Path.Combine(directory, $"combat-log-{hero}-{stamp}.json");
            if (File.Exists(path))
                path = Path.Combine(directory, $"combat-log-{hero}-{stamp}-{Guid.NewGuid():N}.json");

            File.WriteAllText(path, CombatScenePacketBuilder.Serialize(scenes), Encoding.UTF8);
            return path;
        }

        public static string SanitizeFileToken(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "combat";

            var chars = name.Trim().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
            string token = new string(chars);
            while (token.Contains("--", StringComparison.Ordinal))
                token = token.Replace("--", "-", StringComparison.Ordinal);
            token = token.Trim('-');
            if (token.Length > 40)
                token = token[..40].Trim('-');
            return string.IsNullOrEmpty(token) ? "combat" : token;
        }

        private static string? ResolveFileCharacter(IReadOnlyList<CombatScenePacket> scenes)
        {
            foreach (var scene in scenes)
            {
                if (!string.IsNullOrWhiteSpace(scene?.Character))
                    return scene.Character;
            }

            return scenes[0].Environment;
        }
    }
}
