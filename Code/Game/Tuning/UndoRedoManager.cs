using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame.Tuning
{
    public static class UndoRedoManager
    {
        private static readonly Stack<GameConfiguration> UndoStack = new();
        private static readonly Stack<GameConfiguration> RedoStack = new();
        private const int MaxUndoHistory = 10;

        public static void SaveState()
        {
            var config = GameConfiguration.Instance;
            var json = System.Text.Json.JsonSerializer.Serialize(config);
            var copy = System.Text.Json.JsonSerializer.Deserialize<GameConfiguration>(json);
            
            if (copy != null)
            {
                UndoStack.Push(copy);
                if (UndoStack.Count > MaxUndoHistory)
                {
                    var list = UndoStack.ToList();
                    list.RemoveAt(0);
                    UndoStack.Clear();
                    foreach (var item in list)
                    {
                        UndoStack.Push(item);
                    }
                }
                RedoStack.Clear();
            }
        }

        public static bool Undo()
        {
            if (UndoStack.Count == 0)
            {
                return false;
            }

            try
            {
                var current = GameConfiguration.Instance;
                var json = System.Text.Json.JsonSerializer.Serialize(current);
                var copy = System.Text.Json.JsonSerializer.Deserialize<GameConfiguration>(json);
                if (copy != null)
                {
                    RedoStack.Push(copy);
                }

                var previous = UndoStack.Pop();
                AdjustmentExecutor.ApplyConfiguration(previous);
                ScrollDebugLogger.Log("UndoRedoManager: Undo performed");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"UndoRedoManager: Error undoing: {ex.Message}");
                return false;
            }
        }

        public static bool Redo()
        {
            if (RedoStack.Count == 0)
            {
                return false;
            }

            try
            {
                var current = GameConfiguration.Instance;
                var json = System.Text.Json.JsonSerializer.Serialize(current);
                var copy = System.Text.Json.JsonSerializer.Deserialize<GameConfiguration>(json);
                if (copy != null)
                {
                    UndoStack.Push(copy);
                }

                var next = RedoStack.Pop();
                AdjustmentExecutor.ApplyConfiguration(next);
                ScrollDebugLogger.Log("UndoRedoManager: Redo performed");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"UndoRedoManager: Error redoing: {ex.Message}");
                return false;
            }
        }
    }
}

