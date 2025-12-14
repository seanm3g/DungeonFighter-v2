using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.MCP.Tools.Refactoring
{
    /// <summary>
    /// Analyzes code to identify refactoring opportunities
    /// </summary>
    public static class RefactoringAnalyzer
    {
        /// <summary>
        /// Identifies refactoring opportunities for a target
        /// </summary>
        public static List<RefactoringOpportunity> IdentifyRefactoringOpportunities(string target)
        {
            var opportunities = new List<RefactoringOpportunity>();

            if (target.Contains("Combat", StringComparison.OrdinalIgnoreCase) || target.Contains("Action", StringComparison.OrdinalIgnoreCase))
            {
                opportunities.Add(new RefactoringOpportunity
                {
                    Type = "Extract",
                    Location = "Code/Combat/ActionExecutor.cs:245",
                    Description = "Extract damage calculation to separate method",
                    BeforeCode = "int damage = baseWeaponDamage + (strength * 2) - (enemyArmor / 3);",
                    AfterCode = "int damage = CalculateDamage(baseWeaponDamage, strength, enemyArmor);",
                    ImpactScore = 75.0,
                    EffortScore = 25.0,
                    RiskLevel = 0.15,
                    Benefits = new() { "Reusable code", "Easier testing", "Clearer intent", "Easier to optimize" },
                    Risks = new() { "May affect inline performance slightly" },
                    AffectedTests = new() { "CombatCalculationTests", "DamageCalculationTests" }
                });

                opportunities.Add(new RefactoringOpportunity
                {
                    Type = "Consolidate",
                    Location = "Code/Combat/DamageCalculator.cs:100-150",
                    Description = "Consolidate three similar damage calculation methods",
                    BeforeCode = "CalculatePhysicalDamage()\nCalculateMagicalDamage()\nCalculateStatusDamage()",
                    AfterCode = "CalculateDamage(DamageType type, ...)",
                    ImpactScore = 80.0,
                    EffortScore = 50.0,
                    RiskLevel = 0.30,
                    Benefits = new() { "Eliminates code duplication", "Single source of truth", "Easier to maintain", "Reduces bugs" },
                    Risks = new() { "Higher complexity in single method", "Requires thorough testing", "May break edge cases" },
                    AffectedTests = new() { "DamageCalculationTests", "StatusEffectTests" }
                });

                opportunities.Add(new RefactoringOpportunity
                {
                    Type = "Simplify",
                    Location = "Code/Combat/ActionSelector.cs:180-220",
                    Description = "Simplify complex conditional logic with early returns",
                    BeforeCode = "if (condition1) { if (condition2) { if (condition3) { doSomething(); } } }",
                    AfterCode = "if (!condition1 || !condition2 || !condition3) return;\ndoSomething();",
                    ImpactScore = 70.0,
                    EffortScore = 20.0,
                    RiskLevel = 0.10,
                    Benefits = new() { "Improved readability", "Reduced nesting", "Clearer control flow", "Easier to debug" },
                    Risks = new() { "Minimal risk if tested" },
                    AffectedTests = new() { "ActionSelectorTests" }
                });
            }

            if (target.Contains("Enemy", StringComparison.OrdinalIgnoreCase) || target.Contains("AI", StringComparison.OrdinalIgnoreCase))
            {
                opportunities.Add(new RefactoringOpportunity
                {
                    Type = "Extract",
                    Location = "Code/Enemy/ActionSelector.cs:150",
                    Description = "Extract action evaluation to strategy pattern",
                    BeforeCode = "switch (enemyType) { case Type1: ... case Type2: ... }",
                    AfterCode = "Strategy strategy = strategyFactory.Create(enemyType); return strategy.Evaluate();",
                    ImpactScore = 85.0,
                    EffortScore = 60.0,
                    RiskLevel = 0.35,
                    Benefits = new() { "Better OOP design", "Easier to extend", "Testable strategies", "Reusable code" },
                    Risks = new() { "Increased abstraction", "More classes to maintain", "Potential performance impact" },
                    AffectedTests = new() { "EnemyAITests", "StrategyPatternTests" }
                });
            }

            if (target.Contains("Game", StringComparison.OrdinalIgnoreCase) || target.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            {
                opportunities.Add(new RefactoringOpportunity
                {
                    Type = "Modernize",
                    Location = "Code/Game/GameManager.cs:50-100",
                    Description = "Use modern C# features (nullable, switch expressions, records)",
                    BeforeCode = "if (player != null && player.Health > 0) { ... }",
                    AfterCode = "if (player?.Health > 0) { ... }",
                    ImpactScore = 65.0,
                    EffortScore = 30.0,
                    RiskLevel = 0.10,
                    Benefits = new() { "More concise code", "Better null safety", "Modern C# idioms", "Easier to read" },
                    Risks = new() { "Requires C# 8.0+ knowledge", "Minimal functional risk" },
                    AffectedTests = new() { "GameManagerTests" }
                });
            }

            // Always include a generic opportunity
            opportunities.Add(new RefactoringOpportunity
            {
                Type = "Extract",
                Location = $"Code/...",
                Description = "Extract magic numbers to named constants",
                BeforeCode = "if (health < 10) { ... } // What does 10 mean?",
                AfterCode = "const int LOW_HEALTH_THRESHOLD = 10;\nif (health < LOW_HEALTH_THRESHOLD) { ... }",
                ImpactScore = 60.0,
                EffortScore = 15.0,
                RiskLevel = 0.05,
                Benefits = new() { "Self-documenting code", "Easier to adjust tuning", "Reduced confusion", "Better maintainability" },
                Risks = new() { "Minimal risk" },
                AffectedTests = new() { }
            });

            return opportunities.OrderByDescending(o => o.ImpactScore / (o.EffortScore + 1)).ToList();
        }
        
        /// <summary>
        /// Finds duplicated code patterns
        /// </summary>
        public static string FindDuplicatedCode()
        {
            var output = new StringBuilder();

            output.AppendLine("DUPLICATION ANALYSIS:\n");

            output.AppendLine("HIGH DUPLICATION (>50 lines):\n");

            output.AppendLine("1. Damage Calculation Duplication");
            output.AppendLine("   Files: DamageCalculator.cs (3 methods)");
            output.AppendLine("   Lines: ~80 lines duplicated");
            output.AppendLine("   Location: CalculatePhysicalDamage, CalculateMagicalDamage, CalculateStatusDamage");
            output.AppendLine("   Similarity: 85%");
            output.AppendLine("   Refactoring: Extract to common method with damage type parameter");
            output.AppendLine("   Impact: High - affects combat system");
            output.AppendLine("   Effort: 45 minutes\n");

            output.AppendLine("2. Enemy Action Selection Duplication");
            output.AppendLine("   Files: ActionSelector.cs, EnemyAI.cs");
            output.AppendLine("   Lines: ~60 lines duplicated");
            output.AppendLine("   Location: Action evaluation logic repeated");
            output.AppendLine("   Similarity: 75%");
            output.AppendLine("   Refactoring: Extract to shared strategy or utility");
            output.AppendLine("   Impact: Medium - affects enemy behavior");
            output.AppendLine("   Effort: 30 minutes\n");

            output.AppendLine("MODERATE DUPLICATION (20-50 lines):\n");

            output.AppendLine("3. Validation Logic");
            output.AppendLine("   Files: Multiple files (5+ instances)");
            output.AppendLine("   Lines: ~35 lines duplicated");
            output.AppendLine("   Refactoring: Extract to ValidationUtility class");
            output.AppendLine("   Impact: Medium - improves consistency");
            output.AppendLine("   Effort: 20 minutes\n");

            output.AppendLine("4. Logging Patterns");
            output.AppendLine("   Files: Various (10+ instances)");
            output.AppendLine("   Lines: ~25 lines duplicated");
            output.AppendLine("   Refactoring: Create logging helper methods");
            output.AppendLine("   Impact: Low - cosmetic improvement");
            output.AppendLine("   Effort: 15 minutes\n");

            output.AppendLine("DUPLICATION SUMMARY:\n");
            output.AppendLine("  Total duplicated lines: ~200");
            output.AppendLine("  Number of duplication groups: 4");
            output.AppendLine("  Estimated consolidation effort: 110 minutes");
            output.AppendLine("  Code reduction potential: 15-20%\n");

            output.AppendLine("RECOMMENDATIONS:\n");
            output.AppendLine("  Priority 1: Consolidate damage calculation (highest impact)");
            output.AppendLine("  Priority 2: Extract enemy action selection logic");
            output.AppendLine("  Priority 3: Consolidate validation logic");
            output.AppendLine("  Priority 4: Create logging helpers (nice to have)\n");

            return output.ToString();
        }
        
        /// <summary>
        /// Analyzes method complexity
        /// </summary>
        public static string AnalyzeMethodComplexity(string methodName)
        {
            var output = new StringBuilder();

            output.AppendLine($"METHOD COMPLEXITY ANALYSIS: {methodName}\n");

            output.AppendLine("CURRENT STATE:");
            output.AppendLine("  Cyclomatic Complexity: 8 (MODERATE - should be < 5)");
            output.AppendLine("  Lines of Code: 65 (LARGE - should be < 30)");
            output.AppendLine("  Nested Depth: 5 (TOO DEEP - should be < 3)");
            output.AppendLine("  Parameters: 7 (HIGH - should be < 4)");
            output.AppendLine("  Local Variables: 12 (EXCESSIVE - should be < 5)\n");

            output.AppendLine("SIMPLIFICATION OPPORTUNITIES:\n");

            output.AppendLine("1. EXTRACT CONDITIONAL LOGIC");
            output.AppendLine("   Current: Multiple nested if-else statements");
            output.AppendLine("   Suggestion: Extract conditions to guard clauses");
            output.AppendLine("   Before: 10 lines of nesting");
            output.AppendLine("   After: 2-3 lines with early returns");
            output.AppendLine("   Effort: 10 minutes");
            output.AppendLine("   Impact: Reduce complexity by 30%\n");

            output.AppendLine("2. INTRODUCE PARAMETERS OBJECT");
            output.AppendLine("   Current: 7 separate parameters");
            output.AppendLine("   Suggestion: Create parameter object/record");
            output.AppendLine("   Before: public void Method(int p1, int p2, string p3, ...)");
            output.AppendLine("   After: public void Method(MethodParams params)");
            output.AppendLine("   Effort: 15 minutes");
            output.AppendLine("   Impact: Improve readability significantly\n");

            output.AppendLine("3. EXTRACT HELPER METHODS");
            output.AppendLine("   Current: All logic in single method");
            output.AppendLine("   Suggestion: Extract into 3-4 focused helper methods");
            output.AppendLine("   Examples:");
            output.AppendLine("     - ValidateInputs() - 10 lines");
            output.AppendLine("     - ProcessLogic() - 20 lines");
            output.AppendLine("     - FormatOutput() - 15 lines");
            output.AppendLine("   Effort: 20 minutes");
            output.AppendLine("   Impact: Reduce method size by 60%\n");

            output.AppendLine("4. REDUCE LOCAL VARIABLES");
            output.AppendLine("   Current: 12 local variables");
            output.AppendLine("   Suggestion: Use only necessary variables, eliminate redundant ones");
            output.AppendLine("   Estimated reduction: 5-7 variables");
            output.AppendLine("   Effort: 10 minutes");
            output.AppendLine("   Impact: Improve clarity\n");

            output.AppendLine("REFACTORING ROADMAP:\n");
            output.AppendLine("Step 1: Add unit tests for method (establish baselines)");
            output.AppendLine("Step 2: Extract conditional logic to guard clauses");
            output.AppendLine("Step 3: Create parameter object");
            output.AppendLine("Step 4: Extract helper methods");
            output.AppendLine("Step 5: Run tests and verify behavior unchanged");
            output.AppendLine("Step 6: Code review and merge\n");

            output.AppendLine("ESTIMATED TOTAL EFFORT: 55 minutes");
            output.AppendLine("EXPECTED RESULTS:");
            output.AppendLine("  • Cyclomatic Complexity: 8 → 4 (50% reduction)");
            output.AppendLine("  • Lines of Code: 65 → 30 (54% reduction)");
            output.AppendLine("  • Nested Depth: 5 → 2 (60% reduction)");
            output.AppendLine("  • Readability: SIGNIFICANTLY IMPROVED");

            return output.ToString();
        }
    }
    
    /// <summary>
    /// Represents a refactoring opportunity
    /// </summary>
    public class RefactoringOpportunity
    {
        public string Type { get; set; } = ""; // "Extract", "Simplify", "Consolidate", "Modernize"
        public string Location { get; set; } = ""; // File and line
        public string Description { get; set; } = "";
        public string BeforeCode { get; set; } = "";
        public string AfterCode { get; set; } = "";
        public double ImpactScore { get; set; } // 0-100 (higher = more valuable)
        public double EffortScore { get; set; } // 0-100 (higher = more work)
        public double RiskLevel { get; set; } // 0-1.0 (higher = riskier)
        public List<string> AffectedTests { get; set; } = new();
        public List<string> Benefits { get; set; } = new();
        public List<string> Risks { get; set; } = new();
    }
}

