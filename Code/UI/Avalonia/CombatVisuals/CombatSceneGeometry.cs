using System;

namespace RPGGame.UI.Avalonia.CombatVisuals;

/// <summary>Pure grid geometry, independent of sprite resources and gameplay.</summary>
public static class CombatSceneGeometry
{
    public static bool CanSplit(int width, int height, bool enabled) => enabled && width >= 70 && height >= 17;
}
