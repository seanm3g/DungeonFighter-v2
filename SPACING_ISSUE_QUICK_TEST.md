# Quick Spacing Issue Test

## I traced through the code manually and everything LOOKS correct!

But the spaces are definitely being added somewhere. Here's a quick test you can do:

## Test 1: Check if it's the keyword coloring

Add this temporary code to `BlockDisplayManager.cs` at line 19:

```csharp
private static string ApplyKeywordColoring(string text)
{
    Console.WriteLine($"BEFORE keyword coloring: '{text}' (length={text.Length})");
    string result = KeywordColorSystem.Colorize(text);
    Console.WriteLine($"AFTER keyword coloring: '{result}' (length={result.Length})");
    string stripped = ColorParser.StripColorMarkup(result);
    Console.WriteLine($"STRIPPED: '{stripped}' (length={stripped.Length})");
    Console.WriteLine($"Length change: {stripped.Length - text.Length}");
    Console.WriteLine();
    return result;
}
```

Then play one combat and look at the console output. It will show you exactly where the spaces are being added!

## Test 2: Disable keyword coloring temporarily

Change line 19 in `BlockDisplayManager.cs`:

```csharp
private static string ApplyKeywordColoring(string text)
{
    return text;  // TEMPORARILY DISABLED
    // return KeywordColorSystem.Colorize(text);
}
```

Then play combat. **Do the spacing issues disappear?**

- If YES → Problem is in KeywordColorSystem  
- If NO → Problem is in the rendering or somewhere else

## Test 3: Check the actual rollInfo string

In `TextDisplayIntegration.cs` at line 41, add:

```csharp
rollInfo = lines[1].Trim();
Console.WriteLine($"rollInfo after trim: '{rollInfo}'");
Console.WriteLine($"rollInfo length: {rollInfo.Length}");
// Check for any weird characters
for (int i = 0; i < rollInfo.Length; i++)
{
    char c = rollInfo[i];
    if (c == ' ')
    {
        Console.WriteLine($"  Space at index {i}");
    }
}
```

This will show you if there are already extra spaces BEFORE keyword coloring is applied.

## My Theory

Based on my manual trace, I suspect one of these:

1. **The `Trim()` operation isn't working correctly** - maybe there are non-standard whitespace characters?

2. **The template expansion is somehow duplicating text** - though I can't see how from the code

3. **There's a rendering issue** where color code changes are adding visual space even though the string is correct

4. **The problem is in how segments are joined** during rendering

Please run Test 1 or Test 2 above and let me know what you see! That will tell us exactly where the problem is.

If Test 2 makes the spaces disappear, then I know it's in the keyword coloring and I can create a fix.
If Test 2 doesn't help, then it's in the rendering pipeline.

