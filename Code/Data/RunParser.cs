using System;
using System.IO;
using RPGGame.Data;

namespace RPGGame.Data
{
    /// <summary>
    /// Simple runner program for the spreadsheet parser
    /// Can be called from the game or run as a standalone utility
    /// </summary>
    public class RunParser
    {
        public static void Main(string[] args)
        {
            string csvPath = args.Length > 0 ? args[0] : "GameData/actions-1-26-2026.xlsx.csv";
            string outputPath = args.Length > 1 ? args[1] : "GameData/Actions.json";
            
            SpreadsheetParserRunner.ParseAndGenerate(csvPath, outputPath);
        }
    }
}
