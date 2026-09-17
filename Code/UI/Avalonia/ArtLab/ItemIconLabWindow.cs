using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Themes.Fluent;

namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Standalone visual sandbox: never creates a character or writes a save.</summary>
public sealed class ItemIconLabApp : Application
{
    public override void Initialize() { Styles.Add(new FluentTheme()); RequestedThemeVariant=global::Avalonia.Styling.ThemeVariant.Dark; }
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow=desktop.Args?.Any(a=>a.Equals("ICONEDITOR",StringComparison.OrdinalIgnoreCase))==true
                ? new ItemIconLabWindow() : new ItemRarityGalleryWindow();
        base.OnFrameworkInitializationCompleted();
    }
}

public sealed class ItemIconLabWindow : Window
{
    private readonly ComboBox family=new(), item=new(), material=new(), quality=new(), prefix=new(), suffix=new();
    private readonly CheckBox lockItem=new(){Content="Lock item"}, lockMaterial=new(){Content="Lock material"}, lockQuality=new(){Content="Lock quality"}, lockAffixes=new(){Content="Lock affixes"};
    private readonly TextBox seed=new(){Text="1717",Watermark="Seed"};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap}, description=new(){TextWrapping=TextWrapping.Wrap};
    private readonly ItemIconControl large=new(){Width=192,Height=192}, native=new(){Width=16,Height=16};
    private readonly StackPanel chips=new(){Spacing=4};
    private readonly WrapPanel gallery=new(){Orientation=Orientation.Horizontal};
    private readonly List<string> chosenPrefixes=[],chosenSuffixes=[];
    private List<ItemIconCatalog.Entry> entries=[];
    private ItemIconRecipe current=new("","Steel","",[],[]);
    private bool updating;
    private int roll;
    public ItemIconLabWindow()
    {
        Title="Dungeon Fighter — Item Icon Lab"; Width=1240;Height=900;MinWidth=900;MinHeight=650;
        Background=Brush.Parse("#171b22");
        var shell=new Grid{ColumnDefinitions=new ColumnDefinitions("330,*"),Margin=new Thickness(20)};
        var controls=new StackPanel{Spacing=10,Margin=new Thickness(0,0,20,0)};
        controls.Children.Add(new TextBlock{Text="ITEM ICON LAB",FontSize=25,FontWeight=FontWeight.Bold});
        controls.Children.Add(new TextBlock{Text=$"{ItemIconCatalog.Items.Count} items · 16 × 16 pixels\nCosmetic sandbox — combinations do not alter loot rules",TextWrapping=TextWrapping.Wrap,Foreground=Brush.Parse("#a6b2c1")});
        Add("Seed",seed);
        controls.Children.Add(Button("Generate 24 permutations",()=>Generate(false)));
        controls.Children.Add(Button("Next batch",()=>{roll++;Generate(true);}));
        controls.Children.Add(Button("Browse catalog",Browse));
        family.ItemsSource=new[]{"All"}.Concat(ItemIconCatalog.Items.Select(e=>e.Family).Distinct()).ToArray();family.SelectedIndex=0;
        material.ItemsSource=ItemIconRenderer.Materials.Keys.ToArray();quality.ItemsSource=new[]{"None"}.Concat(ItemIconCatalog.Names("quality")).ToArray();quality.SelectedIndex=0;
        prefix.ItemsSource=ItemIconCatalog.Names("adjective").ToArray();prefix.SelectedIndex=0;
        suffix.ItemsSource=ItemIconCatalog.Names("suffix").ToArray();suffix.SelectedIndex=0;
        Add("Family",family);Add("Item",item);controls.Children.Add(lockItem);
        Add("Material",material);controls.Children.Add(lockMaterial);
        Add("Quality",quality);controls.Children.Add(lockQuality);
        Add("Prefix",prefix);controls.Children.Add(Button("Add prefix",()=>AddAffix(true)));
        Add("Suffix — includes animals and stories",suffix);controls.Children.Add(Button("Add suffix",()=>AddAffix(false)));
        controls.Children.Add(chips);controls.Children.Add(lockAffixes);
        controls.Children.Add(Button("Clear affixes",()=>{chosenPrefixes.Clear();chosenSuffixes.Clear();UpdateChips();Refresh();}));
        controls.Children.Add(Button("Export selected PNG",async()=>await Export(false)));
        controls.Children.Add(Button("Export recipe JSON",async()=>await Export(true)));
        controls.Children.Add(status);
        shell.Children.Add(new ScrollViewer{Content=controls,HorizontalScrollBarVisibility=global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled});
        var right=new Grid{RowDefinitions=new RowDefinitions("Auto,*"),Margin=new Thickness(12,0,0,0)};Grid.SetColumn(right,1);
        var preview=new StackPanel{Orientation=Orientation.Horizontal,Spacing=20,Margin=new Thickness(0,0,0,20)};
        preview.Children.Add(new Border{Background=Brush.Parse("#222a33"),CornerRadius=new CornerRadius(8),Child=large,Padding=new Thickness(10)});
        var info=new StackPanel{Spacing=12,MaxWidth=520};info.Children.Add(new TextBlock{Text="SELECTED ITEM",FontSize=12,Foreground=Brush.Parse("#a6b2c1")});info.Children.Add(description);
        info.Children.Add(new TextBlock{Text="Native size"});info.Children.Add(native);preview.Children.Add(info);right.Children.Add(preview);
        var scroll=new ScrollViewer{Content=gallery,HorizontalScrollBarVisibility=global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled};Grid.SetRow(scroll,1);right.Children.Add(scroll);shell.Children.Add(right);Content=new Border{Background=Background,Child=shell};
        family.SelectionChanged+=(_,_)=>UpdateItems();item.SelectionChanged+=(_,_)=>{if(!updating){material.SelectedItem=SelectedEntry.DefaultMaterial;Refresh();}};
        material.SelectionChanged+=(_,_)=>Refresh();quality.SelectionChanged+=(_,_)=>Refresh();
        seed.TextChanged+=(_,_)=>Refresh();
        UpdateItems();Generate(false);
        void Add(string label,Control control){controls.Children.Add(new TextBlock{Text=label,FontSize=12});control.HorizontalAlignment=HorizontalAlignment.Stretch;controls.Children.Add(control);}
    }
    private static Button Button(string text,System.Action action){var b=new Button{Content=text,HorizontalAlignment=HorizontalAlignment.Stretch};b.Click+=(_,_)=>action();return b;}
    private ItemIconCatalog.Entry SelectedEntry=>entries[Math.Clamp(item.SelectedIndex,0,Math.Max(0,entries.Count-1))];
    private void UpdateItems()
    {
        if(updating)return;updating=true;
        entries=ItemIconCatalog.Items.Where(e=>family.SelectedItem is not string f||f=="All"||e.Family==f).ToList();
        item.ItemsSource=entries.Select(e=>$"{e.Name} · T{e.Tier} · {e.Id}").ToArray();item.SelectedIndex=0;
        material.SelectedItem=SelectedEntry.DefaultMaterial;updating=false;Refresh();
    }
    private void AddAffix(bool isPrefix)
    {
        string? value=(isPrefix?prefix:suffix).SelectedItem as string;
        var list=isPrefix?chosenPrefixes:chosenSuffixes;if(value!=null&&!list.Contains(value))list.Add(value);
        UpdateChips();Refresh();
    }
    private void UpdateChips()
    {
        chips.Children.Clear();
        foreach(var pair in chosenPrefixes.Select(s=>(s,true)).Concat(chosenSuffixes.Select(s=>(s,false))).ToArray())
            chips.Children.Add(Button("× "+pair.s,()=>{(pair.Item2?chosenPrefixes:chosenSuffixes).Remove(pair.s);UpdateChips();Refresh();}));
    }
    private void Refresh()
    {
        if(updating||entries.Count==0)return;
        current=new(SelectedEntry.Id,material.SelectedItem as string??SelectedEntry.DefaultMaterial,quality.SelectedItem as string is { } q&&q!="None"?q:"",chosenPrefixes.ToArray(),chosenSuffixes.ToArray(),ReadSeed());
        Show(current);
    }
    private int ReadSeed()=>int.TryParse(seed.Text,out int value)?value:unchecked((int)ItemIconRenderer.StableHash(seed.Text??""));
    private void Show(ItemIconRecipe recipe)
    {
        current=recipe;var entry=ItemIconCatalog.Items.First(e=>e.Id==recipe.CatalogId);var frame=ItemIconRenderer.Render(recipe);
        large.Frame=frame;native.Frame=frame;large.InvalidateVisual();native.InvalidateVisual();
        description.Text=$"{entry.Name} · {entry.Family} · tier {entry.Tier}\n{recipe.Quality} {recipe.Material}\n"+string.Join(", ",recipe.Prefixes.Concat(recipe.Suffixes))+"\n\n"+
            (frame.SuppressedAffixes.Count>0?"Effect budget: "+string.Join(", ",frame.SuppressedAffixes)+" retained in recipe but not drawn.":"All selected affix details fit the visual budget.");
    }
    private void SelectRecipe(ItemIconRecipe recipe)
    {
        updating=true;
        // Gallery selection updates every selector, so the next edit starts from the selected card.
        int index=entries.FindIndex(e=>e.Id==recipe.CatalogId);item.SelectedIndex=index;
        material.SelectedItem=ItemIconRenderer.Materials.Keys.FirstOrDefault(s=>s.Equals(recipe.Material,StringComparison.OrdinalIgnoreCase));
        quality.SelectedItem=recipe.Quality.Length==0?"None":recipe.Quality;
        seed.Text=recipe.Seed.ToString(CultureInfo.InvariantCulture);
        chosenPrefixes.Clear();chosenPrefixes.AddRange(recipe.Prefixes);chosenSuffixes.Clear();chosenSuffixes.AddRange(recipe.Suffixes);
        updating=false;UpdateChips();Show(recipe);
    }
    private void Generate(bool next)
    {
        if(!next)roll=0;
        var random=new Random(unchecked(ReadSeed()+roll*7919));gallery.Children.Clear();
        var materials=ItemIconRenderer.Materials.Keys.ToArray();var qualities=ItemIconCatalog.Names("quality").ToArray();
        var prefixes=ItemIconCatalog.Names("adjective").ToArray();var suffixes=ItemIconCatalog.Names("suffix").ToArray();
        for(int i=0;i<24;i++)
        {
            var e=lockItem.IsChecked==true?SelectedEntry:entries[random.Next(entries.Count)];
            var recipe=new ItemIconRecipe(e.Id,lockMaterial.IsChecked==true?current.Material:materials[random.Next(materials.Length)],lockQuality.IsChecked==true?current.Quality:qualities[random.Next(qualities.Length)],
                lockAffixes.IsChecked==true?chosenPrefixes.ToArray():[prefixes[random.Next(prefixes.Length)]],lockAffixes.IsChecked==true?chosenSuffixes.ToArray():[suffixes[random.Next(suffixes.Length)]],ReadSeed());
            AddCard(recipe);
        }
        status.Text=$"Seed {ReadSeed()} · batch {roll+1}\nSelect a card to inspect or export.";
    }
    private void Browse()
    {
        gallery.Children.Clear();foreach(var e in entries)AddCard(new(e.Id,e.DefaultMaterial,"",[],[],0));
        status.Text=$"Showing {entries.Count} catalog entries. Select any card.";
    }
    private void AddCard(ItemIconRecipe recipe)
    {
        var entry=ItemIconCatalog.Items.First(e=>e.Id==recipe.CatalogId);
        var panel=new StackPanel{Spacing=4,Width=130};panel.Children.Add(new ItemIconControl{Frame=ItemIconRenderer.Render(recipe),Width=96,Height=96});
        panel.Children.Add(new TextBlock{Text=entry.Name,TextWrapping=TextWrapping.Wrap,FontWeight=FontWeight.Bold,FontSize=12});
        panel.Children.Add(new TextBlock{Text=recipe.Material+" · "+(recipe.Quality.Length>0?recipe.Quality:"Base"),TextWrapping=TextWrapping.Wrap,FontSize=10,Foreground=Brush.Parse("#acb9c7")});
        var button=new Button{Content=panel,Margin=new Thickness(0,0,10,10),Padding=new Thickness(10),VerticalContentAlignment=VerticalAlignment.Top};
        ToolTip.SetTip(button,string.Join(", ",recipe.Prefixes.Concat(recipe.Suffixes)));button.Click+=(_,_)=>SelectRecipe(recipe);gallery.Children.Add(button);
    }
    private async Task Export(bool json)
    {
        try
        {
            var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title=json?"Save icon recipe":"Save transparent 16×16 PNG",SuggestedFileName=json?"item-icon.json":"item-icon.png",DefaultExtension=json?"json":"png"});
            if(file==null)return;
            await using var stream=await file.OpenWriteAsync();stream.SetLength(0);
            if(json)await JsonSerializer.SerializeAsync(stream,new{version=ItemIconRenderer.Version,recipe=current},new JsonSerializerOptions{WriteIndented=true});
            else {using var bitmap=ItemIconExport.Bitmap(current,1);bitmap.Save(stream);}
            status.Text="Saved "+file.Name;
        }
        catch(Exception ex){status.Text="Export failed: "+ex.Message;}
    }
}

public static class ItemIconExport
{
    public static RenderTargetBitmap Bitmap(ItemIconRecipe recipe,int scale)
    {
        var control=new ItemIconControl{Frame=ItemIconRenderer.Render(recipe),Width=16*scale,Height=16*scale};
        control.Measure(new Size(16*scale,16*scale));control.Arrange(new Rect(0,0,16*scale,16*scale));
        var bitmap=new RenderTargetBitmap(new PixelSize(16*scale,16*scale),new Vector(96,96));bitmap.Render(control);return bitmap;
    }
}
