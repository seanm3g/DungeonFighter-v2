using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RPGGame
{
    public class FormulaEvaluator
    {
        public static double Evaluate(string formula, Dictionary<string, double> variables)
        {
            try
            {
                // Replace variables in the formula with their values
                string evaluatedFormula = formula;
                
                // Sort variables by length descending to avoid partial replacements
                var sortedVariables = new List<KeyValuePair<string, double>>(variables);
                sortedVariables.Sort((x, y) => y.Key.Length.CompareTo(x.Key.Length));
                
                foreach (var variable in sortedVariables)
                {
                    // Use word boundaries to ensure exact variable name matching
                    string pattern = @"\b" + Regex.Escape(variable.Key) + @"\b";
                    evaluatedFormula = Regex.Replace(evaluatedFormula, pattern, 
                        variable.Value.ToString(CultureInfo.InvariantCulture));
                }
                
                // Handle power operations (^) by converting to Math.Pow
                evaluatedFormula = ConvertPowerOperations(evaluatedFormula);
                
                // Use System.Data.DataTable.Compute for expression evaluation
                var table = new DataTable();
                var result = table.Compute(evaluatedFormula, null);
                
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error evaluating formula '{formula}': {ex.Message}");
                return 0.0; // Return default value on error
            }
        }
        
        private static string ConvertPowerOperations(string formula)
        {
            // Convert power operations from ^ to manual calculation
            // Since DataTable.Compute doesn't support Math.Pow, we'll handle simple cases
            var powerPattern = @"(\d+\.?\d*)\s*\^\s*(\d+\.?\d*)";
            
            while (Regex.IsMatch(formula, powerPattern))
            {
                formula = Regex.Replace(formula, powerPattern, match =>
                {
                    if (double.TryParse(match.Groups[1].Value, out double baseValue) &&
                        double.TryParse(match.Groups[2].Value, out double exponent))
                    {
                        double result = Math.Pow(baseValue, exponent);
                        return result.ToString(CultureInfo.InvariantCulture);
                    }
                    return match.Value; // Return original if parsing fails
                });
            }
            
            return formula;
        }
        
        public static bool ValidateFormula(string formula, Dictionary<string, double> testVariables)
        {
            try
            {
                Evaluate(formula, testVariables);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
