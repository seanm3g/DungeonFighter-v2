// Quick test to verify turn counting
using System;

// Simulate TurnManager behavior
class TurnTest {
    static void Main() {
        int actionCount = 0;
        int turnNumber = 1;
        
        // Simulate 3 actions
        for (int i = 0; i < 3; i++) {
            actionCount++;
            turnNumber++;
            Console.WriteLine($"Action {actionCount}: turnNumber = {turnNumber}, actionCount = {actionCount}");
        }
        
        // After 3 actions:
        // actionCount = 3, turnNumber = 4
        // GetTotalActionCount() returns 3
        // GetCurrentTurn() returns 4
        
        Console.WriteLine($"Final: totalActionCount={actionCount}, currentTurn={turnNumber}");
    }
}
