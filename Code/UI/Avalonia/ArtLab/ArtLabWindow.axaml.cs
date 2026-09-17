using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RPGGame.Combat.Calculators;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.ArtLab;

public partial class ArtLabWindow : Window
{
    private readonly ArtLabSession session;
    private readonly System.Action showClassic;
    private readonly DispatcherTimer refreshTimer;
    private readonly List<Button> roomButtons = [];
    private string mapKey = "", comboKey = "", bagKey = "", menuKey = "", journalText = "";
    private GameState? lastState;
    private Environment? describedRoom;
    private string? actionDetail;

    // XAML designer constructor: does not create a second game or run any simulation.
    public ArtLabWindow()
    {
        InitializeComponent();
        session = null!;
        showClassic = () => { };
        refreshTimer = new DispatcherTimer();
        Shell.IsEnabled = false;
    }

    public ArtLabWindow(GameCoordinator game, CanvasUICoordinator canvas, System.Action showClassic) : this()
    {
        this.showClassic = showClassic;
        session = new ArtLabSession(game, canvas);
        Shell.IsEnabled = true;
        MapLines.Session = session;
        AddHandler(KeyDownEvent, HandleKey, RoutingStrategies.Tunnel);
        refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        refreshTimer.Tick += (_, _) => Refresh();
        Opened += (_, _) => { Refresh(); refreshTimer.Start(); };
        Closed += (_, _) => refreshTimer.Stop();
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
    private static bool UsesArtMenu(GameState state) => state is GameState.MainMenu or GameState.TrainingGroundOffer
        or GameState.PreWeaponPathIntro or GameState.WeaponSelection or GameState.CharacterCreation
        or GameState.GameLoop or GameState.DungeonSelection or GameState.DungeonCompletion or GameState.Death;

    private void Refresh()
    {
        var player = session.Player;
        var state = session.Game.CurrentState;
        if (!ReferenceEquals(describedRoom, session.Location)) { describedRoom = session.Location; actionDetail = null; }
        HeroName.Text = player?.Name.ToUpperInvariant() ?? "NO ACTIVE FIGHTER";
        HeroName.FontSize = (player?.Name.Length ?? 0) > 18 ? 16 : 20;
        ToolTip.SetTip(HeroName, player?.Name ?? "Create or load a fighter from the game menu.");
        HeroClass.Text = player == null ? "CREATE OR LOAD A HERO" : $"LVL {player.Level:00} / {player.GetCurrentClass().ToUpperInvariant()}";
        Portrait.Character = player;
        Portrait.Opacity = player == null ? 0.25 : 1;
        Portrait.InvalidateVisual();
        LoadoutLabel.Text = player == null ? "THE DESCENT AWAITS" : "EQUIPPED / LIVE";
        HealthText.Text = player == null ? "—" : $"{session.Health} / {session.MaxHealth}";
        HealthBar.Maximum = Math.Max(1, session.MaxHealth);
        HealthBar.Value = session.Health;
        HealthBar.Foreground = Brush(session.Health < session.MaxHealth * .25 ? "#F14C45" : "#DCF54A");
        DamageText.Text = player == null ? "—" : DamageCalculator.CalculateRawDamage(player).ToString();
        ArmorText.Text = player?.GetTotalArmor().ToString() ?? "—";
        KillsText.Text = player?.XP.ToString() ?? "—";
        WeaponText.Text = "†  " + (player?.Weapon?.Name ?? "No weapon equipped");
        ToolTip.SetTip(WeaponText, WeaponText.Text);
        HeadText.Text = "HEAD / " + (player?.Head?.Name ?? "None");
        BodyText.Text = "BODY / " + (player?.Body?.Name ?? "None");
        ExtraGearText.Text = $"LEGS / {player?.Legs?.Name ?? "None"}\nFEET / {player?.Feet?.Name ?? "None"}";
        DungeonTitle.Text = session.Dungeon?.Name.ToUpperInvariant() ?? "THE DESCENT AWAITS";
        RegionText.Text = (player?.CurrentRegionId ?? "DUNGEON FIGHTERS").ToUpperInvariant() + " / LIVE GAME";
        GameStateText.Text = StateLabel(state);
        RunProgress.Text = session.Dungeon == null ? "NO ACTIVE DUNGEON" : $"ROOM {Math.Max(0, session.CurrentRoom + 1):00} / {session.Dungeon.Rooms.Count:00}";
        RoomType.Text = session.Location == null ? "READY YOUR FIGHTER" : $"ROOM {session.CurrentRoom + 1:00} / {(session.Location.IsHostile ? "ENCOUNTER" : "EXPLORATION")}";
        RoomTitle.Text = session.IsDead ? "Your fighter has fallen." : session.Won ? "Dungeon complete." : session.Location?.Name ?? "Steel. Static. Survival.";
        RoomDescription.Text = session.LastError ?? actionDetail ?? session.Location?.Description ?? "Choose a dungeon from the game menu. Your existing character, gear, and combat rules drive this view.";
        EnemyPanel.IsVisible = session.InCombat;
        EnemyName.Text = session.Enemy?.Name.ToUpperInvariant() ?? "";
        EnemyBar.Maximum = Math.Max(1, session.Enemy?.MaxHealth ?? 1);
        EnemyBar.Value = session.Enemy?.CurrentHealth ?? 0;
        EnemyHealthText.Text = $"{session.Enemy?.CurrentHealth} / {session.Enemy?.MaxHealth} VITALITY";
        SceneStatus.Text = session.WaitingForRoom ? "ROOM CLEARED / CHOOSE YOUR PATH" : session.InCombat ? "AUTO COMBAT / LIVE" : state is GameState.Dungeon ? "EXPLORING / SEARCHING" : session.Won ? "REWARDS FROM THE GAME ENGINE" : "SAME GAME. NEW VIEW.";
        SearchButton.IsVisible = session.WaitingForRoom;
        StrikeButton.Content = session.WaitingForRoom ? "NEXT ROOM [SPACE]" : session.InCombat ? "AUTO COMBAT" : state is GameState.Dungeon ? "EXPLORING..." : "GAME MENU";
        StrikeButton.IsEnabled = session.WaitingForRoom || state is not (GameState.Combat or GameState.Dungeon);
        MapHint.Text = session.Dungeon == null ? "SELECT A DUNGEON TO REVEAL THE MAP" : session.WaitingForRoom ? "CLICK THE NEXT ROOM TO CONTINUE" : session.InCombat ? "COMBAT RESOLVES THROUGH YOUR COMBO" : $"ROUTE PAGE {session.MapPage + 1} / ORIGINAL ROOM ORDER";
        UpdateMap();
        UpdateCombo();
        UpdateBag();
        UpdateMenu();
        string log = session.Journal;
        if (session.LastError != null) log += "\n" + session.LastError;
        if (log != journalText)
        {
            journalText = log;
            JournalText.Text = string.Join("\n", log.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).TakeLast(50));
            Dispatcher.UIThread.Post(() => JournalScroll.ScrollToEnd(), DispatcherPriority.Loaded);
        }
        if (lastState != state)
        {
            lastState = state;
            SetMenu(UsesArtMenu(state));
        }
    }

    private void UpdateMap()
    {
        var rooms = session.Rooms;
        string key = $"{session.Dungeon?.GetHashCode()}:{rooms.Length}:{session.MapPage}";
        if (key != mapKey)
        {
            mapKey = key;
            MapNodes.Children.Clear();
            roomButtons.Clear();
            foreach (var room in rooms.Where(r => session.IsOnMapPage(r.Id)))
            {
                int id = room.Id;
                var button = new Button { Tag = id };
                button.Classes.Add("room");
                var point = DungeonMapLines.Position(room);
                global::Avalonia.Controls.Canvas.SetLeft(button, point.X - 15);
                global::Avalonia.Controls.Canvas.SetTop(button, point.Y - 15);
                button.Click += async (_, _) => { await session.TravelAsync(id); Refresh(); };
                roomButtons.Add(button);
                MapNodes.Children.Add(button);
            }
        }
        foreach (var button in roomButtons)
        {
            int id = (int)button.Tag!;
            bool current = id == session.CurrentRoom;
            button.Classes.Set("current", current);
            button.IsEnabled = session.CanTravel(id);
            button.Opacity = current ? 1 : session.IsDiscovered(id) ? .75 : .3;
            button.Content = current ? "◆" : session.IsVisited(id) ? "·" : "?";
            button.BorderBrush = Brush(session.CanTravel(id) ? "#DCF54A" : "#435046");
            string name = current ? "Current room: " + rooms[id].Name : session.IsVisited(id) ? rooms[id].Name : $"Room {id + 1}: unexplored";
            AutomationProperties.SetName(button, name);
            ToolTip.SetTip(button, name);
        }
        MapLines.InvalidateVisual();
    }

    private void UpdateCombo()
    {
        var moves = session.Moves;
        int offset = Math.Max(0, session.ActiveMove) / 5 * 5;
        string key = string.Join("|", moves.Select(m => $"{m.Name}:{m.DamageMultiplier}:{m.Length}:{m.Description}")) + $":{session.ActiveMove}";
        if (key == comboKey) return;
        comboKey = key;
        ActionCards.Children.Clear();
        ComboHint.Text = moves.Count == 0 ? "SET YOUR COMBO IN CLASSIC UI" : $"SLOTS {offset + 1}–{Math.Min(offset + 5, moves.Count)} / {moves.Count} · AUTO COMBAT";
        for (int i = 0; i < 5; i++)
        {
            int index = offset + i;
            var move = index < moves.Count ? moves[index] : null;
            var body = new StackPanel { Spacing = 7 };
            body.Children.Add(new TextBlock { Text = $"{index + 1:00}", FontSize = 10, Foreground = Brush("#929A90") });
            body.Children.Add(new TextBlock { Text = move == null ? "·" : index == session.ActiveMove ? "»" : "◇", FontSize = 30, Foreground = Brush("#DCF54A"), Height = 34 });
            body.Children.Add(new TextBlock { Text = move?.Name.ToUpperInvariant() ?? "EMPTY SLOT", TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.Bold, FontSize = 11, MaxWidth = 130 });
            body.Children.Add(new TextBlock { Text = move == null ? "Configure in inventory" : $"DMG {move.DamageMultiplier:P0}\nTIME {move.Length:0.##}×", FontSize = 10, Foreground = Brush("#ADB5A6") });
            var button = new Button { Content = body, Margin = new Thickness(i == 0 ? 0 : 4, 0, i == 4 ? 0 : 4, 0), Padding = new Thickness(12, 10),
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
                HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Left, VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Top };
            button.Classes.Set("selected", index == session.ActiveMove);
            ToolTip.SetTip(button, move?.Description ?? "Use the existing inventory combo editor to add actions.");
            button.Click += (_, _) => { actionDetail = move == null ? "Edit the combo in Classic UI → Inventory." : $"{move.Name}: {move.Description}"; Refresh(); };
            ActionCards.Children.Add(button);
        }
    }

    private void UpdateBag()
    {
        string key = string.Join("|", session.Inventory.Select(item => $"{item.GetHashCode()}:{item.Name}:{ItemIconRenderer.Key(ItemIconRenderer.ForItem(item))}")) + $":{session.CanEquip}";
        if (key == bagKey) return;
        bagKey = key;
        BagItems.Children.Clear();
        if (session.Inventory.Count == 0) BagItems.Children.Add(new TextBlock { Text = "Your bag is empty.\nFind gear in the dungeon.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brush("#929A90") });
        foreach (var item in session.Inventory.ToArray())
        {
            var row = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
            row.Children.Add(new ItemIconControl { Frame = ItemIconRenderer.Render(item), Width = 32, Height = 32 });
            row.Children.Add(new TextBlock { Text = item.Name, TextWrapping = TextWrapping.Wrap, FontSize = 11, MaxWidth = 155 });
            var button = new Button { Content = row,
                IsEnabled = session.CanEquip && InventoryActionPoolEntries.GetEquipSlotForItem(item) != null, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch };
            ToolTip.SetTip(button, session.CanEquip ? item.GetEquipBlockedReason(session.Player!) ?? "Equip using the existing inventory handler" : "Equipment is locked during a dungeon run.");
            button.Click += (_, _) => { session.Equip(item); Refresh(); };
            BagItems.Children.Add(button);
        }
    }

    private void UpdateMenu()
    {
        var state = session.Game.CurrentState;
        var choices = UsesArtMenu(state) || session.WaitingForRoom ? session.Choices : [];
        string key = $"{state}:" + string.Join("|", choices.Select(c => c.Input + c.Label));
        if (key == menuKey) return;
        menuKey = key;
        MenuTitle.Text = StateLabel(state);
        GameChoices.Children.Clear();
        if (choices.Count == 0) GameChoices.Children.Add(new TextBlock { Text = "Use Classic Controls for this screen. Your active game continues in the same session.", TextWrapping = TextWrapping.Wrap });
        foreach (var choice in choices)
        {
            var button = new Button { Content = new TextBlock { Text = $"[{choice.Input}]  {choice.Label}", TextWrapping = TextWrapping.Wrap }, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch, HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Left };
            button.Click += async (_, _) => await Dispatch(choice.Input);
            GameChoices.Children.Add(button);
        }
    }

    private async Task Dispatch(string input)
    {
        SetMenu(false);
        await session.SendInputAsync(input);
        Refresh();
        if (UsesArtMenu(session.Game.CurrentState)) SetMenu(true);
        if (!UsesArtMenu(session.Game.CurrentState) && session.Game.CurrentState is not (GameState.Combat or GameState.Dungeon)) ShowClassic(null, new RoutedEventArgs());
    }
    private void SetMenu(bool visible)
    {
        GameMenuOverlay.IsVisible = visible;
        Shell.IsEnabled = !visible && !HelpOverlay.IsVisible;
    }
    private static string StateLabel(GameState state) => state switch
    {
        GameState.MainMenu => "THE DESCENT AWAITS",
        GameState.TrainingGroundOffer => "TRAINING GROUND",
        GameState.PreWeaponPathIntro => "CHOOSE YOUR PATH",
        GameState.WeaponSelection => "CHOOSE YOUR WEAPON",
        GameState.CharacterCreation => "YOUR FIGHTER",
        GameState.GameLoop => "THE CAMP",
        GameState.DungeonSelection => "CHOOSE YOUR DUNGEON",
        GameState.DungeonCompletion => "DUNGEON CONQUERED",
        GameState.Death => "THE FALLEN",
        _ => state.ToString().ToUpperInvariant()
    };
    private void ShowGameMenu(object? sender, RoutedEventArgs e) { UpdateMenu(); SetMenu(true); }
    private void CloseGameMenu(object? sender, RoutedEventArgs e) => SetMenu(false);
    private void ShowClassic(object? sender, RoutedEventArgs e) { SetMenu(false); WindowState = WindowState.Minimized; showClassic(); }
    private async void ContinueGame(object? sender, RoutedEventArgs e)
    {
        if (session.WaitingForRoom) await session.TravelAsync(session.CurrentRoom + 1);
        else ShowGameMenu(sender, e);
    }
    private async void LeaveDungeon(object? sender, RoutedEventArgs e) { if (session.WaitingForRoom) await session.SendInputAsync("2"); }
    private void ShowHelp(object? sender, RoutedEventArgs e) { HelpOverlay.IsVisible = true; Shell.IsEnabled = false; CloseHelpButton.Focus(); }
    private void HideHelp(object? sender, RoutedEventArgs e) { HelpOverlay.IsVisible = false; Shell.IsEnabled = !GameMenuOverlay.IsVisible; HelpButton.Focus(); }
    private async void HandleKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { HideHelp(sender, e); SetMenu(false); e.Handled = true; return; }
        if (HelpOverlay.IsVisible) return;
        if (e.Key == Key.F1) { ShowHelp(sender, e); e.Handled = true; return; }
        if (GameMenuOverlay.IsVisible && e.Key >= Key.D0 && e.Key <= Key.D9)
        {
            string input = (e.Key - Key.D0).ToString();
            if (session.Choices.Any(c => c.Input == input)) { e.Handled = true; await Dispatch(input); }
        }
        else if (e.Key == Key.Space && session.WaitingForRoom) { e.Handled = true; await session.TravelAsync(session.CurrentRoom + 1); }
    }

    public void Capture(string path)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(Bounds.Width), (int)Math.Ceiling(Bounds.Height)), new Vector(96, 96));
        bitmap.Render(this);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        bitmap.Save(fullPath);
    }
}
