using RPGGame;
using RPGGame.BattleStatistics;
using System.Text.Json;

DeveloperModeState.SetCombatLogInstant(true);
ActionLoader.LoadActions();
EnemyLoader.LoadEnemies();
var dataRoot = Path.GetFullPath(GameConstants.GetGameDataFilePath("Enemies.json"));
if (!dataRoot.StartsWith(Directory.GetCurrentDirectory(), StringComparison.OrdinalIgnoreCase))
    throw new Exception("Refusing playtest outside isolated GameData: " + dataRoot);
Console.WriteLine("DATA " + dataRoot);
Console.WriteLine("BALANCE " + GameConfiguration.TryGetExistingTuningConfigFilePath());
if (args.Contains("--equipped"))
{
    var results = new List<object>();
    RPGGame.Data.ActionSetVisibility.SetMaxTierInclusive(null, persist: false); // sandbox includes higher-tier mechanics
    var catalog = ActionLoader.GetAllActions();
    Console.WriteLine("ACTIONS " + ActionLoader.GetLoadedActionsFilePath());
    foreach (var a in catalog.Where(a => a.Name is "STUN" or "BLEED" or "POISON")) Console.WriteLine($"CATALOG {a.Name} combo={a.IsComboAction} stun={a.CausesStun} bleed={a.CausesBleed} poison={a.CausesPoison}");
    foreach (var weapon in new[] { WeaponType.Sword, WeaponType.Mace, WeaponType.Dagger, WeaponType.Wand })
    foreach (string enemyName in new[] { "Wolf", "Skeleton", "Lava Golem" })
    {
        int wins = 0;
        var losses = new List<long>();
        var loadout = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var hero = TestCharacterFactory.CreateTestCharacterWithWeapon("Equipped", weapon, 8);
            hero.EquipItem(new ChestItem("Playtest plate", tier: 2, armor: 6), "Body");
            hero.ActionLabActionSlotBonus = 3; // explicit sandbox allowance; does not claim natural acquisition
            if (weapon is WeaponType.Dagger or WeaponType.Wand)
                hero.Weapon!.Modifications.Add(new Modification { Name = "Probe proc", Effect = weapon == WeaponType.Dagger ? "weaponBleed" : "weaponPoison", RolledValue = 1, TriggerWhen = "ONCRITICAL" });
            var setup = catalog.FirstOrDefault(a => a.IsComboAction && (weapon == WeaponType.Mace ? a.CausesWeaken : a.CausesHarden));
            if (setup == null) throw new Exception("Required setup mechanic not available: " + weapon);
            hero.AddToCombo(ActionLoader.GetAction(setup.Name)!);
            if (!hero.GetComboActions().Any(a => a.Name == setup.Name)) throw new Exception("Setup action was not installed");
            loadout = hero.GetComboActions().Select(a => a.Name).ToList();
            var foe = EnemyLoader.CreateEnemy(enemyName, 8)!;
            var template = foe;
            foe = new Enemy(name: enemyName, level: 8, maxHealth: template.MaxHealth, damage: template.Damage,
                armor: 10, attackSpeed: template.GetTotalAttackSpeed(), primaryAttribute: template.PrimaryAttribute,
                isLiving: template.IsLiving, archetype: template.Archetype);
            foe.ActionPool.Clear();
            foreach (var entry in template.ActionPool) foe.ActionPool.Add(entry);
            if (foe.GetTotalArmor() != 10) throw new Exception("Armor probe not configured");
            var manager = new CombatManager();
            using var mute = RPGGame.Combat.CombatUiMuteScope.Begin();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            if (await manager.RunCombat(hero, foe, new RPGGame.Environment("Equipped probe", "Controlled test", false, "Crypt"), cancellationToken: timeout.Token)) wins++;
            losses.Add(EncounterReport.Read(hero).Lost);
        }
        var row = new { weapon = weapon.ToString(), enemyName, battles = 10, wins, meanFighterHpLost = losses.Average(), actions = loadout };
        results.Add(row);
        Console.WriteLine(JsonSerializer.Serialize(row));
    }
    File.WriteAllText("equipped-results.json", JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    return;
}
var summaries = new List<object>();
foreach (var level in new[] { 1, 3 })
foreach (var weapon in new[] { WeaponType.Sword, WeaponType.Mace, WeaponType.Dagger, WeaponType.Wand })
foreach (var enemy in new[] { "Wolf", "Skeleton", "Lava Golem" })
{
    var sample = TestCharacterFactory.CreateTestCharacterWithWeapon("Inspect", weapon, level);
    var foe = EnemyLoader.CreateEnemy(enemy, level)!;
    var battles = new List<BattleResult>();
    for (var i = 0; i < 30; i++)
        battles.Add(await BattleExecutor.RunSingleBattleWithWeapon(weapon, enemy, level, level, i));
    var result = new { level, weapon = weapon.ToString(), enemy, battles = battles.Count,
        wins = battles.Count(b => b.PlayerWon), errors = battles.Where(b => b.ErrorMessage != null).Select(b => b.ErrorMessage).ToArray(),
        averageTurns = battles.Average(b => b.Turns), minTurns = battles.Min(b => b.Turns), maxTurns = battles.Max(b => b.Turns),
        averageHealthLeft = battles.Average(b => b.PlayerFinalHealth), playerMaxHealth = sample.MaxHealth, enemyMaxHealth = foe.MaxHealth,
        playerArmor = sample.GetTotalArmor(), enemyArmor = foe.GetTotalArmor(),
        actions = sample.ActionPool.Select(a => a.Item1.Name).ToArray() };
    summaries.Add(result);
    Console.WriteLine(JsonSerializer.Serialize(result));
    File.WriteAllText("results.json", JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
}
var combos = new List<object>();
foreach (var reversed in new[] { false, true })
{
    int wins = 0, totalTurns = 0;
    for (int i = 0; i < 20; i++)
    {
        var hero = TestCharacterFactory.CreateTestCharacterWithWeapon("ComboProbe", WeaponType.Sword, 3);
        hero.Intelligence = 10;
        hero.InitializeDefaultCombo();
        foreach (var existing in hero.GetComboActions()) hero.RemoveFromCombo(existing, ignoreWeaponRequirement: true);
        var names = reversed ? new[] { "SLAM", "STRIKE" } : new[] { "STRIKE", "SLAM" };
        foreach (var name in names) hero.AddToCombo(ActionLoader.GetAction(name)!);
        hero.ComboStep = 0;
        var foe = EnemyLoader.CreateEnemy("Wolf", 3)!;
        var manager = new CombatManager();
        try
        {
            using var mute = RPGGame.Combat.CombatUiMuteScope.Begin(muted: true);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            if (await manager.RunCombat(hero, foe, TestCharacterFactory.CreateTestEnvironment(), cancellationToken: timeout.Token)) wins++;
            totalTurns += manager.GetCurrentTurn();
        }
        finally { manager.Cleanup(); }
    }
    var result = new { reversed, wins, battles = 20, averageTurns = totalTurns / 20.0 };
    combos.Add(result);
    Console.WriteLine("COMBO " + JsonSerializer.Serialize(result));
}
File.WriteAllText("combo-results.json", JsonSerializer.Serialize(combos, new JsonSerializerOptions { WriteIndented = true }));







