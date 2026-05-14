namespace RPGGame
{
    /// <summary>
    /// Food and potions found during post-combat room search (not equipment loot).
    /// </summary>
    public enum RoomSearchConsumableKind
    {
        None = 0,
        Food = 1,
        PotionStrength = 2,
        PotionAgility = 3,
        PotionTechnique = 4,
        PotionIntelligence = 5,
        PotionHit = 6,
        PotionCombo = 7,
        PotionCrit = 8,
        PotionCritMiss = 9
    }
}
