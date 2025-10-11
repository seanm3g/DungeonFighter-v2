using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Result of a generation operation with detailed status information
    /// </summary>
    public class GenerationResult
    {
        public FileGenerationResult ArmorResult { get; set; } = new();
        public FileGenerationResult WeaponResult { get; set; } = new();
        
        public bool HasErrors => ArmorResult.HasErrors || WeaponResult.HasErrors;
        public bool HasWarnings => ArmorResult.HasWarnings || WeaponResult.HasWarnings;
        public int TotalFilesProcessed => (ArmorResult.Processed ? 1 : 0) + (WeaponResult.Processed ? 1 : 0);
        public int TotalFilesUpdated => (ArmorResult.Updated ? 1 : 0) + (WeaponResult.Updated ? 1 : 0);
        
        public void LogSummary()
        {
            if (HasErrors)
            {
                Console.WriteLine($"Generation completed with {TotalFilesProcessed} files processed, {TotalFilesUpdated} updated, but with errors.");
            }
            else if (HasWarnings)
            {
                Console.WriteLine($"Generation completed with {TotalFilesProcessed} files processed, {TotalFilesUpdated} updated, but with warnings.");
            }
            else
            {
                Console.WriteLine($"Generation completed successfully: {TotalFilesProcessed} files processed, {TotalFilesUpdated} updated.");
            }
        }
    }

    /// <summary>
    /// Result of generating a single file
    /// </summary>
    public class FileGenerationResult
    {
        public bool Processed { get; set; }
        public bool Updated { get; set; }
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public List<string> Messages { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        
        public void AddMessage(string message) => Messages.Add(message);
        public void AddError(string error) { Errors.Add(error); HasErrors = true; }
        public void AddWarning(string warning) { Warnings.Add(warning); HasWarnings = true; }
    }
}
