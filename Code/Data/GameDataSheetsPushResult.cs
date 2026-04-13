using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>Human-readable lines describing what each push step did (for UI / CLI).</summary>
    public sealed class GameDataSheetsPushResult
    {
        public List<string> SummaryLines { get; } = new List<string>();

        public void AddLine(string line) => SummaryLines.Add(line);
    }
}
