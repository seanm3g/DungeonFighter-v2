using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Result of data validation containing errors, warnings, and statistics
    /// </summary>
    public class ValidationResult
    {
        public List<ValidationIssue> Errors { get; private set; }
        public List<ValidationIssue> Warnings { get; private set; }
        public Dictionary<string, int> Statistics { get; private set; }

        public ValidationResult()
        {
            Errors = new List<ValidationIssue>();
            Warnings = new List<ValidationIssue>();
            Statistics = new Dictionary<string, int>();
        }

        public bool IsValid => Errors.Count == 0;

        public int TotalIssues => Errors.Count + Warnings.Count;

        public void AddError(string file, string entity, string field, string message)
        {
            Errors.Add(new ValidationIssue
            {
                File = file,
                Entity = entity,
                Field = field,
                Message = message
            });
        }

        public void AddWarning(string file, string entity, string field, string message)
        {
            Warnings.Add(new ValidationIssue
            {
                File = file,
                Entity = entity,
                Field = field,
                Message = message
            });
        }

        public void IncrementStatistic(string key)
        {
            if (Statistics.ContainsKey(key))
            {
                Statistics[key]++;
            }
            else
            {
                Statistics[key] = 1;
            }
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Validation Summary:");
            sb.AppendLine($"  Errors: {Errors.Count}");
            sb.AppendLine($"  Warnings: {Warnings.Count}");
            sb.AppendLine($"  Total Issues: {TotalIssues}");
            
            if (Statistics.Count > 0)
            {
                sb.AppendLine($"  Statistics:");
                foreach (var stat in Statistics)
                {
                    sb.AppendLine($"    {stat.Key}: {stat.Value}");
                }
            }

            return sb.ToString();
        }

        public void Merge(ValidationResult other)
        {
            if (other == null) return;

            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);

            foreach (var stat in other.Statistics)
            {
                if (Statistics.ContainsKey(stat.Key))
                {
                    Statistics[stat.Key] += stat.Value;
                }
                else
                {
                    Statistics[stat.Key] = stat.Value;
                }
            }
        }
    }

    /// <summary>
    /// Represents a single validation issue (error or warning)
    /// </summary>
    public class ValidationIssue
    {
        public string File { get; set; } = "";
        public string Entity { get; set; } = "";
        public string Field { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
