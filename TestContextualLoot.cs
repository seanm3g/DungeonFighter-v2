using System;
using RPGGame;
using RPGGame.Tests;

class Program
{
    static void Main()
    {
        // Initialize loot system
        LootGenerator.Initialize();

        // Run contextual loot tests
        ContextualLootTest.RunTests();
    }
}
